using UnityEngine;
using UnityEditor;

/// <summary>
/// Zet alle texturen in Resources/Sprites op:
/// - TextureType = Sprite, Single mode
/// - FilterMode = Bilinear (geen pixelation)
/// - AlphaIsTransparency = true
/// Tools > Bumpkins > Fix Resource Sprite Import
/// </summary>
public static class FixSpriteImport
{
    [MenuItem("Tools/Bumpkins/Fix Resource Sprite Import")]
    public static void Fix()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Sprites" });
        int fixedCount = 0;
        foreach (var guid in guids)
        {
            var path     = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; dirty = true; }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }

            if (!importer.alphaIsTransparency)
            { importer.alphaIsTransparency = true; dirty = true; }

            // Filter mode: Bilinear voorkomt pixelation
            var settings = importer.GetDefaultPlatformTextureSettings();
            if (importer.filterMode != FilterMode.Bilinear)
            { importer.filterMode = FilterMode.Bilinear; dirty = true; }

            if (dirty)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                fixedCount++;
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Fixed {fixedCount} textures in Resources/Sprites", "OK");
    }
}
