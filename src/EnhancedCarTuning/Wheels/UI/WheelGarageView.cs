using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.UI;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.Wheels.Suspension;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Wheels.UI
{
    internal sealed class WheelGarageView : IDisposable
    {
        private readonly CarGarageUI garage;
        private readonly GarageSession session;
        private readonly List<MenuButtonGroup> buttons = new List<MenuButtonGroup>();
        private readonly List<WheelSlider> sliders = new List<WheelSlider>();
        private readonly List<GameObject> hidden = new List<GameObject>();
        private RectTransform root, menu, popup, colorBackup;
        private Transform colorParent;
        private int colorIndex;
        private bool colorStatic;
        private MenuButtonGroup axleButton, reset;
        private AxleTarget selected = AxleTarget.Both;
        private Font font;
        private readonly Vector3[] corners = new Vector3[4];
        private Sprite[] axleIcons;

        internal WheelGarageView(CarGarageUI garage, GarageSession session) { this.garage = garage; this.session = session; }
        internal void Build()
        {
            menu = garage.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Wheels_menu");
            if (!menu || !GameUI.me) throw new InvalidOperationException("Native Wheels_menu / GameUI missing.");
            var template = garage.GetComponentsInChildren<MenuButtonGroup>(true).FirstOrDefault(b => b.content == menu);
            var source = menu.GetComponentsInChildren<MenuSliderOver>(true).Select(s => s.panelSlider).FirstOrDefault(s => s);
            if (!source) source = UnityEngine.Resources.FindObjectsOfTypeAll<MenuSliderOver>().Select(s => s.panelSlider).FirstOrDefault(s => s);
            if (!template || !source) throw new InvalidOperationException("Native wheel button / MenuSliderPanel template missing.");
            font = EmbeddedAssets.UiFont("BebasNeue") ?? template.text.ui.font;
            root = NativeMenuFactory.Rect("ECT.Wheels", menu, 1920, 1080);
            root.gameObject.SetActive(false);
            var group = root.gameObject.AddComponent<MenuGroup>();
            group.alighContent = MenuGroup.AlignType.None; group.disable_manualy = new MenuGroup[0];
            Layout();
            var right = Box("Suspension", root, 1632, 112, 288, 968);
            var nativePanel = menu.Find("Panel (1)") as RectTransform;
            if (nativePanel)
            {
                var clone = Object.Instantiate(nativePanel.gameObject, right, false);
                clone.name = "ECT.NativeSuspensionPanel";
                Fit((RectTransform)clone.transform, 0, 0, 1, 1);
                clone.transform.SetAsFirstSibling();
                foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    if (!(behaviour is Graphic) && !(behaviour is BlurBehindRequared)) behaviour.enabled = false;
                clone.SetActive(true);
            }
            else
            {
                Plugin.Warn("Wheels_menu/Panel (1) missing; using native blur fallback.");
                NativePanelBackground.Attach(right, garage);
            }
            TextLabel("ПОДВЕСКА", root, 1642, 132, 268, 52);

            using (var factory = new NativeMenuFactory(garage, template))
            {
                axleIcons = new[] { factory.Sprite("wheels_all"), factory.Sprite("wheels_front"), factory.Sprite("wheels_back") };
                popup = Box("AxleSelect", root, 320, 140, 300, 490);
                popup.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.11f, 0.78f);
                axleButton = Button(factory, "Axle", root, "ОБЕ ОСИ", "wheels_all", 6, 146, 262, 130,
                    () => popup.gameObject.SetActive(!popup.gameObject.activeSelf));
                for (int i = 0; i < 3; i++)
                {
                    var choice = (AxleTarget)i;
                    Button(factory, "Axle." + choice, popup, AxleLabel(choice), i == 0 ? "wheels_all" : i == 1 ? "wheels_front" : "wheels_back",
                        10, 10 + i * 160, 280, 150, () => { selected = choice; popup.gameObject.SetActive(false); Refresh(); });
                }
                popup.gameObject.SetActive(false);
                reset = Button(factory, "ResetAxle", root, "СБРОС ОСИ", "", 1642, 972, 268, 92,
                    () => { session.ResetWheels(selected); Refresh(); });
                // Reset is text-only; the axle controls use a large car silhouette above the label.
                Fit(reset.text.ui.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);
            }

            AddSlider(source, WheelParameter.Radius, "РАДИУС", " см", 6, 610, 262, 97);
            AddSlider(source, WheelParameter.Width, "ШИРИНА", " см", 6, 718, 262, 97);
            AddSlider(source, WheelParameter.OffsetX, "СМЕЩЕНИЕ X", " см", 1642, 190, 268, 98);
            AddSlider(source, WheelParameter.OffsetY, "СМЕЩЕНИЕ Y", " см", 1642, 293, 268, 96);
            AddSlider(source, WheelParameter.Travel, "ХОД ПОДВЕСКИ", " см", 1642, 395, 268, 94);
            AddSlider(source, WheelParameter.Stiffness, "ЖЁСТКОСТЬ", "", 1642, 495, 268, 97);
            AddSlider(source, WheelParameter.Camber, "РАЗВАЛ", "°", 1642, 597, 268, 97);

            // Preserve the working stock color picker and its material logic.
            if (garage.wheelColor)
            {
                var rect = (RectTransform)garage.wheelColor.transform;
                colorParent = rect.parent; colorIndex = rect.GetSiblingIndex(); colorStatic = garage.wheelColor.isStaickPos;
                colorBackup = NativeMenuFactory.Rect("ECT.ColorTransformBackup", menu, 0, 0);
                NativeMenuFactory.CopyRect(rect, colorBackup); colorBackup.gameObject.SetActive(false);
                rect.SetParent(root, false); Place(rect, 6, 462, 262, 135); garage.wheelColor.isStaickPos = true;
            }
            if (garage.wheelType) Hide(garage.wheelType.gameObject);
            foreach (var slider in menu.GetComponentsInChildren<MenuSliderOver>(true)) Hide(slider.gameObject);
            // Popup above other controls, so its hit area cannot overlap a slider behind it.
            popup.SetAsLastSibling();
            root.gameObject.SetActive(true); Refresh();
        }

        private void Hide(GameObject go) { if (go.activeSelf) { hidden.Add(go); go.SetActive(false); } }
        private MenuButtonGroup Button(NativeMenuFactory factory, string name, Transform parent, string label, string icon,
            float x, float y, float width, float height, Action action)
        {
            var button = factory.Button("ECT." + name, parent, label, icon, action: () => { if (!MenuBuy.me) action(); });
            buttons.Add(button); button.isStaickPos = true;
            Place((RectTransform)button.transform, x, y, width, height);
            foreach (var text in button.GetComponentsInChildren<Text>(true)) if (text != button.text.ui) text.enabled = false;
            var title = button.text.ui; title.transform.SetParent(button.transform, false);
            Fit(title.rectTransform, 0.04f, 0.02f, 0.96f, 0.32f);
            title.fontSize = title.resizeTextMaxSize = 36; title.alignment = TextAnchor.MiddleCenter;
            foreach (var image in button.GetComponentsInChildren<Image>(true))
                if (image.gameObject != button.gameObject && image.sprite && image.sprite.name.StartsWith("wheels_", StringComparison.Ordinal))
                { image.transform.SetParent(button.transform, false); Fit(image.rectTransform, 0.08f, 0.33f, 0.92f, 0.96f); image.preserveAspect = true; }
            button.gameObject.SetActive(true); return button;
        }

        private void AddSlider(MenuSliderPanel source, WheelParameter parameter, string label, string unit, float x, float y, float w, float h)
        {
            var frame = Box(parameter.ToString(), root, x, y, w, h);
            var clone = Object.Instantiate(source, frame, false);
            clone.name = "MenuSliderPanel"; Fit((RectTransform)clone.transform, 0, 0, 1, 1);
            var slider = clone.gameObject.AddComponent<WheelSlider>();
            slider.Initialize(clone, session, () => selected, parameter, label, unit, font);
            clone.gameObject.SetActive(true); sliders.Add(slider);
        }

        internal void Layout()
        {
            if (!root || !menu || !GameUI.me) return;
            CanvasAspectLayout.Apply(root, menu, GameUI.me.rectUI, corners);
        }
        internal void Refresh()
        {
            if (!root) return;
            Layout();
            NativeMenuFactory.SetLabel(axleButton, AxleLabel(selected));
            // Keep labels in sync when the game language changes during the garage session.
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                if (text && text.gameObject.name == "ПОДВЕСКА") text.text = ECTLocalization.T("ПОДВЕСКА");
            NativeMenuFactory.SetIcon(axleButton, axleIcons[(int)selected]);
            reset.nowButtonDissabe = !session.CanEdit;
            foreach (var slider in sliders) if (slider) slider.Refresh();
        }
        internal bool IsCameraInputArea(Vector2 position)
        {
            if (!root || !root.gameObject.activeInHierarchy) return false;
            var left = menu ? menu.Find("Panel (1)") as RectTransform : null;
            var right = root.Find("Suspension") as RectTransform;
            return (left && left.gameObject.activeInHierarchy && left.isMouseIntersect(position)) ||
                   (right && right.gameObject.activeInHierarchy && right.isMouseIntersect(position)) ||
                   (popup && popup.gameObject.activeInHierarchy && popup.isMouseIntersect(position)) ||
                   sliders.Any(s => s && s.gameObject.activeInHierarchy && ((RectTransform)s.transform).isMouseIntersect(position)) ||
                   buttons.Any(b => b && b.gameObject.activeInHierarchy && ((RectTransform)b.transform).isMouseIntersect(position));
        }

        internal bool ContainsPointer()
        {
            if (!root || !root.gameObject.activeInHierarchy) return false;
            return sliders.Any(s => s && ((RectTransform)s.transform).isMouseIntersect(Input.mousePosition)) ||
                buttons.Any(b => b && b.gameObject.activeInHierarchy && ((RectTransform)b.transform).isMouseIntersect(Input.mousePosition));
        }
        private static string AxleLabel(AxleTarget axle) => axle == AxleTarget.Both ? "ОБЕ ОСИ" : axle == AxleTarget.Front ? "ПЕРЕДНЯЯ ОСЬ" : "ЗАДНЯЯ ОСЬ";
        internal static void Fit(RectTransform rect, float x1, float y1, float x2, float y2)
        {
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchorMin = new Vector2(x1, y1); rect.anchorMax = new Vector2(x2, y2);
            rect.pivot = new Vector2(0.5f, 0.5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        { rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.sizeDelta = new Vector2(w, h); NativeMenuFactory.Place(rect, x, y); }
        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        { var rect = NativeMenuFactory.Rect(name, parent, w, h); Place(rect, x, y, w, h); return rect; }
        private void TextLabel(string label, Transform parent, float x, float y, float w, float h)
        {
            var text = Box(label, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.text = ECTLocalization.T(label); text.font = font; text.fontSize = 38; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.88f, 0.9f, 0.91f); text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = 38;
        }
        public void Dispose()
        {
            if (garage && garage.wheelColor && colorBackup && colorParent)
            {
                var rect = (RectTransform)garage.wheelColor.transform;
                rect.SetParent(colorParent, false); rect.SetSiblingIndex(colorIndex);
                NativeMenuFactory.CopyRect(colorBackup, rect); garage.wheelColor.isStaickPos = colorStatic;
            }
            foreach (var go in hidden) if (go) go.SetActive(true);
            foreach (var button in buttons) NativeMenuFactory.RemoveButton(button);
            if (colorBackup) Object.Destroy(colorBackup.gameObject);
            if (root) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); }
            hidden.Clear(); buttons.Clear(); sliders.Clear();
        }
    }
}
