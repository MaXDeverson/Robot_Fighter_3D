using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Mobile Textures: gives every texture in Assets an Android and iOS override that suits phones —
/// ASTC compression and a capped max size. Lightmaps, lookup ramps, noise, tiny textures, textures read by code
/// and editor-only textures keep their settings. Running it again changes only textures that drifted from the rules.
/// </summary>
public static class MobileTextureOptimizer
{
    private const string MenuRoot = "Tools/Mobile Textures/";
    private static readonly string[] Platforms = { "Android", "iPhone" };

    private const int MaxSize3D = 1024;
    private const int MaxSizeUI = 1024;
    private const int MaxSizeFullScreenUI = 2048;
    private const int MaxSizeCubemap = 2048;
    // A sprite this wide is a background or a full-width panel and needs 2048 to stay sharp on a phone screen.
    private const int FullScreenUIWidth = 1600;
    // Compressing a texture this small saves nothing worth the artifacts.
    private const int TinySize = 32;

    // Gradients and lookup data band or shift when block-compressed.
    private static readonly string[] LookupNameParts = { "ramp", "gradient", "lut", "lookup", "noise" };

    private static readonly HashSet<TextureImporterFormat> UncompressedFormats = new HashSet<TextureImporterFormat>
    {
        TextureImporterFormat.RGB24, TextureImporterFormat.RGBA32, TextureImporterFormat.ARGB32,
        TextureImporterFormat.RGB16, TextureImporterFormat.RGBA16, TextureImporterFormat.ARGB16,
        TextureImporterFormat.R8, TextureImporterFormat.R16, TextureImporterFormat.RG16,
        TextureImporterFormat.RGBAHalf, TextureImporterFormat.RGBAFloat,
    };

    [MenuItem(MenuRoot + "Preview Changes")]
    private static void Preview() => Run(apply: false);

    [MenuItem(MenuRoot + "Optimize All")]
    private static void OptimizeAll() => Run(apply: true);

    private static void Run(bool apply)
    {
        var changes = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        bool cancelled = false;

        try
        {
            if (apply)
                AssetDatabase.StartAssetEditing();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar("Mobile Textures", path, (float)i / guids.Length))
                {
                    cancelled = true;
                    break;
                }

                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || ShouldSkip(path, importer))
                    continue;

                string change = Optimize(path, importer, apply);
                if (change != null)
                    changes.Add($"{path}: {change}");
            }
        }
        finally
        {
            // Every texture saved above is reimported here, in one batch.
            if (apply)
                AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        var log = new StringBuilder();
        log.AppendLine(apply
            ? $"[MobileTextures] Optimized {changes.Count} textures{(cancelled ? " (cancelled)" : string.Empty)}."
            : $"[MobileTextures] {changes.Count} textures would change:");
        foreach (string change in changes)
            log.AppendLine(change);
        Debug.Log(log.ToString());
    }

    private static bool ShouldSkip(string path, TextureImporter importer)
    {
        if (path.Contains("/Editor/"))
            return true;

        switch (importer.textureType)
        {
            case TextureImporterType.Lightmap:
            case TextureImporterType.DirectionalLightmap:
            case TextureImporterType.Shadowmask:
            case TextureImporterType.SingleChannel:
                return true;
        }

        if (importer.isReadable && !IsSprite(importer))
            return true;

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (Mathf.Max(width, height) <= TinySize || Mathf.Min(width, height) <= 4)
            return true;

        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        return LookupNameParts.Any(name.Contains);
    }

    /// <summary>Returns what changes for this texture, or null when it already fits the rules.</summary>
    private static string Optimize(string path, TextureImporter importer, bool apply)
    {
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        var android = importer.GetPlatformTextureSettings("Android");

        int currentMaxSize = android.overridden ? android.maxTextureSize : importer.maxTextureSize;
        int maxSize = Mathf.Min(currentMaxSize, MaxSizeCap(importer, width, height));
        var format = TargetFormat(importer, android);
        bool ui = IsSprite(importer) && path.Contains("/UI/");

        var platformsToSet = Platforms
            .Select(platform => importer.GetPlatformTextureSettings(platform))
            .Where(s => !s.overridden || s.maxTextureSize != maxSize || s.format != format)
            .ToList();
        bool mipmapsOff = ui && importer.mipmapEnabled;
        bool readWriteOff = ui && importer.isReadable;

        if (platformsToSet.Count == 0 && !mipmapsOff && !readWriteOff)
            return null;

        var description = new List<string>();
        if (maxSize != currentMaxSize)
            description.Add($"max {currentMaxSize}→{maxSize}");
        string currentFormat = android.overridden ? android.format.ToString() : importer.textureCompression.ToString();
        if (currentFormat != format.ToString())
            description.Add($"{currentFormat}→{format}");
        if (platformsToSet.Count > 0 && description.Count == 0)
            description.Add("override for " + string.Join("/", platformsToSet.Select(s => s.name)));
        if (mipmapsOff)
            description.Add("mipmaps off");
        if (readWriteOff)
            description.Add("read/write off");

        if (apply)
        {
            foreach (var settings in platformsToSet)
            {
                settings.overridden = true;
                settings.maxTextureSize = maxSize;
                settings.format = format;
                importer.SetPlatformTextureSettings(settings);
            }

            if (mipmapsOff)
                importer.mipmapEnabled = false;
            if (readWriteOff)
                importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return string.Join(", ", description);
    }

    private static int MaxSizeCap(TextureImporter importer, int width, int height)
    {
        if (importer.textureShape == TextureImporterShape.TextureCube)
            return MaxSizeCubemap;
        if (IsSprite(importer))
            return Mathf.Max(width, height) >= FullScreenUIWidth ? MaxSizeFullScreenUI : MaxSizeUI;
        return MaxSize3D;
    }

    private static TextureImporterFormat TargetFormat(TextureImporter importer,
        TextureImporterPlatformSettings android)
    {
        // An ASTC block size someone already picked for this texture is kept.
        if (android.overridden && android.format.ToString().StartsWith("ASTC"))
            return android.format;
        if (importer.textureShape == TextureImporterShape.TextureCube)
            return TextureImporterFormat.ASTC_6x6;
        // A texture kept uncompressed or high quality gets the sharpest ASTC — still 4x smaller than RGBA32.
        return WasHighQuality(importer, android) ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
    }

    private static bool WasHighQuality(TextureImporter importer, TextureImporterPlatformSettings android)
    {
        if (android.overridden && android.format != TextureImporterFormat.Automatic)
            return UncompressedFormats.Contains(android.format);

        var compression = android.overridden ? android.textureCompression : importer.textureCompression;
        return compression == TextureImporterCompression.Uncompressed ||
               compression == TextureImporterCompression.CompressedHQ;
    }

    private static bool IsSprite(TextureImporter importer) =>
        importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI;
}
