using System;

namespace EnhancedCarTuning.Wheels.Geometry
{
    // Display-only support plane, calibrated to the native stock podium height.
    // Does not simulate the world's rigid bodies or change the car's spawn height.
    internal struct PreviewSupportPlane
    {
        private int count;
        private double z, height, zz, zh;

        internal void Add(float wheelZ, float heightChange)
        {
            count++; z += wheelZ; height += heightChange;
            zz += wheelZ * (double)wheelZ; zh += wheelZ * (double)heightChange;
        }

        internal void Solve(float stockHeight, out float lift, out float pitch)
        {
            lift = pitch = 0;
            if (count == 0) return;
            double denominator = zz - z * z / count;
            double slope = denominator > 0.000001 ? (zh - z * height / count) / denominator : 0;
            double intercept = (height - slope * z) / count;
            double angle = -Math.Atan(slope);
            pitch = (float)(angle * 180 / Math.PI);
            lift = (float)(Math.Cos(angle) * (stockHeight + intercept) - stockHeight);
        }

        internal static float HeightChange(float stockHeight, float mountY, float oldRadius, float oldTravel,
            float oldStiffness, float radius, float travel, float stiffness, float offsetY)
        {
            // Recover the stock displayed spring extension from buttomY, then preserve its load.
            float oldLength = Math.Max(0, Math.Min(oldTravel, stockHeight + mountY - oldRadius));
            float compression = oldTravel > 0.00001f ? 1 - oldLength / oldTravel : 0;
            float newCompression = Math.Max(0, Math.Min(1, compression * oldStiffness / Math.Max(stiffness, 0.00001f)));
            return radius - oldRadius + travel * (1 - newCompression) - oldLength - offsetY;
        }
    }
}
