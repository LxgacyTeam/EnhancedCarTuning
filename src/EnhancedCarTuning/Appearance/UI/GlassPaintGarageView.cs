using System;
using System.Collections.Generic;
using EnhancedCarTuning.Appearance.Glass;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class GlassPaintGarageView : IDisposable
    {
        private sealed class Row
        { internal int Mask; internal MenuButtonGroup Button; internal Image Swatch, Marker; }
        private readonly CarGarageUI garage;
        private readonly GlassGaragePrototype session;
        private readonly bool lamps;
        private readonly RectTransform preceding;
        private readonly GlassGroup[] groups;
        private string Prefix => lamps ? "ECT.Lamp" : "ECT.Glass";
        private readonly List<Row> rows = new List<Row>();
        private readonly List<MenuButtonGroup> buttons = new List<MenuButtonGroup>();
        private readonly List<Image> samples = new List<Image>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform colors, root, panel, overlayParent, sampleRoot;
        private MenuButtonGroup entry, many;
        private MenuFocusManager focus;
        private MenuGroup buttonGroup;
        private Text manyCheck;
        private MenuColorPicker picker;
        private Color lastColor;
        private float lastGloss, lastMetallic, nextScan;
        private bool syncing, multiple;
        private int selected;
        internal bool HasParts => Available != 0;
        internal RectTransform Entry => entry ? (RectTransform)entry.transform : null;
        internal bool IsOpen => picker && picker.gameObject.activeInHierarchy && (!picker.top_panel || picker.top_panel.cur == picker);
        private static bool PurchaseOpen => MenuBuy.me && MenuBuy.me.gameObject.activeInHierarchy &&
            (!MenuBuy.me.root || MenuBuy.me.root.activeInHierarchy);
        private int Available
        {
            get { int mask = 0; foreach (var group in groups) if (session.State.GroupCount(group) > 0) mask |= 1 << (int)group; return mask; }
        }
        internal GlassPaintGarageView(CarGarageUI garage, GlassGaragePrototype session, bool lamps = false, RectTransform preceding = null)
        { this.garage = garage; this.session = session; this.lamps = lamps; this.preceding = preceding;
          groups = lamps ? new[] { GlassGroup.LampsFront, GlassGroup.LampsBack } : new[] { GlassGroup.Front, GlassGroup.Back, GlassGroup.Middle, GlassGroup.Sunroof }; }
        internal void Build()
        {
            colors = garage.root.Find("ECT.Colors_menu") as RectTransform;
            var template = garage.root.Find("Main/Paint").GetComponent<MenuButtonGroup>();
            if (!colors || !template || !garage.bodyColor || !garage.bodyColor.picker || !session.State)
                throw new InvalidOperationException("Native glass paint controls unavailable.");
            overlayParent = garage.transform as RectTransform;
            if (!overlayParent) throw new InvalidOperationException("Garage overlay parent unavailable.");
            selected = First(Available);
            root = NativeMenuFactory.Rect(Prefix + "PaintControls", overlayParent, 1920, 1080);
            root.gameObject.SetActive(false);
            focus = root.gameObject.AddComponent<MenuFocusManager>();
            if (template.focusManger) focus.saveSettingsTo = template.focusManger.saveSettingsTo;
            buttonGroup = root.gameObject.AddComponent<MenuGroup>(); buttonGroup.alighContent = MenuGroup.AlignType.None;
            buttonGroup.disable_manualy = new MenuGroup[0];
            panel = Box(Prefix + "Groups", root, 1508, 114, 412, 966);
            NativePanelBackground.Attach(panel, garage);
            using (var factory = new NativeMenuFactory(garage, template))
            {
                entry = factory.Button(Prefix + "Paint", colors, lamps ? "ФАРЫ" : "СТЕКЛА", "", action: OpenPicker);
                entry.isStaickPos = true;
                PrepareText(entry, 36);
                Fit(entry.text.ui.rectTransform, .13f, .60f, .92f, .95f);
                sampleRoot = NativeMenuFactory.Rect(Prefix + "Samples", entry.transform, 0, 0);
                Fit(sampleRoot, .13f, .13f, .90f, .49f);
                for (int i = 0; i < groups.Length; i++)
                {
                    var image = NativeMenuFactory.Rect(Prefix + "Sample." + groups[i], sampleRoot, 48, 48).gameObject.AddComponent<Image>();
                    image.raycastTarget = false; samples.Add(image);
                }
                many = Button(factory, "Many", "ВЫБРАТЬ НЕСКОЛЬКО", 16, 70, () =>
                { FlushPicker(); multiple = !multiple; if (!multiple) selected = First(selected); SyncPicker(); });
                Fit(many.text.ui.rectTransform, .235f, .1f, .94f, .9f);
                manyCheck = Box(Prefix + "SelectManyCheck", many.transform, 40, 16, 38, 38).gameObject.AddComponent<Text>();
                manyCheck.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); manyCheck.fontSize = 32;
                manyCheck.color = Color.white; manyCheck.alignment = TextAnchor.MiddleCenter; manyCheck.raycastTarget = false;
                for (int i = 0; i < groups.Length; i++)
                {
                    int mask = 1 << (int)groups[i];
                    var button = Button(factory, "Group." + groups[i], Label(groups[i]), 106 + i * 108, 100, () => Select(mask));
                    Fit(button.text.ui.rectTransform, .13f, .10f, .73f, .90f);
                    var swatch = Box(Prefix + "GroupColor", button.transform, 292, 22, 56, 56).gameObject.AddComponent<Image>();
                    swatch.raycastTarget = false;
                    var marker = Box(Prefix + "GroupSelected", button.transform, 26, 8, 6, 84).gameObject.AddComponent<Image>();
                    marker.color = new Color(.05f,.90f,.13f,1); marker.raycastTarget = false;
                    rows.Add(new Row { Mask = mask, Button = button, Swatch = swatch, Marker = marker });
                }
                Button(factory, "Reset", "СБРОСИТЬ ВЫБРАННОЕ", Mathf.Max(450,126+groups.Length*108), 80, () =>
                { FlushPicker(); session.ResetGroups(selected); SyncPicker(); RefreshSamples(); });
            }
            Layout(); RefreshSamples();
            entry.gameObject.SetActive(true);
        }
        private static string Label(GlassGroup group) => group == GlassGroup.LampsFront ? "ПЕРЕДНИЕ" : group == GlassGroup.LampsBack ? "ЗАДНИЕ" : group == GlassGroup.Front ? "ПЕРЕД" : group == GlassGroup.Back ? "ЗАД" : group==GlassGroup.Sunroof?"ЛЮК":"СЕРЕДИНА";
        private MenuButtonGroup Button(NativeMenuFactory factory, string name, string label, float y, float height, Action action)
        {
            var button = factory.Button(Prefix + "Paint." + name, panel, label, "", action: () => { if (!PurchaseOpen) action(); });
            button._focusManger = focus; button.manualGroup = buttonGroup;
            if (!buttonGroup.buttons.Contains(button)) buttonGroup.buttons.Add(button);
            button.isStaickPos = true; Place((RectTransform)button.transform, 12, y, 388, height);
            PrepareText(button, 36); buttons.Add(button); button.gameObject.SetActive(true); return button;
        }
        private static void PrepareText(MenuButtonGroup button, int size)
        {
            var title = button.text.ui; title.transform.SetParent(button.transform, false);
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                if (graphic != title && graphic.gameObject != button.gameObject) graphic.gameObject.SetActive(false);
            title.gameObject.SetActive(true); title.enabled = true; title.fontSize = title.resizeTextMaxSize = size; title.resizeTextMinSize = 18;
            title.resizeTextForBestFit = true; title.verticalOverflow = VerticalWrapMode.Truncate;
            title.alignment = TextAnchor.MiddleLeft; title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.raycastTarget = false; Fit(title.rectTransform, .13f,.1f,.92f,.9f);
        }
        private void OpenPicker()
        {
            if (PurchaseOpen || !session.CanEdit || Available == 0 || !colors.gameObject.activeInHierarchy) return;
            if (IsOpen) return;
            garage.GetComponent<GarageTuningPanel>()?.CloseGlassEditors(this);
            ClosePicker(); session.State.Scan(); selected = First(Available);
            garage.GetComponent<GarageTuningPanel>()?.FlushBodyPaint();
            if (garage.bodyColor.Created)
            { garage.bodyColor.Created.gameObject.SetActive(false); }
            garage.bodyColor.picker.gameObject.SetActive(false);
            picker = Object.Instantiate(garage.bodyColor.picker, GameUI.me.rectUI);
            picker.name = Prefix + "ColorPicker"; picker.set_text = ECTLocalization.T(lamps ? "ПОКРАСКА ФАР" : "ПОКРАСКА СТЕКОЛ");
            picker.gameObject.SetActive(true);
            picker.SetMettalicEdit(true);
            var glossRect = (RectTransform)picker.gloss.transform;
            var opacityRect = (RectTransform)picker.opacity.transform;
            NativeMenuFactory.CopyRect(glossRect, opacityRect);
            float step = picker.gloss.panel.rectTransform.rect.height + 6;
            if (step < 20) step = 86;
            glossRect.anchoredPosition -= new Vector2(0, step);
            ((RectTransform)picker.mettalic.transform).anchoredPosition -= new Vector2(0, step);
            picker.opacity.gameObject.SetActive(true);
            ColorPickerHex.Attach(picker).SetExtraHeight(step);
            if (picker.opacity.text)
            { picker.opacity.text.enabled = false; picker.opacity.text.isInit = true; picker.opacity.text.ui.text = ECTLocalization.T("ПРОЗРАЧНОСТЬ"); }
            SyncPicker(); picker.eventUp = FlushPicker;
            root.gameObject.SetActive(true); Layout(); RefreshRows();
        }
        private int First(int mask)
        { for (int i = 0; i < groups.Length; i++) if ((mask & (1 << (int)groups[i])) != 0) return 1 << (int)groups[i]; return 0; }
        private GlassGroupPaint SelectionPaint()
        { for (int i = 0; i < groups.Length; i++) if ((selected & (1 << (int)groups[i])) != 0) return session.State.Effective(groups[i]); return session.State.Effective(groups[0]); }
        private void Select(int mask)
        {
            FlushPicker();
            if (!multiple) selected = mask; else if ((selected ^ mask) != 0) selected ^= mask;
            SyncPicker(); RefreshRows();
        }
        private void SyncPicker()
        {
            var setting = SelectionPaint(); lastColor = setting.Tint;
            lastGloss = setting.Finish?.Gloss ?? 1; lastMetallic = setting.Finish?.Metallic ?? 0;
            if (!picker) return;
            syncing = true;
            try { picker.SetColor(lastColor); picker.gloss.SetCur(lastGloss,false); picker.mettalic.SetCur(lastMetallic,false); }
            finally { syncing = false; }
        }
        internal void FlushPicker()
        {
            if (!picker || syncing) return;
            var color = picker.GetColor(); float gloss = picker.gloss.GetCur(), metallic = picker.mettalic.GetCur();
            bool finishChanged = Mathf.Abs(gloss-lastGloss) > .001f || Mathf.Abs(metallic-lastMetallic) > .001f;
            if (!CarMaterial.isColorChanget(color,lastColor) && !finishChanged) return;
            if (!session.CanEdit) { SyncPicker(); return; }
            session.Paint(selected,color,new GlassFinish(gloss,metallic),finishChanged);
            lastColor = color; lastGloss = gloss; lastMetallic = metallic;
        }
        private static Color Swatch(GlassGroupPaint paint) => new Color(paint.Tint.r,paint.Tint.g,paint.Tint.b,1);
        private void RefreshSamples()
        {
            int visible = 0, available = Available;
            float side = Mathf.Min(56,(sampleRoot.rect.width-16)/3);
            if (side <= 0) return;
            for (int i = 0; i < samples.Count; i++)
            {
                bool show = (available & (1 << (int)groups[i])) != 0; var sample = samples[i]; sample.gameObject.SetActive(show);
                if (!show) continue;
                sample.color = Swatch(session.State.Effective(groups[i]));
                Place(sample.rectTransform,visible++*(side+8),0,side,side);
            }
        }
        private void RefreshRows()
        {
            int available = Available, valid = selected & available;
            if (valid == 0) valid = First(available);
            if (valid != selected) { selected = valid; SyncPicker(); }
            many.text.ui.text = ECTLocalization.T("ВЫБРАТЬ НЕСКОЛЬКО"); manyCheck.text = multiple ? "☑" : "☐";
            int visible = 0;
            foreach (var row in rows)
            {
                bool show = (available & row.Mask) != 0; row.Button.gameObject.SetActive(show); if (!show) continue;
                Place((RectTransform)row.Button.transform,12,106+visible++*108,388,100);
                row.Button.nowButtonDissabe = !session.CanEdit; row.Marker.gameObject.SetActive((selected & row.Mask) != 0);
                for (int i=0; i<groups.Length; i++) if (row.Mask == (1 << (int)groups[i])) row.Swatch.color = Swatch(session.State.Effective(groups[i]));
            }
            foreach (var button in buttons) button.nowButtonDissabe = !session.CanEdit;
        }
        internal void Refresh()
        {
            if (!root || !garage || !session.State) return;
            if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime+1; session.State.Scan(); }
            Layout(); RefreshSamples();
            entry.gameObject.SetActive(Available != 0);
            entry.nowButtonDissabe = !session.CanEdit || Available == 0;
            if (!picker) { root.gameObject.SetActive(false); return; }
            FlushPicker();
            bool bodyPickerOpen = garage.bodyColor.Created && garage.bodyColor.Created.gameObject.activeInHierarchy;
            if (!garage.gameObject.activeInHierarchy || !colors.gameObject.activeSelf || !IsOpen || PurchaseOpen || bodyPickerOpen)
            { ClosePicker(); return; }
            root.gameObject.SetActive(true); RefreshRows();
        }
        internal void SyncImported() { ClosePicker(false); session.State.Scan(); selected = First(Available); RefreshSamples(); }
        internal void CloseEditor() { ClosePicker(); }
        private void Layout()
        {
            if (root && GameUI.me) CanvasAspectLayout.Apply(root,overlayParent,GameUI.me.rectUI,corners);
            if (!entry || !garage.bodyColor) return;
            var body = preceding && preceding.gameObject.activeSelf ? preceding : (RectTransform)garage.bodyColor.transform;
            var rect = (RectTransform)entry.transform; NativeMenuFactory.CopyRect(body,rect);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x,176);
            rect.anchoredPosition = body.anchoredPosition-new Vector2(0,body.rect.height+12);
        }
        internal bool ContainsPointer() => IsOpen && (panel.isMouseIntersect(Input.mousePosition) ||
            picker.panel.rectTransform.isMouseIntersect(Input.mousePosition)) || entry && entry.gameObject.activeInHierarchy && Entry.isMouseIntersect(Input.mousePosition);
        private void ClosePicker(bool apply = true)
        {
            if (picker)
            {
                if (apply) FlushPicker();
                picker.eventUp = null;
                if (garage && garage.gameObject.activeInHierarchy && picker.top_panel && picker.top_panel.cur == picker)
                    picker.top_panel.BackPressed();
                picker.gameObject.SetActive(false); Object.Destroy(picker.gameObject);
            }
            picker = null; if (root) root.gameObject.SetActive(false);
        }
        private static RectTransform Box(string name,Transform parent,float x,float y,float width,float height)
        { var rect = NativeMenuFactory.Rect(name,parent,width,height); Place(rect,x,y,width,height); return rect; }
        private static void Place(RectTransform rect,float x,float y,float width,float height)
        { rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity; rect.sizeDelta=new Vector2(width,height); NativeMenuFactory.Place(rect,x,y); }
        private static void Fit(RectTransform rect,float x1,float y1,float x2,float y2)
        { rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity; rect.pivot=new Vector2(.5f,.5f);
          rect.anchorMin=new Vector2(x1,y1); rect.anchorMax=new Vector2(x2,y2); rect.offsetMin=rect.offsetMax=Vector2.zero; }
        public void Dispose()
        {
            ClosePicker(); NativeMenuFactory.RemoveButton(entry); entry=null;
            foreach (var button in buttons) NativeMenuFactory.RemoveButton(button);
            buttons.Clear(); rows.Clear(); samples.Clear();
            if (root) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); }
        }
    }
}
