using System;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Appearance.Police;
using EnhancedCarTuning.UI;

namespace EnhancedCarTuning
{
    public static class VinylPortBridge
    {
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
                return garage.car.GetComponent<CarSaveLoad>().SaveXml().OuterXml;
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
        }
    }
}
