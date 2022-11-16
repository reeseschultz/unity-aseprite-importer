using Aseprite.Chunks;
using Aseprite.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Aseprite
{
    // See file specs here: https://github.com/aseprite/aseprite/blob/master/docs/ase-file-specs.md

    public class AseFile
    {
        public Header Header { get; private set; } = default;
        public List<Frame> Frames { get; private set; } = default;

        Dictionary<Type, Chunk> chunkCache = new();

        public AseFile(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var header = reader.ReadBytes(128);

            Header = new Header(header);
            Frames = new List<Frame>();

            while (reader.BaseStream.Position < reader.BaseStream.Length)
                Frames.Add(new Frame(this, reader));
        }

        public List<T> GetChunks<T>() where T : Chunk
        {
            var chunks = new List<T>();

            for (var i = 0; i < this.Frames.Count; ++i)
            {
                var cs = this.Frames[i].GetChunks<T>();

                chunks.AddRange(cs);
            }

            return chunks;
        }

        public T GetChunk<T>() where T : Chunk
        {
            if (!chunkCache.ContainsKey(typeof(T)))
            {
                for (var i = 0; i < this.Frames.Count; ++i)
                {
                    var cs = this.Frames[i].GetChunks<T>();

                    if (cs.Count > 0)
                    {
                        chunkCache.Add(typeof(T), cs[0]);
                        break;
                    }
                }
            }

            return (T)chunkCache[typeof(T)];
        }

        // TODO: move someplace else
        [Serializable]
        public class LayerAsFrames
        {
            public string Name = default;
            public List<FrameCel> FrameCels = default;
        }

        public List<LayerAsFrames> GetLayersAsFrames()
        {
            var layerChunks = GetChunks<LayerChunk>();
            var layersOfFrames = new List<LayerAsFrames>();

            for (var i = 0; i < layerChunks.Count; ++i)
            {
                var layerFrames = GetFrameCels(i, layerChunks[i]);

                if (layerFrames.Count < 1) continue;

                layersOfFrames.Add(new LayerAsFrames()
                {
                    Name = layerChunks[i].LayerName,
                    FrameCels = layerFrames
                });
            }

            return layersOfFrames;
        }

        // TODO: move someplace else
        [Serializable]
        public class FrameCel
        {
            public int Frame = default;
            public Texture2D Cel = default;

            public FrameCel(int frame, Texture2D cel)
            {
                Frame = frame;
                Cel = cel;
            }
        }

        public List<FrameCel> GetFrameCels(int layerIndex, LayerChunk layer)
        {
            var layers = GetChunks<LayerChunk>();
            var frameCels = new List<FrameCel>();

            for (var frameIndex = 0; frameIndex < Frames.Count; ++frameIndex)
            {
                var frame = Frames[frameIndex];
                var cels = frame.GetChunks<CelChunk>();

                for (var i = 0; i < cels.Count; ++i)
                {
                    if (cels[i].LayerIndex != layerIndex) continue;

                    var visibility = layer.Visible;

                    var parent = GetParentLayer(layer);
                    while (parent != default)
                    {
                        visibility &= parent.Visible;

                        if (visibility == false) break;

                        parent = GetParentLayer(parent);
                    }

                    if (
                        visibility == false ||
                        layer.LayerType == LayerType.Group
                    ) continue;

                    frameCels.Add(
                        new FrameCel(
                            frameIndex,
                            GetTextureFromCel(cels[i])
                        )
                    );
                }
            }

            return frameCels;
        }

        public Texture2D[] GetFrames()
        {
            var frames = new List<Texture2D>();

            for (var i = 0; i < Frames.Count; ++i) frames.Add(GetFrame(i));

            return frames.ToArray();
        }

        LayerChunk GetParentLayer(LayerChunk layer)
        {
            if (layer.LayerChildLevel == 0) return default;

            var layers = GetChunks<LayerChunk>();
            var index = layers.IndexOf(layer);

            if (index < 0) return default;

            for (var i = index - 1; i > 0; --i)
                if (layers[i].LayerChildLevel == layer.LayerChildLevel - 1)
                    return layers[i];

            return default;
        }

        public Texture2D GetFrame(int index)
        {
            var texture = Texture2DUtil.CreateTransparentTexture(Header.Width, Header.Height);
            var layers = GetChunks<LayerChunk>();
            var frame = Frames[index];
            var cels = frame.GetChunks<CelChunk>();

            cels.Sort((ca, cb) => ca.LayerIndex.CompareTo(cb.LayerIndex));

            for (var i = 0; i < cels.Count; ++i)
            {
                var layer = layers[cels[i].LayerIndex];

                if (layer.LayerName.StartsWith("@")) continue; // ignore metadata layer

                var visibility = layer.Visible;
                var parent = GetParentLayer(layer);

                while (parent != default)
                {
                    visibility &= parent.Visible;

                    if (visibility == false) break;

                    parent = GetParentLayer(parent);
                }

                if (
                    visibility == false ||
                    layer.LayerType == LayerType.Group
                ) continue;

                var celTex = GetTextureFromCel(cels[i]);
                var blendMode = layer.BlendMode;
                var opacity = Mathf.Min(layer.Opacity / 255f, cels[i].Opacity / 255f);

                switch (blendMode)
                {
                    case LayerBlendMode.Normal: texture = Texture2DBlender.Normal(texture, celTex, opacity); break;
                    case LayerBlendMode.Multiply: texture = Texture2DBlender.Multiply(texture, celTex, opacity); break;
                    case LayerBlendMode.Screen: texture = Texture2DBlender.Screen(texture, celTex); break;
                    case LayerBlendMode.Overlay: texture = Texture2DBlender.Overlay(texture, celTex); break;
                    case LayerBlendMode.Darken: texture = Texture2DBlender.Darken(texture, celTex); break;
                    case LayerBlendMode.Lighten: texture = Texture2DBlender.Lighten(texture, celTex); break;
                    case LayerBlendMode.ColorDodge: texture = Texture2DBlender.ColorDodge(texture, celTex); break;
                    case LayerBlendMode.ColorBurn: texture = Texture2DBlender.ColorBurn(texture, celTex); break;
                    case LayerBlendMode.HardLight: texture = Texture2DBlender.HardLight(texture, celTex); break;
                    case LayerBlendMode.SoftLight: texture = Texture2DBlender.SoftLight(texture, celTex); break;
                    case LayerBlendMode.Difference: texture = Texture2DBlender.Difference(texture, celTex); break;
                    case LayerBlendMode.Exclusion: texture = Texture2DBlender.Exclusion(texture, celTex); break;
                    case LayerBlendMode.Hue: texture = Texture2DBlender.Hue(texture, celTex); break;
                    case LayerBlendMode.Saturation: texture = Texture2DBlender.Saturation(texture, celTex); break;
                    case LayerBlendMode.Color: texture = Texture2DBlender.Color(texture, celTex); break;
                    case LayerBlendMode.Luminosity: texture = Texture2DBlender.Luminosity(texture, celTex); break;
                    case LayerBlendMode.Addition: texture = Texture2DBlender.Addition(texture, celTex); break;
                    case LayerBlendMode.Subtract: texture = Texture2DBlender.Subtract(texture, celTex); break;
                    case LayerBlendMode.Divide: texture = Texture2DBlender.Divide(texture, celTex); break;
                }
            }

            return texture;
        }

        public Texture2D GetTextureFromCel(CelChunk cel)
        {
            var canvasWidth = Header.Width;
            var canvasHeight = Header.Height;

            var colors = new Color[canvasWidth * canvasHeight];

            var celXEnd = cel.Width + cel.X;
            var celYEnd = cel.Height + cel.Y;

            var pixelIndex = 0;

            for (var y = cel.Y; y < celYEnd; ++y)
            {
                if (y < 0 || y >= canvasHeight)
                {
                    pixelIndex += cel.Width;
                    continue;
                }

                for (var x = cel.X; x < celXEnd; ++x)
                {
                    if (x >= 0 && x < canvasWidth)
                    {
                        var index = (canvasHeight - 1 - y) * canvasWidth + x;
                        colors[index] = cel.RawPixelData[pixelIndex].GetColor();
                    }

                    ++pixelIndex;
                }
            }

            var texture = Texture2DUtil.CreateTransparentTexture(canvasWidth, canvasHeight);

            texture.SetPixels(0, 0, canvasWidth, canvasHeight, colors);
            texture.Apply();

            return texture;
        }

        public FrameTag[] GetFrameTags()
        {
            var tagChunks = GetChunks<FrameTagsChunk>();
            var animations = new List<FrameTag>();

            foreach (var tagChunk in tagChunks)
                foreach (var tag in tagChunk.Tags)
                    animations.Add(tag);

            return animations.ToArray();
        }

        public MetaData[] GetMetaData(Vector2 spritePivot, int pixelsPerUnit)
        {
            var metadatas = new Dictionary<int, MetaData>();

            for (var index = 0; index < Frames.Count; ++index)
            {
                var layers = GetChunks<LayerChunk>();
                var cels = Frames[index].GetChunks<CelChunk>();

                cels.Sort((ca, cb) => ca.LayerIndex.CompareTo(cb.LayerIndex));

                for (var i = 0; i < cels.Count; ++i)
                {
                    var layerIndex = cels[i].LayerIndex;
                    var layer = layers[layerIndex];

                    if (!layer.LayerName.StartsWith(MetaData.MetaDataChar)) continue; // only read metadata layer

                    if (!metadatas.ContainsKey(layerIndex)) metadatas[layerIndex] = new MetaData(layer.LayerName);

                    var metadata = metadatas[layerIndex];
                    var cel = cels[i];
                    var center = Vector2.zero;
                    var pixelCount = 0;

                    for (var y = 0; y < cel.Height; ++y)
                    {
                        for (var x = 0; x < cel.Width; ++x)
                        {
                            var texX = cel.X + x;
                            var texY = -(cel.Y + y) + Header.Height - 1;
                            var col = cel.RawPixelData[x + y * cel.Width];

                            if (col.GetColor().a > 0.1f)
                            {
                                center += new Vector2(texX, texY);
                                ++pixelCount;
                            }
                        }
                    }

                    if (pixelCount > 0)
                    {
                        center /= pixelCount;

                        var pivot = Vector2.Scale(spritePivot, new Vector2(Header.Width, Header.Height));
                        var posWorld = (center - pivot) / pixelsPerUnit + Vector2.one * 0.5f / pixelsPerUnit; //center pos in middle of pixels

                        metadata.Transforms.Add(index, posWorld);
                    }
                }
            }

            return metadatas.Values.ToArray();
        }

        public Texture2D GetTextureAtlas(List<Texture2D> frames)
        {
            var atlas = Texture2DUtil.CreateTransparentTexture(
                Header.Width * frames.Count,
                Header.Height
            );

            var spriteRects = new List<Rect>();

            var col = 0;
            var row = 0;

            foreach (var frame in frames)
            {
                var spriteRect = new Rect(col++ * Header.Width, atlas.height - ((row + 1) * Header.Height), Header.Width, Header.Height);
                atlas.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, frame.GetPixels());
                atlas.Apply();

                spriteRects.Add(spriteRect);
            }

            return atlas;
        }
    }
}
