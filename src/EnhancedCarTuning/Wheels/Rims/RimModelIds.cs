using System;
namespace EnhancedCarTuning.Wheels.Rims
{
    internal static class RimModelIds
    {
        internal static readonly string[] All = { "rim_01", "rim_02", "rim_03", "rim_05", "rim_06", "rim_07", "rim_08", "rim_09", "rim_10", "rim_12", "rim_13", "rim_14", "rim_15", "rim_16", "rim_17", "rim_18", "rim_19", "rim_20", "rim_21", "rim_22", "rim_24", "rim_25", "rim_26", "rim_27", "rim_28", "rim_29", "rim_30", "rim_31", "rim_32", "rim_33", "rim_34", "rim_35", "rim_36", "rim_37", "rim_38", "rim_39", "rim_40", "rim_41", "rim_42", "rim_43", "rim_44" };
        internal static bool Contains(string id) => Array.IndexOf(All, id) >= 0;
    }
}
