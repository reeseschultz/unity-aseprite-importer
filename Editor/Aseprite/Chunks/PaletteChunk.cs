using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Aseprite.Chunks
{
    public class PaletteEntry
    {
        public ushort EntryFlags { get; private set; } = default;

        public byte Red { get; private set; } = default;
        public byte Green { get; private set; } = default;
        public byte Blue { get; private set; } = default;
        public byte Alpha { get; private set; } = default;

        public string Name { get; private set; } = default;

        public PaletteEntry(BinaryReader reader)
        {
            EntryFlags = reader.ReadUInt16();

            Red = reader.ReadByte();
            Green = reader.ReadByte();
            Blue = reader.ReadByte();
            Alpha = reader.ReadByte();

            if ((EntryFlags & 1) != 0) Name = reader.ReadString();
        }
    }

    public class PaletteChunk : Chunk
    {
        public uint PaletteSize { get; private set; } = default;
        public uint FirstColorIndex { get; private set; } = default;
        public uint LastColorIndex { get; private set; } = default;

        // Future (8) bytes

        public List<PaletteEntry> Entries { get; private set; } = default;

        public PaletteChunk(uint length, BinaryReader reader) : base(length, ChunkType.Palette)
        {
            PaletteSize = reader.ReadUInt32();
            FirstColorIndex = reader.ReadUInt32();
            LastColorIndex = reader.ReadUInt32();

            reader.ReadBytes(8); // For Future

            Entries = new List<PaletteEntry>();

            for (var i = 0; i < PaletteSize; ++i)
                Entries.Add(new PaletteEntry(reader));
        }

        public Color GetColor(byte index)
        {
            if (index >= FirstColorIndex && index <= LastColorIndex)
            {
                var entry = Entries[index];

                var red = (float)entry.Red / 255f;
                var green = (float)entry.Green / 255f;
                var blue = (float)entry.Blue / 255f;
                var alpha = (float)entry.Alpha / 255f;

                return new Color(red, green, blue, alpha);
            }

            return Color.magenta;
        }
    }
}
