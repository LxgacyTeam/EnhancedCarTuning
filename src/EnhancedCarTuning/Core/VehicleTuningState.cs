using EnhancedCarTuning.TuneUp.Drive;
using EnhancedCarTuning.TuneUp.Engine;
using System.Xml;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Persistence;
using UnityEngine;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.Wheels.Models;

namespace EnhancedCarTuning.Core
{
    // The native CarSaveLoad enumerates SaveLoad components, including this one.
    internal sealed class VehicleTuningState : MonoBehaviour, global::SaveLoad
    {
        internal CarControl Car { get; private set; }
        internal TuningSettings Committed { get; private set; } = TuningSettings.Stock;
        internal TuningSettings Active { get; private set; } = TuningSettings.Stock;
        internal bool SerializePreview { get; set; }
        internal WheelService WheelService { get; private set; }
        internal WheelModelService WheelModels { get; private set; }
        private EngineProfile stockEngine;
        private bool[] stockDrive;
        private bool initialized;

        internal static VehicleTuningState Ensure(CarControl car)
        {
            if (!car || !car.ph_car || !car.ph_car.engine || car.ph_car.wheels == null) return null;
            var state = car.GetComponent<VehicleTuningState>() ?? car.gameObject.AddComponent<VehicleTuningState>();
            if (!state.initialized)
            {
                state.Car = car;
                state.stockEngine = EngineProfile.Capture(car.ph_car.engine);
                state.stockDrive = DriveService.Capture(car);
                state.WheelService = new WheelService(car);
                state.WheelModels = new WheelModelService(car);
                state.initialized = true;
            }
            return state;
        }

        internal void Preview(TuningSettings settings)
        {
            if (!initialized || !Car || !Car.ph_car) return;
            if (settings.Equals(Active)) return;
            EngineProfile profile = stockEngine;
            if (settings.EngineId != TuningSettings.StockEngine)
            {
                if (!EngineCatalog.TryGet(settings.EngineId, out EngineOption option)) return;
                profile = option.Profile;
            }
            if (settings.EngineId != Active.EngineId)
            {
                EnsureOwnedEngine();
                profile.Apply(Car.ph_car.engine);
            }
            if (settings.Drive != Active.Drive) DriveService.Apply(Car, settings.Drive, stockDrive);
            if (!settings.Wheels.Equals(Active.Wheels)) { WheelService.Apply(settings.Wheels); WheelModels.Apply(settings.Wheels); }
            Active = settings;
        }

        private void EnsureOwnedEngine()
        {
            CarEngine engine = Car.ph_car.engine;
            if (engine && engine.transform.IsChildOf(Car.transform)) return;
            CarEngine template = engine ? engine : CarData.me.DefEngine;
            var copy = Object.Instantiate(template, Car.transform);
            copy.name = template.name + "_ECT";
            copy.transform.localPosition = Vector3.zero;
            copy.gameObject.SetActive(true);
            copy.Init(Car.ph_car);
            copy.SetState(engine ? engine.cur_state : CarEngine.State.NotEnabled);
            if (engine) { copy.rpm = engine.rpm; copy.cur_gear = engine.cur_gear; }
            Car.ph_car.engine = copy;
        }

        internal void ApplyCommitted(TuningSettings settings)
        {
            var wheels = settings.Wheels;
            if (!WheelModelCatalog.IsKnown(wheels.Front.ModelId)) wheels = wheels.WithModel(AxleTarget.Front, "stock");
            if (!WheelModelCatalog.IsKnown(wheels.Rear.ModelId)) wheels = wheels.WithModel(AxleTarget.Rear, "stock");
            if (!EnhancedCarTuning.Wheels.Tires.TireCatalog.IsKnown(wheels.Front.TireId)) wheels = wheels.WithTire(AxleTarget.Front, "stock");
            if (!EnhancedCarTuning.Wheels.Tires.TireCatalog.IsKnown(wheels.Rear.TireId)) wheels = wheels.WithTire(AxleTarget.Rear, "stock");
            if (!wheels.Equals(settings.Wheels)) settings = new TuningSettings(settings.EngineId, settings.Drive, wheels);
            if (!EngineCatalog.IsKnown(settings.EngineId))
            {
                Plugin.Warn("Unknown engine '" + settings.EngineId + "': restoring this vehicle's stock engine.");
                settings = new TuningSettings(TuningSettings.StockEngine, settings.Drive, settings.Wheels);
            }
            Preview(settings);
            Committed = settings;
        }

        internal void RestoreNativeTuning()
        {
            if (!initialized || !Car || !Car.ph_car) return;
            EngineProfile profile = stockEngine;
            if (Active.EngineId != TuningSettings.StockEngine)
            {
                if (!EngineCatalog.TryGet(Active.EngineId, out EngineOption option)) return;
                profile = option.Profile;
            }
            EnsureOwnedEngine(); profile.Apply(Car.ph_car.engine);
            DriveService.Apply(Car, Active.Drive, stockDrive);
        }

        public void SaveLoad(XmlElement root, bool isSave)
        {
            if (!initialized) return;
            if (isSave) { TuningXmlCodec.Write(root, SerializePreview ? Active : Committed); return; }
            if (!TuningXmlCodec.TryRead(root, out TuningSettings settings, out string error))
            {
                Plugin.Warn(error + " Restoring stock tuning.");
                settings = TuningSettings.Stock;
            }
            ApplyCommitted(settings);
        }

        internal void ResetForPool()
        {
            // Pooled cars can later belong to another player and arrive without any tuning XML.
            Preview(TuningSettings.Stock);
            Committed = TuningSettings.Stock;
            SerializePreview = false;
        }

        private void LateUpdate() { WheelModels?.Refresh(); }
        private void OnDestroy() { WheelModels?.Dispose(); WheelService?.Dispose(); }
    }
}
