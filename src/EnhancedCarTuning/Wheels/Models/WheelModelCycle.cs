namespace EnhancedCarTuning.Wheels.Models
{
    internal static class WheelModelCycle
    {
        internal static int Direction(float normalizedX) => normalizedX < .5f ? -1 : 1;
        internal static int Step(int index, int count, int direction)
        {
            if (count <= 1) return 0;
            return ((index + (direction < 0 ? -1 : 1)) % count + count) % count;
        }
    }
}
