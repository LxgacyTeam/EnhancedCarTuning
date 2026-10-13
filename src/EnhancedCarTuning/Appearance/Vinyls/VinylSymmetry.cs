using System;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Vinyls
{
    internal static class VinylSymmetry
    {
        internal static Vector3 Reflect(Vector3 value) => new Vector3(-value.x, value.y, value.z);
        internal static bool HasSecondSurface(Vector3 position, Vector3 forward) =>
            Math.Abs(position.x) >= .001f || Math.Abs(forward.x) >= .001f;
    }
}
