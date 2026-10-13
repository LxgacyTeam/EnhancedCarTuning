using System;
using UnityEngine;

namespace EnhancedCarTuning.Appearance.Body
{
    internal enum BodyPart { Body, Hood, Front, Trunk, Rear, Doors }

    internal sealed class PaintFinish : IEquatable<PaintFinish>
    {
        internal readonly Color Color;
        internal readonly float Metallic, Gloss;
        internal PaintFinish(Color color, float metallic, float gloss)
        {
            if (!Unit(color.r) || !Unit(color.g) || !Unit(color.b) || !Unit(color.a) || !Unit(metallic) || !Unit(gloss))
                throw new ArgumentException("Invalid body paint finish.");
            Color = new Color(color.r, color.g, color.b, 1f); Metallic = metallic; Gloss = gloss;
        }
        private static bool Unit(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        internal static PaintFinish Capture(CarMaterial.Mat mat) => new PaintFinish(mat.color, mat.Mettalic, mat.Gloss);
        internal void Apply(CarMaterial.Mat mat) { mat.color = Color; mat.Mettalic = Metallic; mat.Gloss = Gloss; }
        public bool Equals(PaintFinish other) => other != null && SameColor(other.Color) &&
            Math.Abs(Metallic - other.Metallic) <= .001f && Math.Abs(Gloss - other.Gloss) <= .001f;
        internal bool SameColor(Color other) => Math.Abs(Color.r - other.r) <= .001f &&
            Math.Abs(Color.g - other.g) <= .001f && Math.Abs(Color.b - other.b) <= .001f;
        public override bool Equals(object obj) => Equals(obj as PaintFinish);
        public override int GetHashCode() => 0;
    }

    internal sealed class BodyPaintSettings : IEquatable<BodyPaintSettings>
    {
        internal const int Count = 6, All = (1 << Count) - 1, BaseColor = 1 << Count;
        internal static readonly BodyPaintSettings Empty = new BodyPaintSettings(new PaintFinish[Count]);
        private readonly PaintFinish[] finishes;
        private BodyPaintSettings(PaintFinish[] values) { finishes = values; }
        internal PaintFinish this[BodyPart part] => finishes[(int)part];
        internal bool IsEmpty { get { foreach (var finish in finishes) if (finish != null) return false; return true; } }
        internal BodyPaintSettings With(BodyPart part, PaintFinish finish)
        { var copy = (PaintFinish[])finishes.Clone(); copy[(int)part] = finish; return new BodyPaintSettings(copy); }
        internal BodyPaintSettings WithMask(int mask, PaintFinish finish)
        {
            var copy = (PaintFinish[])finishes.Clone();
            for (int i = 0; i < Count; i++) if ((mask & (1 << i)) != 0) copy[i] = finish;
            return new BodyPaintSettings(copy);
        }
        public bool Equals(BodyPaintSettings other)
        {
            if (other == null) return false;
            for (int i = 0; i < Count; i++) if (!Equals(finishes[i], other.finishes[i])) return false;
            return true;
        }
        public override bool Equals(object obj) => Equals(obj as BodyPaintSettings);
        public override int GetHashCode() => 0;
    }
}
