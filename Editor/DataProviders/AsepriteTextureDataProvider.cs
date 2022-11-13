using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace AsepriteImporter.DataProviders
{
    public class AsepriteTextureDataProvider : ITextureDataProvider
    {
        readonly AseFileImporter aseFileImporter = default;

        public AsepriteTextureDataProvider(AseFileImporter aseFileImporter)
            => this.aseFileImporter = aseFileImporter;

        public Texture2D texture => aseFileImporter.Texture;

        public Texture2D previewTexture => aseFileImporter.Texture;

        public Texture2D GetReadableTexture2D()
        {
            if (aseFileImporter.textureImporterSettings.spriteMode == (int)SpriteImportMode.Multiple)
                return aseFileImporter.Texture;

            return default;
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = aseFileImporter.Texture.width;
            height = aseFileImporter.Texture.height;
        }
    }
}
