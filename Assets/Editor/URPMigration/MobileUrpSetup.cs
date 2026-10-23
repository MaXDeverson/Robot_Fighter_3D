using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tools > URP Migration > 1. Setup Mobile URP: creates the URP asset tuned for phones, makes it the active pipeline
/// for every quality level and trims the Toony Colors Pro 2 Hybrid shader down to the URP features that asset uses.
/// Running it again re-applies the same settings to the existing asset.
/// </summary>
public static class MobileUrpSetup
{
    private const string MenuRoot = "Tools/URP Migration/";
    private const string PipelinePath = "Assets/ProjectSettings/URP-Mobile.asset";
    private const string RendererPath = "Assets/ProjectSettings/URP-Mobile_Renderer.asset";
    private const string Tcp2ShaderPath = "Assets/JMO Assets/Toony Colors Pro/Shaders/Hybrid 2/TCP2 Hybrid Shader 2.tcp2shader";
    private const string GamePlayScenePath = "Assets/_Scenes/GamePlay.unity";

    // TCP2 Hybrid shader options this pipeline never turns on. Leaving them in only multiplies the variants
    // Unity compiles and ships. Fog, GPU instancing, main light shadows and additional lights stay.
    private static readonly string[] Tcp2UnusedOptions =
    {
        "Adaptive Probe Volumes (URP 16+)",
        "Additional Lights Shadows",
        "DOTS Instancing (Entities Graphics)",
        "Decals (URP 12+)",
        "Forward+ Support (URP 14+)",
        "LOD Crossfading",
        "Light Cookies (URP 12+)",
        "Light Layers (URP 12+)",
        "Reflection Probes Blending",
        "Reflection Probes Box Projection",
        "Rendering Debugger Support (URP 14+)",
        "Rendering Layers (URP 14+)",
        "SSAO (URP 10+)",
        "Soft Shadows (URP)",
    };

    // Post Processing v2 component types. They do nothing under URP but still pull the whole PPv2 shader set into builds.
    private static readonly string[] PostProcessingV2Types =
    {
        "UnityEngine.Rendering.PostProcessing.PostProcessLayer",
        "UnityEngine.Rendering.PostProcessing.PostProcessVolume",
    };

    [MenuItem(MenuRoot + "1. Setup Mobile URP")]
    public static void Setup()
    {
        var pipeline = CreateOrLoadPipeline();
        ConfigurePipeline(pipeline);
        ConfigureRenderer((UniversalRendererData)pipeline.rendererDataList[0]);

        GraphicsSettings.defaultRenderPipeline = pipeline;
        ClearQualityLevelOverrides();
        ConfigureShaderStripping();
        AssetDatabase.SaveAssets();

        TrimTcp2Shader();
        Debug.Log($"[URP Migration] Mobile URP is active: {PipelinePath}");
    }

    [MenuItem(MenuRoot + "Remove Post Processing v2 From GamePlay")]
    public static void RemovePostProcessingV2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
        int removed = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component != null && PostProcessingV2Types.Contains(component.GetType().FullName))
            {
                Object.DestroyImmediate(component);
                removed++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[URP Migration] Removed {removed} Post Processing v2 components from {GamePlayScenePath}");
    }

    private static UniversalRenderPipelineAsset CreateOrLoadPipeline()
    {
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipeline != null)
            return pipeline;

        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(renderer, RendererPath);
        pipeline = UniversalRenderPipelineAsset.Create(renderer);
        AssetDatabase.CreateAsset(pipeline, PipelinePath);
        return pipeline;
    }

    private static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline)
    {
        var so = new SerializedObject(pipeline);

        // Nothing in the game samples the depth or opaque texture; each one costs a full-screen copy per frame.
        Set(so, "m_RequireDepthTexture", false);
        Set(so, "m_RequireOpaqueTexture", false);
        Set(so, "m_SupportsTerrainHoles", false);
        // No bloom or tonemapping is used, so an LDR target halves the colour bandwidth.
        Set(so, "m_SupportsHDR", false);
        // Matches the anti-aliasing the Android quality level had under the built-in pipeline.
        Set(so, "m_MSAA", (int)MsaaQuality.Disabled);
        Set(so, "m_RenderScale", 1f);
        Set(so, "m_EnableLODCrossFade", false);
        Set(so, "m_ShEvalMode", (int)ShEvalMode.PerVertex);

        Set(so, "m_MainLightRenderingMode", (int)LightRenderingMode.PerPixel);
        // None of the lights in the game casts shadows (characters use blob shadows), so no shadow maps at all.
        Set(so, "m_MainLightShadowsSupported", false);
        Set(so, "m_AdditionalLightShadowsSupported", false);
        Set(so, "m_SoftShadowsSupported", false);
        Set(so, "m_ShadowDistance", 30f);
        Set(so, "m_ShadowCascadeCount", 1);
        // Under the built-in pipeline a second directional light fell into the vertex/SH lighting of the Android
        // quality level (one pixel light); per-vertex additional lights are the URP equivalent.
        Set(so, "m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerVertex);
        Set(so, "m_AdditionalLightsPerObjectLimit", 2);
        Set(so, "m_SupportsLightCookies", false);
        Set(so, "m_SupportsLightLayers", false);
        Set(so, "m_MixedLightingSupported", false);
        Set(so, "m_ReflectionProbeBlending", false);
        Set(so, "m_ReflectionProbeBoxProjection", false);

        Set(so, "m_UseSRPBatcher", true);
        Set(so, "m_SupportsDynamicBatching", false);
        Set(so, "m_UseAdaptivePerformance", false);
        Set(so, "m_StoreActionsOptimization", (int)StoreActionsOptimization.Auto);
        Set(so, "m_ColorGradingMode", (int)ColorGradingMode.LowDynamicRange);
        Set(so, "m_ColorGradingLutSize", 16);
        Set(so, "m_SupportDataDrivenLensFlare", false);
        Set(so, "m_SupportScreenSpaceLensFlare", false);
        // No volumes exist, so there is nothing for the volume framework to re-evaluate every frame.
        Set(so, "m_VolumeFrameworkUpdateMode", (int)VolumeFrameworkUpdateMode.ViaScripting);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
    }

    private static void ConfigureRenderer(UniversalRendererData renderer)
    {
        var so = new SerializedObject(renderer);
        // Forward+ only pays off with many lights per object; the game has one or two directional lights.
        Set(so, "m_RenderingMode", (int)RenderingMode.Forward);
        Set(so, "m_DepthPrimingMode", (int)DepthPrimingMode.Disabled);
        so.ApplyModifiedPropertiesWithoutUndo();

        // The game used no post-processing (its PPv2 components were disabled). Without post-process data URP
        // skips the post-processing pass and leaves its shaders out of the build.
        renderer.postProcessData = null;
        EditorUtility.SetDirty(renderer);
    }

    private static void ClearQualityLevelOverrides()
    {
        var qualitySettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
        if (qualitySettings == null)
            return;

        var so = new SerializedObject(qualitySettings);
        var levels = so.FindProperty("m_QualitySettings");
        for (int i = 0; i < levels.arraySize; i++)
            levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureShaderStripping()
    {
        if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpStripping))
        {
            urpStripping.stripUnusedVariants = true;
            urpStripping.stripUnusedPostProcessingVariants = true;
        }

        if (GraphicsSettings.TryGetRenderPipelineSettings<URPTerrainShaderSetting>(out var terrain))
            terrain.includeTerrainShaders = false;
    }

    private static void TrimTcp2Shader()
    {
        var importer = AssetImporter.GetAtPath(Tcp2ShaderPath);
        if (importer == null)
        {
            Debug.LogError($"[URP Migration] TCP2 Hybrid shader not found at {Tcp2ShaderPath}");
            return;
        }

        var so = new SerializedObject(importer);
        var toggled = so.FindProperty("toggledOptions");
        toggled.ClearArray();
        foreach (string option in Tcp2UnusedOptions)
        {
            toggled.InsertArrayElementAtIndex(toggled.arraySize);
            toggled.GetArrayElementAtIndex(toggled.arraySize - 1).stringValue = option;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Reimporting also regenerates the shader for URP now that the pipeline is active.
        importer.SaveAndReimport();
    }

    private static void Set(SerializedObject so, string name, bool value) => Find(so, name).boolValue = value;
    private static void Set(SerializedObject so, string name, int value) => Find(so, name).intValue = value;
    private static void Set(SerializedObject so, string name, float value) => Find(so, name).floatValue = value;

    private static SerializedProperty Find(SerializedObject so, string name) =>
        so.FindProperty(name) ?? throw new System.ArgumentException($"{so.targetObject.GetType().Name} has no field {name}");
}
