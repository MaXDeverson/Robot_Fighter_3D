using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > URP Migration > 2. Convert Materials To TCP2: moves every material of the game's models and props
/// (Art/Models, Art/Materials and the materials embedded in the models there) onto the Toony Colors Pro 2 Hybrid
/// shader in Mobile Mode. Effect, UI and skybox materials keep their shaders.
/// Each source shader has its own mapping, so a converted material keeps its texture and tiling, colour, glow,
/// highlights, metalness, emission and transparency rather than only getting a new shader. The material asset is
/// changed in place, so every prefab and scene keeps its reference. Materials already on TCP2 are left alone,
/// which makes running it again safe. A per-material report goes to Logs/TCP2MaterialConversion.txt.
/// </summary>
public static class Tcp2MaterialConverter
{
    private const string MenuRoot = "Tools/URP Migration/";
    private const string Tcp2ShaderName = "Toony Colors Pro 2/Hybrid Shader 2";
    private const string ReportPath = "Logs/TCP2MaterialConversion.txt";
    private static readonly string[] ScanFolders = { "Assets/Art/Models", "Assets/Art/Materials" };

    // One toon ramp for the whole game so all converted materials shade alike. Threshold is on the wrapped
    // N.L (0.5 = terminator), so 0.6 puts the light/shadow edge a little into the lit side.
    private const float RampThreshold = 0.6f;
    private const float RampSmoothing = 0.15f;
    private static readonly Color ShadowColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    // Shaders of effects, UI and skies, which stay as they are.
    private static readonly string[] KeepShaderParts =
        { "Particles", "Effects/", "Additive", "Skybox", "UI/", "Sprites/", "TextMeshPro", "Cartoon FX", "EffectCore", "Hidden/" };

    // Hard-edged mask tints are baked into a texture this size at most; the Android import caps it at 1024 anyway.
    private const int BakeMaxSize = 2048;

    // Unity Standard specular, square-rooted in gamma space, peaks at 0.5/a * F0; the TCP2 GGX term peaks at
    // sqrt(0.05)/a * colour. This factor makes a converted highlight as bright as the Standard one.
    private const float StandardToTcp2Specular = 2.236f;

    private enum Surface { Opaque, Cutout, Fade, Transparent }

    /// <summary>Everything a converted material needs, worked out from the source material before its shader changes.</summary>
    private class Tcp2Setup
    {
        public Color BaseColor = Color.white;
        public Texture BaseMap;
        public Vector2 Tiling = Vector2.one, Offset = Vector2.zero;
        public Color HighlightColor = Color.white;

        public Texture NormalMap;
        public float NormalScale = 1f;

        public bool Emission;
        public Texture EmissionMap;
        public Color EmissionColor = Color.black;

        public bool Specular;
        public bool StylizedSpecular;
        public Color SpecularColor = Color.white;
        public float SpecularRoughness = 0.5f;
        public float SpecularToonSize = 0.25f, SpecularToonSmoothness = 0.05f;

        public bool Rim;
        public Color RimColor = Color.white;
        public float RimMin = 0.5f, RimMax = 1f;

        public Surface Surface = Surface.Opaque;
        public float Cutoff = 0.5f;
        public bool ZWrite = true;
        public CullMode Cull = CullMode.Back;
        public int RenderQueue = -1;

        public readonly List<string> Notes = new List<string>();
    }

    [MenuItem(MenuRoot + "2. Convert Materials To TCP2")]
    public static void ConvertAll()
    {
        var tcp2 = Shader.Find(Tcp2ShaderName);
        if (tcp2 == null)
        {
            Debug.LogError($"[URP Migration] Shader '{Tcp2ShaderName}' not found. Import Toony Colors Pro 2 first.");
            return;
        }

        var report = new StringBuilder();
        int converted = 0;
        try
        {
            converted += ExtractEmbeddedMaterials(tcp2, report);

            var paths = AssetDatabase.FindAssets("t:Material", ScanFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".mat"))
                .Distinct()
                .ToList();
            for (int i = 0; i < paths.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Convert Materials To TCP2", paths[i], (float)i / paths.Count);
                var material = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
                if (TryConvert(material, tcp2, paths[i], report))
                    converted++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, report.ToString());
        Debug.Log($"[URP Migration] Converted {converted} materials to TCP2. Report: {Path.GetFullPath(ReportPath)}");
    }

    [MenuItem(MenuRoot + "Convert Selected Materials To TCP2")]
    private static void ConvertSelected()
    {
        var tcp2 = Shader.Find(Tcp2ShaderName);
        var report = new StringBuilder();
        foreach (var material in Selection.GetFiltered<Material>(SelectionMode.Assets))
            TryConvert(material, tcp2, AssetDatabase.GetAssetPath(material), report);
        AssetDatabase.SaveAssets();
        Debug.Log("[URP Migration] " + report);
    }

    private static bool TryConvert(Material material, Shader tcp2, string path, StringBuilder report)
    {
        if (material == null || material.shader == tcp2 || ShouldKeep(material.shader))
            return false;

        string sourceShader = material.shader.name;
        var setup = Map(material, path);
        Apply(material, tcp2, setup);

        report.AppendLine($"{path}  [{sourceShader}]");
        report.AppendLine("    " + Describe(setup));
        foreach (string note in setup.Notes)
            report.AppendLine("    ! " + note);
        return true;
    }

    private static bool ShouldKeep(Shader shader) =>
        shader.name.StartsWith("Toony Colors Pro 2") || KeepShaderParts.Any(shader.name.Contains);

    // ---------------------------------------------------------------- source shader → setup

    private static Tcp2Setup Map(Material m, string path)
    {
        string shader = m.shader.name;
        switch (shader)
        {
            case "Flexus/Universal": return FromFlexus(m, transparent: false);
            case "Flexus/Universal Transparent": return FromFlexus(m, transparent: true);
            case "HPMaskTint": return FromHpMaskTint(m, path);
            case "Custom/MatCapSimple": return FromMatCap(m, shine: false);
            case "Custom/MatCapShine": return FromMatCap(m, shine: true);
            case "Custom/UntilIlum": return FromUnlitIllum(m);
            case "Standard": return FromStandard(m, specularWorkflow: false);
            case "Standard (Specular setup)": return FromStandard(m, specularWorkflow: true);
        }

        if (shader.StartsWith("Unlit/"))
            return FromUnlit(m);

        var setup = FromLegacy(m);
        if (!shader.StartsWith("Legacy Shaders/") && !shader.StartsWith("Mobile/") && !shader.StartsWith("Ciconia"))
            setup.Notes.Add($"No dedicated mapping for '{shader}': colour, texture, normal map and emission were carried over.");
        return setup;
    }

    /// <summary>
    /// Flexus Universal: Lambert diffuse plus an additive fresnel "glow" (pow(1 - N.V, power) * colour), a stylized
    /// Phong highlight whose size follows roughness, and an optional colour correction. Glow becomes TCP2 rim
    /// lighting fitted to the same falloff (or emission when the power is 0 and the glow covers the whole surface),
    /// the highlight becomes the stylized TCP2 specular with matching size and softness.
    /// </summary>
    private static Tcp2Setup FromFlexus(Material m, bool transparent)
    {
        var s = new Tcp2Setup();
        var keywords = new HashSet<string>(m.shaderKeywords);
        var color = m.GetColor("_Color");
        float alpha = transparent ? color.a : 1f;
        s.BaseColor = new Color(color.r, color.g, color.b, alpha);

        // The shader only reads the texture when its keyword is on; the property alone can be stale.
        if (keywords.Contains("USE_TEXTURE"))
            CopyBaseMap(m, "_MainTex", s);

        if (keywords.Contains("USE_GLOW"))
        {
            var glow = m.GetColor("_GlowColor");
            float power = m.GetFloat("_GlowPower");
            // In the transparent shader the glow also raises alpha, so where the surface is see-through it
            // shows as roughly glowColor * (alpha + glowAlpha * falloff).
            float strength = transparent ? Mathf.Clamp(alpha + glow.a, 0f, 2f) : 1f;
            var glowColor = new Color(glow.r, glow.g, glow.b) * strength;

            if (power < 0.05f)
            {
                s.Emission = true;
                s.EmissionColor = glowColor;
            }
            else
            {
                s.Rim = true;
                s.RimColor = glowColor;
                float a = alpha, ga = transparent ? glow.a : 0f;
                Func<float, float> falloff = x =>
                    a + ga > 0f ? (a * Mathf.Pow(x, power) + ga * Mathf.Pow(x, 2f * power)) / (a + ga) : Mathf.Pow(x, power);
                (s.RimMin, s.RimMax) = FitRim(falloff);
            }
        }

        if (keywords.Contains("USE_SPECULAR"))
        {
            float roughness = Mathf.Max(m.GetFloat("_Roughness"), 0.001f);
            float peak = m.GetFloat("_SpecularMultiplier") * (1f - roughness);
            if (peak > 0.02f && roughness < 0.98f)
            {
                s.Specular = true;
                s.StylizedSpecular = true;
                s.SpecularColor = new Color(peak, peak, peak, 1f);
                (s.SpecularToonSize, s.SpecularToonSmoothness) = FitFlexusHighlight(roughness);
            }
        }

        // Colour correction brightens lit areas by up to 1.5x with its saturation slider.
        if (keywords.Contains("USE_CC"))
        {
            float boost = 1f + 0.5f * m.GetFloat("_Saturation");
            s.HighlightColor = new Color(boost, boost, boost, 1f);
        }

        if (keywords.Contains("USE_CUBEMAP_CUBEMAP") || keywords.Contains("USE_CUBEMAP_GRADIENT"))
            s.Notes.Add("Flexus cubemap/gradient reflection is not carried over.");

        if (transparent)
        {
            // Flexus Transparent adds glow and highlight on top of the blended colour, which is what TCP2's
            // premultiplied "Transparent" mode does. It also kept depth writes on and its queue.
            s.Surface = Surface.Transparent;
            s.ZWrite = true;
            s.RenderQueue = m.renderQueue;
        }
        return s;
    }

    /// <summary>
    /// HPMaskTint (Robot Hero): Standard Specular whose albedo is tinted per mask channel (R/G/B) with three colours
    /// and powers. TCP2 has no mask tint, so the tinted albedo is baked into a texture. Normal and emission maps are
    /// carried over; the specular colour is black on every material, so no highlight is added.
    /// </summary>
    private static Tcp2Setup FromHpMaskTint(Material m, string path)
    {
        var s = new Tcp2Setup();
        var albedo = m.GetTexture("_Albedo") as Texture2D;
        var mask = m.GetTexture("_Mask") as Texture2D;
        CopyTiling(m, "_Albedo", s);

        if (albedo != null && mask != null)
        {
            s.BaseMap = BakeMaskTint(m, albedo, mask, path, out float scale);
            s.BaseColor = new Color(scale, scale, scale, 1f);
            s.Notes.Add($"Mask tint baked into {AssetDatabase.GetAssetPath(s.BaseMap)}" +
                        (scale > 1f ? $" (values above 1 kept through base colour x{scale:0.##})" : ""));
        }
        else
        {
            s.BaseMap = albedo;
        }

        CopyNormalMap(m, "_Normal", 1f, s);

        var emission = m.GetColor("_EmissionColor") * m.GetFloat("_EmissionPower");
        if (emission.maxColorComponent > 0.004f)
        {
            s.Emission = true;
            s.EmissionMap = m.GetTexture("_EmissionTex");
            s.EmissionColor = new Color(emission.r, emission.g, emission.b, 1f);
        }

        ApplyPbrSpecular(s, m.GetColor("_Specular"), m.GetFloat("_Smoothness"));
        return s;
    }

    /// <summary>
    /// MatCap props (texture * matcap * strength * 2, unlit). The matcap is fake lighting, which TCP2 lighting now
    /// provides: the base colour keeps the matcap's average brightness and tint, and the matcap's highlight becomes
    /// a soft stylized specular. The pickup "shine" (a scrolling ramp on UV2) has no TCP2 equivalent and becomes a
    /// rim glow of the same strength, so pickups still stand out.
    /// </summary>
    private static Tcp2Setup FromMatCap(Material m, bool shine)
    {
        var s = new Tcp2Setup();
        CopyBaseMap(m, "_MainTex", s);
        var tint = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        float strength = m.GetFloat("_MatCapStrength");

        var matcap = m.GetTexture("_MatCap");
        var (average, peak) = matcap != null ? SampleMatCap(matcap) : (Color.white, Color.white);
        var lighting = average * (2f * strength);
        s.BaseColor = new Color(
            Mathf.Min(tint.r * lighting.r, 2f), Mathf.Min(tint.g * lighting.g, 2f), Mathf.Min(tint.b * lighting.b, 2f), 1f);

        float albedoLuminance = s.BaseMap != null ? Luminance(SampleAverage(s.BaseMap)) : 1f;
        var highlight = (peak - average) * (2f * strength * albedoLuminance);
        if (highlight.maxColorComponent > 0.03f)
        {
            s.Specular = true;
            s.StylizedSpecular = true;
            s.SpecularColor = new Color(highlight.r * tint.r, highlight.g * tint.g, highlight.b * tint.b, 1f);
            s.SpecularToonSize = 0.2f;
            s.SpecularToonSmoothness = 0.8f;
        }

        if (shine)
        {
            float shineStrength = m.GetFloat("_ShineStrength");
            s.Rim = true;
            s.RimColor = new Color(shineStrength, shineStrength, shineStrength, 1f) * 0.4f;
            s.RimMin = 0.4f;
            s.RimMax = 1f;
            s.Notes.Add("Scrolling shine ramp (UV2) replaced by a static rim glow.");
        }

        s.Notes.Add("Was unlit (matcap lighting); now lit by the scene light.");
        return s;
    }

    /// <summary>UntilIlum: unlit texture * texture alpha * strength. The alpha mask and strength go into the base colour.</summary>
    private static Tcp2Setup FromUnlitIllum(Material m)
    {
        var s = new Tcp2Setup();
        CopyBaseMap(m, "_MainTex", s);
        float alpha = s.BaseMap != null ? SampleAverage(s.BaseMap).a : 1f;
        float brightness = Mathf.Min(m.GetFloat("_IlumStrength") * alpha, 2f);
        s.BaseColor = new Color(brightness, brightness, brightness, 1f);
        s.Notes.Add("Was unlit; now lit by the scene light.");
        return s;
    }

    private static Tcp2Setup FromUnlit(Material m)
    {
        var s = new Tcp2Setup();
        if (m.HasProperty("_Color"))
            s.BaseColor = m.GetColor("_Color");
        CopyBaseMap(m, "_MainTex", s);

        string shader = m.shader.name;
        if (shader.Contains("Cutout"))
        {
            s.Surface = Surface.Cutout;
            s.Cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
        }
        else if (shader.Contains("Transparent"))
        {
            s.Surface = Surface.Fade;
            s.ZWrite = false;
        }

        s.Notes.Add("Was unlit; now lit by the scene light.");
        return s;
    }

    /// <summary>
    /// Legacy and Mobile surface shaders (Diffuse, Bumped, Specular, Self-Illumin, Transparent, Cutout) and other
    /// simple shaders with the usual _Color/_MainTex/_BumpMap properties.
    /// </summary>
    private static Tcp2Setup FromLegacy(Material m)
    {
        var s = new Tcp2Setup();
        string shader = m.shader.name;
        if (m.HasProperty("_Color"))
            s.BaseColor = m.GetColor("_Color");
        CopyBaseMap(m, "_MainTex", s);
        CopyNormalMap(m, "_BumpMap", m.HasProperty("_NormalIntensity") ? m.GetFloat("_NormalIntensity") : 1f, s);

        if (shader.Contains("Self-Illumin"))
        {
            // Self-Illumin adds albedo * _Illum.a. Without an _Illum texture the mask is white: the whole albedo glows.
            var illum = m.GetTexture("_Illum");
            s.Emission = true;
            s.EmissionMap = s.BaseMap;
            s.EmissionColor = new Color(s.BaseColor.r, s.BaseColor.g, s.BaseColor.b, 1f);
            if (illum != null)
            {
                float mask = SampleAverage(illum).a;
                s.EmissionColor *= mask;
                s.Notes.Add($"Self-illumination mask {illum.name} averaged to {mask:0.##}; per-pixel mask not kept.");
            }
        }

        if (shader.Contains("Specular"))
        {
            // Blinn-Phong with exponent shininess * 128 and the albedo alpha as gloss. Mobile versions have no _SpecColor.
            float shininess = m.HasProperty("_Shininess") ? m.GetFloat("_Shininess") : 0.078f;
            var specColor = m.HasProperty("_SpecColor") ? m.GetColor("_SpecColor") : Color.white;
            if (m.HasProperty("_SpecularIntensity"))
                specColor *= m.GetFloat("_SpecularIntensity");
            float gloss = s.BaseMap != null ? SampleAverage(s.BaseMap).a : 1f;
            float exponent = Mathf.Max(shininess * 128f, 1f);
            float alpha = Mathf.Sqrt(2f / (exponent + 2f));
            // A Blinn-Phong highlight peaks at 1; the TCP2 GGX term peaks at sqrt(0.05)/alpha.
            float gain = gloss * alpha / Mathf.Sqrt(0.05f);
            s.Specular = true;
            s.SpecularRoughness = Mathf.Sqrt(alpha);
            s.SpecularColor = new Color(specColor.r * gain, specColor.g * gain, specColor.b * gain, 1f);
        }

        if (shader.Contains("Double Sided"))
            s.Cull = CullMode.Off;

        if (shader.Contains("Cutout"))
        {
            s.Surface = Surface.Cutout;
            s.Cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
        }
        else if (shader.Contains("Transparent"))
        {
            s.Surface = Surface.Fade;
            s.ZWrite = false;
        }
        return s;
    }

    /// <summary>
    /// Standard (metallic and specular setup): colour, maps and tiling, normal map, emission, rendering mode,
    /// and a GGX highlight with the same roughness and peak brightness. Metalness darkens the diffuse colour and
    /// tints the highlight with the albedo, as in the Standard shader.
    /// </summary>
    private static Tcp2Setup FromStandard(Material m, bool specularWorkflow)
    {
        var s = new Tcp2Setup();
        var color = m.GetColor("_Color");
        CopyBaseMap(m, "_MainTex", s);
        if (m.GetTexture("_BumpMap") != null)
            CopyNormalMap(m, "_BumpMap", m.GetFloat("_BumpScale"), s);

        bool smoothnessFromAlbedo = m.GetFloat("_SmoothnessTextureChannel") > 0.5f;
        float glossScale = m.GetFloat("_GlossMapScale");
        float albedoAlpha = smoothnessFromAlbedo && s.BaseMap != null ? SampleAverage(s.BaseMap).a : 1f;

        Color f0;
        float smoothness;
        float diffuse;
        if (specularWorkflow)
        {
            var specMap = m.GetTexture("_SpecGlossMap");
            var specSample = specMap != null ? SampleAverage(specMap) : Color.white;
            f0 = specMap != null ? specSample : m.GetColor("_SpecColor");
            smoothness = specMap != null ? specSample.a * glossScale : m.GetFloat("_Glossiness");
            diffuse = 1f - f0.maxColorComponent;
            if (specMap != null)
                s.Notes.Add($"Specular map {specMap.name} averaged into one specular colour.");
        }
        else
        {
            var metalMap = m.GetTexture("_MetallicGlossMap");
            var metalSample = metalMap != null ? SampleAverage(metalMap) : Color.white;
            float metallic = metalMap != null ? metalSample.r : m.GetFloat("_Metallic");
            smoothness = metalMap != null ? metalSample.a * glossScale : m.GetFloat("_Glossiness");
            f0 = Color.Lerp(new Color(0.04f, 0.04f, 0.04f), color, metallic);
            diffuse = 1f - metallic;
            if (metalMap != null)
                s.Notes.Add($"Metallic map {metalMap.name} averaged (metallic {metallic:0.##}, smoothness {smoothness:0.##}).");
        }
        if (smoothnessFromAlbedo)
            smoothness = albedoAlpha * glossScale;

        s.BaseColor = new Color(color.r * diffuse, color.g * diffuse, color.b * diffuse, color.a);
        if (m.GetFloat("_SpecularHighlights") > 0.5f)
            ApplyPbrSpecular(s, f0, smoothness);

        if (m.IsKeywordEnabled("_EMISSION"))
        {
            var emission = m.GetColor("_EmissionColor");
            if (emission.maxColorComponent > 0.004f)
            {
                s.Emission = true;
                s.EmissionMap = m.GetTexture("_EmissionMap");
                s.EmissionColor = new Color(emission.r, emission.g, emission.b, 1f);
            }
        }

        if (m.GetTexture("_OcclusionMap") != null)
            s.Notes.Add("Occlusion map dropped (it only darkens ambient light; saves a texture read on mobile).");
        if (m.GetTexture("_DetailAlbedoMap") != null || m.GetTexture("_DetailNormalMap") != null)
            s.Notes.Add("Detail maps are not supported by TCP2 Hybrid and were dropped.");

        switch ((int)m.GetFloat("_Mode"))
        {
            case 1:
                s.Surface = Surface.Cutout;
                s.Cutoff = m.GetFloat("_Cutoff");
                break;
            case 2:
                s.Surface = Surface.Fade;
                s.ZWrite = false;
                break;
            case 3:
                s.Surface = Surface.Transparent;
                s.ZWrite = false;
                break;
        }
        return s;
    }

    /// <summary>
    /// Adds a GGX highlight matching Unity's Standard BRDF for this F0 and smoothness, or nothing when the Standard
    /// highlight would be too faint to see (rough dielectrics), which keeps the specular code out of the shader.
    /// </summary>
    private static void ApplyPbrSpecular(Tcp2Setup s, Color f0, float smoothness)
    {
        float perceptualRoughness = Mathf.Clamp01(1f - smoothness);
        float alpha = Mathf.Max(perceptualRoughness * perceptualRoughness, 0.002f);
        float peak = 0.5f / alpha * f0.maxColorComponent;
        if (peak < 0.05f)
            return;

        s.Specular = true;
        s.StylizedSpecular = false;
        s.SpecularRoughness = Mathf.Max(perceptualRoughness, 0.05f);
        s.SpecularColor = new Color(f0.r, f0.g, f0.b, 1f) * StandardToTcp2Specular;
    }

    // ---------------------------------------------------------------- apply

    private static void Apply(Material m, Shader tcp2, Tcp2Setup s)
    {
        // Old properties are dropped before the shader changes: textures still listed there would keep being
        // pulled into builds (the HPMaskTint masks are 4K) even though no shader reads them.
        var so = new SerializedObject(m);
        foreach (string list in new[] { "m_SavedProperties.m_TexEnvs", "m_SavedProperties.m_Floats",
                     "m_SavedProperties.m_Colors", "m_SavedProperties.m_Ints", "m_ValidKeywords", "m_InvalidKeywords",
                     "stringTagMap", "disabledShaderPasses" })
            so.FindProperty(list)?.ClearArray();
        so.ApplyModifiedPropertiesWithoutUndo();

        m.shader = tcp2;
        m.shaderKeywords = Array.Empty<string>();

        // Mobile Mode moves rim and fresnel to the vertex shader and replaces the per-channel texture
        // selection branches with fixed channels.
        m.SetFloat("_UseMobileMode", 1f);

        m.SetColor("_BaseColor", s.BaseColor);
        m.SetTexture("_BaseMap", s.BaseMap);
        m.SetTextureScale("_BaseMap", s.Tiling);
        m.SetTextureOffset("_BaseMap", s.Offset);

        m.SetColor("_HColor", s.HighlightColor);
        m.SetColor("_SColor", ShadowColor);
        m.SetFloat("_ShadowColorLightAtten", 1f);
        m.SetFloat("_RampType", 0f);
        m.SetFloat("_RampThreshold", RampThreshold);
        m.SetFloat("_RampSmoothing", RampSmoothing);
        m.SetFloat("_IndirectIntensity", 1f);

        m.SetFloat("_UseNormalMap", s.NormalMap != null ? 1f : 0f);
        if (s.NormalMap != null)
        {
            m.SetTexture("_BumpMap", s.NormalMap);
            m.SetTextureScale("_BumpMap", s.Tiling);
            m.SetTextureOffset("_BumpMap", s.Offset);
            m.SetFloat("_BumpScale", s.NormalScale);
        }

        m.SetFloat("_UseSpecular", s.Specular ? 1f : 0f);
        if (s.Specular)
        {
            m.SetFloat("_SpecularType", s.StylizedSpecular ? 1f : 0f);
            m.SetColor("_SpecularColor", s.SpecularColor);
            m.SetFloat("_SpecularRoughness", s.SpecularRoughness);
            m.SetFloat("_SpecularToonSize", s.SpecularToonSize);
            m.SetFloat("_SpecularToonSmoothness", s.SpecularToonSmoothness);
        }

        m.SetFloat("_UseEmission", s.Emission ? 1f : 0f);
        if (s.Emission)
        {
            m.SetTexture("_EmissionMap", s.EmissionMap);
            m.SetTextureScale("_EmissionMap", s.Tiling);
            m.SetTextureOffset("_EmissionMap", s.Offset);
            m.SetColor("_EmissionColor", s.EmissionColor);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        m.SetFloat("_UseRim", s.Rim ? 1f : 0f);
        m.SetFloat("_UseRimLightMask", 0f);
        if (s.Rim)
        {
            m.SetColor("_RimColor", s.RimColor);
            m.SetFloat("_RimMin", s.RimMin);
            m.SetFloat("_RimMax", s.RimMax);
        }

        m.SetFloat("_UseMatCap", 0f);
        m.SetFloat("_UseReflections", 0f);
        m.SetFloat("_UseOcclusion", 0f);
        m.SetFloat("_UseOutline", 0f);
        m.SetFloat("_Cull", (float)s.Cull);
        m.SetFloat("_ReceiveShadowsOff", 1f);

        m.SetFloat("_UseAlphaTest", s.Surface == Surface.Cutout ? 1f : 0f);
        m.SetFloat("_Cutoff", s.Cutoff);

        // Turns the toggle and enum floats above into their keywords, the way the material inspector does.
        MaterialEditor.ApplyMaterialPropertyDrawers(m);
        SetKeyword(m, "TCP2_MOBILE", true);

        ApplySurface(m, s);
        EditorUtility.SetDirty(m);
    }

    /// <summary>Same blend, depth and queue setup as the Rendering Mode popup of the TCP2 Hybrid inspector.</summary>
    private static void ApplySurface(Material m, Tcp2Setup s)
    {
        switch (s.Surface)
        {
            case Surface.Opaque:
            case Surface.Cutout:
                m.SetFloat("_RenderingMode", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                SetKeyword(m, "_ALPHAPREMULTIPLY_ON", false);
                bool cutout = s.Surface == Surface.Cutout;
                m.SetOverrideTag("RenderType", cutout ? "TransparentCutout" : "");
                m.renderQueue = cutout ? (int)RenderQueue.AlphaTest : -1;
                break;

            case Surface.Fade:
                m.SetFloat("_RenderingMode", 1f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", s.ZWrite ? 1f : 0f);
                SetKeyword(m, "_ALPHAPREMULTIPLY_ON", false);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                break;

            case Surface.Transparent:
                m.SetFloat("_RenderingMode", 2f);
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", s.ZWrite ? 1f : 0f);
                SetKeyword(m, "_ALPHAPREMULTIPLY_ON", true);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                break;
        }

        if (s.RenderQueue >= 0)
            m.renderQueue = s.RenderQueue;
    }

    private static void SetKeyword(Material m, string keyword, bool on)
    {
        if (on)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }

    private static string Describe(Tcp2Setup s)
    {
        var parts = new List<string> { $"{s.Surface}", $"base {Format(s.BaseColor)}" + (s.BaseMap ? $" × {s.BaseMap.name}" : "") };
        if (s.Tiling != Vector2.one || s.Offset != Vector2.zero)
            parts.Add($"tiling {s.Tiling} offset {s.Offset}");
        if (s.HighlightColor != Color.white)
            parts.Add($"highlight {Format(s.HighlightColor)}");
        if (s.NormalMap)
            parts.Add($"normal {s.NormalMap.name} ×{s.NormalScale:0.##}");
        if (s.Emission)
            parts.Add($"emission {Format(s.EmissionColor)}" + (s.EmissionMap ? $" × {s.EmissionMap.name}" : ""));
        if (s.Specular)
            parts.Add(s.StylizedSpecular
                ? $"stylized specular {Format(s.SpecularColor)} size {s.SpecularToonSize:0.###} smooth {s.SpecularToonSmoothness:0.##}"
                : $"GGX specular {Format(s.SpecularColor)} roughness {s.SpecularRoughness:0.##}");
        if (s.Rim)
            parts.Add($"rim {Format(s.RimColor)} {s.RimMin:0.##}..{s.RimMax:0.##}");
        if (s.Cull != CullMode.Back)
            parts.Add($"cull {s.Cull}");
        if (s.RenderQueue >= 0)
            parts.Add($"queue {s.RenderQueue}");
        return string.Join(", ", parts);
    }

    private static string Format(Color c) => $"({c.r:0.##}, {c.g:0.##}, {c.b:0.##}{(c.a < 0.999f ? $", a {c.a:0.##}" : "")})";

    // ---------------------------------------------------------------- property helpers

    private static void CopyBaseMap(Material m, string property, Tcp2Setup s)
    {
        if (!m.HasProperty(property))
            return;
        s.BaseMap = m.GetTexture(property);
        CopyTiling(m, property, s);
    }

    private static void CopyTiling(Material m, string property, Tcp2Setup s)
    {
        s.Tiling = m.GetTextureScale(property);
        s.Offset = m.GetTextureOffset(property);
    }

    private static void CopyNormalMap(Material m, string property, float scale, Tcp2Setup s)
    {
        if (!m.HasProperty(property))
            return;
        var texture = m.GetTexture(property);
        if (texture == null)
            return;

        // A texture not imported as a normal map decodes to noise; the source shader showed broken shading with it.
        if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) is TextureImporter importer &&
            importer.textureType != TextureImporterType.NormalMap)
        {
            s.Notes.Add($"'{texture.name}' in {property} is not a normal map texture; normal mapping skipped.");
            return;
        }

        s.NormalMap = texture;
        s.NormalScale = scale;
    }

    // ---------------------------------------------------------------- curve fitting

    /// <summary>Best TCP2 rim (smoothstep(min, max, 1 - N.V)) for a falloff curve, weighted toward camera-facing pixels.</summary>
    private static (float min, float max) FitRim(Func<float, float> falloff)
    {
        const int samples = 50;
        var target = new float[samples + 1];
        for (int i = 0; i <= samples; i++)
            target[i] = falloff(i / (float)samples);

        float bestMin = 0.5f, bestMax = 1f, bestError = float.MaxValue;
        for (float a = 0f; a <= 1f; a += 0.02f)
        for (float b = a + 0.02f; b <= 2f; b += 0.02f)
        {
            float error = 0f;
            for (int i = 0; i <= samples; i++)
            {
                float x = i / (float)samples;
                float d = SmoothStep(a, b, x) - target[i];
                // Faces looking at the camera cover far more of the screen than grazing ones.
                error += d * d * (1.05f - x);
            }

            if (error < bestError)
            {
                bestError = error;
                bestMin = a;
                bestMax = b;
            }
        }
        return (bestMin, bestMax);
    }

    /// <summary>
    /// TCP2 stylized size and smoothness for the Flexus highlight, which fades linearly in R.L from the light direction
    /// out to R.L = 1 - 2 * roughness. Flexus measures the reflected view ray (Phong) and TCP2 the half vector
    /// (Blinn), whose angle is half as large, so the highlight edge sits at N.H = sqrt(1 - roughness).
    /// </summary>
    private static (float size, float smoothness) FitFlexusHighlight(float roughness)
    {
        float edge = Mathf.Sqrt(1f - roughness);
        float size = 1f - edge;

        float bestSmoothness = 1f, bestError = float.MaxValue;
        for (float smoothness = 0.05f; smoothness <= 1.001f; smoothness += 0.05f)
        {
            float error = 0f;
            for (int i = 0; i <= 40; i++)
            {
                float nh = edge + size * i / 40f;
                float flexus = Mathf.Clamp01((nh * nh - 1f + roughness) / roughness);
                float tcp2 = SmoothStep(0f, smoothness, (nh - edge) / size);
                error += (tcp2 - flexus) * (tcp2 - flexus);
            }

            if (error < bestError)
            {
                bestError = error;
                bestSmoothness = smoothness;
            }
        }
        return (size, bestSmoothness);
    }

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    // ---------------------------------------------------------------- texture reading and baking

    private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

    /// <summary>Average colour of a texture, read through a small render target so import settings don't matter.</summary>
    private static Color SampleAverage(Texture texture)
    {
        var pixels = ReadThroughRenderTarget(texture, 64);
        var sum = Color.clear;
        foreach (var p in pixels)
            sum += p;
        return sum / pixels.Length;
    }

    /// <summary>Average and brightest colour inside the matcap circle (the corners are never sampled).</summary>
    private static (Color average, Color peak) SampleMatCap(Texture texture)
    {
        const int size = 64;
        var pixels = ReadThroughRenderTarget(texture, size);
        var sum = Color.clear;
        var peak = Color.black;
        int count = 0;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
            if (dx * dx + dy * dy > 0.25f)
                continue;
            var p = pixels[y * size + x];
            sum += p;
            count++;
            if (Luminance(p) > Luminance(peak))
                peak = p;
        }
        return (sum / count, peak);
    }

    private static Color[] ReadThroughRenderTarget(Texture texture, int size)
    {
        // Gamma colour space: the blit copies the stored values unchanged, which is what the old shaders worked on.
        var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        var readback = new Texture2D(size, size, TextureFormat.RGBA32, false);
        readback.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        readback.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        var pixels = readback.GetPixels();
        Object.DestroyImmediate(readback);
        return pixels;
    }

    /// <summary>
    /// Bakes HPMaskTint's albedo (lerp(albedo, saturate(albedo * tint) * overall, mask.r + mask.g + mask.b), with
    /// tint = sum of min(mask channel, colour) * power) from the full-resolution source files into a new texture,
    /// imported with the source albedo's settings. Results above 1 are stored divided by <paramref name="scale"/>.
    /// </summary>
    private static Texture2D BakeMaskTint(Material m, Texture2D albedoTexture, Texture2D maskTexture, string materialPath, out float scale)
    {
        var albedo = ReadSourcePixels(albedoTexture, out int width, out int height);
        var mask = ReadSourcePixels(maskTexture, out int maskWidth, out int maskHeight);

        Color[] colors = { m.GetColor("_Color01"), m.GetColor("_Color02"), m.GetColor("_Color03") };
        float[] powers = { m.GetFloat("_Color01Power"), m.GetFloat("_Color02Power"), m.GetFloat("_Color03Power") };
        float overall = m.GetFloat("_OverallPower");

        int factor = 1;
        while (width / factor > BakeMaxSize || height / factor > BakeMaxSize)
            factor *= 2;
        int outWidth = width / factor, outHeight = height / factor;
        var baked = new float[outWidth * outHeight * 3];
        float max = 1f;

        for (int oy = 0; oy < outHeight; oy++)
        for (int ox = 0; ox < outWidth; ox++)
        {
            float r = 0f, g = 0f, b = 0f;
            for (int fy = 0; fy < factor; fy++)
            for (int fx = 0; fx < factor; fx++)
            {
                int x = ox * factor + fx, y = oy * factor + fy;
                var a = albedo[y * width + x];
                var k = mask[(y * maskHeight / height) * maskWidth + x * maskWidth / width];
                float mr = k.r / 255f, mg = k.g / 255f, mb = k.b / 255f;
                float ar = a.r / 255f, ag = a.g / 255f, ab = a.b / 255f;

                float tr = Mathf.Min(mr, colors[0].r) * powers[0] + Mathf.Min(mg, colors[1].r) * powers[1] + Mathf.Min(mb, colors[2].r) * powers[2];
                float tg = Mathf.Min(mr, colors[0].g) * powers[0] + Mathf.Min(mg, colors[1].g) * powers[1] + Mathf.Min(mb, colors[2].g) * powers[2];
                float tb = Mathf.Min(mr, colors[0].b) * powers[0] + Mathf.Min(mg, colors[1].b) * powers[1] + Mathf.Min(mb, colors[2].b) * powers[2];
                float weight = mr + mg + mb;

                r += ar + (Mathf.Clamp01(ar * tr) * overall - ar) * weight;
                g += ag + (Mathf.Clamp01(ag * tg) * overall - ag) * weight;
                b += ab + (Mathf.Clamp01(ab * tb) * overall - ab) * weight;
            }

            float n = factor * factor;
            int i = (oy * outWidth + ox) * 3;
            baked[i] = Mathf.Max(r / n, 0f);
            baked[i + 1] = Mathf.Max(g / n, 0f);
            baked[i + 2] = Mathf.Max(b / n, 0f);
            max = Mathf.Max(max, baked[i], baked[i + 1], baked[i + 2]);
        }

        scale = Mathf.Ceil(max * 100f) / 100f;
        var output = new Color32[outWidth * outHeight];
        for (int p = 0; p < output.Length; p++)
            output[p] = new Color32(ToByte(baked[p * 3] / scale), ToByte(baked[p * 3 + 1] / scale), ToByte(baked[p * 3 + 2] / scale), 255);

        var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);
        texture.SetPixels32(output);
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);

        string folder = BakeFolder(materialPath);
        string path = $"{folder}/{Path.GetFileNameWithoutExtension(materialPath)}_TintBaked.png";
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        CopyImportSettings(AssetDatabase.GetAssetPath(albedoTexture), path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

    /// <summary>"Model/Materials/x.mat" bakes into "Model/Textures/TCP2 Baked", next to the source textures.</summary>
    private static string BakeFolder(string materialPath)
    {
        string materialFolder = Path.GetDirectoryName(materialPath).Replace('\\', '/');
        string modelFolder = Path.GetDirectoryName(materialFolder).Replace('\\', '/');
        string parent = AssetDatabase.IsValidFolder(modelFolder + "/Textures") ? modelFolder + "/Textures" : materialFolder;
        string folder = parent + "/TCP2 Baked";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(parent, "TCP2 Baked");
        return folder;
    }

    private static Color32[] ReadSourcePixels(Texture2D texture, out int width, out int height)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if ((extension == ".png" || extension == ".jpg" || extension == ".jpeg") && copy.LoadImage(File.ReadAllBytes(path)))
        {
            width = copy.width;
            height = copy.height;
            var pixels = copy.GetPixels32();
            Object.DestroyImmediate(copy);
            return pixels;
        }

        // Other formats: read the imported texture at its imported size.
        Object.DestroyImmediate(copy);
        width = texture.width;
        height = texture.height;
        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        var previous = RenderTexture.active;
        Graphics.Blit(texture, rt);
        RenderTexture.active = rt;
        var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
        readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        var result = readback.GetPixels32();
        Object.DestroyImmediate(readback);
        return result;
    }

    private static void CopyImportSettings(string sourcePath, string targetPath)
    {
        if (!(AssetImporter.GetAtPath(sourcePath) is TextureImporter source) ||
            !(AssetImporter.GetAtPath(targetPath) is TextureImporter target))
            return;

        var settings = new TextureImporterSettings();
        source.ReadTextureSettings(settings);
        // The baked texture is opaque RGB; without an alpha channel the compressed formats are smaller.
        settings.alphaSource = TextureImporterAlphaSource.None;
        target.SetTextureSettings(settings);
        target.maxTextureSize = Mathf.Min(source.maxTextureSize, BakeMaxSize);
        target.textureCompression = source.textureCompression;
        foreach (string platform in new[] { "Standalone", "Android", "iPhone" })
        {
            var platformSettings = source.GetPlatformTextureSettings(platform);
            if (platformSettings.overridden)
                target.SetPlatformTextureSettings(platformSettings);
        }
        target.SaveAndReimport();
    }

    // ---------------------------------------------------------------- materials embedded in models

    /// <summary>
    /// Models that still render with the Standard materials embedded in the FBX get those materials extracted to
    /// "Materials/" next to the model, converted, and remapped in the model importer. Prefabs and scenes that
    /// point at the embedded material directly are repointed at the extracted one.
    /// </summary>
    private static int ExtractEmbeddedMaterials(Shader tcp2, StringBuilder report)
    {
        var replacements = new Dictionary<string, string>();
        int converted = 0;

        foreach (string modelPath in AssetDatabase.FindAssets("t:Model", ScanFolders).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer))
                continue;

            var embedded = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToList();
            if (embedded.Count == 0)
                continue;

            string modelGuid = AssetDatabase.AssetPathToGUID(modelPath);
            string modelFolder = Path.GetDirectoryName(modelPath).Replace('\\', '/');
            string folder = modelFolder + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(modelFolder, "Materials");

            foreach (var source in embedded)
            {
                string path = $"{folder}/{SafeFileName(source.name)}.mat";
                if (File.Exists(path))
                    path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{Path.GetFileNameWithoutExtension(modelPath)}_{SafeFileName(source.name)}.mat");

                var extracted = new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(extracted, path);
                if (TryConvert(extracted, tcp2, path, report))
                    converted++;
                report.AppendLine($"    (extracted from {modelPath} '{source.name}')");

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), extracted);
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string _, out long localId))
                    replacements[$"fileID: {localId}, guid: {modelGuid}, type: 3"] =
                        $"fileID: 2100000, guid: {AssetDatabase.AssetPathToGUID(path)}, type: 2";
            }

            AssetDatabase.SaveAssets();
            importer.SaveAndReimport();
        }

        if (replacements.Count > 0)
            RepointReferences(replacements, report);
        return converted;
    }

    private static void RepointReferences(Dictionary<string, string> replacements, StringBuilder report)
    {
        var files = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && (p.EndsWith(".prefab") || p.EndsWith(".unity")));
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            string updated = replacements.Aggregate(text, (current, pair) => current.Replace(pair.Key, pair.Value));
            if (updated == text)
                continue;

            File.WriteAllText(file, updated);
            AssetDatabase.ImportAsset(file);
            report.AppendLine($"Repointed embedded material references in {file}");
        }
    }

    private static string SafeFileName(string name) =>
        Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
}
