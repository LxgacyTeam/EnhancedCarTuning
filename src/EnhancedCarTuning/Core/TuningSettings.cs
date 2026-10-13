using EnhancedCarTuning.TuneUp.Drive;
using System;
using EnhancedCarTuning.Wheels.Suspension;

namespace EnhancedCarTuning.Core
{
    internal sealed class TuningSettings : IEquatable<TuningSettings>
    {
        internal const string StockEngine = "stock";
        internal static readonly TuningSettings Stock = new TuningSettings(StockEngine, DriveLayout.Stock);
        internal string EngineId { get; }
        internal DriveLayout Drive { get; }
        internal WheelSettings Wheels { get; }
        internal bool IsStock => Equals(Stock);

        internal TuningSettings(string engineId, DriveLayout drive, WheelSettings wheels = null)
        {
            EngineId = string.IsNullOrEmpty(engineId) ? StockEngine : engineId;
            Drive = drive;
            Wheels = wheels ?? WheelSettings.Stock;
        }

        public bool Equals(TuningSettings other) => other != null &&
            string.Equals(EngineId, other.EngineId, StringComparison.Ordinal) && Drive == other.Drive && Wheels.Equals(other.Wheels);
        public override bool Equals(object obj) => Equals(obj as TuningSettings);
        public override int GetHashCode() => (EngineId.GetHashCode() * 397 ^ (int)Drive) * 397 ^ Wheels.GetHashCode();
    }
}
