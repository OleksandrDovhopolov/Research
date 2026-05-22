using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Core.Editor
{
    public static class SpriteAtlasSourceFixer
    {
        [MenuItem("Tools/Fix SpriteAtlas Source Compression")]
        public static void FixAll()
        {
            var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
            int fixedCount = 0;

            foreach (var guid in atlasGuids)
            {
                var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null) continue;

                var packedSprites = atlas.GetPackables();
                foreach (var obj in packedSprites)
                {
                    if (obj is DefaultAsset folder)
                    {
                        fixedCount += FixFolder(AssetDatabase.GetAssetPath(folder));
                    }
                    else if (obj is Sprite sprite)
                    {
                        if (FixSprite(AssetDatabase.GetAssetPath(sprite)))
                            fixedCount++;
                    }
                    else if (obj is Texture2D tex)
                    {
                        if (FixSprite(AssetDatabase.GetAssetPath(tex)))
                            fixedCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SpriteAtlasSourceFixer] Done. Fixed {fixedCount} texture(s).");
        }

        private static int FixFolder(string folderPath)
        {
            int count = 0;
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (var g in guids)
            {
                if (FixSprite(AssetDatabase.GUIDToAssetPath(g)))
                    count++;
            }
            return count;
        }

        private static bool FixSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                return false;

            var settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.format == TextureImporterFormat.Automatic &&
                importer.textureCompression == TextureImporterCompression.Uncompressed)
                return false;

            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return true;
        }
    }
}
