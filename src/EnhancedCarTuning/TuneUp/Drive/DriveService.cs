using EnhancedCarTuning.Core;

namespace EnhancedCarTuning.TuneUp.Drive
{
    internal static class DriveService
    {
        internal static bool[] Capture(CarControl car)
        {
            var wheels = car.ph_car.wheels;
            var result = new bool[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) result[i] = wheels[i] && wheels[i].is_leading;
            return result;
        }

        internal static void Apply(CarControl car, DriveLayout layout, bool[] stock)
        {
            var wheels = car.ph_car.wheels;
            int leading = 0;
            for (int i = 0; i < wheels.Length; i++)
            {
                CarPhWheel wheel = wheels[i];
                if (!wheel) continue;
                switch (layout)
                {
                    case DriveLayout.Stock: wheel.is_leading = i < stock.Length && stock[i]; break;
                    case DriveLayout.FWD: wheel.is_leading = !wheel.IsBack; break;
                    case DriveLayout.RWD: wheel.is_leading = wheel.IsBack; break;
                    case DriveLayout.AWD: wheel.is_leading = true; break;
                }
                if (wheel.is_leading) leading++;
                // CarWheel.Init copies its own flag back to CarPhWheel on initialization.
                if (car.wheels != null)
                    foreach (CarWheel view in car.wheels)
                        if (view && view.ph_wheel == wheel) view.is_leading = wheel.is_leading;
            }
            car.ph_car.leadingWheelsMul = leading == 0 ? 0f : 1f / leading;
        }
    }
}
