using UnityEngine;

namespace EnhancedCarTuning.TuneUp.Engine
{
    // Own copies of mutable curves/arrays prevent edits from leaking into other vehicles.
    internal sealed class EngineProfile
    {
        private AnimationCurve forward, back, volume, pitch;
        private float[] gears;
        private float topGear, reverseGear, maxRpm, raceMultiplier;
        private bool canRace;
        private AudioClip clip;

        internal static EngineProfile Capture(CarEngine engine) => new EngineProfile
        {
            forward = Clone(engine.forward), back = Clone(engine.back),
            volume = Clone(engine.volumeCurve), pitch = Clone(engine.pitchCurve),
            gears = engine.gears == null ? new float[0] : (float[])engine.gears.Clone(),
            topGear = engine.top_gear, reverseGear = engine.gear_back,
            maxRpm = engine.max_rmp, raceMultiplier = engine.raceTimeMultiply,
            canRace = engine.canUseInRace, clip = engine.source ? engine.source.clip : null
        };

        internal void Apply(CarEngine engine)
        {
            engine.forward = Clone(forward);
            engine.back = Clone(back);
            engine.volumeCurve = Clone(volume);
            engine.pitchCurve = Clone(pitch);
            engine.gears = (float[])gears.Clone();
            engine.top_gear = topGear;
            engine.gear_back = reverseGear;
            engine.max_rmp = maxRpm;
            engine.raceTimeMultiply = raceMultiplier;
            engine.canUseInRace = canRace;
            engine.cur_gear = Mathf.Clamp(engine.cur_gear, -1, Mathf.Max(0, gears.Length - 1));
            if (engine.source && engine.source.clip != clip)
            {
                bool wasPlaying = engine.source.isPlaying;
                engine.source.Stop();
                engine.source.clip = clip;
                if (wasPlaying && clip) engine.source.Play();
            }
        }

        private static AnimationCurve Clone(AnimationCurve curve)
        {
            if (curve == null) return new AnimationCurve();
            return new AnimationCurve(curve.keys) { preWrapMode = curve.preWrapMode, postWrapMode = curve.postWrapMode };
        }
    }
}
