using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Parts
{
    internal sealed class BodyPartsState : MonoBehaviour
    {
        private sealed class Target
        {
            internal Transform Root; internal BodyElement Part; internal int Bed;
            internal MeshLoadFromResources[] Loaders; internal bool[] BeforeLoad;
            internal bool Owned, BeforeActive, Last;
        }
        private readonly List<Target> targets=new List<Target>();
        private CarControl car;
        private float nextScan;
        private bool applying;
        internal BodyPartsSettings Active {get;private set;}=BodyPartsSettings.Stock;
        internal BodyPartsSettings Committed {get;private set;}=BodyPartsSettings.Stock;
        internal bool SerializePreview;
        internal int Available {get;private set;}
        internal int[] Beds {get{var variants=targets.Where(t=>t.Root&&t.Bed>0).Select(t=>t.Bed).Distinct().OrderBy(n=>n).ToArray();return variants.Length==0?variants:new[]{0}.Concat(variants).ToArray();}}
        internal static bool IsBedVehicle(CarControl car)=>string.Equals(Core.CarIdentity.Name(car),"Zil_130",StringComparison.OrdinalIgnoreCase);
        internal static BodyPartsState Ensure(CarControl car)
        {
            if(!car)return null;
            var state=car.GetComponent<BodyPartsState>()??car.gameObject.AddComponent<BodyPartsState>();
            if(!state.car){state.car=car;state.Scan();state.Apply(true);}
            return state;
        }
        internal void Scan()
        {
            if(!car)return;
            var root=car.transform;
            bool zil=IsBedVehicle(car);
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
            {
                if(node==root||node.GetComponentInParent<CarControl>()!=car||targets.Any(t=>t.Root==node))continue;
                var loader=node.GetComponent<MeshLoadFromResources>();
                string name=loader&&!string.IsNullOrEmpty(loader.mesh_name)?loader.mesh_name:node.name;
                int bed=zil?BodyElementNames.Bed(name):0;
                if(!BodyElementNames.TryClassify(name,out var part)&&bed==0)continue;
                if(!loader&&!node.GetComponent<Renderer>()&&node.GetComponentsInChildren<MeshLoadFromResources>(true).Length==0)continue;
                if(targets.Any(t=>t.Bed==bed&&(bed>0||t.Part==part)&&node.IsChildOf(t.Root)))continue;
                // Prefer the outer matching node so each loader has a single owner.
                foreach(var old in targets.Where(t=>t.Bed==bed&&(bed>0||t.Part==part)&&t.Root.IsChildOf(node)).ToArray())
                {Release(old);targets.Remove(old);}
                targets.Add(new Target{Root=node,Part=part,Bed=bed,Loaders=node.GetComponentsInChildren<MeshLoadFromResources>(true)});
            }
            targets.RemoveAll(t=>!t.Root);Available=0;
            foreach(var t in targets)if(t.Bed==0)Available|=1<<(int)t.Part;
            nextScan=Time.unscaledTime+1;
        }
        private static bool NativeVisible(Target target)
        {var loader=target.Root.GetComponent<MeshLoadFromResources>();return loader?loader.activateWhenLoaded:target.Root.gameObject.activeSelf;}
        internal bool IsVisible(BodyElement part)
        {
            var setting=Active.Get(part);if(setting.HasValue)return setting.Value;
            return targets.Any(t=>t.Root&&t.Bed==0&&t.Part==part&&NativeVisible(t));
        }
        internal int CurrentBed=>Active.Bed;
        internal void Preview(BodyPartsSettings settings){if(settings.Equals(Active))return;Scan();Active=settings;Apply(true);}
        internal void Commit(BodyPartsSettings settings){Preview(settings);Committed=settings;}
        private static readonly System.Reflection.FieldInfo OnCar=AccessTools.Field(typeof(CarDetail),"isOnCar");
        private static bool CanShow(Target target)
        {var detail=target.Root.GetComponent<CarDetail>();return !detail||OnCar==null||(bool)OnCar.GetValue(detail);}
        internal void Apply(bool force=false)
        {
            if(applying||!car)return;applying=true;
            try
            {
                foreach(var t in targets)
                {
                    if(!t.Root)continue;
                    bool? desired=t.Bed>0?(bool?)(Active.Bed==t.Bed):Active.Get(t.Part);
                    if(!desired.HasValue){Release(t);continue;}
                    bool changed=!t.Owned||t.Last!=desired.Value;
                    if(!t.Owned)
                    {
                        t.BeforeActive=t.Root.gameObject.activeSelf;t.BeforeLoad=t.Loaders.Select(l=>l&&l.activateWhenLoaded).ToArray();t.Owned=true;
                    }
                    bool visible=desired.Value;t.Last=visible;
                    foreach(var l in t.Loaders)if(l)l.activateWhenLoaded=visible;
                    if(!visible&&t.Root.gameObject.activeSelf)t.Root.gameObject.SetActive(false);
                    else if(visible&&(changed||force)&&CanShow(t)&&!t.Root.gameObject.activeSelf)t.Root.gameObject.SetActive(true);
                }
            }
            finally{applying=false;}
        }
        private static void Release(Target t)
        {
            if(!t.Owned)return;
            for(int i=0;i<t.Loaders.Length;i++)if(t.Loaders[i])t.Loaders[i].activateWhenLoaded=t.BeforeLoad[i];
            if(t.Root&&t.Root.gameObject.activeSelf!=t.BeforeActive&&(!t.BeforeActive||CanShow(t)))t.Root.gameObject.SetActive(t.BeforeActive);
            t.Owned=false;t.BeforeLoad=null;
        }
        internal void ResetForPool(){Active=Committed=BodyPartsSettings.Stock;SerializePreview=false;foreach(var t in targets)Release(t);}
        private void LateUpdate(){if(targets.Count==0&&Time.unscaledTime>=nextScan)Scan();if(!Active.Equals(BodyPartsSettings.Stock)||IsBedVehicle(car))Apply();}
        private void OnDisable(){ResetForPool();}
        private void OnDestroy(){foreach(var t in targets)Release(t);targets.Clear();}
    }
}
