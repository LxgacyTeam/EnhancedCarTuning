using System;

namespace EnhancedCarTuning.TuneUp.Engine
{
    internal static class EngineOrder
    {
        // MadOut2 9.4 preset progression. These are preset names, not measured horsepower.
        private static readonly string[] progression =
        {
            "Engine.Tractor", "Engine.70hp.Gaz", "Engine.120hp", "Engine.200hp",
            "Engine.300hp", "Engine.300hp.Turbo", "Engine.500hp_MusculCar"
        };

        internal static int Compare(string first, string second)
        {
            int a = Array.IndexOf(progression, first), b = Array.IndexOf(progression, second);
            if (a < 0) a = int.MaxValue;
            if (b < 0) b = int.MaxValue;
            int result = a.CompareTo(b);
            return result != 0 ? result : string.Compare(first, second, StringComparison.OrdinalIgnoreCase);
        }
    }
}
