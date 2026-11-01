using System;
namespace EnhancedCarTuning.Core
{
    internal static class CarIdentity
    {
        internal static string Name(CarControl car)
        {
            if(!car)return "";
            string name=string.IsNullOrEmpty(car.prefabName)?car.name:car.prefabName;
            return name.Replace("(Clone)","").Trim();
        }
    }
}
