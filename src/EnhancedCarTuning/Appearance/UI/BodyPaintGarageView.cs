using System;
using System.Collections.Generic;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class BodyPaintGarageView : IDisposable
    {
        private sealed class Row
        { internal int Mask; internal MenuButtonGroup Button; internal Text Label; internal Image Swatch, Marker; }
        private readonly CarGarageUI garage;
        private readonly BodyPaintSession session;
        private readonly List<Row> rows = new List<Row>();
        private readonly List<Image> samples = new List<Image>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform root, panel, overlayParent;
        private MenuButtonGroup many;
        private MenuFocusManager focus;
        private MenuGroup buttonGroup;
        private Graphic nativeResult;
        private bool nativeResultActive;
        private Vector2 nativeBodySize;
        private Text nativeBodyLabel;
        private TextAnchor nativeBodyAlignment;
        private RectTransform labelBackup, sampleRoot;
        private bool bodyLayoutChanged;
        private Text manyLabel;
        private Text manyCheck;
        private MenuColorPicker picker;
        private PaintFinish lastPicker;
        private bool multiple;
        private int selected = 1 << (int)BodyPart.Body;
        private int Available => session.State.Renderer?.AvailableMask ?? 0;
        internal BodyPaintGarageView(CarGarageUI garage, BodyPaintSession session) { this.garage = garage; this.session = session; }
        internal bool IsOpen => garage && garage.bodyColor && garage.bodyColor.Created &&
            garage.bodyColor.Created.gameObject.activeInHierarchy;
        private static bool PurchaseOpen => MenuBuy.me && MenuBuy.me.gameObject.activeInHierarchy &&
            (!MenuBuy.me.root || MenuBuy.me.root.activeInHierarchy);
        internal void Build()
        {
            var template = garage.root.Find("Main/Paint").GetComponent<MenuButtonGroup>();
            if (!template || !garage.bodyColor) throw new InvalidOperationException("Body paint controls unavailable.");
            session.State.Renderer?.ScanNow();
            selected = First(Available);
            overlayParent = garage.transform as RectTransform;
            if (!overlayParent) throw new InvalidOperationException("Garage overlay parent unavailable.");
            root = NativeMenuFactory.Rect("ECT.BodyPaintControls", overlayParent, 1920, 1080); root.gameObject.SetActive(false);
            focus = root.gameObject.AddComponent<MenuFocusManager>();
            if (template.focusManger) focus.saveSettingsTo = template.focusManger.saveSettingsTo;
            buttonGroup = root.gameObject.AddComponent<MenuGroup>(); buttonGroup.alighContent = MenuGroup.AlignType.None;
            buttonGroup.disable_manualy = new MenuGroup[0];
            panel = Box("ECT.BodyParts", root, 1508, 114, 412, 966);
            NativePanelBackground.Attach(panel, garage);
            using (var factory = new NativeMenuFactory(garage, template))
            {
                many = Button(factory, "Many", "ВЫБРАТЬ НЕСКОЛЬКО", 12, 16, 388, 70, () =>
                { FlushPicker(); multiple = !multiple; if (!multiple) selected = First(selected); SyncPicker(); });
                manyLabel = many.text.ui;
                Fit(manyLabel.rectTransform, .235f, .1f, .94f, .9f);
                manyCheck = Box("ECT.SelectManyCheck", many.transform, 40, 16, 38, 38).gameObject.AddComponent<Text>();
                manyCheck.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); manyCheck.fontSize = 32;
                manyCheck.color = Color.white; manyCheck.alignment = TextAnchor.MiddleCenter; manyCheck.raycastTarget = false;
                for (int i = 0; i < BodyPaintSettings.Count; i++)
                {
                    int mask = 1 << i;
                    string label = BodyPartNames.Label((BodyPart)i);
                    var button = Button(factory, "Part." + i, label, 12, 106 + i * 108, 388, 100, () => Select(mask));
                    // The native background sprite has transparent margins; keep content inside its visible face.
                    Fit(button.text.ui.rectTransform, .13f, .10f, .73f, .90f);
                    var swatch = Box("ECT.PartColor", button.transform, 292, 22, 56, 56).gameObject.AddComponent<Image>();
                    swatch.raycastTarget = false;
                    var marker = Box("ECT.PartSelected", button.transform, 26, 8, 6, 84).gameObject.AddComponent<Image>();
                    marker.color = new Color(.05f, .90f, .13f, 1); marker.raycastTarget = false;
                    rows.Add(new Row { Mask = mask, Button = button, Label = button.text.ui, Swatch = swatch, Marker = marker });
                }
            }
            BuildSamples();
            Layout();
        }
        private void BuildSamples()
        {
            var body = (RectTransform)garage.bodyColor.transform;
            nativeBodySize = body.sizeDelta;
            bodyLayoutChanged = true;
            body.sizeDelta = new Vector2(body.sizeDelta.x, 224);
            nativeResult = garage.bodyColor.Result;
            if (nativeResult) { nativeResultActive = nativeResult.gameObject.activeSelf; nativeResult.gameObject.SetActive(false); }
            nativeBodyLabel = garage.bodyColor.text;
            if (nativeBodyLabel)
            {
                nativeBodyAlignment = nativeBodyLabel.alignment;
                labelBackup = NativeMenuFactory.Rect("ECT.BodyLabelBackup", body, 0, 0);
                NativeMenuFactory.CopyRect(nativeBodyLabel.rectTransform, labelBackup); labelBackup.gameObject.SetActive(false);
                Fit(nativeBodyLabel.rectTransform, .20f, .69f, .95f, .97f);
                nativeBodyLabel.rectTransform.anchoredPosition += new Vector2(-22f, -10f);
                nativeBodyLabel.alignment = TextAnchor.MiddleLeft;
            }
            sampleRoot = NativeMenuFactory.Rect("ECT.BodyPaintSamples", body, 0, 0);
            Fit(sampleRoot, .200f, .06f, .95f, .65f);
            sampleRoot.anchoredPosition += new Vector2(-22f, 0f);
            for (int i = 0; i < BodyPaintSettings.Count; i++)
            {
                var image = NativeMenuFactory.Rect("ECT.BodySample." + (BodyPart)i, sampleRoot, 48, 48).gameObject.AddComponent<Image>();
                image.raycastTarget = false; samples.Add(image);
            }
        }
        private void RefreshSamples()
        {
            if (!sampleRoot) return;
            int count = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                bool show = (Available & (1 << i)) != 0;
                samples[i].gameObject.SetActive(show);
                if (show) count++;
            }
            if (count == 0) return;
            int columns = Math.Min(4, count), lines = (count + columns - 1) / columns;
            float side = Mathf.Min((sampleRoot.rect.width - (columns - 1) * 8) / columns,
                Mathf.Min(60, (sampleRoot.rect.height - (lines - 1) * 8) / lines));
            if (side <= 0) return;
            int visible = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if (!samples[i].gameObject.activeSelf) continue;
                var image = samples[i]; image.color = session.Get((BodyPart)i).Color;
                Place(image.rectTransform, (visible % columns) * (side + 8), (visible / columns) * (side + 8), side, side);
                visible++;
            }
        }
        private MenuButtonGroup Button(NativeMenuFactory factory, string name, string label, float x, float y, float width, float height, Action action)
        {
            var button = factory.Button("ECT.BodyPaint." + name, panel, label, "", action: () => { if (!PurchaseOpen) action(); });
            button._focusManger = focus;
            button.manualGroup = buttonGroup;
            if (!buttonGroup.buttons.Contains(button)) buttonGroup.buttons.Add(button);
            button.isStaickPos = true; Place((RectTransform)button.transform, x, y, width, height);
            var title = button.text.ui; title.transform.SetParent(button.transform, false);
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                if (graphic != title && graphic.gameObject != button.gameObject) graphic.gameObject.SetActive(false);
            title.gameObject.SetActive(true); title.enabled = true;
            title.fontSize = title.resizeTextMaxSize = 36; title.resizeTextMinSize = 18;
            title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.raycastTarget = false; Fit(title.rectTransform, .05f, .1f, .95f, .9f);
            button.gameObject.SetActive(true); return button;
        }
        private void Select(int mask)
        {
            FlushPicker();
            if (!multiple) selected = mask;
            else if ((selected ^ mask) != 0) selected ^= mask;
            SyncPicker(); RefreshRows();
        }
        private static int First(int mask)
        {
            for (int i = 0; i < BodyPaintSettings.Count; i++) if ((mask & (1 << i)) != 0) return 1 << i;
            return 0;
        }
        private PaintFinish SelectionFinish()
        {
            for (int i = 0; i < BodyPaintSettings.Count; i++) if ((selected & (1 << i)) != 0) return session.Get((BodyPart)i);
            return session.Base;
        }
        private void SyncPicker()
        {
            lastPicker = SelectionFinish();
            garage.bodyColor.SetColor(lastPicker.Color); garage.bodyColor.SetMettalic(lastPicker.Metallic); garage.bodyColor.SetGloss(lastPicker.Gloss);
            if (!picker) return;
            picker.SetColor(lastPicker.Color); picker.mettalic.SetCur(lastPicker.Metallic, false); picker.gloss.SetCur(lastPicker.Gloss, false);
        }
        internal void FlushPicker()
        {
            if (!garage || !garage.bodyColor || lastPicker == null) return;
            var finish = new PaintFinish(garage.bodyColor.GetColor(), garage.bodyColor.GetMettalic(), garage.bodyColor.GetGloss());
            if (finish.Equals(lastPicker)) return;
            if (session.CanEdit) { session.Paint(selected, finish); lastPicker = finish; }
            else SyncPicker();
        }
        internal void SyncImported()
        {
            selected = First(Available);
            picker = garage.bodyColor.Created;
            SyncPicker(); RefreshSamples(); RefreshRows();
        }
        internal void Refresh()
        {
            if (!root || !garage) return;
            FlushPicker();
            var current = garage.bodyColor.Created;
            if (current != picker)
            {
                picker = current;
                if (picker) { selected = First(Available); SyncPicker(); }
            }
            bool open = IsOpen && !PurchaseOpen;
            root.gameObject.SetActive(open);
            RefreshSamples();
            if (!open) return;
            Layout(); RefreshRows();
        }
        private void RefreshRows()
        {
            if (!root) return;
            manyLabel.text = ECTLocalization.T("ВЫБРАТЬ НЕСКОЛЬКО");
            manyCheck.text = multiple ? "☑" : "☐";
            int available = Available;
            int validSelection = selected & available;
            if (validSelection == 0) validSelection = First(available);
            if (validSelection != selected) { selected = validSelection; SyncPicker(); }
            int visible = 0;
            foreach (var row in rows)
            {
                bool show = (available & row.Mask) != 0;
                row.Button.gameObject.SetActive(show); if (!show) continue;
                Place((RectTransform)row.Button.transform, 12, 106 + visible++ * 108, 388, 100);
                row.Button.nowButtonDissabe = !session.CanEdit;
                row.Marker.gameObject.SetActive((selected & row.Mask) != 0);
                row.Swatch.color = session.Get(Part(row.Mask)).Color;
                row.Label.text = ECTLocalization.T(BodyPartNames.Label(Part(row.Mask)));
            }
        }
        private static BodyPart Part(int mask)
        { for (int i = 0; i < BodyPaintSettings.Count; i++) if ((mask & (1 << i)) != 0) return (BodyPart)i; return BodyPart.Body; }
        private void Layout() { if (root && GameUI.me) CanvasAspectLayout.Apply(root, overlayParent, GameUI.me.rectUI, corners); }
        internal bool ContainsPointer() => root && root.gameObject.activeInHierarchy && panel.isMouseIntersect(Input.mousePosition);
        private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height)
        { var rect = NativeMenuFactory.Rect(name, parent, width, height); Place(rect, x, y, width, height); return rect; }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.sizeDelta = new Vector2(width, height); NativeMenuFactory.Place(rect, x, y); }
        private static void Fit(RectTransform rect, float x1, float y1, float x2, float y2)
        { rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.pivot = new Vector2(.5f, .5f);
          rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2); rect.offsetMin = rect.offsetMax = Vector2.zero; }
        public void Dispose()
        {
            if (many) NativeMenuFactory.RemoveButton(many);
            foreach (var row in rows) if (row.Button) NativeMenuFactory.RemoveButton(row.Button);
            rows.Clear(); if (root) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); }
            if (bodyLayoutChanged && garage && garage.bodyColor)
            {
                ((RectTransform)garage.bodyColor.transform).sizeDelta = nativeBodySize;
                if (nativeResult) nativeResult.gameObject.SetActive(nativeResultActive);
                if (nativeBodyLabel && labelBackup)
                { NativeMenuFactory.CopyRect(labelBackup, nativeBodyLabel.rectTransform); nativeBodyLabel.alignment = nativeBodyAlignment; }
            }
            samples.Clear(); if (sampleRoot) Object.Destroy(sampleRoot.gameObject); if (labelBackup) Object.Destroy(labelBackup.gameObject);
        }
    }
}
