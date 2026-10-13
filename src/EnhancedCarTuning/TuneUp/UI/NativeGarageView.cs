using EnhancedCarTuning.TuneUp.Drive;
using EnhancedCarTuning.TuneUp.Engine;
using EnhancedCarTuning.UI;
using EnhancedCarTuning.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.TuneUp.UI
{
    internal sealed class NativeGarageView : IDisposable
    {
        private readonly CarGarageUI garage;
        private readonly GarageSession session;
        private readonly List<MenuButtonGroup> ownedButtons = new List<MenuButtonGroup>();
        private readonly List<Action> refreshChoices = new List<Action>();
        private Transform main;
        private RectTransform tuneMenu;
        private RectTransform choiceListOffsetRoot;
        private MenuButtonGroup entry, engineRow, driveRow;
        private CarGarageUI_Back back;
        private const float ChoiceHeight = 78f;
        private const float ChoiceStep = 84f;
        private const float CategoryX = 12.55f;
        private const float ChoiceListX = 275f;
        private const float ChoiceListVisualOffsetX = 47f;
        private NativeChoiceList engines, drives;
        private Text engineSelectedValue, driveSelectedValue;
        private readonly Dictionary<DriveLayout, Sprite> driveIcons = new Dictionary<DriveLayout, Sprite>();
        private readonly Vector3[] viewportCorners = new Vector3[4];

        internal NativeGarageView(CarGarageUI garage, GarageSession session) { this.garage = garage; this.session = session; }
        internal bool IsActive => tuneMenu && tuneMenu.gameObject.activeInHierarchy;

        internal void Build()
        {
            // Object and sprite names come from MDMP's InitTuneUpTab.
            main = garage.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Main");
            var wheels = garage.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Wheels_menu");
            if (!main || !wheels) throw new InvalidOperationException("Main / Wheels_menu not found.");
            var candidates = main.GetComponentsInChildren<MenuButtonGroup>(true);
            var template = candidates.FirstOrDefault(b => b.content == wheels) ??
                candidates.FirstOrDefault(b => b.name.IndexOf("wheel", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!template) throw new InvalidOperationException("Native wheels MenuButtonGroup not found.");
            var sourceRect = template.GetComponent<RectTransform>();
            float height = Mathf.Max(50, sourceRect.rect.height);
            float step = height + 6;
            var siblings = main.GetComponentsInChildren<MenuButton>(true)
                .Where(b => b.transform.parent == template.transform.parent).ToArray();
            foreach (var sibling in siblings)
            {
                float distance = Mathf.Abs(sibling.GetComponent<RectTransform>().anchoredPosition.y - sourceRect.anchoredPosition.y);
                if (distance > 1 && distance < step * 1.5f) step = distance;
            }

            tuneMenu = NativeMenuFactory.Rect("TuneUp_menu", wheels.parent, 0, 0);
            tuneMenu.gameObject.SetActive(false);
            // Match Main's coordinate system, including its scale, so Main/Panel and its rows line up exactly.
            NativeMenuFactory.CopyRect((RectTransform)main, tuneMenu);
            var sourcePanel = main.Find("Panel");
            if (sourcePanel)
            {
                var panel = Object.Instantiate(sourcePanel.gameObject, tuneMenu, false);
                panel.name = "Panel";
                panel.transform.SetAsFirstSibling();
                panel.SetActive(true);
            }
            else Plugin.Warn("Native garage Main/Panel was not found; TUNE-UP background is unavailable.");
            var group = tuneMenu.gameObject.AddComponent<MenuGroup>();
            group.alighContent = MenuGroup.AlignType.None;
            group.disable_manualy = new MenuGroup[0];
            back = tuneMenu.gameObject.AddComponent<CarGarageUI_Back>();
            back.gr = garage;
            back.activateWhenBack = main.gameObject;
            back.top_txt = "TUNE-UP";

            using (var factory = new NativeMenuFactory(garage, template))
            {
                entry = Track(factory.Button("BtnTuneUp", template.transform.parent, "TUNE-UP", "Parameters_icon", tuneMenu));
                entry.isHideWhenPress = true;
                entry.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                var entryRect = entry.GetComponent<RectTransform>();
                NativeMenuFactory.CopyRect(sourceRect, entryRect);
                float lastY = siblings.Where(b => b == garage.fixCar ? MenuEsc.editCarInSceneGarage : b.gameObject.activeSelf)
                    .Select(b => b.GetComponent<RectTransform>().anchoredPosition.y).DefaultIfEmpty(sourceRect.anchoredPosition.y).Min();
                entryRect.anchoredPosition = new Vector2(sourceRect.anchoredPosition.x, lastY - step);

                engineRow = Track(factory.Button("BtnEngine", tuneMenu, "ДВИГАТЕЛЬ", "Engine_icon"));
                driveRow = Track(factory.Button("BtnDrive", tuneMenu, "ПРИВОД", "wheels_all"));
                var engineRect = engineRow.GetComponent<RectTransform>();
                var driveRect = driveRow.GetComponent<RectTransform>();
                // Preserve the native garage button's anchors, pivot, scale and
                // anchoredPosition. TuneUp_menu duplicates Main's RectTransform,
                // so both panels use the same anchored coordinate system.
                // The later GetWorldCorners -> Place() rewrite changed those anchors
                // to (0, 1) and moved the entire left column out of its native slot.
                NativeMenuFactory.CopyRect(sourceRect, engineRect);
                var first = siblings.OrderByDescending(b => b.GetComponent<RectTransform>().anchoredPosition.y).FirstOrDefault();
                if (first)
                    engineRect.anchoredPosition = first.GetComponent<RectTransform>().anchoredPosition;
                // Initial horizontal coordinate; the one-time alignment below
                // resolves it to local coordinates.
                engineRect.anchoredPosition = new Vector2(CategoryX, engineRect.anchoredPosition.y);

                // Keep the new, larger category buttons and their ECT.Visual layout.
                float headerWidth = Mathf.Clamp(sourceRect.rect.width, 260f, 280f);
                float headerHeight = Mathf.Max(105f, sourceRect.rect.height);
                engineRect.sizeDelta = new Vector2(headerWidth, headerHeight);
                NativeMenuFactory.CopyRect(engineRect, driveRect);
                driveRect.anchoredPosition = new Vector2(CategoryX, driveRect.anchoredPosition.y - step);

                engineSelectedValue = NativeMenuFactory.LayoutCategoryHeader(engineRow, "ДВИГАТЕЛЬ");
                driveSelectedValue = NativeMenuFactory.LayoutCategoryHeader(driveRow, "ПРИВОД", true);
                foreach (DriveLayout layout in new[] { DriveLayout.Stock, DriveLayout.FWD, DriveLayout.RWD, DriveLayout.AWD })
                    driveIcons[layout] = factory.Sprite(DriveIcon(layout));

                // Keep the native anchors/pivot and Y. Initial local X for the
                // lists is set once after construction, never on every Update.
                var corners = new Vector3[4];
                engineRect.GetWorldCorners(corners);
                Vector3 topRight = tuneMenu.InverseTransformPoint(corners[2]);
                float y = tuneMenu.rect.yMax - topRight.y;
                float listWidth = Mathf.Max(260f, headerWidth);
                // The cloned Main panel is shorter than the actual screen on some aspect
                // ratios; list backgrounds must extend to the viewport's lower edge.
                float listHeight = Mathf.Max(height * 3, tuneMenu.rect.height - y - 2);
                // Keep the root SelectUI positions unchanged. Only the two lists
                // live in this offset parent; the category buttons are direct children
                // of TuneUp_menu and must never be translated with the lists.
                choiceListOffsetRoot = NativeMenuFactory.Rect("ECT.SelectListOffset", tuneMenu, 0f, 0f);
                choiceListOffsetRoot.anchorMin = Vector2.zero;
                choiceListOffsetRoot.anchorMax = Vector2.one;
                choiceListOffsetRoot.pivot = tuneMenu.pivot;
                choiceListOffsetRoot.offsetMin = Vector2.zero;
                choiceListOffsetRoot.offsetMax = Vector2.zero;
                choiceListOffsetRoot.localRotation = Quaternion.identity;
                choiceListOffsetRoot.localScale = Vector3.one;
                choiceListOffsetRoot.anchoredPosition = new Vector2(ChoiceListVisualOffsetX, 0f);

                engines = MakeList("EngineSelectUI", ChoiceListX, y, listWidth, listHeight);
                drives = MakeList("DriveSelectUI", ChoiceListX, y, listWidth, listHeight);
                engineRow.content = (RectTransform)engines.transform;
                driveRow.content = (RectTransform)drives.transform;

                int index = 0;
                foreach (var engine in EngineCatalog.Options) AddEngine(factory, engine.Id, index++, listWidth);
                engines.ContentHeight = index * ChoiceStep;
                engines.Content.sizeDelta = new Vector2(listWidth, engines.ContentHeight);
                index = 0;
                foreach (DriveLayout drive in new[] { DriveLayout.FWD, DriveLayout.RWD, DriveLayout.AWD })
                {
                    var choice = drive;
                    var price = TuningPrice.Drivetrain(drive, EnhancedCarTuning.Configuration.TuningConfig.ChargeForChanges, EnhancedCarTuning.Configuration.TuningConfig.DrivePrice);
                    var button = Choice(factory, drives, "Drive." + drive, drive.ToString(), price, DriveIcon(drive), index++, listWidth,
                        () => { session.SelectDrive(choice); Refresh(); });
                    AddSelectionMarker(button, () => session.Selection.Draft.Drive == choice);
                }
                drives.ContentHeight = index * ChoiceStep;
                drives.Content.sizeDelta = new Vector2(listWidth, drives.ContentHeight);

                entry.selectAfterPress = engineRow;
                engineRow.gameObject.SetActive(true);
                driveRow.gameObject.SetActive(true);
                entry.gameObject.SetActive(EnhancedCarTuning.Configuration.TuningConfig.ShowGarageButton);
            }
            // Initialize native local X positions ONCE. Reapplying them from Update
            // makes runtime UI edits snap back every 0.15 seconds and fights
            // Unity's own Canvas / RectTransform coordinate conversion.
            AlignHorizontalPositions();
            LayoutLists();
            Refresh();
        }

        internal void LayoutLists()
        {
            if (!tuneMenu) return;
            // Only update the vertical size of the list backgrounds. Never write
            // horizontal positions here: GarageTuningPanel.Update() calls this
            // every 0.15 s, which otherwise undoes RUE edits / parent offsets.
            if (!GameUI.me || !GameUI.me.rectUI) return;
            GameUI.me.rectUI.GetWorldCorners(viewportCorners);
            float viewportBottom = tuneMenu.InverseTransformPoint(viewportCorners[0]).y;
            SizeListToBottom(engines, viewportBottom);
            SizeListToBottom(drives, viewportBottom);
        }

        private void AlignHorizontalPositions()
        {
            if (engineRow) SetLocalX(engineRow.GetComponent<RectTransform>(), CategoryX);
            if (driveRow) SetLocalX(driveRow.GetComponent<RectTransform>(), CategoryX);
            if (engines) SetLocalX((RectTransform)engines.transform, ChoiceListX);
            if (drives) SetLocalX((RectTransform)drives.transform, ChoiceListX);
        }

        private static void SetLocalX(RectTransform rect, float x)
        {
            if (!rect) return;
            Vector3 pos = rect.localPosition;
            if (Mathf.Abs(pos.x - x) < 0.01f) return;
            rect.localPosition = new Vector3(x, pos.y, pos.z);
        }

        private void SizeListToBottom(NativeChoiceList list, float viewportBottom)
        {
            if (!list) return;
            var rect = (RectTransform)list.transform;
            rect.GetWorldCorners(viewportCorners);
            float top = tuneMenu.InverseTransformPoint(viewportCorners[1]).y;
            float wantedHeight = Mathf.Max(ChoiceHeight * 3f, top - viewportBottom);
            if (Mathf.Abs(rect.sizeDelta.y - wantedHeight) > 1f)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, wantedHeight);
        }

        private NativeChoiceList MakeList(string name, float x, float y, float width, float height)
        {
            var rect = NativeMenuFactory.Rect(name, choiceListOffsetRoot, width, height);
            rect.gameObject.SetActive(false);
            NativeMenuFactory.Place(rect, x, y);
            rect.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.78f);
            rect.gameObject.AddComponent<RectMask2D>();
            rect.gameObject.AddComponent<MenuMaskArea>();
            var list = rect.gameObject.AddComponent<NativeChoiceList>();
            list.Content = NativeMenuFactory.Rect("Content", rect, width, 0);
            var group = list.Content.gameObject.AddComponent<MenuGroup>();
            group.alighContent = MenuGroup.AlignType.None;
            group.disable_manualy = new MenuGroup[0];
            return list;
        }

        private void AddEngine(NativeMenuFactory factory, string id, int index, float width)
        {
            var price = TuningPrice.Engine(id, EnhancedCarTuning.Configuration.TuningConfig.ChargeForChanges);
            var button = Choice(factory, engines, "Engine." + id, EngineCatalog.Label(id), price, "Engine_icon", index, width,
                () => { session.SelectEngine(id); Refresh(); });
            AddSelectionMarker(button, () => session.Selection.Draft.EngineId == id);
        }

        private MenuButtonGroup Choice(NativeMenuFactory factory, NativeChoiceList list, string name, string label, TuningPrice price, string icon, int index, float width, Action action)
        {
            var button = Track(factory.Button(name, list.Content, label, icon, action: action));
            var rect = button.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width - 12, ChoiceHeight);
            NativeMenuFactory.Place(rect, 6, index * ChoiceStep + 3);
            factory.LayoutChoice(button, label, price);
            button.maskArea = list.GetComponent<MenuMaskArea>();
            button.gameObject.SetActive(true);
            return button;
        }

        private void AddSelectionMarker(MenuButtonGroup button, Func<bool> selected)
        {
            var marker = NativeMenuFactory.Rect("ECT.Selected", button.transform, 6, ChoiceHeight - 9);
            NativeMenuFactory.Place(marker, 12, 5);
            var image = marker.gameObject.AddComponent<Image>(); image.color = new Color(0.1f, 0.66f, 0.1f, 0.6f); image.raycastTarget = false;
            refreshChoices.Add(() => { if (image) image.enabled = selected(); });
            refreshChoices.Add(() => { if (button) button.nowButtonDissabe = !session.CanEdit; });
        }

        internal void Refresh()
        {
            if (!entry || !engineRow || !driveRow) return;
            var draft = session.Selection.Draft;
            if (engineSelectedValue) engineSelectedValue.text = draft.EngineId == TuningSettings.StockEngine
                ? string.Empty : EngineCatalog.Label(draft.EngineId);
            if (driveSelectedValue) driveSelectedValue.text = draft.Drive == DriveLayout.Stock
                ? string.Empty : draft.Drive.ToString();
            Sprite driveSprite;
            if (driveIcons.TryGetValue(draft.Drive, out driveSprite) && driveSprite)
                NativeMenuFactory.SetIcon(driveRow, driveSprite);
            foreach (var refresh in refreshChoices) refresh();
        }

        internal bool TryCollapseList(MenuButtonGroup pressed, bool isMouseClick)
        {
            if (!isMouseClick || (pressed != engineRow && pressed != driveRow) || !pressed.content ||
                !pressed.content.gameObject.activeSelf) return false;
            pressed.content.gameObject.SetActive(false);
            var group = tuneMenu ? tuneMenu.GetComponent<MenuGroup>() : null;
            if (group && group.CurGroup == pressed) group.CurGroup = null;
            // Only clear this button's visual focus; other menu buttons are unaffected.
            if (pressed._focusManger && pressed._focusManger.curActive == pressed)
                pressed._focusManger.curActive = null;
            pressed.SetContentActive(false);
            return true;
        }

        internal void SetVisible(bool visible)
        {
            if (!entry) return;
            if (entry.gameObject.activeSelf != visible) entry.gameObject.SetActive(visible);
            if (!visible && IsActive) back.pressCallBack();
        }

        internal bool ContainsPointer()
        {
            if (entry && entry.gameObject.activeInHierarchy && entry.GetComponent<RectTransform>().isMouseIntersect(Input.mousePosition)) return true;
            return IsActive && (engineRow.GetComponent<RectTransform>().isMouseIntersect(Input.mousePosition) ||
                driveRow.GetComponent<RectTransform>().isMouseIntersect(Input.mousePosition) || engines.ContainsPointer() || drives.ContainsPointer());
        }

        private MenuButtonGroup Track(MenuButtonGroup button) { ownedButtons.Add(button); return button; }
        private static string DriveIcon(DriveLayout drive) => drive == DriveLayout.FWD ? "wheels_front" : drive == DriveLayout.RWD ? "wheels_back" : "wheels_all";

        public void Dispose()
        {
            if (tuneMenu && tuneMenu.gameObject.activeSelf)
            {
                tuneMenu.gameObject.SetActive(false);
                if (main && garage && garage.gameObject.activeInHierarchy) main.gameObject.SetActive(true);
            }
            foreach (var button in ownedButtons) NativeMenuFactory.RemoveButton(button);
            ownedButtons.Clear(); refreshChoices.Clear(); driveIcons.Clear();
            if (tuneMenu) Object.Destroy(tuneMenu.gameObject);
        }
    }
}
