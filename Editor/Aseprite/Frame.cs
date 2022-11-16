using Aseprite.Chunks;
using System.Collections.Generic;
using System.IO;

namespace Aseprite
{
    public class Frame
    {
        public AseFile File = default;

        public uint Length { get; private set; } = default;
        public ushort MagicNumber { get; private set; } = default;

        public ushort OldChunksCount { get; private set; } = default;
        public uint ChunksCount { get; private set; } = default;
        public ushort FrameDuration { get; private set; } = default;

        public List<Chunk> Chunks { get; private set; } = default;

        bool useNewChunkCount = true;

        public uint GetChunkCount()
            => useNewChunkCount ? ChunksCount : OldChunksCount;

        public Frame(AseFile file, BinaryReader reader)
        {
            File = file;

            Length = reader.ReadUInt32();
            MagicNumber = reader.ReadUInt16();

            OldChunksCount = reader.ReadUInt16();
            FrameDuration = reader.ReadUInt16();

            reader.ReadBytes(2); // For Future

            ChunksCount = reader.ReadUInt32();

            if (ChunksCount == 0) useNewChunkCount = false;

            Chunks = new List<Chunk>();

            for (var i = 0; i < GetChunkCount(); ++i)
            {
                var chunk = Chunk.ReadChunk(this, reader);

                if (chunk != default) Chunks.Add(chunk);
            }
        }

        public T GetChunk<T>() where T : Chunk
        {
            for (var i = 0; i < Chunks.Count; ++i)
                if (Chunks[i] is T)
                    return (T)Chunks[i];

            return default;
        }

        public T GetCelChunk<T>(int layerIndex) where T : CelChunk
        {
            for (var i = 0; i < Chunks.Count; ++i)
                if (Chunks[i] is T && (Chunks[i] as CelChunk).LayerIndex == layerIndex)
                    return (T)Chunks[i];

            return default;
        }

        public List<T> GetChunks<T>() where T : Chunk
        {
            var chunks = new List<T>();

            for (var i = 0; i < Chunks.Count; ++i)
                if (Chunks[i] is T)
                    chunks.Add((T)Chunks[i]);

            return chunks;
        }
    }
}
