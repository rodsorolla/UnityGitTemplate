using UnityEditor;

namespace Sorolla
{
    /// <summary>
    /// Sets default texture import settings for new images.
    /// </summary>
    public class TextureImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            // Only apply to first-time imports
            if (assetImporter.importSettingsMissing)
            {
                var textureImporter = (TextureImporter)assetImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
