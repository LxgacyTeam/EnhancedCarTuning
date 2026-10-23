using System;
using EnhancedCarTuning.Appearance.Body;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.UI;

namespace EnhancedCarTuning.Appearance.UI
{
    // One native color picker layout serves body parts and the two rim submeshes.
    internal sealed class PartPaintBinding
    {
        internal MenuColor ColorControl;
        internal int Count;
        internal string Prefix;
        internal Func<int> Available;
        internal Func<bool> CanEdit;
        internal Func<PaintFinish> Base;
        internal Func<int, PaintFinish> Get;
        internal Func<int, string> Label;
        internal Action Scan;
        internal Action<int, PaintFinish> Paint;
        internal static PartPaintBinding Body(CarGarageUI garage, BodyPaintSession session) => new PartPaintBinding {
            ColorControl = garage.bodyColor, Count = BodyPaintSettings.Count, Prefix = "Body", Available = () => session.State.Renderer?.AvailableMask ?? 0,
            CanEdit = () => session.CanEdit, Base = () => session.Base, Get = i => session.Get((BodyPart)i), Label = i => BodyPartNames.Label((BodyPart)i),
            Scan = () => session.State.Renderer?.ScanNow(), Paint = session.Paint };
        internal static PartPaintBinding Rims(CarGarageUI garage, GarageSession session, Func<AxleTarget> target)
        {
            Func<AxleSettings> axle = () => target() == AxleTarget.Rear ? session.Selection.Draft.Wheels.Rear : session.Selection.Draft.Wheels.Front;
            var original = PaintFinish.Capture(garage.carMat.wheel);
            Func<PaintFinish> fallback = () => garage.GetComponent<GarageTuningPanel>()?.WheelPaintBase ?? original;
            return new PartPaintBinding { ColorControl = garage.wheelColor, Count = 2, Prefix = "Rim",
                Available = () => {
                    var wheels = session.Selection.Draft.Wheels; var selected = target();
                    bool custom = selected != AxleTarget.Rear && wheels.Front.ModelId.StartsWith("rim_", StringComparison.Ordinal) || selected != AxleTarget.Front && wheels.Rear.ModelId.StartsWith("rim_", StringComparison.Ordinal);
                    return custom ? 3 : 0;
                }, CanEdit = () => session.CanEdit, Base = fallback,
                Get = i => (i == 0 ? axle().OuterFinish : axle().InnerFinish) ?? fallback(), Label = i => i == 0 ? "ВНЕШНЯЯ ЧАСТЬ" : "ВНУТРЕННЯЯ ЧАСТЬ",
                Scan = () => { }, Paint = (mask, finish) => session.SelectRimPaint(target(),mask,finish,fallback()) };
        }
    }
}
