namespace EnhancedCarTuning.Appearance.Glass
{
    internal sealed class GlassFinish
    {
        internal readonly float Gloss, Metallic;
        internal GlassFinish(float gloss, float metallic) { Gloss = gloss; Metallic = metallic; }
        internal static bool Same(GlassFinish a, GlassFinish b) => ReferenceEquals(a, b) ||
            a != null && b != null && System.Math.Abs(a.Gloss - b.Gloss) < .001f && System.Math.Abs(a.Metallic - b.Metallic) < .001f;
    }
}
