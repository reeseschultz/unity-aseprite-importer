using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class GrayscalePixel : Pixel
    {
        public byte[] Color { get; private set; } = default;

        public GrayscalePixel(Frame frame, byte[] color) : base(frame)
            => Color = color;

        public override Color GetColor()
        {
            var value = (float)Color[0] / 255;
            var alpha = (float)Color[1] / 255;

            return new Color(value, value, value, alpha);
        }
    }
}
