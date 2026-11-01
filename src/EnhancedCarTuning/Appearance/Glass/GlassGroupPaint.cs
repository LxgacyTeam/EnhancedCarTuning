using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Glass
{
    internal enum GlassGroup { Front, Back, Middle, LampsFront, LampsBack, Sunroof }
    internal sealed class GlassGroupPaint
    {
        internal const int GroupCount=6;
        internal static bool IsLamp(GlassGroup group)=>group==GlassGroup.LampsFront||group==GlassGroup.LampsBack;
        internal readonly Color Tint;
        internal readonly GlassFinish Finish;
        internal GlassGroupPaint(Color tint, GlassFinish finish) { Tint = tint; Finish = finish; }
        internal static Dictionary<GlassGroup, GlassGroupPaint> Copy(Dictionary<GlassGroup, GlassGroupPaint> source) => new Dictionary<GlassGroup, GlassGroupPaint>(source);
        internal static bool Same(Dictionary<GlassGroup, GlassGroupPaint> a, Dictionary<GlassGroup, GlassGroupPaint> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var entry in a)
            {
                if (!b.TryGetValue(entry.Key, out var value)) return false;
                var tint = entry.Value.Tint; var other = value.Tint;
                if (Math.Abs(tint.r-other.r)>.001f || Math.Abs(tint.g-other.g)>.001f || Math.Abs(tint.b-other.b)>.001f || Math.Abs(tint.a-other.a)>.001f ||
                    !GlassFinish.Same(entry.Value.Finish,value.Finish)) return false;
            }
            return true;
        }
        internal static string Label(GlassGroup group) => group == GlassGroup.Front ? "Перед" : group == GlassGroup.Back ? "Зад" : group==GlassGroup.Sunroof?"Люк":"Середина";
    }
}
