using UnityEngine;

namespace EnhancedCarTuning.Wheels.Models
{
    internal static class WheelModelGeometry
    {
        internal static bool Valid(Vector3 size) => FinitePositive(size.x) && FinitePositive(size.y) && FinitePositive(size.z);
        private static bool FinitePositive(float value) => value > .0001f && !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Fit(Bounds target, Bounds donor, out Vector3 scale, out Vector3 offset)
        {
            scale = Vector3.one; offset = Vector3.zero;
            if (!Valid(target.size) || !Valid(donor.size)) return false;
            scale = new Vector3(target.size.x / donor.size.x, target.size.y / donor.size.y, target.size.z / donor.size.z);
            offset = target.center - Vector3.Scale(donor.center, scale);
            return true;
        }
    }
}
