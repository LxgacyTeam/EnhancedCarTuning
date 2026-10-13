using EnhancedCarTuning.TuneUp.Drive;
using EnhancedCarTuning.Wheels.Suspension;

namespace EnhancedCarTuning.Core
{
    // A preview is never itself a saved purchase.
    internal sealed class GarageSelection
    {
        internal TuningSettings Original { get; }
        internal TuningSettings Draft { get; private set; }
        internal bool EngineChanged => Draft.EngineId != Original.EngineId;
        internal bool DriveChanged => Draft.Drive != Original.Drive;
        internal bool FrontChanged => !Draft.Wheels.Front.Equals(Original.Wheels.Front);
        internal bool RearChanged => !Draft.Wheels.Rear.Equals(Original.Wheels.Rear);
        internal bool HasChanges => !Draft.Equals(Original);

        internal GarageSelection(TuningSettings original) { Original = Draft = original; }
        internal void SelectEngine(string id) { Draft = new TuningSettings(id, Draft.Drive, Draft.Wheels); }
        internal void SelectDrive(DriveLayout drive) { Draft = new TuningSettings(Draft.EngineId, drive, Draft.Wheels); }
        internal void SelectWheels(WheelSettings wheels) { Draft = new TuningSettings(Draft.EngineId, Draft.Drive, wheels); }
        internal void Reset() { Draft = Original; }
        internal TuningSettings ResolvePurchase(bool buyEngine, bool buyDrive, bool buyFront = false, bool buyRear = false) => new TuningSettings(
            buyEngine ? Draft.EngineId : Original.EngineId, buyDrive ? Draft.Drive : Original.Drive,
            new WheelSettings(buyFront ? Draft.Wheels.Front : Original.Wheels.Front, buyRear ? Draft.Wheels.Rear : Original.Wheels.Rear));
    }
}
