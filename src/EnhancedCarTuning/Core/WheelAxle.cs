using System;
namespace EnhancedCarTuning.Core
{
    internal static class WheelAxle
    {
        internal static bool IsRear(CarControl car, CarPhWheel wheel) => IsRear(CarIdentity.Name(car), wheel.name, wheel.IsBack);
        internal static bool IsRear(string car, string wheel, bool original)
        {
            if (!string.Equals(car, "Kamaz_5320", StringComparison.OrdinalIgnoreCase)) return original;
            return Back(wheel, "Wheel_R_Back") || Back(wheel, "Wheel_L_Back") || original;
        }
        private static bool Back(string name, string token) => string.Equals(name,token,StringComparison.OrdinalIgnoreCase) ||
            (name ?? "").StartsWith(token + " (",StringComparison.OrdinalIgnoreCase);
    }
}
