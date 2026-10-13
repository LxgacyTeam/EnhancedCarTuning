using EnhancedCarTuning.TuneUp.Drive;
using System;
using System.IO;
using System.Xml;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.Appearance.Vinyls;
using EnhancedCarTuning.Appearance.Body;

namespace EnhancedCarTuning.Persistence
{
    internal static class TuningXmlCodec
    {
        // Persistent XML node is deliberately left unchanged for backwards compatibility.
        internal const string ElementName = "ExtendedCarTuning";

        internal static bool TryRead(XmlElement root, out TuningSettings settings, out string error)
        {
            settings = TuningSettings.Stock;
            error = null;
            if (root == null) return true;
            if (!VinylXml.Validate(root)) { error = "Недопустимые текстовые винилы или симметрия."; return false; }
            if (!BodyPaintXml.TryRead(root, out _)) { error = "Недопустимые параметры покраски кузова."; return false; }
            XmlElement tuning = null;
            foreach (XmlNode child in root.ChildNodes)
            {
                if (child.Name != ElementName || !(child is XmlElement element)) continue;
                if (tuning != null) { error = "Повторяющийся элемент тюнинга."; return false; }
                tuning = element;
            }
            string engine, drive;
            if (tuning != null)
            {
                if (tuning.GetAttribute("version") != "1" && tuning.GetAttribute("version") != "2")
                { error = "Неизвестная версия тюнинга."; return false; }
                engine = tuning.GetAttribute("engine");
                drive = tuning.GetAttribute("drive");
                if (engine.Length == 0 || drive.Length == 0) { error = "Неполные параметры тюнинга."; return false; }
            }
            else
            {
                // Import the MDMP XML attributes; never consult per-machine local preferences.
                engine = root.GetAttribute("EngineSel");
                drive = root.GetAttribute("DriveSel");
                if (engine.Length == 0) engine = TuningSettings.StockEngine;
                if (drive.Length == 0) drive = nameof(DriveLayout.Stock);
            }
            if (engine.Length > 128 || engine.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            { error = "Недопустимый идентификатор двигателя."; return false; }
            DriveLayout layout;
            if (!Enum.TryParse(drive, true, out layout) || !Enum.IsDefined(typeof(DriveLayout), layout) ||
                !string.Equals(layout.ToString(), drive, StringComparison.OrdinalIgnoreCase))
            { error = "Неизвестный тип привода."; return false; }
            var wheels = WheelSettings.Stock;
            if (tuning != null && !WheelXml.Read(tuning, out wheels))
            { error = "Недопустимые параметры колёс."; return false; }
            if (tuning != null && tuning.GetAttribute("version") == "1" && tuning.SelectSingleNode("Wheels") != null)
            { error = "Параметрам колёс требуется версия XML 2."; return false; }
            settings = new TuningSettings(engine, layout, wheels);
            return true;
        }

        internal static bool TryRead(string xml, out TuningSettings settings, out string error)
        {
            settings = TuningSettings.Stock;
            error = null;
            if (string.IsNullOrEmpty(xml)) return true;
            try
            {
                var document = new XmlDocument { XmlResolver = null };
                var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                    MaxCharactersInDocument = 2 * 1024 * 1024 };
                using (var input = new StringReader(xml))
                using (var reader = XmlReader.Create(input, options)) document.Load(reader);
                return TryRead(document.DocumentElement, out settings, out error);
            }
            catch (XmlException) { error = "Некорректный XML автомобиля."; return false; }
        }

        internal static void Write(XmlElement root, TuningSettings settings)
        {
            if (root == null) return;
            // Replace our own node only; preserve paint, vinyl, privacy and other plugins' data.
            for (int i = root.ChildNodes.Count - 1; i >= 0; i--)
                if (root.ChildNodes[i].Name == ElementName) root.RemoveChild(root.ChildNodes[i]);
            var element = root.OwnerDocument.CreateElement(ElementName);
            element.SetAttribute("version", "2");
            element.SetAttribute("engine", settings.EngineId);
            element.SetAttribute("drive", settings.Drive.ToString());
            WheelXml.Write(element, settings.Wheels);
            root.AppendChild(element);
        }
    }
}
