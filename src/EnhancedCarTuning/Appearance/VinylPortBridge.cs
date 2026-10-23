using System;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using EnhancedCarTuning.UI;

namespace EnhancedCarTuning
{
    public static class VinylPortBridge
    {
        private static CarControl paintTarget;
        internal static bool IsApplyingPaint(CarControl car) { return paintTarget && paintTarget == car; }

        public static void ApplyGaragePaintXml(CarGarageUI garage, string xml)
        {
            if (!garage || !garage.car) throw new InvalidOperationException("No garage car");
            var document = new System.Xml.XmlDocument { XmlResolver = null };
            using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml), new System.Xml.XmlReaderSettings {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2 * 1024 * 1024 })) document.Load(reader);
            var panel = garage.GetComponent<GarageTuningPanel>();
            panel?.FlushVinyls(); panel?.FlushBodyPaint();
            garage.CarPaint_Finish();
            var previous = paintTarget;
            try
            {
                paintTarget = garage.car;
                garage.car.GetComponent<CarMaterial>().SaveLoad(document.DocumentElement, false);
                garage.car.paintItems.SaveLoad(document.DocumentElement, false);
                garage.car.paintItems.UpImage();
                panel?.BodyPaintSession?.RebaseImported();
                panel?.RefreshImportedBodyPaint(true);
            }
            finally { paintTarget = previous; }
        }

        public static string CaptureGarageXml(CarGarageUI garage)
        {
            if (!garage || !garage.car) throw new InvalidOperationException("No garage car");
            var panel = garage.GetComponent<GarageTuningPanel>();
            panel?.FlushVinyls(); panel?.FlushBodyPaint();
            var state = garage.car.GetComponent<BodyPaintState>();
            var police = garage.car.GetComponent<PolicePaintState>();
            bool previous = state && state.SerializePreview;
            bool previousPolice = police && police.SerializePreview;
            try
            {
                if (state) state.SerializePreview = true;
                if (police) police.SerializePreview = true;
                var glass = garage.car.GetComponent<EnhancedCarTuning.Appearance.Glass.GlassPrototypeState>();
                bool oldGlass = glass && glass.SerializePreview;
                try { if (glass) glass.SerializePreview = true; return garage.car.GetComponent<CarSaveLoad>().SaveXml().OuterXml; }
                finally { if (glass) glass.SerializePreview = oldGlass; }
            }
            finally { if (state) state.SerializePreview = previous; if (police) police.SerializePreview = previousPolice; }
        }

        public static void ApplyGarageXml(CarGarageUI garage, string xml)
        {
            if (!garage || !garage.car) throw new InvalidOperationException("No garage car");
            var panel = garage.GetComponent<GarageTuningPanel>();
            panel?.FlushVinyls(); panel?.FlushBodyPaint();
            garage.CarPaint_Finish();
            garage.car.GetComponent<CarSaveLoad>().LoadXml(xml);
            panel?.BodyPaintSession?.RebaseImported();
            panel?.PolicePaintSession?.RebaseImported();
            panel?.RefreshImportedBodyPaint();
            garage.GetComponent<EnhancedCarTuning.Appearance.Glass.GlassGaragePrototype>()?.RebaseImported();
        }
    }
}
