using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EnhancedCarTuning.UI
{
    internal static class NativeTextField
    {
        internal static InputField Create(string name,Transform parent,string hint,int limit)
        {
            var fields=UnityEngine.Resources.FindObjectsOfTypeAll<InputField>();
            var source=fields.FirstOrDefault(f=>f&&f.name=="ECT.VinylTextInput")??fields.FirstOrDefault(f=>f&&f.name=="Nik Name"&&f.transform.parent&&f.transform.parent.name=="NikTxt");
            var staging=NativeMenuFactory.Rect("ECT.InputStaging",parent,0,0);staging.gameObject.SetActive(false);
            GameObject go;
            if(source)go=Object.Instantiate(source.gameObject,staging,false);
            else
            {
                var rect=NativeMenuFactory.Rect(name,staging,264,62);go=rect.gameObject;
                var image=go.AddComponent<Image>();image.color=Color.white;
                var field=go.AddComponent<InputField>();field.targetGraphic=image;
                var text=NativeMenuFactory.Rect("Text",rect,0,0).gameObject.AddComponent<Text>();text.font=UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");text.alignment=TextAnchor.MiddleLeft;field.textComponent=text;
                var placeholder=NativeMenuFactory.Rect("Placeholder",rect,0,0).gameObject.AddComponent<Text>();placeholder.font=text.font;placeholder.color=new Color(.35f,.35f,.35f,.7f);placeholder.fontSize=28;field.placeholder=placeholder;
            }
            go.SetActive(false);var input=go.GetComponent<InputField>();
            foreach(var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                if(component&&component!=input&&!(component is UnityEngine.EventSystems.UIBehaviour)){component.enabled=false;Object.DestroyImmediate(component);}
            foreach(var caret in go.GetComponentsInChildren<RectTransform>(true))if(caret.name.IndexOf("Input Caret",StringComparison.OrdinalIgnoreCase)>=0)Object.DestroyImmediate(caret.gameObject);
            input.onValueChanged=new InputField.OnChangeEvent();input.onEndEdit=new InputField.SubmitEvent();
            input.onValidateInput=(text,index,c)=>char.IsControl(c)?'\0':c;
            input.contentType=InputField.ContentType.Standard;input.characterValidation=InputField.CharacterValidation.None;
            input.lineType=InputField.LineType.SingleLine;input.characterLimit=limit;input.readOnly=false;input.text="";input.interactable=true;input.enabled=true;
            input.customCaretColor=true;input.caretColor=new Color(.1f,.1f,.1f);
            if(input.textComponent)
            {
                var text=input.textComponent;text.supportRichText=false;text.fontSize=28;text.transform.SetParent(input.transform,false);text.gameObject.SetActive(true);text.enabled=true;
                text.color=new Color(.1f,.1f,.1f);text.resizeTextForBestFit=false;Fit(text.rectTransform,.03f,.05f,.97f,.95f);
            }
            if(input.placeholder is Text placeholderText){placeholderText.text=hint;Fit(placeholderText.rectTransform,.03f,.05f,.97f,.95f);}
            go.name=name;go.transform.SetParent(parent,false);go.SetActive(true);if(Application.isPlaying)Object.Destroy(staging.gameObject);else Object.DestroyImmediate(staging.gameObject);return input;
        }
        private static void Fit(RectTransform r,float x,float y,float xx,float yy)
        {r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
    }
}
