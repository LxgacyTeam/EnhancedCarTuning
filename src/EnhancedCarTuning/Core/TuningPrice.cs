using EnhancedCarTuning.TuneUp.Drive;
namespace EnhancedCarTuning.Core
{
    internal struct TuningPrice
    {
        internal readonly int Amount;
        internal readonly bool Coins;
        internal TuningPrice(int amount, bool coins = true) { Amount = amount; Coins = coins; }
        internal string Label => Amount == 0 ? "бесплатно" : Amount.ToString("N0") + (Coins ? " монет" : " алмазов");

        internal static TuningPrice Engine(string id, bool charge)
        {
            if (!charge || id == TuningSettings.StockEngine) return new TuningPrice(0);
            switch (id)
            {
                case "Engine.Tractor": return new TuningPrice(3000);
                case "Engine.70hp.Gaz": return new TuningPrice(5000);
                case "Engine.120hp": return new TuningPrice(12000);
                case "Engine.200hp": return new TuningPrice(32000);
                case "Engine.300hp": return new TuningPrice(72000);
                case "Engine.300hp.Turbo": return new TuningPrice(87000);
                case "Engine.500hp_MusculCar": return new TuningPrice(145, false);
                default: return new TuningPrice(5000);
            }
        }

        internal static TuningPrice Drivetrain(DriveLayout drive, bool charge, int cost) =>
            new TuningPrice(charge && drive != DriveLayout.Stock ? System.Math.Max(0, cost) : 0);
    }
}
