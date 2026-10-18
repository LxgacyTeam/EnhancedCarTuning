using EnhancedCarTuning.TuneUp.Drive;
using EnhancedCarTuning.TuneUp.Engine;
using System;
using EnhancedCarTuning.Core;
using EnhancedCarTuning.Networking;
using EnhancedCarTuning.Wheels.Suspension;
using EnhancedCarTuning.Wheels.Models;

namespace EnhancedCarTuning.Core
{
    internal sealed class GarageSession
    {
        internal VehicleTuningState State { get; }
        internal GarageSelection Selection { get; }
        internal MenuBuy PendingBuy { get; private set; }
        internal bool Confirmed { get; private set; }
        private MenuBuy_Item engineItem, driveItem, frontItem, rearItem;
        private readonly object frontToken = new object(), rearToken = new object();
        private readonly object engineToken = new object(), driveToken = new object();

        internal GarageSession(VehicleTuningState state)
        { State = state; Selection = new GarageSelection(state.Committed); }

        internal bool CanEdit => State && State.Car && State.Car.isAlive &&
            (EnhancedCarTuning.Configuration.TuningConfig.AllowDuringEvents || !NetworkTuningPolicy.LocalPlayerInEvent());

        internal void SelectEngine(string id)
        {
            if (!CanEdit || !EngineCatalog.IsKnown(id)) return;
            Selection.SelectEngine(id);
            State.Preview(Selection.Draft);
        }

        internal void SelectDrive(DriveLayout drive)
        {
            if (!CanEdit) return;
            Selection.SelectDrive(drive);
            State.Preview(Selection.Draft);
        }

        internal void Cancel()
        {
            if (Confirmed || !State) return;
            Selection.Reset();
            State.SerializePreview = false;
            State.ApplyCommitted(Selection.Original);
        }

        internal void SelectWheel(AxleTarget axle, WheelParameter parameter, float value)
        {
            if (!CanEdit || !WheelLimits.Valid(parameter, value)) return;
            Selection.SelectWheels(Selection.Draft.Wheels.With(axle, parameter, value));
            State.Preview(Selection.Draft);
        }

        internal void SelectWheelModel(AxleTarget axle, string id)
        {
            if (!CanEdit || !WheelModelCatalog.IsKnown(id)) return;
            Selection.SelectWheels(Selection.Draft.Wheels.WithModel(axle, id));
            State.Preview(Selection.Draft);
        }

        internal void SelectTire(AxleTarget axle, string id)
        {
            if (!CanEdit || !EnhancedCarTuning.Wheels.Tires.TireCatalog.IsKnown(id)) return;
            Selection.SelectWheels(Selection.Draft.Wheels.WithTire(axle, id));
            State.Preview(Selection.Draft);
        }

        internal void ResetWheels(AxleTarget axle)
        {
            if (!CanEdit) return;
            Selection.SelectWheels(Selection.Draft.Wheels.Reset(axle));
            State.Preview(Selection.Draft);
        }

        internal void AddPurchaseItems(MenuBuy buy)
        {
            PendingBuy = buy;
            engineItem = driveItem = frontItem = rearItem = null;
            if (!CanEdit) { Cancel(); return; }
            bool charge = EnhancedCarTuning.Configuration.TuningConfig.ChargeForChanges;
            if (Selection.EngineChanged)
            {
                TuningPrice price = TuningPrice.Engine(Selection.Draft.EngineId, charge);
                engineItem = buy.AddIfNotExits(engineToken, price.Amount, price.Coins,
                    "Двигатель: " + EngineCatalog.Label(Selection.Draft.EngineId), null);
            }
            if (Selection.DriveChanged)
            {
                TuningPrice price = TuningPrice.Drivetrain(Selection.Draft.Drive, charge, EnhancedCarTuning.Configuration.TuningConfig.DrivePrice);
                driveItem = buy.AddIfNotExits(driveToken, price.Amount, price.Coins,
                    "Привод: " + (Selection.Draft.Drive == DriveLayout.Stock ? "Штатный" : Selection.Draft.Drive.ToString()), null);
            }
            int axleCost = charge ? EnhancedCarTuning.Configuration.TuningConfig.WheelAxlePrice : 0;
            if (Selection.FrontChanged) frontItem = buy.AddIfNotExits(frontToken, axleCost, true, "Передняя ось: подвеска и колёса", null);
            if (Selection.RearChanged) rearItem = buy.AddIfNotExits(rearToken, axleCost, true, "Задняя ось: подвеска и колёса", null);
        }

        internal void WrapPurchase()
        {
            if (!PendingBuy) return;
            var callback = PendingBuy.eventBuy;
            if (callback == null) return;
            PendingBuy.eventBuy = delegate
            {
                if (!State) { callback(); return; }
                bool allowed = CanEdit;
                if (!allowed)
                {
                    if (engineItem) engineItem.SetCost(0, true);
                    if (driveItem) driveItem.SetCost(0, true);
                    if (frontItem) frontItem.SetCost(0, true);
                    if (rearItem) rearItem.SetCost(0, true);
                    PendingBuy.UpTotalCosts();
                }
                TuningSettings chosen = Selection.ResolvePurchase(allowed && engineItem && engineItem.isActive(), allowed && driveItem && driveItem.isActive(),
                    allowed && frontItem && frontItem.isActive(), allowed && rearItem && rearItem.isActive());
                State.Preview(chosen);
                State.SerializePreview = true;
                try
                {
                    callback();
                    State.ApplyCommitted(chosen);
                    Confirmed = true;
                }
                catch
                {
                    State.ApplyCommitted(Selection.Original);
                    throw;
                }
                finally { if (State) State.SerializePreview = false; }
            };
        }
    }
}
