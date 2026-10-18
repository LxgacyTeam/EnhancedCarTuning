using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Globalization;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.UI
{
    internal sealed class NativeMenuFactory : IDisposable
    {
        private sealed class LocalizedLabel { internal string Source; }
        private static readonly ConditionalWeakTable<Text, LocalizedLabel> localized = new ConditionalWeakTable<Text, LocalizedLabel>();
        private readonly GameObject staging;
        private readonly MenuButtonGroup template;
        private readonly Sprite[] sprites;
        private readonly Image coinIcon, diamondIcon;

        internal NativeMenuFactory(CarGarageUI garage, MenuButtonGroup template)
        {
            this.template = template;
            sprites = UnityEngine.Resources.FindObjectsOfTypeAll<Sprite>();
            coinIcon = CurrencyIcons.Find(true);
            diamondIcon = CurrencyIcons.Find(false);
            staging = new GameObject("ECT.Build", typeof(RectTransform));
            staging.SetActive(false);
            staging.transform.SetParent(garage.transform, false);
        }

        internal Sprite Sprite(string name) => sprites.FirstOrDefault(s => s && s.name == name);

        internal MenuButtonGroup Button(string name, Transform parent, string label, string icon, RectTransform content = null, Action action = null)
        {
            // Awake must not see the copied Wheels_menu reference: configure an inactive clone first.
            var go = Object.Instantiate(template.gameObject, staging.transform, false);
            go.name = name;
            go.SetActive(false);
            var button = go.GetComponent<MenuButtonGroup>();
            button.content = content;
            button.selectAfterPress = null;
            button.manualGroup = null;
            button.SwithEvent = new UnityEvent();
            if (action != null) button.SwithEvent.AddListener(() => action());
            button.isHideWhenPress = false;
            button.isInitParent = false;
            button._focusManger = null;
            button.maskArea = null;
            button.customIntersectionRect = null;
            button.isSkipSaves = true;
            button.styles.Clear();
            button.enabled = true;
            foreach (var events in go.GetComponentsInChildren<MenuButtonSomeEvents>(true)) events.whenBecomeActive_ActiveGO = null;
            foreach (var localize in go.GetComponentsInChildren<Localize>(true))
            { localize.isInit = true; localize.enabled = false; }
            SetLabel(button, label);
            SetIcon(button, Sprite(icon));
            go.transform.SetParent(parent, false);
            button.InitParent();
            return button;
        }

        internal static void SetLabel(MenuButtonGroup button, string value)
        {
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                SetLabel(text, value);
        }
        internal static void SetLabel(Text text, string value)
        {
            localized.GetValue(text, _ => new LocalizedLabel()).Source = value;
            text.text = ECTLocalization.T(value);
            var replacementFont = EmbeddedAssets.UiFont("BebasNeue");
            if (replacementFont) text.font = replacementFont;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = Mathf.Max(18, text.fontSize);
        }

        internal static void RefreshLocalizedLabels(Transform scope)
        {
            if (!scope) return;
            foreach (var text in scope.GetComponentsInChildren<Text>(true))
            {
                LocalizedLabel entry;
                if (text && localized.TryGetValue(text, out entry) && entry != null)
                {
                    string value = ECTLocalization.T(entry.Source);
                    if (text.text != value) text.text = value;
                }
            }
        }

        internal static void SetIcon(MenuButtonGroup button, Sprite sprite)
        {
            var icon = FindIcon(button);
            if (!icon) return;
            icon.gameObject.SetActive(sprite);
            if (sprite) { icon.sprite = sprite; icon.type = Image.Type.Simple; icon.preserveAspect = true; }
        }

        internal static Image FindIcon(MenuButtonGroup button)
        {
            // After a custom layout exists, update its visible icon rather than the
            // hidden sprite left behind by the cloned vanilla MenuButtonGroup.
            var customIcon = button.transform.Find("ECT.Visual/ECT.Icon");
            if (customIcon) return customIcon.GetComponent<Image>();
            var icon = button.GetComponentsInChildren<Image>(true).FirstOrDefault(i =>
                i.gameObject != button.gameObject && i.sprite &&
                (i.sprite.name.IndexOf("wheel", StringComparison.OrdinalIgnoreCase) >= 0 || i.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0));
            if (!icon) icon = button.GetComponentsInChildren<Image>(true).FirstOrDefault(i =>
                i.gameObject != button.gameObject && i.sprite && i.type == Image.Type.Simple);
            return icon;
        }

        // The prefab's text and icon transforms are controlled by the game's native
        // layout and do not follow a shorter list row reliably. Keep its MenuButtonGroup
        // (hit-testing, focus, selection and background) but render our labels/icons
        // in an independent, non-raycasting overlay with known local coordinates.
        internal void LayoutChoice(MenuButtonGroup button, string name, TuningPrice price)
        {
            var nativeText = button.text ? button.text.ui : button.GetComponentInChildren<Text>(true);
            if (!nativeText) throw new InvalidOperationException("Native button has no Text component.");
            Sprite nativeSprite = GetNativeIconSprite(button);
            var visuals = MakeVisuals(button);

            var title = NewText(visuals, "ECT.Title", nativeText, name, 26, 21);
            LayoutText(title, 27f, 6f, 74f, 34f);

            var amount = NewText(visuals, "ECT.Amount", nativeText,
                price.Amount.ToString("N0", CultureInfo.GetCultureInfo("ru-RU")), 22, 19);
            amount.color = new Color(0.88f, 0.89f, 0.90f, 1f);
            LayoutText(amount, 54f, 42f, 60f, 27f);

            // Keep the engine/drive sprite in the first row, to the left of the
            // boundary and without covering either the title or price.
            AddIcon(visuals, nativeSprite, 32f, 18f, 34f, 34f);

            var currencySource = price.Coins ? coinIcon : diamondIcon;
            Sprite currencySprite = currencySource ? currencySource.overrideSprite : null;
            if (currencySprite)
            {
                var currency = AddImage(visuals, "ECT.Currency", currencySprite);
                currency.color = currencySource.color;
                LayoutFromTopLeft(currency.rectTransform, 27f, 48f, 21f, 21f);
            }
            else
            {
                amount.text = price.Label;
                LayoutText(amount, 27f, 42f, 80f, 27f);
                Plugin.Warn("Currency icon not found: " + (price.Coins ? "Coins" : "Almaz"));
            }
        }

        internal static Text LayoutCategoryHeader(MenuButtonGroup button, string caption, bool wideIcon = false)
        {
            var nativeText = button.text ? button.text.ui : button.GetComponentInChildren<Text>(true);
            if (!nativeText) return null;
            Sprite nativeSprite = GetNativeIconSprite(button);
            var visuals = MakeVisuals(button);

            // Readable category title with a real inset from the left edge.
            // Leave a dedicated slot at the right for the large drive/engine icon.
            var title = NewText(visuals, "ECT.Title", nativeText, caption, 39, 30);
            LayoutText(title, 27f, 10f, 87f, 49f);

            // Full-width second line, below the icon: long engine names must
            // remain readable without colliding with the right-hand artwork.
            var selected = NewText(visuals, "ECT.SelectedValue", nativeText, string.Empty, 31, 24);
            LayoutText(selected, 27f, 72f, 17f, 34f);
            selected.horizontalOverflow = HorizontalWrapMode.Wrap;

            // The drivetrain sprite is a wide car silhouette. In a 66x62 box its
            // visible content is tiny because Image.preserveAspect keeps the wide
            // source aspect ratio. Give it a genuinely wider slot, with padding.
            // Engine icons remain square and keep their existing dimensions.
            if (wideIcon)
                AddIcon(visuals, nativeSprite, 10f, 7f, 118f, 88f);
            else
                AddIcon(visuals, nativeSprite, 18f, 8f, 66f, 62f);
            return selected;
        }

        private static Sprite GetNativeIconSprite(MenuButtonGroup button)
        {
            var icon = FindIcon(button);
            return icon ? icon.overrideSprite : null;
        }

        private static RectTransform MakeVisuals(MenuButtonGroup button)
        {
            // Hide the native children only, never the root background or
            // MenuButtonGroup. They are still referenced by the game's Localize
            // and focus components, so do not Destroy them.
            foreach (var text in button.GetComponentsInChildren<Text>(true))
                text.enabled = false;
            var originalIcon = FindIcon(button);
            if (originalIcon) originalIcon.enabled = false;

            var root = Rect("ECT.Visual", button.transform, 0f, 0f);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.SetAsLastSibling();
            return root;
        }

        private static Text NewText(RectTransform parent, string id, Text source,
            string content, int fontSize, int minFontSize)
        {
            var rect = Rect(id, parent, 0f, 0f);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = source.font;
            text.fontStyle = source.fontStyle;
            text.color = source.color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minFontSize;
            text.resizeTextMaxSize = fontSize;
            text.lineSpacing = 1f;
            text.raycastTarget = false;
            text.text = ECTLocalization.T(content);
            // Empty content marks a runtime-controlled field (selected engine/drive).
            // Do not register it as a localized label: RefreshLocalizedLabels would
            // otherwise overwrite the selected value with an empty string each tick.
            if (!string.IsNullOrEmpty(content))
                localized.GetValue(text, _ => new LocalizedLabel()).Source = content;
            return text;
        }

        private static Image AddImage(RectTransform root, string name, Sprite sprite)
        {
            var rect = Rect(name, root, 0f, 0f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void AddIcon(RectTransform root, Sprite sprite,
            float right, float top, float width, float height)
        {
            if (!sprite) return;
            var icon = AddImage(root, "ECT.Icon", sprite);
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void LayoutFromTopLeft(RectTransform rect,
            float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void LayoutText(Text text,
            float left, float top, float right, float height)
        {
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static RectTransform Rect(string name, Transform parent, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            Place(rect, 0, 0);
            return rect;
        }

        internal static void Place(RectTransform rect, float x, float y)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); }

        internal static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax;
            target.pivot = source.pivot; target.sizeDelta = source.sizeDelta;
            target.anchoredPosition3D = source.anchoredPosition3D;
            target.localRotation = source.localRotation; target.localScale = source.localScale;
        }

        internal static void RemoveButton(MenuButtonGroup button)
        {
            if (!button) return;
            var group = button.GetComponentInParent<MenuGroup>();
            if (group) group.buttons.Remove(button);
            var focus = button.GetComponentInParent<MenuFocusManager>();
            if (focus && focus.curActive == button) focus.ResetActive();
            button.gameObject.SetActive(false);
            Object.Destroy(button.gameObject);
        }

        public void Dispose() { if (staging) Object.Destroy(staging); }
    }
}
