using System;
using System.Collections.Generic;

namespace EnhancedCarTuning.Wheels.Suspension
{
    internal static class WheelVehicleLimits
    {
        private static readonly HashSet<string> large = new HashSet<string>(StringComparer.Ordinal)
        { "Q7", "Tahoe", "Jeep_Wran", "H2", "Picap", "Zil_130", "TruckMedium", "Avalanche", "Kamaz_5320" };
        internal static float ForCar(CarControl car) => car ? MaxRadius(string.IsNullOrEmpty(car.prefabName) ? car.name : car.prefabName) : 42;
        internal static float MaxRadius(string prefabName)
        {
            string name = (prefabName ?? "").Trim();
            if (name.EndsWith("(Clone)", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 7).TrimEnd();
            return name == "Traktor" ? 100 : large.Contains(name) ? 50 : 42;
        }
    }
}
