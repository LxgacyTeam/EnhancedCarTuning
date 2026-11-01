using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.UI;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.Appearance.Police;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.UI
{
    // Separate navigation, keeping the original MenuColor, vinyl editor and native checkout alive.
    internal sealed class GarageAppearanceView : IDisposable
    {
        private readonly CarGarageUI garage;
        private RectTransform main, colors, vinyls, bodyBackup;
        private MenuButtonGroup colorEntry, vinylEntry;
        private Transform bodyParent;
        private int bodyIndex;
        private bool bodyStatic;
        private MenuGroup bodyGroup;
        private readonly Dictionary<RectTransform, Vector2> shifted = new Dictionary<RectTransform, Vector2>();
        private readonly List<Graphic> hiddenIcons = new List<Graphic>();
        private readonly Dictionary<MenuButtonGroup, MenuButton> oldFocusTargets = new Dictionary<MenuButtonGroup, MenuButton>();
        private GameObject vinylIcon;
        private string oldLabel;
        private bool oldLocalizeEnabled, oldLocalizeInitialized;
        private bool movedBody;
        private Color? lastBodyColor;
        private readonly PolicePaintSession policeSession;
        private MenuButtonGroup policeButton;
        private PolicePaintArrow policeArrow;
        private string policeLabel;
        private RectTransform glassEntry;
        internal void SetGlassEntry(RectTransform entry) { glassEntry = entry; }

        internal GarageAppearanceView(CarGarageUI garage, PolicePaintSession policeSession)
        { this.garage = garage; this.policeSession = policeSession; }
        internal bool IsColors => colors && colors.gameObject.activeInHierarchy;
        internal string Title => IsColors ? ECTLocalization.T("ПОКРАСКА АВТО") : vinyls && vinyls.gameObject.activeInHierarchy ? ECTLocalization.T("ВИНИЛЫ") : null;

        internal void Build()
        {
            main = garage.root.Find("Main") as RectTransform;
            var paint = main ? main.Find("Paint") : null;
            vinylEntry = paint ? paint.GetComponent<MenuButtonGroup>() : null;
            vinyls = garage.root.Find("PaintVinils") as RectTransform;
            var add = vinyls ? vinyls.Find("left_panel/Add") as RectTransform : null;
            var iconSource = vinyls ? vinyls.Find("left_panel/Align/Result") : null;
            if (!main || !vinylEntry || !vinyls || !add || !garage.bodyColor || !vinylEntry.text)
                throw new InvalidOperationException("Native Main/Paint, PaintVinils/left_panel/Add or body color missing.");
            if (vinylEntry.content != vinyls)
                throw new InvalidOperationException("Main/Paint points to an unexpected menu; leaving native navigation intact.");

            colors = NativeMenuFactory.Rect("ECT.Colors_menu", vinyls.parent, 0, 0);
            colors.gameObject.SetActive(false); NativeMenuFactory.CopyRect(main, colors);
            var background = main.Find("Panel");
            if (background)
            {
                var copy = Object.Instantiate(background.gameObject, colors, false);
                copy.name = "Panel"; copy.SetActive(true); copy.transform.SetAsFirstSibling();
            }
            var group = colors.gameObject.AddComponent<MenuGroup>();
            group.alighContent = MenuGroup.AlignType.None; group.disable_manualy = new MenuGroup[0];
            var back = colors.gameObject.AddComponent<CarGarageUI_Back>();
            back.gr = garage; back.activateWhenBack = main.gameObject; back.top_txt = ECTLocalization.T("ПОКРАСКА АВТО");

            var paintRect = (RectTransform)vinylEntry.transform;
            var body = (RectTransform)garage.bodyColor.transform;
            bodyParent = body.parent; bodyIndex = body.GetSiblingIndex(); bodyStatic = garage.bodyColor.isStaickPos;
            bodyGroup = garage.bodyColor.GetComponentInParent<MenuGroup>();
            bodyBackup = NativeMenuFactory.Rect("ECT.BodyTransformBackup", bodyParent, 0, 0);
            NativeMenuFactory.CopyRect(body, bodyBackup); bodyBackup.gameObject.SetActive(false);

            using (var factory = new NativeMenuFactory(garage, vinylEntry))
            {
                // Clone before changing the original paint label/icon. Its serialized vinyl callbacks are cleared in the factory.
                colorEntry = factory.Button("ECT.PaintColors", main, "ПОКРАСКА", "", colors, () => EndVinylEditing());
                CopyIcon(vinylEntry, colorEntry);
                colorEntry.isHideWhenPress = true; colorEntry.isStaickPos = vinylEntry.isStaickPos;
                NativeMenuFactory.CopyRect(paintRect, (RectTransform)colorEntry.transform);
                colorEntry.transform.SetSiblingIndex(vinylEntry.transform.GetSiblingIndex());

                float step = paintRect.rect.height + 6;
                var siblings = main.GetComponentsInChildren<MenuButton>(true)
                    .Where(b => b.transform.parent == main && b != colorEntry && b.gameObject.activeSelf).ToArray();
                var distances = siblings.Select(b => paintRect.anchoredPosition.y - ((RectTransform)b.transform).anchoredPosition.y)
                    .Where(d => d > 1).ToArray();
                if (distances.Length > 0) step = distances.Min();
                float top = paintRect.anchoredPosition.y;
                foreach (var button in siblings)
                {
                    var rect = (RectTransform)button.transform;
                    if (rect.anchoredPosition.y <= top + 1) Shift(rect, -step);
                }
                var mainGroup = vinylEntry.GetComponentInParent<MenuGroup>();
                if (mainGroup)
                {
                    mainGroup.buttons.Remove(colorEntry);
                    mainGroup.buttons.Insert(Math.Max(0, mainGroup.buttons.IndexOf(vinylEntry)), colorEntry);
                }
            }

            if (bodyParent == add.parent)
            {
                float gap = body.anchoredPosition.y - add.anchoredPosition.y;
                float addY = add.anchoredPosition.y;
                foreach (var button in add.parent.GetComponentsInChildren<MenuButton>(true))
                    if (button != garage.bodyColor && button.transform.parent == add.parent)
                    {
                        var rect = (RectTransform)button.transform;
                        if (rect.anchoredPosition.y <= addY + 1) Shift(rect, gap);
                    }
            }
            if (bodyGroup) bodyGroup.buttons.Remove(garage.bodyColor);
            body.SetParent(colors, false); movedBody = true;
            NativeMenuFactory.CopyRect((RectTransform)colorEntry.transform, body);
            garage.bodyColor.isStaickPos = true; garage.bodyColor.isInitParent = false; garage.bodyColor.InitParent();
            colorEntry.selectAfterPress = garage.bodyColor;
            // Includes return buttons from nested vinyl menus, not only the Main entry.
            foreach (var button in garage.GetComponentsInChildren<MenuButtonGroup>(true))
                if (button.content == vinyls && button.selectAfterPress == garage.bodyColor)
                {
                    oldFocusTargets.Add(button, button.selectAfterPress);
                    button.selectAfterPress = add.GetComponent<MenuButton>();
                }
            if (vinylEntry.text)
            {
                oldLabel = vinylEntry.text.ui.text;
                oldLocalizeEnabled = vinylEntry.text.enabled; oldLocalizeInitialized = vinylEntry.text.isInit;
                vinylEntry.text.enabled = false; vinylEntry.text.isInit = true;
                NativeMenuFactory.SetLabel(vinylEntry, "ВИНИЛЫ");
            }
            if (iconSource) SetVinylIcon(iconSource.GetComponent<Graphic>());
            colorEntry.gameObject.SetActive(true);
        }

        private void Shift(RectTransform rect, float offset)
        { if (!shifted.ContainsKey(rect)) shifted.Add(rect, rect.anchoredPosition); rect.anchoredPosition += new Vector2(0, offset); }

        internal void BuildPoliceButton()
        {
            if (policeSession == null || !policeSession.State.Supported || !colors) return;
            using (var factory = new NativeMenuFactory(garage, vinylEntry))
            {
                policeButton = factory.Button("ECT.PolicePaint", colors, "POLICE\nNO", "", action: () =>
                {
                    if (MenuBuy.me && MenuBuy.me.gameObject.activeInHierarchy) return;
                    policeSession.Toggle(); RefreshPoliceButton();
                });
                policeButton.isStaickPos = true;
                var label = policeButton.text.ui;
                label.transform.SetParent(policeButton.transform, false);
                foreach (var graphic in policeButton.GetComponentsInChildren<Graphic>(true))
                    if (graphic != label && graphic.transform != policeButton.transform) graphic.gameObject.SetActive(false);
                label.gameObject.SetActive(true); label.enabled = true; label.raycastTarget = false;
                label.fontSize = label.resizeTextMaxSize = 42; label.resizeTextMinSize = 26;
                label.alignment = TextAnchor.MiddleRight; label.horizontalOverflow = HorizontalWrapMode.Wrap;
                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(.25f, .09f); rect.anchorMax = new Vector2(.93f, .91f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var arrowRect = NativeMenuFactory.Rect("ECT.PoliceArrow", policeButton.transform, 23, 34);
                arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new Vector2(0, .5f);
                arrowRect.anchoredPosition = new Vector2(34, 0);
                policeArrow = arrowRect.gameObject.AddComponent<PolicePaintArrow>();
                policeArrow.color = label.color; policeArrow.raycastTarget = false;
                RefreshPoliceButton(); policeButton.gameObject.SetActive(true);
            }
        }

        internal void RefreshPoliceButton()
        {
            if (!policeButton || !garage || !garage.bodyColor) return;
            if (policeArrow) policeArrow.color = policeButton.text.ui.color;
            var body = glassEntry ? glassEntry : (RectTransform)garage.bodyColor.transform;
            var rect = (RectTransform)policeButton.transform;
            NativeMenuFactory.CopyRect(body, rect);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 148);
            rect.anchoredPosition = body.anchoredPosition - new Vector2(0, body.rect.height + 12);
            string label = policeSession.State.Active ? "POLICE\nYES" : "POLICE\nNO";
            if (label == policeLabel) return;
            policeLabel = label; NativeMenuFactory.SetLabel(policeButton, label);
        }

        private static Graphic Icon(MenuButtonGroup button) => button.GetComponentsInChildren<Graphic>(true)
            .FirstOrDefault(g => !(g is Text) && g.transform != button.transform && (g is Image || g is RawImage));

        private static void CopyIcon(MenuButtonGroup source, MenuButtonGroup target)
        {
            var from = Icon(source); var to = Icon(target);
            if (!from || !to) return;
            to.gameObject.SetActive(from.gameObject.activeSelf); to.material = from.material; to.color = from.color;
            if (from is Image a && to is Image b) { b.sprite = a.overrideSprite; b.type = a.type; }
            if (from is RawImage x && to is RawImage y) { y.texture = x.texture; y.uvRect = x.uvRect; }
        }

        private void SetVinylIcon(Graphic source)
        {
            if (!(source is Image) && !(source is RawImage)) return;
            var previous = Icon(vinylEntry);
            var rect = NativeMenuFactory.Rect("ECT.VinylIcon", vinylEntry.transform, 64, 64);
            if (previous) NativeMenuFactory.CopyRect(previous.rectTransform, rect);
            else
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
                rect.anchoredPosition = new Vector2(-20, 0);
            }
            if (previous && previous.gameObject.activeSelf)
            { hiddenIcons.Add(previous); previous.gameObject.SetActive(false); }
            Graphic icon;
            if (source is Image image)
            {
                var copy = rect.gameObject.AddComponent<Image>(); copy.sprite = image.overrideSprite;
                copy.type = Image.Type.Simple; copy.preserveAspect = true; icon = copy;
            }
            else
            {
                var copy = rect.gameObject.AddComponent<RawImage>(); var raw = (RawImage)source;
                copy.texture = raw.texture; copy.uvRect = raw.uvRect; icon = copy;
            }
            icon.material = source.material; icon.color = source.color; icon.raycastTarget = false;
            vinylIcon = rect.gameObject;
        }

        internal void EndVinylEditing()
        {
            if (!garage) return;
            garage.CarPaint_Finish();
            garage.isCanStartAlign = garage.isStartAlignMouse0 = false;
            garage.isStartAlign_FingerID = -1;
            if (garage.paintSprites) garage.paintSprites.gameObject.SetActive(false);
        }

        internal void RefreshPaintTexture()
        {
            if (!garage || !garage.carMat || !garage.carPaint) return;
            var color = garage.carMat.body.color;
            if (lastBodyColor.HasValue && !CarMaterial.isColorChanget(lastBodyColor.Value, color)) return;
            lastBodyColor = color;
            // UpPaint used to redraw while a vinyl was selected. Colors can now be edited with editItem == null.
            // LateUpdate observes the color already copied from the native picker by CarGarageUI.Update.
            garage.carPaint.UpImage();
        }

        public void Dispose()
        {
            NativeMenuFactory.RemoveButton(policeButton); policeButton = null;
            if (colors && colors.gameObject.activeSelf) { colors.gameObject.SetActive(false); if (main) main.gameObject.SetActive(true); }
            if (movedBody && garage && garage.bodyColor && bodyParent && bodyBackup)
            {
                var currentGroup = garage.bodyColor.GetComponentInParent<MenuGroup>();
                if (currentGroup) currentGroup.buttons.Remove(garage.bodyColor);
                var body = (RectTransform)garage.bodyColor.transform;
                body.SetParent(bodyParent, false); body.SetSiblingIndex(bodyIndex); NativeMenuFactory.CopyRect(bodyBackup, body);
                garage.bodyColor.isStaickPos = bodyStatic; garage.bodyColor.isInitParent = false; garage.bodyColor.InitParent();
            }
            if (vinylEntry && oldLabel != null)
            {
                vinylEntry.text.ui.text = oldLabel; vinylEntry.text.enabled = oldLocalizeEnabled; vinylEntry.text.isInit = oldLocalizeInitialized;
            }
            foreach (var pair in oldFocusTargets) if (pair.Key) pair.Key.selectAfterPress = pair.Value;
            foreach (var pair in shifted) if (pair.Key) pair.Key.anchoredPosition = pair.Value;
            foreach (var icon in hiddenIcons) if (icon) icon.gameObject.SetActive(true);
            if (vinylIcon) Object.Destroy(vinylIcon);
            NativeMenuFactory.RemoveButton(colorEntry);
            if (colors) Object.Destroy(colors.gameObject);
            if (bodyBackup) Object.Destroy(bodyBackup.gameObject);
            shifted.Clear(); hiddenIcons.Clear(); oldFocusTargets.Clear();
        }
    }
}
