using Aseprite.Chunks;
using UnityEngine;

namespace Aseprite.PixelFormats
{
    public class IndexedPixel : Pixel
    {
        public byte Index { get; private set; } = default;

        public IndexedPixel(Frame frame, byte index) : base(frame)
            => Index = index;

        public override Color GetColor()
        {
            var palette = Frame.File.GetChunk<PaletteChunk>();

            if (palette != default) return palette.GetColor(Index);
            else return Color.magenta;
        }
    }
}
