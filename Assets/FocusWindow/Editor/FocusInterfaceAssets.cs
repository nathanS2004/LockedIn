using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LockedIn.Editor
{
    /// <summary>Small UI photographs, generated once in the editor rather than loading six panoramas on Quest.</summary>
    [InitializeOnLoad]
    internal sealed class FocusInterfaceAssets : AssetPostprocessor
    {
        private const string PreviewFolder = "Assets/Resources/LockedIn/Interface/Previews";
        static FocusInterfaceAssets() { EditorApplication.delayCall += Generate; }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] from)
        {
            foreach (string path in imported)
                if (path.StartsWith("Assets/Resources/LockedIn/Backgrounds/", StringComparison.Ordinal) && path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                { EditorApplication.delayCall += Generate; break; }
        }

        [MenuItem("LockedIn/Interface/Generate environment previews")]
        private static void Generate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += Generate; return; }
            Directory.CreateDirectory(PreviewFolder);
            string[] names = { "sundowner_overlook", "secluded_beach", "shanghai_bund", "library", "beach", "space_stars" };
            bool changed = false;
            foreach (string name in names)
            {
                string source = "Assets/Resources/LockedIn/Backgrounds/" + name + ".jpg";
                string output = PreviewFolder + "/" + name + ".png";
                if (!File.Exists(source) || File.Exists(output) && File.GetLastWriteTimeUtc(output) >= File.GetLastWriteTimeUtc(source)) continue;
                Texture2D original = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
                if (original == null) continue;
                RenderTexture temporary = RenderTexture.GetTemporary(512, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                RenderTexture previous = RenderTexture.active;
                Texture2D thumbnail = null;
                try
                {
                    Graphics.Blit(original, temporary);
                    RenderTexture.active = temporary;
                    thumbnail = new Texture2D(512, 256, TextureFormat.RGB24, false);
                    thumbnail.ReadPixels(new Rect(0, 0, 512, 256), 0, 0); thumbnail.Apply();
                    File.WriteAllBytes(output, thumbnail.EncodeToPNG()); changed = true;
                }
                finally
                {
                    RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary);
                    if (thumbnail != null) UnityEngine.Object.DestroyImmediate(thumbnail);
                }
            }
            if (changed) AssetDatabase.Refresh();
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(PreviewFolder + "/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
            importer.maxTextureSize = 512; importer.mipmapEnabled = false; importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Android", overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_6x6
            });
        }
    }
}
