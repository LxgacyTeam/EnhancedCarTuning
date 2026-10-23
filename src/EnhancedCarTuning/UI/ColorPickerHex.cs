using System.Collections.Generic;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EnhancedCarTuning.Appearance.UI;
namespace EnhancedCarTuning.UI
{
    internal sealed class ColorPickerHex : MonoBehaviour
    {
        private static readonly HashSet<ColorPickerHex> active=new HashSet<ColorPickerHex>();
        private static readonly System.Reflection.FieldInfo BaseSize=AccessTools.Field(typeof(MenuColorPicker),"baseSizeDelta");
        private MenuColorPicker picker;private InputField input;private RectTransform background;
        private GameObject ownEventSystem;private VinylInputModule ownInputModule;
        private Coroutine pendingFocus;private bool focusRequested;
        // Runtime picker templates are cloned by the native paint controls. Carry the
        // unextended geometry into each clone, rather than adding another HEX strip.
        [SerializeField] private bool hasNativeGeometry;
        [SerializeField] private Vector2 unextendedSize,unextendedPosition;
        [SerializeField] private float extraHeight;
        private Vector2 nativePosition,lastPosition;private bool positioned,syncing;
        internal bool IsTyping=>input&&(input.isFocused||focusRequested);
        internal static bool AnyTyping {get{foreach(var h in active)if(h&&h.IsTyping)return true;return false;}}
        internal static bool ContainsPointer(Vector2 pos){foreach(var h in active)if(h&&h.background&&h.background.isMouseIntersect(pos))return true;return false;}
        internal static ColorPickerHex Attach(MenuColorPicker picker)
        {var component=picker.GetComponent<ColorPickerHex>()??picker.gameObject.AddComponent<ColorPickerHex>();component.Initialize(picker);return component;}
        private void Initialize(MenuColorPicker target)
        {
            if(input)return;picker=target;background=target.panel.rectTransform;
            if(hasNativeGeometry){BaseSize?.SetValue(picker,unextendedSize);background.anchoredPosition=unextendedPosition;}
            else{unextendedSize=BaseSize!=null?(Vector2)BaseSize.GetValue(picker):background.sizeDelta;unextendedPosition=background.anchoredPosition;hasNativeGeometry=true;}
            nativePosition=unextendedPosition;
            input=background.Find("ECT.ColorHexInput")?.GetComponent<InputField>()??NativeTextField.Create("ECT.ColorHexInput",background,"#RRGGBB",9);
            input.onValueChanged=new InputField.OnChangeEvent();input.onEndEdit=new InputField.SubmitEvent();
            var rect=(RectTransform)input.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(-42,44);rect.anchoredPosition=new Vector2(0,23);
            if(input.targetGraphic)input.targetGraphic.raycastTarget=true;
            input.onValueChanged.AddListener(Edit);input.onEndEdit.AddListener(_=>Sync(true));ApplySize();Sync(true);
        }
        internal void SetExtraHeight(float height){extraHeight=height;ApplySize();}
        private void Update(){if(Input.GetMouseButtonDown(0))HandlePointer(Input.mousePosition);}
        internal void HandlePointer(Vector2 position)
        {
            if(!input||!input.gameObject.activeInHierarchy)return;
            if(!((RectTransform)input.transform).isMouseIntersect(position)){Blur();return;}
            // The native menu does its own hit testing and may have no uGUI input
            // module. Use the same selected-field event pump as text vinyls.
            var events=EventSystem.current;
            if(!events)
            {
                ownEventSystem=new GameObject("ECT.ColorInputEvents",typeof(EventSystem));
                events=ownEventSystem.GetComponent<EventSystem>();EventSystem.current=events;ownInputModule=ownEventSystem.AddComponent<VinylInputModule>();
            }
            if(!events.currentInputModule&&!ownInputModule)ownInputModule=events.gameObject.AddComponent<VinylInputModule>();
            if(!events.currentInputModule)
            {
                if(pendingFocus!=null)StopCoroutine(pendingFocus);
                focusRequested=true;pendingFocus=StartCoroutine(FocusWhenReady(events,position));return;
            }
            SelectInput(events,position);
        }
        private IEnumerator FocusWhenReady(EventSystem events,Vector2 position)
        {
            // InputField also dereferences the current module while activating.
            // Wait for the native EventSystem to select the newly installed pump.
            while(events&&!events.currentInputModule)yield return null;
            pendingFocus=null;focusRequested=false;
            if(events&&input&&input.gameObject.activeInHierarchy)SelectInput(events,position);
        }
        private void SelectInput(EventSystem events,Vector2 position)
        {
            events.SetSelectedGameObject(input.gameObject);
            var pointer=new PointerEventData(events){position=position,button=PointerEventData.InputButton.Left};
            input.OnPointerDown(pointer);
            input.OnPointerClick(pointer);input.ActivateInputField();
        }
        private void Blur()
        {
            if(pendingFocus!=null)StopCoroutine(pendingFocus);pendingFocus=null;focusRequested=false;
            if(!input)return;
            if(input.isFocused)input.DeactivateInputField();
            var events=EventSystem.current;if(events&&events.currentSelectedGameObject==input.gameObject)events.SetSelectedGameObject(null);
        }
        private void Edit(string value)
        {
            if(syncing||!picker)return;
            if(HexColor.TryParse(value,picker.GetColor().a,out var color)){if(!picker.opacity.gameObject.activeSelf)color.a=picker.GetColor().a;picker.SetColor(color);picker.eventUp?.Invoke();}
        }
        internal void ApplySize()
        {
            if(!picker||!background)return;
            Vector2 size=BaseSize!=null?(Vector2)BaseSize.GetValue(picker):background.sizeDelta;
            float removed=picker.opacity.gameObject.activeSelf&&!picker.mettalic.gameObject.activeSelf?picker.opacity.panel.rectTransform.sizeDelta.y:
                !picker.mettalic.gameObject.activeSelf?picker.mettalic.panel.rectTransform.sizeDelta.y*2:0;
            if(positioned&&(background.anchoredPosition-lastPosition).sqrMagnitude>.001f)nativePosition=background.anchoredPosition;
            unextendedPosition=nativePosition;
            background.sizeDelta=new Vector2(size.x,size.y-removed+extraHeight+60);
            lastPosition=nativePosition-new Vector2(0,60*(1-background.pivot.y));background.anchoredPosition=lastPosition;positioned=true;
        }
        private void Sync(bool force=false)
        {
            if(!input||!picker||!force&&input.isFocused)return;
            string text=HexColor.Format(picker.GetColor(),picker.opacity.gameObject.activeSelf);
            if(input.text==text)return;syncing=true;input.text=text;syncing=false;
        }
        private void LateUpdate(){ApplySize();Sync();}
        private void OnEnable(){active.Add(this);}
        private void OnDisable(){Blur();active.Remove(this);}
        private void OnDestroy(){Blur();active.Remove(this);if(ownInputModule)DestroyOwned(ownInputModule);if(ownEventSystem)DestroyOwned(ownEventSystem);}
        private static void DestroyOwned(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
