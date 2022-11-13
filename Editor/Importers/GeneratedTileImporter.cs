using System;
using System.Collections.Generic;
using System.IO;
using Aseprite.Utils;
using UnityEditor;
using UnityEngine;

namespace AsepriteImporter
{
    public class GeneratedTileImporter : SpriteImporter
    {
        int padding = 1;
        Vector2Int size = default;
        string fileName = default;
        string filePath = default;
        Texture2D atlas = default;

        public GeneratedTileImporter(AseFileImporter importer) : base(importer) { }

        public override void OnImport()
        {
            size = new Vector2Int(AsepriteFile.Header.Width, AsepriteFile.Header.Height);

            var frame = AsepriteFile.GetFrames()[0];

            BuildAtlas(AssetPath, frame);
        }

        protected override bool OnUpdate()
            => GenerateSprites(filePath, size);

        void BuildAtlas(string acePath, Texture2D sprite)
        {
            fileName = Path.GetFileNameWithoutExtension(acePath);

            var directoryName = Path.GetDirectoryName(acePath) + "/" + fileName;

            if (!AssetDatabase.IsValidFolder(directoryName))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(acePath), fileName);

            filePath = directoryName + "/" + fileName + ".png";

            GenerateAtlas(sprite);

            try
            {
                File.WriteAllBytes(filePath, atlas.EncodeToPNG());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        void GenerateAtlas(Texture2D sprite)
        {
            var spriteSizeW = Settings.tileSize.x + padding * 2;
            var spriteSizeH = Settings.tileSize.y + padding * 2;
            var cols = sprite.width / Settings.tileSize.x;
            var rows = sprite.height / Settings.tileSize.y;
            var width = cols * spriteSizeW;
            var height = rows * spriteSizeH;

            atlas = Texture2DUtil.CreateTransparentTexture(width, height);

            for (var row = 0; row < rows; ++row)
            {
                for (var col = 0; col < cols; ++col)
                {
                    var from = new RectInt(
                        col * Settings.tileSize.x,
                        row * Settings.tileSize.y,
                        Settings.tileSize.x,
                        Settings.tileSize.y
                    );

                    var to = new RectInt(
                        col * spriteSizeW + padding,
                        row * spriteSizeH + padding,
                        Settings.tileSize.x,
                        Settings.tileSize.y
                    );

                    CopyColors(sprite, atlas, from, to);

                    atlas.Apply();
                }
            }
        }

        Color[] GetPixels(Texture2D sprite, RectInt from)
        {
            var res = sprite.GetPixels(from.x, from.y, from.width, from.height);

            if (Settings.transparencyMode != TransparencyMode.Mask) return res;

            for (var index = 0; index < res.Length;)
            {
                var color = res[index++];

                if (color == Settings.transparentColor)
                {
                    color.r = color.g = color.b = color.a = 0;
                    res[index] = color;
                }
            }

            return res;
        }

        Color GetPixel(Texture2D sprite, int x, int y)
        {
            var color = sprite.GetPixel(x, y);

            if (
                Settings.transparencyMode == TransparencyMode.Mask &&
                color == Settings.transparentColor
            ) color.r = color.g = color.b = color.a = 0;

            return color;
        }

        void CopyColors(Texture2D sprite, Texture2D atlas, RectInt from, RectInt to)
        {
            atlas.SetPixels(to.x, to.y, to.width, to.height, GetPixels(sprite, from));

            for (var index = 0; index < padding; ++index)
            {
                var lf = new RectInt(from.x, from.y, 1, from.height);
                var lt = new RectInt(to.x - index - 1, to.y, 1, to.height);
                var rf = new RectInt(from.xMax - 1, from.y, 1, from.height);
                var rt = new RectInt(to.xMax + index, to.y, 1, to.height);

                atlas.SetPixels(lt.x, lt.y, lt.width, lt.height, GetPixels(sprite, lf));
                atlas.SetPixels(rt.x, rt.y, rt.width, rt.height, GetPixels(sprite, rf));
            }

            for (var index = 0; index < padding; ++index)
            {
                var tf = new RectInt(from.x, from.y, from.width, 1);
                var tt = new RectInt(to.x, to.y - index - 1, to.width, 1);
                var bf = new RectInt(from.x, from.yMax - 1, from.width, 1);
                var bt = new RectInt(to.x, to.yMax + index, to.width, 1);

                atlas.SetPixels(tt.x, tt.y, tt.width, tt.height, GetPixels(sprite, tf));
                atlas.SetPixels(bt.x, bt.y, bt.width, bt.height, GetPixels(sprite, bf));
            }

            for (var x = 0; x < padding; ++x)
            {
                for (var y = 0; y < padding; ++y)
                {
                    atlas.SetPixel(to.x - x - 1, to.y - y - 1, GetPixel(sprite, from.x, from.y));
                    atlas.SetPixel(to.xMax + x, to.y - y - 1, GetPixel(sprite, from.xMax - 1, from.y));
                    atlas.SetPixel(to.x - x - 1, to.yMax + y, GetPixel(sprite, from.x, from.yMax - 1));
                    atlas.SetPixel(to.xMax + x, to.yMax + y, GetPixel(sprite, from.xMax - 1, from.yMax - 1));
                }
            }
        }

        bool GenerateSprites(string path, Vector2Int size)
        {
            this.size = size;

            var fileName = Path.GetFileNameWithoutExtension(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == default) return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = Settings.pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;

            var metaList = CreateMetaData(fileName);
            var oldProperties = AseSpritePostProcess.GetPhysicsShapeProperties(importer, metaList);

            importer.spritesheet = metaList.ToArray();
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            EditorUtility.SetDirty(importer);

            try
            {
                importer.SaveAndReimport();
            }
            catch (Exception e)
            {
                Debug.LogWarning("There was a problem with generating sprite file: " + e);
            }

            var newProperties = AseSpritePostProcess.GetPhysicsShapeProperties(importer, metaList);

            AseSpritePostProcess.RecoverPhysicsShapeProperty(newProperties, oldProperties);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            return true;
        }

        List<SpriteMetaData> CreateMetaData(string fileName)
        {
            var tileSize = Settings.tileSize;
            var cols = size.x / tileSize.x;
            var rows = size.y / tileSize.y;
            var res = new List<SpriteMetaData>();
            var index = 0;
            var height = rows * (tileSize.y + padding * 2);

            for (var row = 0; row < rows; ++row)
            {
                for (var col = 0; col < cols; ++col)
                {
                    var rect = new Rect(
                        col * (tileSize.x + padding * 2) + padding,
                        height - (row + 1) * (tileSize.y + padding * 2) + padding,
                        tileSize.x,
                        tileSize.y
                    );

                    var meta = new SpriteMetaData();

                    if (Settings.tileEmpty == EmptyTileBehaviour.Remove && IsTileEmpty(rect, atlas))
                    {
                        ++index;
                        continue;
                    }

                    meta.name = fileName + "_" + index;

                    if (Settings.tileNameType == TileNameType.RowCol)
                        meta.name = GetRowColTileSpriteName(fileName, col, row, cols, rows);

                    meta.rect = rect;
                    meta.alignment = Settings.spriteAlignment;
                    meta.pivot = Settings.spritePivot;
                    res.Add(meta);

                    ++index;
                }
            }

            return res;
        }

        string GetRowColTileSpriteName(string fileName, int x, int y, int cols, int rows)
        {
            var yHat = y;
            var row = yHat.ToString();
            var col = x.ToString();

            if (rows > 100) row = yHat.ToString("D3");
            else if (rows > 10) row = yHat.ToString("D2");

            if (cols > 100) col = x.ToString("D3");
            else if (cols > 10) col = x.ToString("D2");

            return string.Format("{0}_{1}_{2}", fileName, row, col);
        }

        SerializedProperty GetPhysicsShapeProperty(TextureImporter importer, string spriteName)
        {
            var serializedImporter = new SerializedObject(importer);

            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                var spriteSheetSP = serializedImporter.FindProperty("m_SpriteSheet.m_Sprites");

                for (var i = 0; i < spriteSheetSP.arraySize; ++i)
                    if (importer.spritesheet[i].name == spriteName)
                        spriteSheetSP.GetArrayElementAtIndex(i).FindPropertyRelative("m_PhysicsShape");
            }

            return serializedImporter.FindProperty("m_SpriteSheet.m_PhysicsShape");
        }

        bool IsTileEmpty(Rect tileRect, Texture2D atlas)
        {
            var tilePixels = atlas.GetPixels(
                (int)tileRect.xMin,
                (int)tileRect.yMin,
                (int)tileRect.width,
                (int)tileRect.height
            );

            for (var i = 0; i < tilePixels.Length; ++i)
                if (tilePixels[i].a != 0)
                    return false;

            return true;
        }
    }
}
