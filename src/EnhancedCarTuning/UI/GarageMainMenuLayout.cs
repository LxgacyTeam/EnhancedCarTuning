using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EnhancedCarTuning.UI
{
    internal sealed class GarageMainMenuLayout : IDisposable
    {
        private readonly CarGarageUI garage;
        private readonly List<Action> restore=new List<Action>();
        private MenuButton[] ordered;
        private Vector2 start;private float step;
        private MenuGroup group;private MenuGroup.AlignType alignment;
        internal GarageMainMenuLayout(CarGarageUI garage){this.garage=garage;}
        internal void Build()
        {
            var main=garage.root.Find("Main");var source=main.Find("Wheels") as RectTransform;
            string[] names={"BtnTuneUp","ECT.PaintColors","Paint","Wheels","Other","FixCar"};
            ordered=names.Select(n=>main.Find(n)?.GetComponent<MenuButton>()).Where(b=>b).ToArray();
            if(!source||ordered.Length!=6)throw new InvalidOperationException("Garage main menu entries unavailable.");
            var paint=main.Find("ECT.PaintColors") as RectTransform;var vinyl=main.Find("Paint") as RectTransform;
            start=paint.anchoredPosition;step=Mathf.Abs(paint.anchoredPosition.y-vinyl.anchoredPosition.y);
            if(step<source.rect.height)step=source.rect.height+6;
            foreach(var button in ordered)
            {
                var rect=(RectTransform)button.transform;var pos=rect.anchoredPosition;var size=rect.sizeDelta;var anchorMin=rect.anchorMin;var anchorMax=rect.anchorMax;var pivot=rect.pivot;
                int index=rect.GetSiblingIndex();bool stat=button.isStaickPos;
                restore.Add(()=>{if(!button)return;rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.pivot=pivot;rect.sizeDelta=size;rect.anchoredPosition=pos;rect.SetSiblingIndex(index);button.isStaickPos=stat;});
                NativeMenuFactory.CopyRect(source,rect);button.isStaickPos=true;
            }
            group=main.GetComponent<MenuGroup>();
            if(group){alignment=group.alighContent;group.alighContent=MenuGroup.AlignType.None;foreach(var b in ordered)group.buttons.Remove(b);group.buttons.InsertRange(0,ordered);}
            Refresh();
        }
        internal void Refresh()
        {
            if(ordered==null)return;
            for(int i=0;i<ordered.Length;i++)if(ordered[i])
            {
                var rect=(RectTransform)ordered[i].transform;rect.anchoredPosition=start-new Vector2(0,i*step);
                rect.SetSiblingIndex(i+1);
            }
            if(garage.fixCar)garage.fixCar.gameObject.SetActive(MenuEsc.editCarInSceneGarage);
        }
        public void Dispose(){foreach(var action in restore)action();restore.Clear();if(group)group.alighContent=alignment;}
    }
}
