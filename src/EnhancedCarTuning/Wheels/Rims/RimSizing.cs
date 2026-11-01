using System;

namespace EnhancedCarTuning.Wheels.Rims
{
    internal static class RimSizing
    {
        internal static float ScaleDiameter(float diameter,float oldWheelRadius,float newWheelRadius)
        {return UnityEngine.Mathf.Clamp(oldWheelRadius>.0001f?diameter*newWheelRadius/oldWheelRadius:newWheelRadius*1.44f,newWheelRadius*.6f,newWheelRadius*1.88f);}
        internal static float LocalRadius(float wheelRadius, float worldScale, float? diameterCm)
        {
            float requested = diameterCm.HasValue ? diameterCm.Value / (200 * Math.Max(Math.Abs(worldScale), .0001f)) : wheelRadius * .72f;
            return Math.Max(wheelRadius * .3f, Math.Min(wheelRadius * .94f, requested));
        }
    }
}
