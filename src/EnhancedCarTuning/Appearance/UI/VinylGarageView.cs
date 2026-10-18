using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Appearance.Glass;
using EnhancedCarTuning.UI;
using EnhancedCarTuning.Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class VinylGarageView : IDisposable
    {
        private readonly CarGarageUI garage;
        private readonly List<MenuButtonGroup> buttons = new List<MenuButtonGroup>();
        private readonly List<Backup> originals = new List<Backup>();
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform menu, root, textPanel, nativeImg, nativeImgBackup;
        private Transform nativeImgParent;
        private int nativeImgIndex;
        private Image previewImage;
        private RawImage previewRaw;
        private bool manualNativeImgClick;
        private bool nativeImgWasActive;
        private MenuButtonGroup symmetry, bold, italic, font, textSurface;
        private VinylStyleCheck boldCheck, italicCheck;
        private InputField input;
        private GameObject ownEventSystem;
        private VinylInputModule ownInputModule;
        private RectTransform addRect;
        private readonly List<VisualBackup> visuals = new List<VisualBackup>();
        private readonly List<NativeImageGraphicBackup> nativeImageGraphics = new List<NativeImageGraphicBackup>();
        private readonly List<NativeImageLocalizeBackup> nativeImageLabels = new List<NativeImageLocalizeBackup>();
        private CarPaintItems.Item selected;
        private string selectedName;
        private bool syncing;
        private int lastInputFrame = -10;
        private float nextText, nextInputSearch;
        private TextVinylData pending;
        private int fontIndex, style;
        private readonly HashSet<GameObject> hidden = new HashSet<GameObject>();
        internal VinylGarageView(CarGarageUI garage) { this.garage = garage; }
        internal void Build()
        {
            menu = garage.root.Find("PaintVinils") as RectTransform;
            var add = menu ? menu.Find("left_panel/Add") : null;
            var align = menu ? menu.Find("left_panel/Align") : null;
            nativeImg = menu ? menu.Find("left_panel/Img") as RectTransform : null;
            var template = garage.root.Find("Main/Paint").GetComponent<MenuButtonGroup>();
            if (!menu || !add || !align || !nativeImg || !template || !garage.paintColor)
                throw new InvalidOperationException("Native vinyl controls not found.");
            root = NativeMenuFactory.Rect("ECT.VinylControls", menu, 1920, 1080);
            root.gameObject.SetActive(false);
            var group = root.gameObject.AddComponent<MenuGroup>(); group.alighContent = MenuGroup.AlignType.None;
            group.disable_manualy = new MenuGroup[0];
            addRect = (RectTransform)add;
            Move(garage.paintColor, 4, 334, 126, 132);
            Move(align.GetComponent<MenuButton>(), 146, 334, 122, 132);
            SaveVisuals(garage.paintColor.transform);
            SaveVisuals(align);
            garage.paintColor.text.text = ECTLocalization.T("ЦВЕТ");
            garage.paintColor.text.alignment = TextAnchor.MiddleCenter;
            garage.paintColor.text.transform.SetParent(garage.paintColor.transform, false);
            garage.paintColor.Result.transform.SetParent(garage.paintColor.transform, false);
            Fit(garage.paintColor.text.rectTransform, .02f, .62f, .98f, .96f);
            garage.paintColor.text.resizeTextForBestFit = true; garage.paintColor.text.resizeTextMaxSize = 32;
            Fit(garage.paintColor.Result.rectTransform, .26f, .12f, .74f, .53f);
            var alignText = align.GetComponentInChildren<Text>(true);
            if (alignText)
            { alignText.transform.SetParent(align, false); Fit(alignText.rectTransform, .02f, .64f, .98f, .96f);
              alignText.resizeTextForBestFit = true; alignText.resizeTextMaxSize = 30;
              alignText.alignment = TextAnchor.MiddleCenter; alignText.horizontalOverflow = HorizontalWrapMode.Wrap; }
            var alignIcon = align.Find("Result") as RectTransform;
            if (alignIcon) { alignIcon.SetParent(align, false); Fit(alignIcon, .23f, .12f, .77f, .56f); }
            using (var factory = new NativeMenuFactory(garage, template))
            {
                symmetry = Button(factory, "Symmetry", "СИММЕТРИЯ", 4, 496, 126, 142, ToggleSymmetry);
                var source = menu.Find("old_not_used/Symetry/Icon");
                if (source) CopyGraphic(source.GetComponent<Graphic>(), symmetry.transform);
                // Use the original left_panel/Img as the fourth control; the separate Mode popup is obsolete.
                nativeImgParent = nativeImg.parent;
                nativeImgWasActive = nativeImg.gameObject.activeSelf;
                nativeImgIndex = nativeImg.GetSiblingIndex();
                nativeImgBackup = NativeMenuFactory.Rect("ECT.NativeImgBackup", nativeImgParent, 0, 0);
                NativeMenuFactory.CopyRect(nativeImg, nativeImgBackup);
                nativeImgBackup.gameObject.SetActive(false);
                var nativeButton = nativeImg.GetComponent<MenuButton>();
                if (nativeButton != null)
                    Move(nativeButton, 146, 496, 122, 142);
                else
                {
                    nativeImg.SetParent(root, false);
                    Place(nativeImg, 146, 496, 122, 142);
                    // No EventSystem is required: the parent GarageTuningPanel samples mouse input.
                    manualNativeImgClick = true;
                }
                nativeImg.gameObject.SetActive(true);
                // The stock Img is a whole button: hide its original arrow and label,
                // not just overlay a new thumbnail on top of them.
                HideNativeImgDecorations();
                var iconRect = NativeMenuFactory.Rect("ECT.SelectedVinylIcon", nativeImg, 0, 0);
                Fit(iconRect, .16f, .15f, .84f, .85f);
                previewImage = iconRect.gameObject.AddComponent<Image>();
                previewImage.preserveAspect = true;
                previewImage.raycastTarget = false;
                var rawRect = NativeMenuFactory.Rect("ECT.SelectedVinylTexture", nativeImg, 0, 0);
                Fit(rawRect, .16f, .15f, .84f, .85f);
                previewRaw = rawRect.gameObject.AddComponent<RawImage>();
                previewRaw.raycastTarget = false;
                previewRaw.gameObject.SetActive(false);
                textPanel = NativeMenuFactory.Rect("ECT.TextVinylEditor", root, 266, 144);
                NativeMenuFactory.Place(textPanel, 4, 656);
                var editorBackground = textPanel.gameObject.AddComponent<Image>();
                editorBackground.color = new Color(.20f, .25f, .27f, .88f);
                editorBackground.raycastTarget = false;
                bold = Button(factory, "Bold", "B", 4, 746, 66, 72, () => SetStyle(style ^ 1));
                italic = Button(factory, "Italic", "B", 78, 746, 66, 72, () => SetStyle(style ^ 2));
                font = Button(factory, "Font", TextVinylData.Fonts[0], 152, 746, 116, 72, () =>
                { fontIndex = (fontIndex + 1) % TextVinylData.Fonts.Length; QueueText(); });
                bold.transform.SetParent(textPanel, false);
                italic.transform.SetParent(textPanel, false);
                font.transform.SetParent(textPanel, false);
                textSurface = Button(factory, "TextSurface", "НАНЕСЕНИЕ: КУЗОВ", 4, 826, 250, 54, ToggleTextSurface);
                textSurface.transform.SetParent(textPanel, false);
                textSurface.text.ui.resizeTextForBestFit = true;
                textSurface.text.ui.resizeTextMinSize = 16;
                textSurface.text.ui.resizeTextMaxSize = 27;
                bold.text.ui.fontStyle = FontStyle.Bold;
                italic.text.ui.fontStyle = FontStyle.BoldAndItalic;
                boldCheck = StyleCheck(bold);
                italicCheck = StyleCheck(italic);
                var yellow = new Color(.79f, .69f, .24f, 1f);
                var ink = new Color(.12f, .15f, .16f, 1f);
                font.style = ButtonStyle(font.gameObject, yellow);
                font.style.styleForText = ButtonStyle(font.gameObject, ink);
                font.style.Init();
                font.panelColor = yellow; font.textColor = ink;
                font.SetContentActive(false);
                font.text.ui.resizeTextForBestFit = true;
                font.text.ui.resizeTextMinSize = 16;
                font.text.ui.resizeTextMaxSize = 27;
            }
            foreach (var rect in menu.GetComponentsInChildren<RectTransform>(true))
                if (rect != root && !rect.IsChildOf(root) && rect.name == "Back" && rect.GetComponent<MenuButton>())
                { if (rect.gameObject.activeSelf) hidden.Add(rect.gameObject); rect.gameObject.SetActive(false); }
            TryBuildInput(); root.gameObject.SetActive(true); Layout(); Refresh();
        }
        private MenuButtonGroup Button(NativeMenuFactory factory, string name, string label,
            float x, float y, float w, float h, Action action)
        {
            var button = factory.Button("ECT.Vinyl." + name, root, label, "", null, action);
            button.isStaickPos = true; Place((RectTransform)button.transform, x, y, w, h);
            var text = button.text.ui;
            text.transform.SetParent(button.transform, false);
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                if (graphic != text && graphic.gameObject != button.gameObject) graphic.gameObject.SetActive(false);
            text.gameObject.SetActive(true); text.enabled = true; Fit(text.rectTransform, .05f, .08f, .95f, .92f);
            text.alignment = TextAnchor.MiddleCenter; text.fontSize = 27; text.resizeTextMaxSize = 27;
            var uiFont = EmbeddedAssets.UiFont("BebasNeue");
            if (uiFont) text.font = uiFont;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false; text.raycastTarget = false;
            button.gameObject.SetActive(true); buttons.Add(button); return button;
        }
        private void Move(MenuButton button, float x, float y, float w, float h)
        {
            if (!button) throw new InvalidOperationException("Missing native vinyl button.");
            var rect = (RectTransform)button.transform;
            var backup = NativeMenuFactory.Rect("ECT.VinylBackup", rect.parent, 0, 0);
            NativeMenuFactory.CopyRect(rect, backup); backup.gameObject.SetActive(false);
            var oldGroup = button.GetComponentInParent<MenuGroup>();
            originals.Add(new Backup { Button = button, Parent = rect.parent, Rect = backup,
                Index = rect.GetSiblingIndex(), Static = button.isStaickPos,
                Active = button.gameObject.activeSelf, Mask = button.maskArea,
                Intersection = button.customIntersectionRect, Focus = button._focusManger });
            if (oldGroup) oldGroup.buttons.Remove(button);
            rect.SetParent(root, false); Place(rect, x, y, w, h);
            button.isStaickPos = true; button.isInitParent = false; button.InitParent();
            button.customIntersectionRect = null; button.maskArea = null; button._focusManger = null;
        }
        private static VinylStyleCheck StyleCheck(MenuButtonGroup button)
        {
            var rect = NativeMenuFactory.Rect("ECT.StyleCheck", button.transform, 26, 26);
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0); rect.anchoredPosition = new Vector2(-2, 2);
            rect.SetAsLastSibling();
            var check = rect.gameObject.AddComponent<VinylStyleCheck>();
            check.color = Color.white;
            check.raycastTarget = false;
            return check;
        }
        private static MenuStyle ButtonStyle(GameObject target, Color color)
        {
            var result = target.AddComponent<MenuStyle>();
            result.normal = new MenuStyle.ColorItem { color = color };
            result.select = new MenuStyle.ColorItem { color = color };
            result.mouseOver = new MenuStyle.ColorItem { color = color };
            result.press = new MenuStyle.ColorItem { color = color };
            result.disabled = new MenuStyle.ColorItem { color = color };
            result.Init();
            return result;
        }
        private void TryBuildInput()
        {
            if (input || !textPanel || Time.unscaledTime < nextInputSearch) return;
            nextInputSearch = Time.unscaledTime + 1;
            var source = UnityEngine.Resources.FindObjectsOfTypeAll<InputField>().FirstOrDefault(f => f &&
                f.name == "Nik Name" && f.transform.parent && f.transform.parent.name == "NikTxt");
            var staging = NativeMenuFactory.Rect("ECT.InputStaging", textPanel, 0, 0);
            staging.gameObject.SetActive(false);
            GameObject go;
            if (source) go = Object.Instantiate(source.gameObject, staging, false);
            else
            {
                var rect = NativeMenuFactory.Rect("ECT.TextInput", staging, 264, 62); go = rect.gameObject;
                var background = go.AddComponent<Image>(); background.color = Color.white;
                var field = go.AddComponent<InputField>(); field.targetGraphic = background;
                var text = NativeMenuFactory.Rect("Text", rect, 0, 0).gameObject.AddComponent<Text>();
                text.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf"); text.color = new Color(.18f,.18f,.18f,1);
                text.alignment = TextAnchor.MiddleLeft; field.textComponent = text;
                var hint = NativeMenuFactory.Rect("Placeholder", rect, 0, 0).gameObject.AddComponent<Text>();
                hint.font = text.font; hint.color = new Color(.35f,.35f,.35f,.7f);
                hint.fontSize = 30; field.placeholder = hint;
                Plugin.Warn("NikTxt input template unavailable; using a native uGUI input field for text vinyls.");
            }
            go.SetActive(false);
            input = go.GetComponent<InputField>();
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (component && component != input && !(component is UnityEngine.EventSystems.UIBehaviour))
                { component.enabled = false; Object.DestroyImmediate(component); }
            foreach (var caret in go.GetComponentsInChildren<RectTransform>(true))
                if (caret.name.IndexOf("Input Caret", StringComparison.OrdinalIgnoreCase) >= 0) Object.DestroyImmediate(caret.gameObject);
            input.onValueChanged = new InputField.OnChangeEvent(); input.onEndEdit = new InputField.SubmitEvent();
            input.onValidateInput = (text, index, character) => char.IsControl(character) ? '\0' : character;
            input.contentType = InputField.ContentType.Standard;
            input.characterValidation = InputField.CharacterValidation.None;
            input.lineType = InputField.LineType.SingleLine; input.characterLimit = TextVinylData.MaxLength;
            input.readOnly = false; input.text = "";
            input.interactable = true; input.enabled = true;
            input.customCaretColor = true; input.caretColor = new Color(.1f,.1f,.1f,1);
            if (input.textComponent)
            { input.textComponent.supportRichText = false; input.textComponent.fontSize = 28;
              input.textComponent.transform.SetParent(input.transform, false);
              input.textComponent.gameObject.SetActive(true); input.textComponent.enabled = true;
              input.textComponent.color = new Color(.1f,.1f,.1f,1); input.textComponent.resizeTextForBestFit = false;
              Fit(input.textComponent.rectTransform, .03f, .05f, .97f, .95f); }
            if (input.placeholder is Text placeholder)
            { placeholder.text = ECTLocalization.T("Введите текст"); Fit(placeholder.rectTransform, .03f, .05f, .97f, .95f); }
            go.name = "ECT.VinylTextInput"; go.transform.SetParent(textPanel, false);
            Place((RectTransform)go.transform, 8, 8, 250, 54);
            input.onValueChanged.AddListener(_ => { if (!syncing) QueueText(); });
            input.onEndEdit.AddListener(_ => Flush());
            go.SetActive(true); Object.Destroy(staging.gameObject);
            selected = null; selectedName = null;
        }
        private void ToggleSymmetry()
        {
            if (garage.editItem == null) return;
            garage.editItem.isSymetry = !garage.editItem.isSymetry;
            garage.paintSymetry.SetBool(garage.editItem.isSymetry); garage.carPaint.UpImage(); Refresh();
        }
        private void MakeText()
        {
            Flush();
            var data = new TextVinylData("MAD OUT 2", fontIndex, style);
            var info = TextVinylTextures.Info(data.Name);
            var item = garage.editItem;
            if (item == null)
            { item = garage.carPaint.AddItem(info); item.size_y = .2f; garage.SetLastSetItem(item); }
            else { item.SetSpriteInfo(info); item.acpect = info.GetSizeAcpect(); garage.SetLastSetItem(item); }
            garage.carPaint.UpImage(); selected = null; Refresh();
        }
        private void SetStyle(int value) { style = value; QueueText(); }
        private void ToggleTextSurface()
        {
            Flush();
            var item = garage.editItem;
            if (item == null || !TextVinylData.IsText(item.spriteName) ||
                garage.GetComponent<GarageTuningPanel>()?.Session?.CanEdit != true) return;
            var state = GlassPrototypeState.Ensure(garage.carPaint);
            bool glass = GlassPrototypeState.IsGlass(item);
            state.Scan();
            if (!glass && (state.WindowCount == 0 || state.GlassCount >= 32)) return;
            GlassPrototypeState.SetGlass(item, !glass);
            garage.carPaint.needDraw = true;
            garage.carPaint.UpImage();
            Refresh();
        }
        private void QueueText()
        {
            if (garage.editItem == null || !TextVinylData.IsText(garage.editItem.spriteName)) return;
            if (!ReferenceEquals(selected, garage.editItem))
            { selected = garage.editItem; selectedName = selected.spriteName; }
            try { pending = new TextVinylData(input ? input.text : "MAD OUT 2", fontIndex, style); }
            catch (ArgumentException) { return; }
            nextText = Time.unscaledTime + .2f;
        }
        internal void Flush()
        {
            if (pending == null || selected == null || !ReferenceEquals(selected, garage.editItem)) { pending = null; return; }
            selected.SetSpriteInfo(TextVinylTextures.Info(pending.Name)); selectedName = selected.spriteName;
            pending = null; garage.carPaint.UpImage();
        }
        internal void Refresh()
        {
            if (!root || !garage || !garage.carPaint) return;
            Layout(); TryBuildInput();
            var item = garage.editItem;
            if (!ReferenceEquals(selected, item) || item != null && selectedName != item.spriteName)
            {
                pending = null; selected = item; selectedName = item?.spriteName;
                if (item != null && TextVinylData.TryRead(item.spriteName, out var data))
                { fontIndex = data.Font; style = data.Style; if (input) { syncing = true; input.text = data.Text; syncing = false; } }
            }
            bool hasText = item != null && TextVinylData.IsText(item.spriteName);
            if (input && !hasText && input.isFocused) input.DeactivateInputField();
            if (input && input.placeholder is Text hint) hint.text = ECTLocalization.T("Введите текст");
            if (garage.paintColor && garage.paintColor.text) garage.paintColor.text.text = ECTLocalization.T("ЦВЕТ");
            textPanel.gameObject.SetActive(hasText);
            bool hasVinyls = garage.carPaint.items != null && garage.carPaint.items.Count > 0;
            symmetry.gameObject.SetActive(hasVinyls);
            symmetry.nowButtonDissabe = item == null;
            NativeMenuFactory.SetLabel(symmetry, item != null && item.isSymetry ? "СИММЕТРИЯ ✓" : "СИММЕТРИЯ");
            Fit(symmetry.text.ui.rectTransform, .02f, .68f, .98f, .98f);
            UpdateNativeVinylPreview(item);
            if (manualNativeImgClick && nativeImg && nativeImg.gameObject.activeInHierarchy &&
                Input.GetMouseButtonDown(0) && nativeImg.isMouseIntersect(Input.mousePosition))
            { Flush(); garage.PaintShowSelect(); }
            boldCheck.gameObject.SetActive((style & 1) != 0);
            italicCheck.gameObject.SetActive((style & 2) != 0);
            boldCheck.color = bold.text.ui.color;
            italicCheck.color = italic.text.ui.color;
            NativeMenuFactory.SetLabel(font, TextVinylData.Fonts[fontIndex]);
            NativeMenuFactory.SetLabel(textSurface, GlassPrototypeState.IsGlass(item) ? "НАНЕСЕНИЕ: СТЕКЛА" : "НАНЕСЕНИЕ: КУЗОВ");
            textSurface.nowButtonDissabe = !hasText || garage.GetComponent<GarageTuningPanel>()?.Session?.CanEdit != true;
            if (pending != null && Time.unscaledTime >= nextText) Flush();
            if (!menu.gameObject.activeInHierarchy) Flush();
            if (input && input.isFocused)
            { lastInputFrame = Time.frameCount;
              var focus = garage.GetComponentInChildren<MenuFocusManager>(); if (focus && focus.curActive != null) focus.ResetActive(); }
            HandleInputFocus();
        }
        private void HandleInputFocus()
        {
            if (!input || !input.gameObject.activeInHierarchy || !Input.GetMouseButtonDown(0)) return;
            if (MenuColorPicker.IsUnderMouse(Input.mousePosition) || MenuSliderPanel.IsUnderMouse(Input.mousePosition)) return;
            if (!input.GetComponent<RectTransform>().isMouseIntersect(Input.mousePosition))
            { if (input.isFocused) { input.DeactivateInputField(); Flush(); } return; }
            var events = EventSystem.current;
            if (!events)
            {
                ownEventSystem = new GameObject("ECT.VinylInputEvents", typeof(EventSystem));
                events = ownEventSystem.GetComponent<EventSystem>();
                ownInputModule = ownEventSystem.AddComponent<VinylInputModule>();
            }
            if (!events.currentInputModule)
            {
                if (!ownInputModule) ownInputModule = events.gameObject.AddComponent<VinylInputModule>();
            }
            events.SetSelectedGameObject(input.gameObject);
            input.OnPointerClick(new PointerEventData(events) { position = Input.mousePosition, button = PointerEventData.InputButton.Left });
            input.ActivateInputField();
        }
        internal bool IsTyping => input && input.gameObject.activeInHierarchy &&
            (input.isFocused || Time.frameCount <= lastInputFrame + 1);
        internal void Layout()
        {
            if (!root || !menu || !GameUI.me) return;
            CanvasAspectLayout.Apply(root, menu, GameUI.me.rectUI, corners);
            if (!addRect || !textPanel || !nativeImg) return;
            addRect.GetWorldCorners(corners);
            var lower = root.InverseTransformPoint(corners[0]);
            // Larger square targets like the newer mobile UI. Keep one uniform design scale
            // and minimal spacing; allow a small extension past the original left panel.
            float x = Mathf.Clamp(lower.x + 6f, 6f, 1920f - 256f);
            float top = Mathf.Clamp(1080f - lower.y + 8f, 115f, 600f);
            const float side = 122f, gap = 6f;
            float width = side * 2f + gap;
            Place((RectTransform)garage.paintColor.transform, x, top, side, side);
            Place((RectTransform)originals[1].Button.transform, x + side + gap, top, side, side);
            Place((RectTransform)symmetry.transform, x, top + side + gap, side, side);
            Place(nativeImg, x + side + gap, top + side + gap, side, side);
            float inputY = top + 2 * (side + gap) + 8f;
            const float padding = 8f, styleSide = 58f, inputHeight = 54f;
            float fontWidth = width - 2 * (styleSide + gap);
            float styleY = padding + inputHeight + 12f;
            float surfaceY = styleY + styleSide + gap;
            Place(textPanel, x - padding, inputY - padding, width + 2 * padding, surfaceY + inputHeight + padding);
            if (input) Place((RectTransform)input.transform, padding, padding, width, inputHeight);
            Place((RectTransform)bold.transform, padding, styleY, styleSide, styleSide);
            Place((RectTransform)italic.transform, padding + styleSide + gap, styleY, styleSide, styleSide);
            Place((RectTransform)font.transform, padding + 2 * (styleSide + gap), styleY, fontWidth, styleSide);
            Place((RectTransform)textSurface.transform, padding, surfaceY, width, inputHeight);
        }

        private void HideNativeImgDecorations()
        {
            // Keep the native button, background and click handling. Only hide the
            // graphics of its original arrow/label. Preserve all values for Dispose.
            foreach (var graphic in nativeImg.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic || graphic.transform == nativeImg) continue;
                nativeImageGraphics.Add(new NativeImageGraphicBackup { Graphic = graphic, WasEnabled = graphic.enabled });
                graphic.enabled = false;
            }
            foreach (var localized in nativeImg.GetComponentsInChildren<Localize>(true))
            {
                nativeImageLabels.Add(new NativeImageLocalizeBackup { Label = localized, WasEnabled = localized.enabled });
                localized.enabled = false;
            }
        }

        private void UpdateNativeVinylPreview(CarPaintItems.Item item)
        {
            if (!previewImage || !previewRaw) return;
            var info = item != null ? item.spriteInfo : null;
            var sprite = info != null ? info.sprite : null;
            if (item != null && TextVinylData.IsText(item.spriteName)) sprite = EmbeddedAssets.TextVinylIcon;
            if (!sprite && info == null) sprite = EmbeddedAssets.TextVinylIcon;
            previewImage.sprite = sprite;
            previewImage.gameObject.SetActive(sprite != null);
            if (sprite) { previewRaw.gameObject.SetActive(false); return; }
            // For vanilla catalog assets without a Sprite, use their already-loading texture.
            Texture texture = null;
            if (info != null) try { texture = info.texture; } catch { }
            previewRaw.texture = texture;
            previewRaw.gameObject.SetActive(texture != null);
        }
        internal bool ContainsPointer() => root && root.gameObject.activeInHierarchy &&
            (input && input.isFocused || (
            (buttons.Any(b => b && b.gameObject.activeInHierarchy && ((RectTransform)b.transform).isMouseIntersect(Input.mousePosition)) ||
             originals.Any(b => b.Button && b.Button.gameObject.activeInHierarchy && ((RectTransform)b.Button.transform).isMouseIntersect(Input.mousePosition)) ||
             nativeImg && nativeImg.gameObject.activeInHierarchy && nativeImg.isMouseIntersect(Input.mousePosition) ||
             input && input.gameObject.activeInHierarchy && ((RectTransform)input.transform).isMouseIntersect(Input.mousePosition))));
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        { rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.sizeDelta = new Vector2(w,h); NativeMenuFactory.Place(rect,x,y); }
        private static void Fit(RectTransform rect, float x1, float y1, float x2, float y2)
        { rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity; rect.pivot = new Vector2(.5f,.5f);
          rect.anchorMin = new Vector2(x1,y1); rect.anchorMax = new Vector2(x2,y2); rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private void SaveVisuals(Transform parent)
        {
            foreach (var graphic in parent.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.transform == parent) continue;
                var rect = NativeMenuFactory.Rect("ECT.VisualBackup", parent, 0, 0);
                NativeMenuFactory.CopyRect(graphic.rectTransform, rect); rect.gameObject.SetActive(false);
                var text = graphic as Text;
                visuals.Add(new VisualBackup { Graphic = graphic, Parent = graphic.transform.parent, Rect = rect,
                    Label = text ? text.text : null, FontSize = text ? text.fontSize : 0,
                    Align = text ? text.alignment : TextAnchor.MiddleCenter,
                    Resize = text && text.resizeTextForBestFit, MaxSize = text ? text.resizeTextMaxSize : 0,
                    Overflow = text ? text.horizontalOverflow : HorizontalWrapMode.Wrap });
            }
        }
        private static void CopyGraphic(Graphic source, Transform target)
        {
            if (!source) return;
            var rect = NativeMenuFactory.Rect("Icon", target, 70, 70); Fit(rect, .16f, .1f, .84f, .64f);
            Graphic graphic;
            if (source is Image image) { var copy = rect.gameObject.AddComponent<Image>(); copy.sprite = image.overrideSprite; copy.preserveAspect = true; graphic = copy; }
            else if (source is RawImage raw) { var copy = rect.gameObject.AddComponent<RawImage>(); copy.texture = raw.texture; copy.uvRect = raw.uvRect; graphic = copy; }
            else { Object.Destroy(rect.gameObject); return; }
            graphic.material = source.material; graphic.color = source.color; graphic.raycastTarget = false;
        }
        public void Dispose()
        {
            if (input) { input.onValueChanged.RemoveAllListeners(); input.onEndEdit.RemoveAllListeners(); input.DeactivateInputField(); }
            foreach (var backup in originals)
            {
                if (backup.Button && backup.Parent && backup.Rect)
                {
                    var current = backup.Button.GetComponentInParent<MenuGroup>(); if (current) current.buttons.Remove(backup.Button);
                    var rect = (RectTransform)backup.Button.transform; rect.SetParent(backup.Parent,false); rect.SetSiblingIndex(backup.Index);
                    NativeMenuFactory.CopyRect(backup.Rect,rect); backup.Button.isStaickPos = backup.Static;
                    backup.Button.maskArea = backup.Mask; backup.Button.customIntersectionRect = backup.Intersection;
                    backup.Button._focusManger = backup.Focus;
                    backup.Button.isInitParent = false; backup.Button.InitParent(); backup.Button.gameObject.SetActive(backup.Active);
                }
                if (backup.Rect) Object.Destroy(backup.Rect.gameObject);
            }
            foreach (var button in buttons) NativeMenuFactory.RemoveButton(button);
            foreach (var backup in visuals)
            {
                if (backup.Graphic && backup.Parent)
                {
                    backup.Graphic.transform.SetParent(backup.Parent, false);
                    NativeMenuFactory.CopyRect(backup.Rect, backup.Graphic.rectTransform);
                    if (backup.Graphic is Text label)
                    { label.text = backup.Label; label.fontSize = backup.FontSize; label.alignment = backup.Align;
                      label.resizeTextForBestFit = backup.Resize; label.resizeTextMaxSize = backup.MaxSize; label.horizontalOverflow = backup.Overflow; }
                }
                if (backup.Rect) Object.Destroy(backup.Rect.gameObject);
            }
            foreach (var state in nativeImageGraphics)
                if (state.Graphic) state.Graphic.enabled = state.WasEnabled;
            foreach (var state in nativeImageLabels)
                if (state.Label) state.Label.enabled = state.WasEnabled;
            nativeImageGraphics.Clear(); nativeImageLabels.Clear();
            if (previewImage) Object.Destroy(previewImage.gameObject);
            if (previewRaw) Object.Destroy(previewRaw.gameObject);
            if (nativeImg && nativeImgParent && nativeImgBackup && manualNativeImgClick)
            {
                var rect = nativeImg;
                rect.SetParent(nativeImgParent, false); rect.SetSiblingIndex(nativeImgIndex);
                NativeMenuFactory.CopyRect(nativeImgBackup, rect);
                rect.gameObject.SetActive(nativeImgWasActive);
            }
            if (nativeImgBackup) Object.Destroy(nativeImgBackup.gameObject);
            if (ownInputModule) Object.Destroy(ownInputModule);
            if (ownEventSystem) Object.Destroy(ownEventSystem);
            foreach (var go in hidden) if (go) go.SetActive(true);
            if (root) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); }
            originals.Clear(); buttons.Clear(); hidden.Clear();
        }
        private sealed class Backup
        { internal MenuButton Button; internal Transform Parent; internal RectTransform Rect; internal int Index;
          internal bool Static, Active; internal MenuMaskArea Mask; internal RectTransform Intersection; internal MenuFocusManager Focus; }
        private sealed class NativeImageGraphicBackup { internal Graphic Graphic; internal bool WasEnabled; }
        private sealed class NativeImageLocalizeBackup { internal Localize Label; internal bool WasEnabled; }
        private sealed class VisualBackup
        { internal Graphic Graphic; internal Transform Parent; internal RectTransform Rect; internal string Label;
          internal int FontSize, MaxSize; internal bool Resize; internal TextAnchor Align; internal HorizontalWrapMode Overflow; }
    }
}
