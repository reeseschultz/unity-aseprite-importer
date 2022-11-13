using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class RGBAPixel : Pixel
    {
        public byte[] Color { get; private set; } = default;

        public RGBAPixel(Frame frame, byte[] color) : base(frame)
            => Color = color;

        public override Color GetColor()
        {
            if (Color.Length == 4)
            {
                var red = (float)Color[0] / 255f;
                var green = (float)Color[1] / 255f;
                var blue = (float)Color[2] / 255f;
                var alpha = (float)Color[3] / 255f;

                return new Color(red, green, blue, alpha);
            }

            return UnityEngine.Color.magenta;
        }
    }
}
