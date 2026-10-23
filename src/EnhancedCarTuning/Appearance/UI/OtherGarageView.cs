using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedCarTuning.Appearance.Parts;
using EnhancedCarTuning.Appearance.Police;
using EnhancedCarTuning.Localization;
using EnhancedCarTuning.UI;
using EnhancedCarTuning.Wheels.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object=UnityEngine.Object;

namespace EnhancedCarTuning.Appearance.UI
{
    internal sealed class OtherGarageView : IDisposable
    {
        private sealed class Row{internal BodyElement Part;internal MenuButtonGroup Button;internal Text Check;internal Image Marker;}
        private readonly CarGarageUI garage;private readonly BodyPartsSession session;private readonly PolicePaintSession police;
        private readonly List<MenuButtonGroup> buttons=new List<MenuButtonGroup>();private readonly List<Row> rows=new List<Row>();
        private readonly Dictionary<GameObject,bool> hidden=new Dictionary<GameObject,bool>();private readonly Vector3[] corners=new Vector3[4];
        private MenuButtonGroup entry,body,policeButton,bed;private RectTransform menu,left,overlay,panel;private Text bedValue;
        private WheelSwitchArrow bodyArrow;private bool bodyOpen,ownedMenu,oldEntryActive;private RectTransform originalContent;
        private UnityEvent originalEvent;private MenuButton originalSelect;private MenuGroup group;
        private MenuButton port;private Transform portParent;private RectTransform portBackup;private int portIndex;private MenuGroup portGroup;private bool portStatic;
        private MenuGroup portManual;private MenuFocusManager leftFocus,portFocus;private bool originalHide;
        internal OtherGarageView(CarGarageUI garage,BodyPartsSession session,PolicePaintSession police){this.garage=garage;this.session=session;this.police=police;}
        internal bool IsOpen=>menu&&menu.gameObject.activeInHierarchy;
        private static bool Buying=>MenuBuy.me&&MenuBuy.me.gameObject.activeInHierarchy;
        internal void Build()
        {
            var main=garage.root.Find("Main");entry=main.Find("Other").GetComponent<MenuButtonGroup>();
            var template=main.Find("Paint").GetComponent<MenuButtonGroup>();
            oldEntryActive=entry.gameObject.activeSelf;originalContent=entry.content;originalEvent=entry.SwithEvent;originalSelect=entry.selectAfterPress;
            originalHide=entry.isHideWhenPress;
            menu=entry.content as RectTransform;
            if(!menu){menu=NativeMenuFactory.Rect("ECT.Other_menu",main.parent,0,0);NativeMenuFactory.CopyRect((RectTransform)main,menu);ownedMenu=true;}
            if(menu==main)throw new InvalidOperationException("Other menu points to Main.");
            menu.gameObject.SetActive(false);
            foreach(Transform child in menu)if(child.name!="Panel"){hidden[child.gameObject]=child.gameObject.activeSelf;child.gameObject.SetActive(false);}
            if(!menu.Find("Panel")&&main.Find("Panel")){var background=Object.Instantiate(main.Find("Panel").gameObject,menu,false);background.name="Panel";background.SetActive(true);}
            if(!menu.GetComponent<MenuGroup>())menu.gameObject.AddComponent<MenuGroup>().disable_manualy=new MenuGroup[0];
            var back=menu.GetComponent<CarGarageUI_Back>()??menu.gameObject.AddComponent<CarGarageUI_Back>();back.gr=garage;back.activateWhenBack=main.gameObject;back.top_txt="ДЕТАЛИ КУЗОВА";
            entry.content=menu;entry.SwithEvent=new UnityEvent();entry.selectAfterPress=null;entry.isHideWhenPress=true;entry.gameObject.SetActive(true);
            // Native Other uses Main's transformed coordinate system. Match both
            // columns to the viewport instead of assuming its local units are pixels.
            left=NativeMenuFactory.Rect("ECT.OtherControls",menu,1920,1080);
            leftFocus=left.gameObject.AddComponent<MenuFocusManager>();if(template.focusManger)leftFocus.saveSettingsTo=template.focusManger.saveSettingsTo;
            group=left.gameObject.AddComponent<MenuGroup>();group.alighContent=MenuGroup.AlignType.None;group.disable_manualy=new MenuGroup[0];
            overlay=NativeMenuFactory.Rect("ECT.BodyPartsControls",garage.transform,1920,1080);overlay.gameObject.SetActive(false);
            var focus=overlay.gameObject.AddComponent<MenuFocusManager>();if(template.focusManger)focus.saveSettingsTo=template.focusManger.saveSettingsTo;
            panel=Box("ECT.BodyParts",overlay,1508,114,412,966);NativePanelBackground.Attach(panel,garage);
            var rightGroup=panel.gameObject.AddComponent<MenuGroup>();rightGroup.alighContent=MenuGroup.AlignType.None;rightGroup.disable_manualy=new MenuGroup[0];
            using(var factory=new NativeMenuFactory(garage,template))
            {
                body=Button(factory,"ECT.Other.Body",left,"КУЗОВ",()=>{bodyOpen=!bodyOpen;Refresh();});body.gameObject.SetActive(true);entry.selectAfterPress=body;Prepare(body,TextAnchor.MiddleLeft);
                Fit(body.text.ui.rectTransform,.07f,.10f,.76f,.90f);
                body.text.ui.rectTransform.anchoredPosition+=new Vector2(15,-3);
                var a=Box("ECT.BodyPartsArrow",body.transform,214,25,23,34);bodyArrow=a.gameObject.AddComponent<WheelSwitchArrow>();bodyArrow.raycastTarget=false;
                foreach(var part in BodyElementNames.Specials)
                {
                    var p=part;var button=Button(factory,"ECT.Other."+part,left,BodyElementNames.Label(part),()=>{session.Toggle(p);Refresh();});
                    Prepare(button,TextAnchor.MiddleRight);rows.Add(new Row{Part=part,Button=button});AddToggleArrow(button);
                }
                if(police!=null)
                {policeButton=Button(factory,"ECT.Other.Police",left,"POLICE",()=>{police.Toggle();Refresh();});Prepare(policeButton,TextAnchor.MiddleRight);AddToggleArrow(policeButton);}
                bed=Button(factory,"ECT.Other.TruckBed",left,"ГРУЗОВОЙ КУЗОВ",()=>{float x=((RectTransform)bed.transform).MouseToLocal(bed.pressPos).x;session.StepBed(x<.5f?-1:1);Refresh();});
                Prepare(bed,TextAnchor.MiddleCenter);bedValue=WheelSelectorView.Create((RectTransform)bed.transform,bed.text.ui,bed.text.ui.font,out var bedArrows);
                foreach(var arrow in bedArrows)((RectTransform)arrow.transform).anchoredPosition+=new Vector2(0,-20);
                foreach(var part in BodyElementNames.Body)
                {
                    var p=part;var button=Button(factory,"ECT.BodyParts.Part."+part,panel,BodyElementNames.Label(part),()=>{session.Toggle(p);Refresh();});
                    button._focusManger=focus;button.manualGroup=rightGroup;
                    Prepare(button,TextAnchor.MiddleLeft);Fit(button.text.ui.rectTransform,.13f,.10f,.78f,.90f);
                    var check=Box("ECT.PartCheck",button.transform,320,24,42,52).gameObject.AddComponent<Text>();check.font=UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");check.fontSize=36;check.color=Color.white;check.raycastTarget=false;
                    var marker=Box("ECT.PartSelected",button.transform,26,8,6,84).gameObject.AddComponent<Image>();marker.color=new Color(.05f,.9f,.13f);marker.raycastTarget=false;
                    rows.Add(new Row{Part=part,Button=button,Check=check,Marker=marker});
                }
            }
            Refresh();
        }
        private MenuButtonGroup Button(NativeMenuFactory factory,string name,Transform parent,string label,Action action)
        {
            var b=factory.Button(name,parent,label,"",action:()=>{if(!Buying&&session.CanEdit)action();});b.isStaickPos=true;buttons.Add(b);return b;
        }
        private static void Prepare(MenuButtonGroup b,TextAnchor alignment)
        {
            var label=b.text.ui;label.transform.SetParent(b.transform,false);
            foreach(var g in b.GetComponentsInChildren<Graphic>(true))if(g!=label&&g.transform!=b.transform)g.gameObject.SetActive(false);
            label.gameObject.SetActive(true);label.enabled=true;label.raycastTarget=false;label.fontSize=label.resizeTextMaxSize=34;label.resizeTextMinSize=20;label.resizeTextForBestFit=true;label.alignment=alignment;
            Fit(label.rectTransform,.25f,.08f,.94f,.92f);
        }
        private static void AddToggleArrow(MenuButtonGroup b)
        {
            b.text.ui.rectTransform.anchoredPosition+=new Vector2(-10,0);
            var rect=NativeMenuFactory.Rect("ECT.ToggleArrow",b.transform,23,34);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(34,0);
            var arrow=rect.gameObject.AddComponent<PolicePaintArrow>();arrow.color=b.text.ui.color;arrow.raycastTarget=false;
        }
        internal void Refresh()
        {
            if(!menu||!body)return;
            MovePort();if(GameUI.me)CanvasAspectLayout.Apply(left,menu,GameUI.me.rectUI,corners);
            overlay.gameObject.SetActive(IsOpen&&bodyOpen&&!Buying);if(overlay.gameObject.activeSelf&&GameUI.me)CanvasAspectLayout.Apply(overlay,garage.transform as RectTransform,GameUI.me.rectUI,corners);
            int mask=session.State.Available,visible=0;
            var leftRows=new List<MenuButtonGroup>{body};
            foreach(var row in rows)
            {
                bool available=(mask&(1<<(int)row.Part))!=0;row.Button.gameObject.SetActive(available);if(!available)continue;
                bool enabled=session.State.IsVisible(row.Part);row.Button.nowButtonDissabe=!session.CanEdit;
                if(row.Check){Place((RectTransform)row.Button.transform,12,106+visible++*108,388,100);row.Check.text=enabled?"☑":"☐";row.Marker.gameObject.SetActive(enabled);}
                else{NativeMenuFactory.SetLabel(row.Button,BodyElementNames.Label(row.Part)+"\n"+(enabled?"YES":"NO"));leftRows.Add(row.Button);}
            }
            if(policeButton){policeButton.gameObject.SetActive(police.State.Supported);policeButton.nowButtonDissabe=!police.CanEdit;NativeMenuFactory.SetLabel(policeButton,"POLICE\n"+(police.State.Active?"YES":"NO"));if(police.State.Supported)leftRows.Insert(1,policeButton);}
            bool showBed=session.State.Beds.Length>1;bed.gameObject.SetActive(showBed);bed.nowButtonDissabe=!session.CanEdit;if(showBed){bedValue.text=session.State.CurrentBed==0?ECTLocalization.T("НЕТ"):session.State.CurrentBed.ToString();leftRows.Add(bed);}
            float step=Mathf.Min(141,670f/Mathf.Max(1,leftRows.Count-1));
            for(int i=0;i<leftRows.Count;i++)Place((RectTransform)leftRows[i].transform,6,i==0?141:235+(i-1)*step,262,i==0?86:step-6);
            bodyArrow.Left=bodyOpen;bodyArrow.SetVerticesDirty();bodyArrow.color=body.text.ui.color;
        }
        private void MovePort()
        {
            if(!port)
            {
                var node=garage.root.Find("Main/BigCityLegacyVinylPort");if(!node)return;port=node.GetComponent<MenuButton>();if(!port)return;
                portParent=node.parent;portIndex=node.GetSiblingIndex();portStatic=port.isStaickPos;portGroup=port.GetComponentInParent<MenuGroup>();portFocus=port._focusManger;
                if(port is MenuButtonGroup grouped)portManual=grouped.manualGroup;
                portBackup=NativeMenuFactory.Rect("ECT.VinylPortBackup",portParent,0,0);NativeMenuFactory.CopyRect((RectTransform)node,portBackup);portBackup.gameObject.SetActive(false);
                if(portGroup)portGroup.buttons.Remove(port);node.SetParent(left,false);port._focusManger=leftFocus;port.isStaickPos=true;if(port is MenuButtonGroup moved)moved.manualGroup=group;port.isInitParent=false;port.InitParent();
            }
            var rect=(RectTransform)port.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.localScale=Vector3.one;rect.sizeDelta=new Vector2(262,135);rect.anchoredPosition=new Vector2(6,32);
        }
        internal bool ContainsPointer()=>IsOpen&&menu.Find("Panel")&&((RectTransform)menu.Find("Panel")).isMouseIntersect(Input.mousePosition)||overlay&&overlay.gameObject.activeInHierarchy&&panel.isMouseIntersect(Input.mousePosition);
        private static RectTransform Box(string n,Transform p,float x,float y,float w,float h){var r=NativeMenuFactory.Rect(n,p,w,h);Place(r,x,y,w,h);return r;}
        private static void Place(RectTransform r,float x,float y,float w,float h){r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;NativeMenuFactory.Place(r,x,y);}
        private static void Fit(RectTransform r,float x,float y,float xx,float yy)=>WheelSelectorView.Fit(r,x,y,xx,yy);
        public void Dispose()
        {
            if(IsOpen){menu.gameObject.SetActive(false);var main=garage.root.Find("Main");if(main&&garage.gameObject.activeInHierarchy)main.gameObject.SetActive(true);}
            if(port&&portParent){if(group)group.buttons.Remove(port);port.transform.SetParent(portParent,false);port.transform.SetSiblingIndex(portIndex);NativeMenuFactory.CopyRect(portBackup,(RectTransform)port.transform);port._focusManger=portFocus;port.isStaickPos=portStatic;if(port is MenuButtonGroup restored)restored.manualGroup=portManual;port.isInitParent=false;port.InitParent();}
            if(portBackup)DestroyOwned(portBackup.gameObject);
            foreach(var b in buttons)NativeMenuFactory.RemoveButton(b);buttons.Clear();
            foreach(var pair in hidden)if(pair.Key)pair.Key.SetActive(pair.Value);hidden.Clear();
            if(entry){entry.content=originalContent;entry.SwithEvent=originalEvent;entry.selectAfterPress=originalSelect;entry.isHideWhenPress=originalHide;entry.gameObject.SetActive(oldEntryActive);}
            if(left)DestroyOwned(left.gameObject);if(ownedMenu&&menu)DestroyOwned(menu.gameObject);if(overlay)DestroyOwned(overlay.gameObject);
        }
        private static void DestroyOwned(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
