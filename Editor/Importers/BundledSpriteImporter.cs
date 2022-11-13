using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aseprite;
using AsepriteImporter.Data;
using AsepriteImporter.DataProviders;
using AsepriteImporter.Settings;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace AsepriteImporter.Importers
{
    public class BundledSpriteImporter : SpriteImporter
    {
        [SerializeField] public Texture2D atlas = default;

        [SerializeField] Texture2D thumbnail = default;

        [SerializeField] int frameCount = default;
        [SerializeField] int textureWidth = default;
        [SerializeField] int textureHeight = default;

        [SerializeField] Sprite[] sprites = default;

        public Texture2D Thumbnail => thumbnail;
        public override Sprite[] Sprites => sprites;

        public override SpriteImportMode spriteImportMode => (SpriteImportMode)TextureImportSettings.spriteMode;
        public override float pixelsPerUnit => TextureImportSettings.spritePixelsPerUnit;
        public override UnityEngine.Object targetObject => Importer;

        string name = default;

        public BundledSpriteImporter(AseFileImporter importer) : base(importer) { }

        public override void OnImport()
        {
            name = GetFileName(AssetPath);
            sprites = new Sprite[0];

            GenerateAtlasTexture();

            if (
                SpriteImportData == default ||
                SpriteImportData.Length == 0 ||
                TextureImportSettings.spriteMode == (int)SpriteImportMode.Single
            ) SetSingleSpriteImportData();

            ProcessAnimationSettings();

            GenerateTexture(AssetPath);

            ApplySpritesToAnimation();

            Context.AddObjectToAsset("Texture", Texture);
            Context.SetMainObject(Texture);

            foreach (Sprite sprite in sprites) Context.AddObjectToAsset(sprite.name, sprite);

            if (Settings.generateAnimations)
            {
                var animationImporter = new AnimationImporter(AsepriteFile);
                var animations = animationImporter.GenerateAnimations(name, AnimationSettings);

                foreach (var clip in animations)
                {
                    if (clip == default) continue;

                    Context.AddObjectToAsset(clip.name, clip);
                }
            }
        }

        public void SetSingleSpriteImportData()
        {
            var spriteRect = new Rect(0, atlas.height - AsepriteFile.Header.Height, AsepriteFile.Header.Width, AsepriteFile.Header.Height);

            SpriteImportData = new AseFileSpriteImportData[]
            {
                new AseFileSpriteImportData()
                {
                    alignment = SpriteAlignment.Center,
                    border = Vector4.zero,
                    name = name,
                    outline = SpriteAtlasBuilder.GenerateRectOutline(spriteRect),
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = spriteRect,
                    spriteID = GUID.Generate().ToString(),
                    tessellationDetail = 0
                }
            };

            AnimationSettings = default;
        }

        public AseFileSpriteImportData[] GetSingleSpriteImportData()
        {
            var spriteRect = new Rect(0, 0, textureWidth, textureHeight);

            return new AseFileSpriteImportData[]
            {
                new AseFileSpriteImportData()
                {
                    alignment = SpriteAlignment.Center,
                    border = Vector4.zero,
                    name = name,
                    outline = SpriteAtlasBuilder.GenerateRectOutline(spriteRect),
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = spriteRect,
                    spriteID = GUID.Generate().ToString(),
                    tessellationDetail = 0
                }
            };
        }

        void GenerateTexture(string assetPath)
        {
            var textureInformation = new SourceTextureInformation()
            {
                containsAlpha = true,
                hdr = false,
                height = textureHeight,
                width = textureWidth
            };

            var platformSettings = new TextureImporterPlatformSettings()
            {
                overridden = false
            };

            var settings = new TextureGenerationSettings()
            {
                assetPath = assetPath,
                spriteImportData = ConvertAseFileSpriteImportDataToUnity(SpriteImportData),
                textureImporterSettings = TextureImportSettings.ToImporterSettings(),
                enablePostProcessor = false,
                sourceTextureInformation = textureInformation,
                qualifyForSpritePacking = true,
                platformSettings = platformSettings,
                spritePackingTag = "aseprite",
                secondarySpriteTextures = new SecondarySpriteTexture[0]
            };

            var output = TextureGenerator.GenerateTexture(
                settings,
                new Unity.Collections.NativeArray<Color32>(
                    atlas.GetPixels32(),
                    Unity.Collections.Allocator.Temp
                )
            );

            Texture = output.texture;
            thumbnail = output.thumbNail;
            sprites = output.sprites;
        }

        public void GenerateAtlasTexture(bool overwriteSprites = false)
        {
            if (atlas != default) return;

            var atlasBuilder = new SpriteAtlasBuilder(AsepriteFile.Header.Width, AsepriteFile.Header.Height);

            var frames = AsepriteFile.GetFrames();

            atlas = atlasBuilder.GenerateAtlas(frames, out var importData, false);

            textureWidth = atlas.width;
            textureHeight = atlas.height;
            frameCount = importData.Length;

            if (!overwriteSprites) return;

            // Rename sprites:

            for (var i = 0; i < importData.Length; ++i)
                importData[i].name = string.Format("{0}_{1}", name, importData[i].name);

            SpriteRects = new SpriteRect[0];
            SpriteImportData = importData;

            if (SpriteImportData.Length > 1)
                TextureImportSettings.spriteMode = (int)SpriteImportMode.Multiple;

            AssetDatabase.WriteImportSettingsIfDirty(AssetPath);
        }

        void ProcessAnimationSettings()
        {
            var animationImporter = new AnimationImporter(AsepriteFile);

            if (AnimationSettings == default || AnimationSettings.Length == 0)
            {
                AnimationSettings = animationImporter.GetAnimationImportSettings();
            }
            else
            {
                var settings = animationImporter.GetAnimationImportSettings();
                var newSettings = new List<AseFileAnimationSettings>();

                foreach (var setting in settings)
                {
                    var currentSetting = Array.Find(AnimationSettings, s => s.animationName == setting.animationName);

                    if (currentSetting != default) newSettings.Add(currentSetting); // settings already exist
                    else newSettings.Add(setting); // new settings
                }

                AnimationSettings = newSettings.ToArray();
            }
        }

        void ApplySpritesToAnimation()
        {
            if (sprites.Length != frameCount) return;

            for (var i = 0; i < AnimationSettings.Length; ++i)
            {
                var settings = AnimationSettings[i];

                for (var n = 0; n < settings.sprites.Length; ++n)
                    if (settings.sprites[n] == default)
                        settings.sprites[n] = sprites[settings.frameNumbers[n]];
            }
        }

        string GetFileName(string assetPath)
        {
            var parts = assetPath.Split('/');
            var filename = parts[parts.Length - 1];

            return filename.Substring(0, filename.LastIndexOf('.'));
        }

        string GetPath(string assetPath)
        {
            var parts = assetPath.Split('/');
            var filename = parts[parts.Length - 1];

            return assetPath.Replace(filename, "");
        }

        static AseFile ReadAseFile(string assetPath)
        {
            var fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.Read);
            var aseFile = new AseFile(fileStream);

            fileStream.Close();

            return aseFile;
        }

        public override void Apply()
        {
            if (SpriteRects != default && SpriteRects.Length > 0)
            {
                var newImportData = new List<AseFileSpriteImportData>();

                foreach (SpriteRect spriteRect in SpriteRects)
                {
                    var data = new AseFileSpriteImportData()
                    {
                        alignment = spriteRect.alignment,
                        border = spriteRect.border,
                        name = spriteRect.name,
                        pivot = spriteRect.pivot,
                        rect = spriteRect.rect,
                        spriteID = spriteRect.spriteID.ToString()
                    };

                    var current = Array.Find<AseFileSpriteImportData>(
                        SpriteImportData,
                        d => d.spriteID == spriteRect.spriteID.ToString()
                    );

                    if (current != default)
                    {
                        data.outline = current.outline;
                        data.tessellationDetail = current.tessellationDetail;
                    }
                    else
                    {
                        data.outline = SpriteAtlasBuilder.GenerateRectOutline(data.rect);
                        data.tessellationDetail = 0;
                    }

                    newImportData.Add(data);
                }

                SpriteRects = new SpriteRect[0];
                SpriteImportData = newImportData.ToArray();
                EditorUtility.SetDirty(Importer);
            }

            AssetDatabase.WriteImportSettingsIfDirty(AssetPath);
            AssetDatabase.Refresh();
            AssetDatabase.LoadAllAssetsAtPath(AssetPath);
            EditorApplication.RepaintProjectWindow();
        }

        static SpriteImportData[] ConvertAseFileSpriteImportDataToUnity(AseFileSpriteImportData[] spriteImportData)
        {
            var importData = new SpriteImportData[spriteImportData.Length];

            for (var i = 0; i < spriteImportData.Length; ++i)
                importData[i] = spriteImportData[i].ToSpriteImportData();

            return importData;
        }
    }
}