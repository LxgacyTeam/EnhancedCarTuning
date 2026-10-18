using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildGlassBundle
{
    public static void Build()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/GlassOverlay.shader");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new System.Exception("Glass overlay shader did not compile.");
        var finish = AssetDatabase.LoadAssetAtPath<Shader>("Assets/GlassFinish.shader");
        if (!finish || ShaderUtil.ShaderHasError(finish)) throw new System.Exception("Glass finish shader did not compile.");
        var output = Path.GetFullPath("../src/EnhancedCarTuning.Resources/assets");
        Directory.CreateDirectory(output);
        var build = new AssetBundleBuild { assetBundleName = "glass-prototype.bundle", assetNames = new[] { "Assets/GlassOverlay.shader", "Assets/GlassFinish.shader" } };
        if (!BuildPipeline.BuildAssetBundles(output, new[] { build }, BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64))
            throw new System.Exception("Glass bundle build failed.");
        Debug.Log("GLASS_PROTOTYPE_BUNDLE_OK " + output);
    }
}
