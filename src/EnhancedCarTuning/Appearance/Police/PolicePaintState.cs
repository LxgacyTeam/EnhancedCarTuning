using System;
using UnityEngine;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Networking;
using UnityEngine.Networking;

namespace EnhancedCarTuning.Appearance.Police
{
    internal sealed class PolicePaintState : MonoBehaviour
    {
        internal CarMaterial CarMaterial { get; private set; }
        internal bool Committed { get; private set; }
        internal bool Active { get; private set; }
        internal bool SerializePreview { get; set; }
        internal int SirenMode { get; private set; }
        internal int CommittedSirenMode { get; private set; }
        internal bool Supported => police;
        private CarUpgradeMaterial police, previous;
        private bool ownsOverride;
        private CarSirena siren;
        private MeshLoadFromResources[] loaders;
        private bool[] previousLoaderFlags;
        private bool ownsEquipment, previousEquipmentActive;
        private CarSirena.State previousSiren;
        private bool applying;
        private float nextSend;
        private bool dirtySiren;
        private bool awaitingSiren;

        internal static PolicePaintState Ensure(CarMaterial material)
        {
            if (!material) return null;
            var state = material.GetComponent<PolicePaintState>() ?? material.gameObject.AddComponent<PolicePaintState>();
            if (!state.CarMaterial)
            {
                state.CarMaterial = material;
                var car = material.car ? material.car : material.GetComponent<CarControl>();
                if (car) state.police = FindPoliceMaterial(car);
                if (state.police)
                {
                    state.siren = state.police.GetComponentInChildren<CarSirena>(true);
                    state.loaders = state.police.GetComponentsInChildren<MeshLoadFromResources>(true);
                    state.previousLoaderFlags = new bool[state.loaders.Length];
                }
            }
            return state;
        }
        private static CarUpgradeMaterial FindPoliceMaterial(CarControl car)
        {
            foreach (var upgrade in car.GetComponentsInChildren<CarUpgrade>(true))
            {
                if (!string.Equals(upgrade.txt, "Police", StringComparison.OrdinalIgnoreCase) || upgrade.items == null) continue;
                foreach (var item in upgrade.items)
                {
                    if (item == null || !item.tr) continue;
                    var candidate = item.tr.GetComponent<CarUpgradeMaterial>();
                    if (candidate && candidate.isOverrideBodyTex && candidate.bodyTexture &&
                        candidate.GetComponentInParent<CarControl>() == car) return candidate;
                }
            }
            return null;
        }
        internal void PrepareOverride()
        {
            if (!CarMaterial || NetManager.isServer) return;
            if (Active && police)
            {
                if (!ownsOverride) { previous = CarMaterial.overrideMat; ownsOverride = true; }
                CarMaterial.overrideMat = police;
            }
            else if (ownsOverride)
            {
                if (CarMaterial.overrideMat == police) CarMaterial.overrideMat = previous;
                previous = null; ownsOverride = false;
            }
        }
        internal void Preview(bool enabled, int sirenMode = 2)
        {
            Active = enabled && Supported;
            SirenMode = Active ? sirenMode : 0;
            PrepareOverride();
            ApplyEquipment();
            if (CarMaterial && !NetManager.isServer) CarMaterial.Apply();
            if (CarMaterial) CarMaterial.GetComponent<BodyPaintState>()?.Invalidate();
        }
        internal void Commit(bool enabled, int sirenMode = 2)
        {
            var car = CarMaterial ? CarMaterial.car : null;
            bool localRuntime = enabled && Committed && siren && car && !car.isUsedInMenu &&
                NetworkClient.active && car.net_control && car.net_control.hasAuthority && !NetManager.isServer;
            if (localRuntime)
            {
                int actual = (int)siren.GetCurrentState();
                if (actual != SirenMode) { SirenMode = actual; dirtySiren = true; }
            }
            bool pending = localRuntime && (dirtySiren || awaitingSiren) && sirenMode != SirenMode;
            Preview(enabled, pending ? SirenMode : sirenMode);
            Committed = Active; CommittedSirenMode = SirenMode;
            if (!pending) { dirtySiren = false; awaitingSiren = false; }
        }
        internal void ApplyEquipment()
        {
            if (!police || NetManager.isServer || applying) return;
            applying = true;
            try
            {
                if (Active)
                {
                    if (!ownsEquipment)
                    {
                        previousEquipmentActive = police.gameObject.activeSelf;
                        previousSiren = siren ? siren.GetCurrentState() : CarSirena.State.NotActive;
                        for (int i = 0; i < loaders.Length; i++)
                            if (loaders[i]) previousLoaderFlags[i] = loaders[i].activateWhenLoaded;
                        ownsEquipment = true;
                    }
                    for (int i = 0; i < loaders.Length; i++) if (loaders[i]) loaders[i].activateWhenLoaded = true;
                    if (siren) siren.SetState(CarMaterial.car && !CarMaterial.car.isAlive ? CarSirena.State.NotActive : (CarSirena.State)SirenMode);
                    if (!police.gameObject.activeSelf) police.gameObject.SetActive(true);
                }
                else if (ownsEquipment)
                {
                    if (siren) siren.SetState(previousEquipmentActive ? previousSiren : CarSirena.State.NotActive);
                    for (int i = 0; i < loaders.Length; i++) if (loaders[i]) loaders[i].activateWhenLoaded = previousLoaderFlags[i];
                    if (police.gameObject.activeSelf != previousEquipmentActive) police.gameObject.SetActive(previousEquipmentActive);
                    ownsEquipment = false;
                }
            }
            finally { applying = false; }
        }
        private void LateUpdate()
        {
            if (!Active || !police || NetManager.isServer) return;
            if (!police.gameObject.activeSelf) ApplyEquipment();
            var car = CarMaterial.car;
            var net = car ? car.net_control : null;
            if (!Committed || !siren || !car || car.isUsedInMenu || !NetworkClient.active || !net || !net.hasAuthority) return;
            int mode = (int)siren.GetCurrentState();
            if (mode != SirenMode) { SirenMode = CommittedSirenMode = mode; dirtySiren = true; }
            if (!dirtySiren || Time.unscaledTime < nextSend) return;
            if (!PoliceSirenSync.TryUpdate(net.xmlData, mode, out string xml)) return;
            nextSend = Time.unscaledTime + .15f; dirtySiren = false;
            if (xml != net.xmlData) { awaitingSiren = true; net.CallCmd_XmlData(xml); }
        }
        internal void ResetForPool()
        {
            Active = Committed = false; SerializePreview = false;
            SirenMode = CommittedSirenMode = 0; dirtySiren = awaitingSiren = false; nextSend = 0;
            ApplyEquipment();
            PrepareOverride();
            previous = null; ownsOverride = false;
        }
        private void OnDisable() { ResetForPool(); }
    }
}
