using System.Globalization;
using System.Xml;

namespace EnhancedCarTuning.Wheels.Suspension
{
    internal static class WheelXml
    {
        internal static bool Read(XmlElement tuning, out WheelSettings settings)
        {
            settings = WheelSettings.Stock;
            XmlElement wheels = null;
            foreach (XmlNode node in tuning.ChildNodes)
                if (node is XmlElement element && element.Name == "Wheels")
                { if (wheels != null) return false; wheels = element; }
            if (wheels == null) return true;
            var front = AxleSettings.Stock; var rear = AxleSettings.Stock;
            bool hasFront = false, hasRear = false;
            foreach (XmlNode node in wheels.ChildNodes)
            {
                if (!(node is XmlElement axle)) continue;
                if (axle.Name != "Front" && axle.Name != "Rear") return false;
                bool isRear = axle.Name == "Rear";
                if (isRear ? hasRear : hasFront) return false;
                var values = AxleSettings.Stock;
                foreach (XmlAttribute attr in axle.Attributes)
                {
                    if (!System.Enum.TryParse(attr.Name, out WheelParameter p) || p.ToString() != attr.Name || (int)p > (int)WheelParameter.Camber) return false;
                    if (!float.TryParse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || !WheelLimits.Valid(p, v)) return false;
                    values = values.With(p, v);
                }
                if (isRear) { rear = values; hasRear = true; } else { front = values; hasFront = true; }
            }
            settings = new WheelSettings(front, rear); return true;
        }

        internal static void Write(XmlElement tuning, WheelSettings settings)
        {
            if (!HasParameters(settings.Front) && !HasParameters(settings.Rear)) return;
            var wheels = tuning.OwnerDocument.CreateElement("Wheels"); tuning.AppendChild(wheels);
            WriteAxle(wheels, "Front", settings.Front); WriteAxle(wheels, "Rear", settings.Rear);
        }
        private static void WriteAxle(XmlElement wheels, string name, AxleSettings settings)
        {
            if (!HasParameters(settings)) return;
            var axle = wheels.OwnerDocument.CreateElement(name);
            foreach (var p in WheelLimits.Parameters)
                if (settings.Get(p).HasValue) axle.SetAttribute(p.ToString(), settings.Get(p).Value.ToString("R", CultureInfo.InvariantCulture));
            wheels.AppendChild(axle);
        }
        private static bool HasParameters(AxleSettings settings)
        { foreach (var p in WheelLimits.Parameters) if (settings.Get(p).HasValue) return true; return false; }
    }
}
