using EnhancedCarTuning.TuneUp.Engine;
using BepInEx;
using BepInEx.Bootstrap;
using EnhancedCarTuning.Configuration;
using EnhancedCarTuning.Patches;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.UI;
using HarmonyLib;
using UnityEngine;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;

namespace EnhancedCarTuning
{
    [BepInPlugin(Id, "EnhancedCarTuning", "1.0.0")]
    [BepInDependency("com.bigcitylegacy.baseplugin", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "com.bigcitylegacy.enhancedcartuning";
        private static Plugin instance;
        private Harmony harmony;

        private void Awake()
        {
            instance = this;
            if (Application.version != "9.4")
            {
                Logger.LogError("This build targets MadOut2 BCO 9.4. Detected: " + Application.version);
                return;
            }
            if (!Chainloader.PluginInfos.ContainsKey("com.bigcitylegacy.baseplugin"))
            {
                Logger.LogError("EnhancedCarTuning requires BigCityLegacy.");
                return;
            }
            harmony = new Harmony(Id);
            try
            {
                TuningGraphicsSettings.Register();
                PatchInstaller.Install(harmony);
                Logger.LogInfo("EnhancedCarTuning loaded: TUNE-UP, wheels, body-part paint, text vinyls, symmetry and synchronized police equipment.");
                if (NetManager.isServer || NetManagerTools.isCommandLineArgHaveServerStr())
                    LegacyServerConsole.LogAfterConsoleInit(
                        "EnhancedCarTuning started; vehicle tuning synchronization is active on this server.", LogType.Log);
            }
            catch (System.Exception error)
            {
                harmony.UnpatchSelf();
                Logger.LogError("Patch installation failed; all EnhancedCarTuning patches removed. " + error);
            }
        }

        internal static void Warn(string message) { if (instance) instance.Logger.LogWarning(message); }
        private float nextCacheTrim;
        private void Update()
        {
            if (Time.unscaledTime < nextCacheTrim) return;
            nextCacheTrim = Time.unscaledTime + 10; TextVinylTextures.Trim(); VinylRenderTextures.Trim();
        }

        private void OnDestroy()
        {
            foreach (var panel in UnityEngine.Resources.FindObjectsOfTypeAll<GarageTuningPanel>())
            { panel.Cancel(); Destroy(panel); }
            foreach (var state in UnityEngine.Resources.FindObjectsOfTypeAll<VehicleTuningState>())
            { state.ResetForPool(); Destroy(state); }
            foreach (var police in UnityEngine.Resources.FindObjectsOfTypeAll<PolicePaintState>())
            {
                police.ResetForPool();
                if (police.CarMaterial && !NetManager.isServer) police.CarMaterial.Apply();
                Destroy(police);
            }
            foreach (var paint in UnityEngine.Resources.FindObjectsOfTypeAll<BodyPaintState>())
            { paint.ResetForPool(); Destroy(paint); }
            VinylRenderTextures.Clear();
            harmony?.UnpatchSelf();
            EngineCatalog.Reset();
            TextVinylTextures.Clear();
            PrimitiveVinylTextures.Clear();
            EmbeddedAssets.Clear();
            instance = null;
        }
    }
}

