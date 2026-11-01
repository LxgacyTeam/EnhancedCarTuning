using System;
using EnhancedCarTuning.Wheels.Models;

namespace EnhancedCarTuning.Wheels.Tires
{
    internal static class TireCatalog
    {
        internal static readonly string[] Ids = { "tire_04", "tire_05", "tire_06", "tire_07", "tire_08", "tire_09", "tire_1", "tire_10", "tire_11", "tire_12", "tire_13", "tire_14", "tire_15", "tire_16", "tire_17", "tire_18", "tire_19", "tire_1_2", "tire_2", "tire_20", "tire_21", "tire_22", "tire_23", "tire_2_2" };
        internal static bool IsKnown(string id) => id == "stock" || Array.IndexOf(Ids, id) >= 0;
        internal static string Label(string id) => id == "stock" ? "Штатные" : "Whell_Tire_" + id.Substring(5);
        internal static string Step(string id, int direction)
        {
            int index = WheelModelCycle.Step(Array.IndexOf(Ids, id) + 1, Ids.Length + 1, direction);
            return index == 0 ? "stock" : Ids[index - 1];
        }
    }
}
