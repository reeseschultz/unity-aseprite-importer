using System.IO;

namespace Aseprite.Chunks
{
    public class CelExtraChunk : Chunk
    {
        public uint Flags { get; private set; } = default;
        public float PreciseX { get; private set; } = default;
        public float PreciseY { get; private set; } = default;
        public float Width { get; private set; } = default;
        public float Height { get; private set; } = default;

        public CelExtraChunk(uint length, BinaryReader reader) : base(length, ChunkType.CelExtra)
        {
            Flags = reader.ReadUInt32();
            PreciseX = reader.ReadSingle();
            PreciseY = reader.ReadSingle();
            Width = reader.ReadSingle();
            Height = reader.ReadSingle();

            reader.ReadBytes(16); // For Future
        }
    }
}
