using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tools > URP Migration > 3. Fix Effects And Validate: checks every material the build uses (build scenes and
/// Resources) for shaders URP cannot draw. Effect materials still on the built-in Standard shader are moved to
/// URP's Particles/Unlit with the same texture, colour, emission and blending, since an effect has no use for
/// toon lighting. Anything else that URP would draw as the magenta error shader is listed in the console.
/// </summary>
public static class UrpMaterialValidator
{
    private const string MenuRoot = "Tools/URP Migration/";
    private const string EffectsFolder = "Assets/Art/Effects/";
    private const string ParticlesUnlit = "Universal Render Pipeline/Particles/Unlit";

    // Passes URP draws; anything else (ForwardBase, Always, Vertex...) falls back to the error shader.
    private static readonly HashSet<string> UrpLightModes = new HashSet<string>
        { "", "SRPDefaultUnlit", "UniversalForward", "UniversalForwardOnly", "Universal2D" };

    [MenuItem(MenuRoot + "3. Fix Effects And Validate")]
    public static void FixAndValidate()
    {
        var log = new StringBuilder();
        var materials = MaterialsUsedByBuild();

        foreach (var (material, path) in materials)
        {
            if (!path.StartsWith(EffectsFolder) || !material.shader.name.StartsWith("Standard"))
                continue;
            ConvertEffectToParticlesUnlit(material);
            log.AppendLine($"Effect moved to {ParticlesUnlit}: {path}");
        }
        AssetDatabase.SaveAssets();

        int problems = 0;
        foreach (var (material, path) in materials)
        {
            if (CanRenderInUrp(material.shader))
                continue;
            problems++;
            log.AppendLine($"URP can't render {path} [{material.shader.name}]");
        }

        log.Insert(0, $"[URP Migration] {materials.Count} materials used by the build, {problems} that URP can't render.\n");
        if (problems > 0)
            Debug.LogWarning(log.ToString());
        else
            Debug.Log(log.ToString());
    }

    private static List<(Material material, string path)> MaterialsUsedByBuild()
    {
        var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
            .Concat(AssetDatabase.GetAllAssetPaths().Where(p =>
                p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)))
            .ToArray();

        var result = new List<(Material, string)>();
        foreach (string path in AssetDatabase.GetDependencies(roots, true))
        {
            // Materials embedded in models are sub-assets, so every material of each dependency is checked.
            foreach (var material in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                result.Add((material, path));
        }
        return result;
    }

    private static bool CanRenderInUrp(Shader shader)
    {
        if (shader == null || !shader.isSupported)
            return false;

        var subshader = ShaderUtil.GetShaderData(shader).ActiveSubshader;
        if (subshader == null)
            return false;

        for (int i = 0; i < subshader.PassCount; i++)
        {
            string lightMode = subshader.GetPass(i).FindTagValue(new ShaderTagId("LightMode")).name;
            if (UrpLightModes.Contains(lightMode ?? ""))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Standard → Particles/Unlit, keeping the rendering mode: Opaque, Cutout (alpha clip), Fade (alpha blend)
    /// or Transparent (premultiplied).
    /// </summary>
    private static void ConvertEffectToParticlesUnlit(Material m)
    {
        var baseMap = m.GetTexture("_MainTex");
        var tiling = m.GetTextureScale("_MainTex");
        var offset = m.GetTextureOffset("_MainTex");
        var color = m.GetColor("_Color");
        bool emission = m.IsKeywordEnabled("_EMISSION");
        var emissionColor = m.GetColor("_EmissionColor");
        var emissionMap = m.GetTexture("_EmissionMap");
        int mode = (int)m.GetFloat("_Mode");
        float cutoff = m.GetFloat("_Cutoff");

        m.shader = Shader.Find(ParticlesUnlit);
        m.SetTexture("_BaseMap", baseMap);
        m.SetTextureScale("_BaseMap", tiling);
        m.SetTextureOffset("_BaseMap", offset);
        m.SetColor("_BaseColor", color);
        if (emission)
        {
            m.SetColor("_EmissionColor", emissionColor);
            m.SetTexture("_EmissionMap", emissionMap);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        m.SetFloat("_Surface", mode >= 2 ? 1f : 0f);
        m.SetFloat("_Blend", mode == 3 ? 1f : 0f); // 0 alpha, 1 premultiply
        m.SetFloat("_AlphaClip", mode == 1 ? 1f : 0f);
        m.SetFloat("_Cutoff", cutoff);
        m.renderQueue = -1;

        BaseShaderGUI.SetMaterialKeywords(m, null, ParticleGUI.SetMaterialKeywords);
        EditorUtility.SetDirty(m);
    }
}
