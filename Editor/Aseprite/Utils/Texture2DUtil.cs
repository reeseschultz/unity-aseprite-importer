using UnityEngine;

namespace Aseprite.Utils
{
    public class Texture2DUtil
    {
        public static Texture2D CreateTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color[width * height];

            for (var i = 0; i < pixels.Length; ++i) pixels[i] = UnityEngine.Color.clear;

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
