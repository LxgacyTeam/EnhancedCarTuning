// Compatibility entry point for existing Unity batch commands.
public static class BuildGlassBundle
{
    public static void Build() { BuildEctAssets.BuildWindows(); }
}
