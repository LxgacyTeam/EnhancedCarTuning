using System;
using EnhancedCarTuning.Appearance.Body;

namespace EnhancedCarTuning.Wheels.Suspension
{
    internal enum AxleTarget { Both, Front, Rear }
    internal enum WheelParameter { Radius, Width, OffsetX, OffsetY, Travel, Stiffness, Camber, RimDiameter, RimWidth }

    internal static class WheelLimits
    {
        internal static readonly WheelParameter[] Parameters = new[] { WheelParameter.Radius, WheelParameter.Width, WheelParameter.OffsetX, WheelParameter.OffsetY, WheelParameter.Travel, WheelParameter.Stiffness, WheelParameter.Camber };
        internal static readonly WheelParameter[] AllParameters = (WheelParameter[])Enum.GetValues(typeof(WheelParameter));
        internal static float Min(WheelParameter p)
        {
            switch (p) { case WheelParameter.RimDiameter: return 1; case WheelParameter.RimWidth: return 10; case WheelParameter.Radius: return 22; case WheelParameter.Width: return 17;
                case WheelParameter.OffsetX: case WheelParameter.Camber: return -20;
                case WheelParameter.OffsetY: return -25; default: return 5; }
        }
        internal static float Max(WheelParameter p)
        {
            switch (p) { case WheelParameter.RimDiameter: return 200; case WheelParameter.RimWidth: return 60; case WheelParameter.Radius: return 100; case WheelParameter.Width: return 38;
                case WheelParameter.Travel: return 16; case WheelParameter.Stiffness: return 30;
                case WheelParameter.Camber: return 20; default: return 15; }
        }
        internal static bool Valid(WheelParameter p, float v) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= Min(p) && v <= Max(p);
    }

    // Null means the original value of each individual wheel, including values outside the editor's range.
    internal sealed class AxleSettings : IEquatable<AxleSettings>
    {
        internal static readonly AxleSettings Stock = new AxleSettings(new float?[9]);
        private readonly float?[] values;
        internal string ModelId { get; }
        internal PaintFinish OuterFinish { get; }
        internal PaintFinish InnerFinish { get; }
        private AxleSettings(float?[] values, string modelId = "stock", PaintFinish outer = null, PaintFinish inner = null)
        { this.values = values; ModelId = modelId; OuterFinish = outer; InnerFinish = inner; }
        internal float? Get(WheelParameter p) => values[(int)p];
        internal AxleSettings With(WheelParameter p, float value)
        {
            if (!WheelLimits.Valid(p, value)) throw new ArgumentOutOfRangeException(nameof(value));
            var copy = (float?[])values.Clone(); copy[(int)p] = value; return new AxleSettings(copy, ModelId, OuterFinish, InnerFinish);
        }
        internal AxleSettings WithModel(string id) => new AxleSettings((float?[])values.Clone(), string.IsNullOrEmpty(id) ? "stock" : id, OuterFinish, InnerFinish);
        internal AxleSettings WithPaint(int mask, PaintFinish finish) => new AxleSettings((float?[])values.Clone(), ModelId, (mask & 1) != 0 ? finish : OuterFinish, (mask & 2) != 0 ? finish : InnerFinish);
        internal bool IsStock => Equals(Stock);
        internal bool PhysicalEquals(AxleSettings other)
        { if (other == null) return false; foreach (var p in WheelLimits.Parameters) if (Get(p) != other.Get(p)) return false; return true; }
        public bool Equals(AxleSettings other)
        {
            if (other == null || ModelId != other.ModelId || !Equals(OuterFinish, other.OuterFinish) || !Equals(InnerFinish, other.InnerFinish)) return false;
            foreach (var p in WheelLimits.AllParameters) if (Get(p) != other.Get(p)) return false;
            return true;
        }
        public override bool Equals(object other) => Equals(other as AxleSettings);
        public override int GetHashCode() { int hash = ModelId.GetHashCode(); foreach (var v in values) hash = hash * 397 ^ v.GetHashCode(); return hash; }
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
        internal WheelSettings WithModel(AxleTarget axle, string id) => new WheelSettings(
            axle == AxleTarget.Rear ? Front : Front.WithModel(id), axle == AxleTarget.Front ? Rear : Rear.WithModel(id));
        internal WheelSettings WithPaint(AxleTarget axle, int mask, PaintFinish finish) => new WheelSettings(
            axle == AxleTarget.Rear ? Front : Front.WithPaint(mask, finish), axle == AxleTarget.Front ? Rear : Rear.WithPaint(mask, finish));
        internal WheelSettings Reset(AxleTarget axle) => new WheelSettings(
            axle == AxleTarget.Rear ? Front : AxleSettings.Stock, axle == AxleTarget.Front ? Rear : AxleSettings.Stock);
        public bool Equals(WheelSettings other) => other != null && Front.Equals(other.Front) && Rear.Equals(other.Rear);
        public override bool Equals(object other) => Equals(other as WheelSettings);
        public override int GetHashCode() => Front.GetHashCode() * 397 ^ Rear.GetHashCode();
    }
}
