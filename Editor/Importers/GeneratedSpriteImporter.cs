using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aseprite;
using Aseprite.Chunks;
using Aseprite.Utils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static Aseprite.AseFile;

namespace AsepriteImporter.Importers
{
    public class GeneratedSpriteImporter : SpriteImporter
    {
        int padding = 1;
        Vector2Int size = default;
        string fileName = default;
        string directoryName = default;
        string filePath = default;
        int rows = default;
        int cols = default;
        Texture2D[] frames = default;

        public GeneratedSpriteImporter(AseFileImporter importer) : base(importer) { }

        public override void OnImport()
        {
            size = new Vector2Int(AsepriteFile.Header.Width, AsepriteFile.Header.Height);
            frames = AsepriteFile.GetFrames();
            BuildAtlas(AssetPath);
        }

        protected override bool OnUpdate()
        {
            var generatedSprites = false;

            if (!Settings.splitLayers && GenerateSprites(filePath, fileName))
            {
                var sprites = GetAllSpritesFromAssetFile(filePath);

                if (Settings.buildAtlas) CreateSpriteAtlas(filePath, sprites);

                GenerateAnimations(sprites, filePath);

                generatedSprites = true;
            }
            else if (Settings.splitLayers && GenerateSpritesSeparatedByLayer(out var layerPaths, out var layerNames))
            {
                var allLayeredSprites = new List<Sprite>();

                for (var i = 0; i < layerPaths.Length; ++i)
                {
                    var sprites = GetAllSpritesFromAssetFile(layerPaths[i]);

                    if (Settings.buildAtlas) allLayeredSprites.AddRange(sprites);

                    GenerateAnimations(sprites, layerPaths[i], layerNames[i]);
                }

                if (Settings.buildAtlas)
                    CreateSpriteAtlas(filePath, allLayeredSprites, true);

                generatedSprites = true;
            }

            return generatedSprites;
        }

        void GenerateAnimations(List<Sprite> sprites, string path, string layerName = "")
        {
            var clips = GenerateAnimationClips(path, sprites, layerName);

            if (Settings.animType == AseAnimatorType.AnimatorController) CreateAnimatorController(clips, path);
            else if (Settings.animType == AseAnimatorType.AnimatorOverrideController) CreateAnimatorOverrideController(clips, path);
        }

        void CreateSpriteAtlas(string path, List<Sprite> sprites, bool forLayers = false)
        {
            var atlasExt = ".spriteatlas";

            if (forLayers) atlasExt = ".Layers" + atlasExt;

            var atlasPath = path.Replace(".png", atlasExt);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);

            if (atlas == default)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            var texSetting = new SpriteAtlasTextureSettings();
            texSetting.filterMode = FilterMode.Point;
            texSetting.generateMipMaps = false;
            texSetting.readable = TextureImportSettings.readable;

            var packSetting = new SpriteAtlasPackingSettings();
            packSetting.padding = 2;
            packSetting.enableRotation = false;
            packSetting.enableTightPacking = true;

            var platformSetting = new TextureImporterPlatformSettings();
            platformSetting.textureCompression = TextureImporterCompression.Uncompressed;
            platformSetting.maxTextureSize = 8192;

            atlas.SetTextureSettings(texSetting);
            atlas.SetPackingSettings(packSetting);
            atlas.SetPlatformSettings(platformSetting);
            atlas.Add(sprites.ToArray());

            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
        }

        List<AnimationClip> GenerateAnimationClips(string path, List<Sprite> sprites, string layerName = "")
        {
            var clips = new List<AnimationClip>();
            var frameTagSet = AsepriteFile.GetFrameTags().ToHashSet();
            var metadata = AsepriteFile.GetMetadata(Settings.spritePivot, Settings.pixelsPerUnit);

            foreach (var frameTag in frameTagSet)
            {
                var tag = frameTag.TagName;
                var spritesForTag = sprites.FindAll(s => s.name.Split(Settings.tagDelimiter)[0] == tag);

                var animPath = path
                    .Replace("/" + fileName + ".png", "")
                    .Replace("/" + layerName + ".png", "") + "/" + tag + ".anim";

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);

                if (clip == default)
                {
                    clip = new AnimationClip();

                    AssetDatabase.CreateAsset(clip, animPath);

                    clip.wrapMode = GetDefaultWrapMode(tag);
                }
                else
                {
                    var animSettings = AnimationUtility.GetAnimationClipSettings(clip);
                    clip.wrapMode = animSettings.loopTime ? WrapMode.Loop : WrapMode.Once;
                }

                clip.frameRate = Settings.samplesPerSecond;
                clip.name = tag;

                var editorBinding = new EditorCurveBinding();
                editorBinding.path = "";
                editorBinding.propertyName = "m_Sprite";

                switch (Settings.bindType)
                {
                    case AseAnimationBindType.SpriteRenderer:
                        editorBinding.type = typeof(SpriteRenderer);
                        break;
                    case AseAnimationBindType.UIImage:
                        editorBinding.type = typeof(Image);
                        break;
                }

                var time = 0f;
                var keyFrames = new ObjectReferenceKeyframe[spritesForTag.Count];
                var transformCurveX = new Dictionary<string, AnimationCurve>();
                var transformCurveY = new Dictionary<string, AnimationCurve>();
                var keyFrameIndex = 0;

                foreach (var sprite in spritesForTag)
                {
                    keyFrames[keyFrameIndex] = new ObjectReferenceKeyframe
                    {
                        time = time,
                        value = sprite
                    };

                    var frame = Int32.Parse(sprite.name.Split(Settings.tagDelimiter)[1]);

                    foreach (var datum in metadata)
                    {
                        if (
                            datum.Type != MetadataType.TRANSFORM ||
                            !datum.Transforms.ContainsKey(frame)
                        ) continue;

                        var childTransform = datum.Args[0];

                        if (!transformCurveX.ContainsKey(childTransform))
                        {
                            transformCurveX[childTransform] = new AnimationCurve();
                            transformCurveY[childTransform] = new AnimationCurve();
                        }

                        var pos = datum.Transforms[frame];

                        transformCurveX[childTransform].AddKey(keyFrameIndex, pos.x);
                        transformCurveY[childTransform].AddKey(keyFrameIndex, pos.y);
                    }

                    if (Settings.constantFrameDuration) time = (keyFrameIndex + 1) / Settings.samplesPerSecond;
                    else time += AsepriteFile.Frames[frame].FrameDuration / 1000f;

                    ++keyFrameIndex;
                }

                if (frameTag.Animation == LoopAnimation.Reverse) // TODO: fix by reversing times
                    keyFrames = keyFrames.Reverse().ToArray();

                AnimationUtility.SetObjectReferenceCurve(clip, editorBinding, keyFrames);

                foreach (var childTransform in transformCurveX.Keys)
                {
                    var bindingX = new EditorCurveBinding
                    {
                        path = childTransform,
                        type = typeof(Transform),
                        propertyName = "m_LocalPosition.x"
                    };

                    var bindingY = new EditorCurveBinding
                    {
                        path = childTransform,
                        type = typeof(Transform),
                        propertyName = "m_LocalPosition.y"
                    };

                    MakeConstant(transformCurveX[childTransform]);
                    AnimationUtility.SetEditorCurve(clip, bindingX, transformCurveX[childTransform]);

                    MakeConstant(transformCurveY[childTransform]);
                    AnimationUtility.SetEditorCurve(clip, bindingY, transformCurveY[childTransform]);
                }

                var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.loopTime = (clip.wrapMode == WrapMode.Loop);

                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

                EditorUtility.SetDirty(clip);

                clips.Add(clip);
            }

            return clips;
        }

        static void MakeConstant(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; ++i)
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }

        static List<Sprite> GetAllSpritesFromAssetFile(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();

            foreach (var item in assets)
                if (item is Sprite)
                    sprites.Add(item as Sprite);

            return sprites;
        }

        void CreateAnimatorController(List<AnimationClip> animations, string path)
        {
            var animPath = path.Replace(".png", "") + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animPath);

            if (controller == default)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(animPath);
                controller.AddLayer("Default");

                foreach (var animation in animations)
                {
                    var stateName = animation.name;
                    stateName = stateName.Replace(fileName + "_", "");

                    var state = controller.layers[0].stateMachine.AddState(stateName);
                    state.motion = animation;
                }
            }
            else
            {
                var clips = new Dictionary<string, AnimationClip>();
                foreach (var anim in animations)
                {
                    var stateName = anim.name;
                    stateName = stateName.Replace(fileName + "_", "");
                    clips[stateName] = anim;
                }

                var childStates = controller.layers[0].stateMachine.states;
                foreach (var childState in childStates)
                    if (clips.TryGetValue(childState.state.name, out AnimationClip clip))
                        childState.state.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        void CreateAnimatorOverrideController(List<AnimationClip> animations, string path)
        {
            var animPath = path.Replace(".png", "") + ".Override.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(animPath);
            var baseController = controller?.runtimeAnimatorController;

            if (controller == default)
            {
                controller = new AnimatorOverrideController();
                AssetDatabase.CreateAsset(controller, animPath);
                baseController = Settings.baseAnimator;
            }

            if (baseController == default)
            {
                Debug.LogError("Cannot make override controller without base controller");
                return;
            }

            controller.runtimeAnimatorController = baseController;
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var anim in animations)
            {
                var stateName = anim.name;
                stateName = stateName.Replace(fileName + "_", "");
                clips[stateName] = anim;
            }

            var clipPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(controller.overridesCount);
            controller.GetOverrides(clipPairs);

            foreach (var pair in clipPairs)
            {
                string animationName = pair.Key.name;
                if (clips.TryGetValue(animationName, out AnimationClip clip))
                    controller[animationName] = clip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        void BuildAtlas(string acePath)
        {
            fileName = Path.GetFileNameWithoutExtension(acePath);
            directoryName = Path.GetDirectoryName(acePath) + "/" + fileName;

            if (!AssetDatabase.IsValidFolder(directoryName))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(acePath), fileName);

            filePath = directoryName + "/" + fileName + ".png";

            if (!Settings.splitLayers)
                WriteTexture(filePath, GenerateAtlas(frames));
        }

        public void WriteTexture(string path, Texture2D texture)
        {
            try
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public Vector2Int GetColsAndRows(int spriteCount)
        {
            var area = size.x * size.y * spriteCount;
            var cols = 0;
            var rows = 0;

            if (spriteCount < 4)
            {
                if (size.x <= size.y)
                {
                    cols = spriteCount;
                    rows = 1;
                }
                else
                {
                    cols = 1;
                    rows = spriteCount;
                }
            }
            else
            {
                var sqrt = Mathf.Sqrt(area);
                cols = Mathf.CeilToInt(sqrt / size.x);
                rows = Mathf.CeilToInt(sqrt / size.y);
            }

            return new Vector2Int(cols, rows);
        }

        public Vector2Int GetWidthAndHeight(Vector2Int colsAndRows)
        {
            var width = colsAndRows.x * (size.x + padding * 2);
            var height = colsAndRows.y * (size.y + padding * 2);

            return new Vector2Int(width, height);
        }

        public Texture2D GenerateAtlas(Texture2D[] sprites)
        {
            var colsAndRows = GetColsAndRows(sprites.Length);

            cols = colsAndRows.x;
            rows = colsAndRows.y;

            var widthAndHeight = GetWidthAndHeight(colsAndRows);
            var atlas = Texture2DUtil.CreateTransparentTexture(widthAndHeight.x, widthAndHeight.y);
            var frame = 0;

            for (var row = 0; row < rows; ++row)
            {
                for (var col = 0; col < cols; ++col)
                {
                    if (frame == sprites.Length) break;

                    var sprite = sprites[frame++];

                    var rect = new RectInt(
                        col * (size.x + padding * 2) + padding,
                        widthAndHeight.y - (row + 1) * (size.y + padding * 2) + padding,
                        size.x,
                        size.y
                    );

                    CopyColors(sprite, atlas, rect);
                }
            }

            return atlas;
        }

        Color[] GetPixels(Texture2D sprite)
        {
            var res = sprite.GetPixels();

            if (Settings.transparencyMode == TransparencyMode.Mask)
            {
                for (var index = 0; index < res.Length; ++index)
                {
                    var color = res[index];

                    if (color == Settings.transparentColor)
                    {
                        color.r = color.g = color.b = color.a = 0;
                        res[index] = color;
                    }
                }
            }

            return res;
        }

        void CopyColors(Texture2D sprite, Texture2D atlas, RectInt to)
            => atlas.SetPixels(to.x, to.y, to.width, to.height, GetPixels(sprite));

        bool GenerateSprites(string path, string filename, List<FrameCel> frameCels = default)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == default) return false;

            importer.maxTextureSize = 8192; // anything above 8192 cannot be readable

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = Settings.pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.isReadable = TextureImportSettings.readable;

            var metaList = CreateMetadata(filename, frameCels);
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

        List<SpriteMetaData> CreateMetadata(string filename, List<FrameCel> frameCels = default)
        {
            var metadata = new List<SpriteMetaData>();
            var height = rows * (size.y + padding * 2);
            var numRows = rows;
            var numCols = cols;

            if (frameCels != default)
            {
                var colsAndRows = GetColsAndRows(frameCels.Count);

                numCols = colsAndRows.x;
                numRows = colsAndRows.y;

                var widthAndHeight = GetWidthAndHeight(colsAndRows);

                height = widthAndHeight.y;
            }

            var localFrame = 0;
            var frameTags = AsepriteFile.GetFrameTags();
            var done = false;

            for (var row = 0; row < numRows; ++row)
            {
                for (var col = 0; col < numCols; ++col)
                {
                    if (localFrame >= frames.Length)
                    {
                        done = true;
                        break;
                    }

                    var globalFrame = localFrame;
                    if (frameCels != default)
                    {
                        if (localFrame >= frameCels.Count)
                        {
                            done = true;
                            break;
                        }

                        globalFrame = frameCels.ElementAt(localFrame).Frame;
                    }

                    var tag = "Untagged";

                    foreach (var frameTag in frameTags)
                    {
                        if (globalFrame >= frameTag.FrameFrom && globalFrame <= frameTag.FrameTo)
                        {
                            tag = frameTag.TagName;
                            break;
                        }
                    }

                    var meta = new SpriteMetaData();
                    meta.name = tag + Settings.tagDelimiter + globalFrame.ToString("D");
                    meta.alignment = Settings.spriteAlignment;
                    meta.pivot = Settings.spritePivot;
                    meta.rect = new Rect(
                        col * (size.x + padding * 2) + padding,
                        height - (row + 1) * (size.y + padding * 2) + padding,
                        size.x,
                        size.y
                    );

                    metadata.Add(meta);

                    ++localFrame;
                }

                if (done) break;
            }

            return metadata;
        }

        bool GenerateSpritesSeparatedByLayer(out string[] layerPaths, out string[] layerNames)
        {
            var layers = AsepriteFile.GetLayersAsFrames();
            var parentPath = directoryName + "/";
            var generatedSprites = false;

            if (
                Settings.layerMergeOptions != default &&
                Settings.layerMergeOptions.Count > 0
            )
            {
                var layersToMergeMap = new Dictionary<string, List<LayerAsFrames>>();

                foreach (var layer in layers)
                {
                    foreach (var option in Settings.layerMergeOptions)
                    {
                        if (option.Layers.Contains(layer.Name))
                        {
                            if (layersToMergeMap.ContainsKey(option.Name))
                            {
                                layersToMergeMap[option.Name].Add(layer);
                            }
                            else
                            {
                                layersToMergeMap.TryAdd(option.Name, new List<LayerAsFrames>());
                                layersToMergeMap[option.Name].Add(layer);
                            }

                            break;
                        }
                    }
                }

                var mergedLayers = new List<LayerAsFrames>();

                foreach (var layersToMerge in layersToMergeMap)
                {
                    var mergedLayerAsFrames = new LayerAsFrames();
                    mergedLayerAsFrames.Name = layersToMerge.Key;
                    mergedLayerAsFrames.FrameCels = new List<FrameCel>();

                    var i = 0;

                    var uniqueFrames = layersToMerge.Value.SelectMany(f => f.FrameCels)
                        .Select(f => f.Frame).ToHashSet();

                    foreach (var frame in uniqueFrames)
                    {
                        mergedLayerAsFrames.FrameCels.Add(new FrameCel(
                            frame,
                            Texture2DUtil.CreateTransparentTexture(size.x, size.y),
                            LayerBlendMode.Normal,
                            255f
                        ));

                        var matchingFrameCels = layersToMerge.Value.SelectMany(f => f.FrameCels)
                            .Where(f => frame == f.Frame);

                        foreach (var frameCel in matchingFrameCels)
                            Blend(ref mergedLayerAsFrames.FrameCels[i].Cel, frameCel);

                        ++i;
                    }

                    mergedLayers.Add(mergedLayerAsFrames);
                }

                layers = mergedLayers;
            }

            List<string> layerPathList = new();

            foreach (var layer in layers)
            {
                var layerDirPath = parentPath + layer.Name;

                if (!AssetDatabase.IsValidFolder(layerDirPath)) AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(parentPath),
                    layer.Name
                );

                var cels = new List<Texture2D>();
                foreach (var frameCel in layer.FrameCels)
                    cels.Add(frameCel.Cel);

                var atlas = GenerateAtlas(cels.ToArray());
                var layerFilename = layer.Name;
                var layerFilePath = layerDirPath + "/" + layerFilename + ".png";

                WriteTexture(layerFilePath, atlas);

                if (GenerateSprites(layerFilePath, layerFilename, layer.FrameCels))
                {
                    generatedSprites = true;

                    layerPathList.Add(layerFilePath);
                }
            }

            layerPaths = layerPathList.ToArray();
            layerNames = layers.Select(l => l.Name).ToArray();

            return generatedSprites;
        }

        void Blend(ref Texture2D baseLayer, FrameCel frameCel)
        {
            switch (frameCel.BlendMode)
            {
                case LayerBlendMode.Normal: Texture2DBlender.Normal(ref baseLayer, frameCel.Cel, frameCel.Opacity); break;
                case LayerBlendMode.Multiply: Texture2DBlender.Multiply(ref baseLayer, frameCel.Cel, frameCel.Opacity); break;
                case LayerBlendMode.Screen: Texture2DBlender.Screen(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Overlay: Texture2DBlender.Overlay(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Darken: Texture2DBlender.Darken(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Lighten: Texture2DBlender.Lighten(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.ColorDodge: Texture2DBlender.ColorDodge(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.ColorBurn: Texture2DBlender.ColorBurn(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.HardLight: Texture2DBlender.HardLight(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.SoftLight: Texture2DBlender.SoftLight(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Difference: Texture2DBlender.Difference(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Exclusion: Texture2DBlender.Exclusion(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Hue: Texture2DBlender.Hue(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Saturation: Texture2DBlender.Saturation(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Color: Texture2DBlender.Color(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Luminosity: Texture2DBlender.Luminosity(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Addition: Texture2DBlender.Addition(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Subtract: Texture2DBlender.Subtract(ref baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Divide: Texture2DBlender.Divide(ref baseLayer, frameCel.Cel); break;
            }
        }

        WrapMode GetDefaultWrapMode(string animName)
        {
            animName = animName.ToLower();

            var defaults = new string[] { "walk", "run", "crawl", "idle" };

            foreach (var def in defaults)
                if (animName.IndexOf(def, StringComparison.Ordinal) >= 0)
                    return WrapMode.Loop;

            return WrapMode.Once;
        }
    }
}
