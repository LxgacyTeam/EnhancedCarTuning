using System;
using System.Collections.Generic;
using EnhancedCarTuning.Wheels.Geometry;
using UnityEngine;

namespace EnhancedCarTuning.Wheels.Suspension
{
    internal sealed class WheelService
    {
        private sealed class Snapshot
        {
            internal CarPhWheel Wheel;
            internal ParamWheel OriginalParams, PrivateParams;
            internal WheelVisualTuning Visual;
            internal Vector3 Position, Scale;
            internal float Radius, Width, Travel, Stiffness, Damper, Stabilizer, Friction;
            internal bool Rear, Left;
            internal float Value(WheelParameter p)
            {
                switch (p) { case WheelParameter.Radius: return Radius; case WheelParameter.Width: return Width;
                    case WheelParameter.Travel: return Travel; case WheelParameter.Stiffness: return Stiffness * 100;
                    default: return 0; }
            }
        }
        private readonly List<Snapshot> snapshots = new List<Snapshot>();
        private readonly CarControl car;
        private readonly float stockPodiumHeight;
        private WheelSettings applied = WheelSettings.Stock;

        internal WheelService(CarControl car)
        {
            this.car = car;
            stockPodiumHeight = car.buttomY;
            foreach (var wheel in car.ph_car.wheels)
            {
                if (!wheel || !wheel.wheelParams) continue;
                var p = wheel.wheelParams;
                snapshots.Add(new Snapshot { Wheel = wheel, OriginalParams = p, Position = wheel.create_pos,
                    Scale = wheel.transform.localScale, Radius = wheel.Radius_cm, Width = wheel.Width_cm,
                    Travel = p.susp_cm, Stiffness = p.stiffness, Damper = p.damper, Stabilizer = p.stabilizer, Friction = p.friction,
                    Rear = wheel.IsBack, Left = wheel.IsLeft });
            }
        }

        internal bool Read(WheelSettings settings, AxleTarget target, WheelParameter p, out float value, out bool mixed)
        {
            value = 0; mixed = false; bool found = false;
            foreach (var snapshot in snapshots)
            {
                if (!snapshot.Wheel || target == AxleTarget.Front && snapshot.Rear || target == AxleTarget.Rear && !snapshot.Rear) continue;
                var axle = settings.For(snapshot.Rear);
                bool rimSize = p == WheelParameter.RimDiameter || p == WheelParameter.RimWidth;
                float v = axle.Get(p) ?? (p == WheelParameter.RimDiameter ? 2 * (axle.Get(WheelParameter.Radius) ?? snapshot.Radius) * .72f :
                    p == WheelParameter.RimWidth ? axle.Get(WheelParameter.Width) ?? snapshot.Width : snapshot.Value(p));
                if (rimSize)
                {
                    var models = car.GetComponent<EnhancedCarTuning.Core.VehicleTuningState>()?.WheelModels;
                    if (models == null || !models.ReadRimSize(snapshot.Wheel, p, out v, out _)) return false;
                }
                if (found && Math.Abs(v - value) > 0.001f) mixed = true;
                if (!found) value = v;
                found = true;
            }
            return found;
        }
        internal void Range(AxleTarget target, WheelParameter p, out float min, out float max)
        {
            min = WheelLimits.Min(p); max = p == WheelParameter.Radius ? WheelVehicleLimits.ForCar(car) : WheelLimits.Max(p);
            if (p != WheelParameter.RimDiameter) return;
            var models = car.GetComponent<EnhancedCarTuning.Core.VehicleTuningState>()?.WheelModels;
            if (models == null) return;
            foreach (var snapshot in snapshots)
            {
                if (!snapshot.Wheel || target == AxleTarget.Front && snapshot.Rear || target == AxleTarget.Rear && !snapshot.Rear) continue;
                if (models.ReadRimSize(snapshot.Wheel, p, out _, out var limit)) max = Mathf.Min(max, Mathf.Floor(limit));
            }
            min = Mathf.Max(min,max/.94f*.3f);
            min = Mathf.Min(min, max);
        }

        internal void Apply(WheelSettings settings)
        {
            bool physicsChanged = false;
            foreach (var s in snapshots)
            {
                var w = s.Wheel; if (!w) continue;
                var a = settings.For(s.Rear);
                if (a.PhysicalEquals(applied.For(s.Rear))) continue;
                physicsChanged = true;
                if (a.Get(WheelParameter.Travel).HasValue || a.Get(WheelParameter.Stiffness).HasValue)
                {
                    // A native ParamWheel can be shared by all wheels or even referenced as an asset.
                    if (!s.PrivateParams) s.PrivateParams = w.gameObject.AddComponent<ParamWheel>();
                    w.wheelParams = s.PrivateParams;
                    w.wheelParams.susp_cm = a.Get(WheelParameter.Travel) ?? s.Travel;
                    w.wheelParams.stiffness = (a.Get(WheelParameter.Stiffness) ?? s.Stiffness * 100) / 100;
                    w.wheelParams.damper = s.Damper;
                    w.wheelParams.stabilizer = s.Stabilizer;
                    w.wheelParams.friction = s.Friction;
                }
                else w.wheelParams = s.OriginalParams;
                w.Radius_cm = a.Get(WheelParameter.Radius) ?? s.Radius;
                w.Width_cm = a.Get(WheelParameter.Width) ?? s.Width;
                w.radius_mm = w.radius_with_tire_mm = Math.Max(w.Radius_cm / 100, 0.001f);
                w.width_mm = w.Width_cm / 100;
                w.suspension_mm = Math.Max((a.Get(WheelParameter.Travel) ?? s.Travel) / 100, 0.001f);
                w.create_pos = s.Position + new Vector3((s.Left ? -1 : 1) * (a.Get(WheelParameter.OffsetX) ?? 0) / 100,
                    -(a.Get(WheelParameter.OffsetY) ?? 0) / 100, 0);
                // Scaling relative to the original mesh also preserves vehicles with unusual wheel meshes.
                w.transform.localScale = new Vector3(s.Scale.x * w.Width_cm / Math.Max(s.Width, 0.001f),
                    s.Scale.y * w.Radius_cm / Math.Max(s.Radius, 0.001f), s.Scale.z * w.Radius_cm / Math.Max(s.Radius, 0.001f));
                if (!s.Visual && a.Get(WheelParameter.Camber).HasValue) s.Visual = w.gameObject.AddComponent<WheelVisualTuning>();
                if (s.Visual) s.Visual.Camber = a.Get(WheelParameter.Camber) ?? 0;
                w.m_suspensionLength = Math.Min(Math.Max(w.m_suspensionLength, 0), w.suspension_mm);
                w.m_suspensionLengthLast = w.m_suspensionLength;
                w.resultPosForView = w.create_pos - new Vector3(0, w.m_suspensionLength, 0);
                w.transform.localPosition = w.resultPosForView;
                w.contactInfo.Reset(); w.isNeedApplyImpulce = false;
                w.ResetCacheData(); w.WasMovedResetForces();
            }
            if (physicsChanged && car && car.body) car.body.WakeUp();
            applied = settings;
            // Time.timeScale can be zero in the main menu: waking the rigidbody cannot settle it.
            // UpPos starts with the native absolute pose, so repeated previews never accumulate a shift.
            var preview = EditCar_Preview.me;
            if (car && car.isUsedInMenu && preview && preview.car == car) preview.UpPos();
        }

        internal void GetPreviewCorrection(out float lift, out float pitch)
        {
            var plane = new PreviewSupportPlane();
            foreach (var s in snapshots)
            {
                if (!s.Wheel) continue;
                var a = applied.For(s.Rear);
                float delta = PreviewSupportPlane.HeightChange(stockPodiumHeight, s.Position.y, s.Radius / 100, s.Travel / 100,
                    s.Stiffness, (a.Get(WheelParameter.Radius) ?? s.Radius) / 100,
                    (a.Get(WheelParameter.Travel) ?? s.Travel) / 100,
                    (a.Get(WheelParameter.Stiffness) ?? s.Stiffness * 100) / 100,
                    -(a.Get(WheelParameter.OffsetY) ?? 0) / 100);
                plane.Add(s.Position.z, delta);
            }
            plane.Solve(stockPodiumHeight, out lift, out pitch);
        }

        internal void Dispose()
        {
            Apply(WheelSettings.Stock);
            foreach (var s in snapshots)
            {
                if (s.PrivateParams) UnityEngine.Object.Destroy(s.PrivateParams);
                if (s.Visual) UnityEngine.Object.Destroy(s.Visual);
            }
            snapshots.Clear();
        }
    }
}
