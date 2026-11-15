using UnityEditor;
using UnityEngine;
namespace LockedIn.Editor {
 internal sealed class PanoramaImport : AssetPostprocessor {
  void OnPreprocessTexture() {
   if (!assetPath.StartsWith("Assets/Resources/LockedIn/Backgrounds/")) return;
   var importer = (TextureImporter)assetImporter;
   importer.textureType = TextureImporterType.Default;
   importer.textureShape = TextureImporterShape.Texture2D;
   importer.sRGBTexture = true;
   importer.maxTextureSize = 4096;
   importer.npotScale = TextureImporterNPOTScale.ToNearest;
   importer.isReadable = false;
   importer.mipmapEnabled = true;
   importer.wrapModeU = TextureWrapMode.Repeat;
   importer.wrapModeV = TextureWrapMode.Clamp;
   importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
    name = "Android", overridden = true, maxTextureSize = 4096,
    format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50
   });
  }
 }
}
