using UnityEngine;

namespace EnhancedCarTuning.Wheels.Rims
{
    internal static class WheelSideResolver
    {
        internal static bool NativeVariant(CarPhWheel wheel)=>NativeVariant(wheel.IsLeft,wheel.create_rot);
        internal static bool CustomVariant(CarPhWheel wheel)=>CustomVariant(wheel.IsLeft,wheel.create_rot);
        internal static bool NativeVariant(bool isLeft,Quaternion initialLocalRotation)
        {
            // CarControl's root is separate from the rotating Body/View objects.
            // Init captures this local pose before steering and wheel spin change it.
            bool reversed=(initialLocalRotation*Vector3.right).x<0;
            return isLeft!=reversed;
        }
        internal static bool CustomVariant(bool isLeft,Quaternion initialLocalRotation)
        {
            // Bundled SB /left addresses contain source _R and face +X, whereas
            // native left-wheel meshes face -X. Keep the existing bundles and map
            // their addresses to the wheel's physical side explicitly.
            return !NativeVariant(isLeft,initialLocalRotation);
        }
    }
}
