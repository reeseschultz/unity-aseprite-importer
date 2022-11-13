using Aseprite.PixelFormats;
using System.IO;

namespace Aseprite.Chunks
{
    public enum CelType : ushort
    {
        Raw = 0,
        Linked = 1,
        Compressed = 2
    }

    public class CelChunk : Chunk
    {
        public ushort LayerIndex { get; private set; } = default;
        public short X { get; private set; } = default;
        public short Y { get; private set; } = default;
        public virtual ushort Width { get; protected set; } = default;
        public virtual ushort Height { get; protected set; } = default;
        public byte Opacity { get; set; } = default;
        public CelType CelType { get; set; } = default;
        public virtual Pixel[] RawPixelData { get; protected set; } = default;

        public CelChunk(uint length, ushort layerIndex, short x, short y, byte opacity, CelType type) : base(length, ChunkType.Cel)
        {
            LayerIndex = layerIndex;
            X = x;
            Y = y;
            Opacity = opacity;
            CelType = type;
        }

        protected void ReadPixelData(BinaryReader reader, Frame frame)
        {
            var size = Width * Height;

            RawPixelData = new Pixel[size];

            switch (frame.File.Header.ColorDepth)
            {
                case ColorDepth.RGBA:
                    for (var i = 0; i < size; ++i)
                    {
                        var color = reader.ReadBytes(4);

                        RawPixelData[i] = new RGBAPixel(frame, color);
                    }
                    break;
                case ColorDepth.Grayscale:
                    for (var i = 0; i < size; ++i)
                    {
                        var color = reader.ReadBytes(2);

                        RawPixelData[i] = new GrayscalePixel(frame, color);
                    }
                    break;
                case ColorDepth.Indexed:
                    for (var i = 0; i < size; ++i)
                    {
                        var color = reader.ReadByte();

                        RawPixelData[i] = new IndexedPixel(frame, color);
                    }
                    break;
            }
        }

        public static CelChunk ReadCelChunk(uint length, BinaryReader reader, Frame frame)
        {
            var layerIndex = reader.ReadUInt16();
            var x = reader.ReadInt16();
            var y = reader.ReadInt16();
            var opacity = reader.ReadByte();
            var type = (CelType)reader.ReadUInt16();

            reader.ReadBytes(7); // For Future

            switch (type)
            {
                case CelType.Raw:
                    return new RawCelChunk(length, layerIndex, x, y, opacity, frame, reader);
                case CelType.Linked:
                    return new LinkedCelChunk(length, layerIndex, x, y, opacity, frame, reader);
                case CelType.Compressed:
                    return new CompressedCelChunk(length, layerIndex, x, y, opacity, frame, reader);
            }

            return default;
        }
    }
}
