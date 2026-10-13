using System;

namespace EnhancedCarTuning.Wheels.Suspension
{
    internal enum AxleTarget { Both, Front, Rear }
    internal enum WheelParameter { Radius, Width, OffsetX, OffsetY, Travel, Stiffness, Camber }

    internal static class WheelLimits
    {
        internal static readonly WheelParameter[] Parameters = (WheelParameter[])Enum.GetValues(typeof(WheelParameter));
        internal static float Min(WheelParameter p)
        {
            switch (p) { case WheelParameter.Radius: return 22; case WheelParameter.Width: return 17;
                case WheelParameter.OffsetX: case WheelParameter.Camber: return -20;
                case WheelParameter.OffsetY: return -25; default: return 5; }
        }
        internal static float Max(WheelParameter p)
        {
            switch (p) { case WheelParameter.Radius: return 42; case WheelParameter.Width: return 38;
                case WheelParameter.Travel: return 16; case WheelParameter.Stiffness: return 30;
                case WheelParameter.Camber: return 20; default: return 15; }
        }
        internal static bool Valid(WheelParameter p, float v) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= Min(p) && v <= Max(p);
    }

    // Null means the original value of each individual wheel, including values outside the editor's range.
    internal sealed class AxleSettings : IEquatable<AxleSettings>
    {
        internal static readonly AxleSettings Stock = new AxleSettings(new float?[7]);
        private readonly float?[] values;
        private AxleSettings(float?[] values) { this.values = values; }
        internal float? Get(WheelParameter p) => values[(int)p];
        internal AxleSettings With(WheelParameter p, float value)
        {
            if (!WheelLimits.Valid(p, value)) throw new ArgumentOutOfRangeException(nameof(value));
            var copy = (float?[])values.Clone(); copy[(int)p] = value; return new AxleSettings(copy);
        }
        internal bool IsStock => Equals(Stock);
        public bool Equals(AxleSettings other)
        {
            if (other == null) return false;
            foreach (var p in WheelLimits.Parameters) if (Get(p) != other.Get(p)) return false;
            return true;
        }
        public override bool Equals(object other) => Equals(other as AxleSettings);
        public override int GetHashCode() { int hash = 0; foreach (var v in values) hash = hash * 397 ^ v.GetHashCode(); return hash; }
    }

    internal sealed class WheelSettings : IEquatable<WheelSettings>
    {
        internal static readonly WheelSettings Stock = new WheelSettings(AxleSettings.Stock, AxleSettings.Stock);
        internal AxleSettings Front { get; }
        internal AxleSettings Rear { get; }
        internal WheelSettings(AxleSettings front, AxleSettings rear) { Front = front; Rear = rear; }
        internal AxleSettings For(bool rear) => rear ? Rear : Front;
        internal WheelSettings With(AxleTarget axle, WheelParameter p, float value) => new WheelSettings(
            axle == AxleTarget.Rear ? Front : Front.With(p, value), axle == AxleTarget.Front ? Rear : Rear.With(p, value));
        internal WheelSettings Reset(AxleTarget axle) => new WheelSettings(
            axle == AxleTarget.Rear ? Front : AxleSettings.Stock, axle == AxleTarget.Front ? Rear : AxleSettings.Stock);
        public bool Equals(WheelSettings other) => other != null && Front.Equals(other.Front) && Rear.Equals(other.Rear);
        public override bool Equals(object other) => Equals(other as WheelSettings);
        public override int GetHashCode() => Front.GetHashCode() * 397 ^ Rear.GetHashCode();
    }
}
