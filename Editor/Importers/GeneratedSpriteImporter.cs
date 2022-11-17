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

            if (GenerateSprites(filePath, fileName))
            {
                // TODO: animation

                // GeneratorAnimations();

                generatedSprites = true;
            }

            if (Settings.splitLayers && GenerateSpritesSeparatedByLayer())
            {
                // TODO: animation

                generatedSprites = true;
            }

            return generatedSprites;
        }

        void BuildAtlas(string acePath)
        {
            fileName = Path.GetFileNameWithoutExtension(acePath);
            directoryName = Path.GetDirectoryName(acePath) + "/" + fileName;

            if (!AssetDatabase.IsValidFolder(directoryName))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(acePath), fileName);

            filePath = directoryName + "/" + fileName + ".png";

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

            // TODO: dynamically set max texture size
            // note that anything above 8192 cannot be set to readable
            importer.maxTextureSize = 8192;

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
            var tagCountMap = new Dictionary<string, int>();
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

                    if (tagCountMap.ContainsKey(tag)) ++tagCountMap[tag];
                    else tagCountMap.TryAdd(tag, 0);

                    var meta = new SpriteMetaData();
                    meta.name = tag + tagCountMap[tag].ToString("D");
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

        void GeneratorAnimations()
        {
            var sprites = GetAllSpritesFromAssetFile(filePath);
            sprites.Sort((lhs, rhs) => String.CompareOrdinal(lhs.name, rhs.name));

            var clips = GenerateAnimations(sprites);

            if (Settings.buildAtlas) CreateSpriteAtlas(sprites);

            if (Settings.animType == AseAnimatorType.AnimatorController) CreateAnimatorController(clips);
            else if (Settings.animType == AseAnimatorType.AnimatorOverrideController) CreateAnimatorOverrideController(clips);
        }

        bool GenerateSpritesSeparatedByLayer()
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
                    generatedSprites = true;
            }

            return generatedSprites;
        }

        void Blend(ref Texture2D baseLayer, FrameCel frameCel)
        {
            switch (frameCel.BlendMode)
            {
                case LayerBlendMode.Normal: baseLayer = Texture2DBlender.Normal(baseLayer, frameCel.Cel, frameCel.Opacity); break;
                case LayerBlendMode.Multiply: baseLayer = Texture2DBlender.Multiply(baseLayer, frameCel.Cel, frameCel.Opacity); break;
                case LayerBlendMode.Screen: baseLayer = Texture2DBlender.Screen(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Overlay: baseLayer = Texture2DBlender.Overlay(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Darken: baseLayer = Texture2DBlender.Darken(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Lighten: baseLayer = Texture2DBlender.Lighten(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.ColorDodge: baseLayer = Texture2DBlender.ColorDodge(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.ColorBurn: baseLayer = Texture2DBlender.ColorBurn(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.HardLight: baseLayer = Texture2DBlender.HardLight(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.SoftLight: baseLayer = Texture2DBlender.SoftLight(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Difference: baseLayer = Texture2DBlender.Difference(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Exclusion: baseLayer = Texture2DBlender.Exclusion(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Hue: baseLayer = Texture2DBlender.Hue(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Saturation: baseLayer = Texture2DBlender.Saturation(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Color: baseLayer = Texture2DBlender.Color(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Luminosity: baseLayer = Texture2DBlender.Luminosity(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Addition: baseLayer = Texture2DBlender.Addition(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Subtract: baseLayer = Texture2DBlender.Subtract(baseLayer, frameCel.Cel); break;
                case LayerBlendMode.Divide: baseLayer = Texture2DBlender.Divide(baseLayer, frameCel.Cel); break;
            }
        }

        WrapMode GetDefaultWrapMode(string animName)
        {
            animName = animName.ToLower();

            if (
                animName.IndexOf("walk", StringComparison.Ordinal) >= 0 ||
                animName.IndexOf("run", StringComparison.Ordinal) >= 0 ||
                animName.IndexOf("idle", StringComparison.Ordinal) >= 0
            ) return WrapMode.Loop;

            return WrapMode.Once;
        }

        List<AnimationClip> GenerateAnimations(List<Sprite> sprites, string layerPath = "")
        {
            var res = new List<AnimationClip>();
            var animations = AsepriteFile.GetFrameTags();

            if (animations.Length <= 0) return res;

            var metadatas = AsepriteFile.GetMetaData(Settings.spritePivot, Settings.pixelsPerUnit);
            var index = 0;

            foreach (var animation in animations)
            {
                var path = directoryName + "/" + fileName + "/" + layerPath + animation.TagName + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip == default)
                {
                    clip = new AnimationClip();
                    AssetDatabase.CreateAsset(clip, path);
                    clip.wrapMode = GetDefaultWrapMode(animation.TagName);
                }
                else
                {
                    var animSettings = AnimationUtility.GetAnimationClipSettings(clip);
                    clip.wrapMode = animSettings.loopTime ? WrapMode.Loop : WrapMode.Once;
                }

                clip.name = animation.TagName;
                clip.frameRate = 25;

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

                // plus last frame to keep the duration
                var length = animation.FrameTo - animation.FrameFrom + 1;
                var spriteKeyFrames = new ObjectReferenceKeyframe[length + 1];
                var transformCurveX = new Dictionary<string, AnimationCurve>();
                var transformCurveY = new Dictionary<string, AnimationCurve>();

                var time = 0f;
                int from = (animation.Animation != LoopAnimation.Reverse) ? animation.FrameFrom : animation.FrameTo;
                var step = (animation.Animation != LoopAnimation.Reverse) ? 1 : -1;
                var keyIndex = from;

                for (var i = 0; i < length; ++i)
                {
                    if (i >= length) keyIndex = from;

                    var frame = new ObjectReferenceKeyframe();
                    frame.time = time;
                    frame.value = sprites[keyIndex];

                    time += AsepriteFile.Frames[keyIndex].FrameDuration / 1000f;
                    spriteKeyFrames[i] = frame;

                    foreach (var metadata in metadatas)
                    {
                        if (
                            metadata.Type != MetaDataType.TRANSFORM ||
                            !metadata.Transforms.ContainsKey(keyIndex)
                        ) continue;

                        var childTransform = metadata.Args[0];

                        if (!transformCurveX.ContainsKey(childTransform))
                        {
                            transformCurveX[childTransform] = new AnimationCurve();
                            transformCurveY[childTransform] = new AnimationCurve();
                        }

                        var pos = metadata.Transforms[keyIndex];

                        transformCurveX[childTransform].AddKey(i, pos.x);
                        transformCurveY[childTransform].AddKey(i, pos.y);
                    }

                    keyIndex += step;
                }

                var frameTime = 1f / clip.frameRate;
                var lastFrame = new ObjectReferenceKeyframe();
                lastFrame.time = time - frameTime;
                lastFrame.value = sprites[keyIndex - step];

                spriteKeyFrames[spriteKeyFrames.Length - 1] = lastFrame;
                foreach (var metadata in metadatas)
                {
                    if (
                        metadata.Type != MetaDataType.TRANSFORM ||
                        !metadata.Transforms.ContainsKey(keyIndex - step)
                    ) continue;

                    var childTransform = metadata.Args[0];
                    var pos = metadata.Transforms[keyIndex - step];
                    transformCurveX[childTransform].AddKey(spriteKeyFrames.Length - 1, pos.x);
                    transformCurveY[childTransform].AddKey(spriteKeyFrames.Length - 1, pos.y);
                }

                AnimationUtility.SetObjectReferenceCurve(clip, editorBinding, spriteKeyFrames);
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
                ++index;
                res.Add(clip);
            }

            return res;
        }

        static void MakeConstant(AnimationCurve curve)
        {
            for (var i = 0; i < curve.length; ++i)
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }

        static List<Sprite> GetAllSpritesFromAssetFile(string imageFilename)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(imageFilename);
            var sprites = new List<Sprite>();

            foreach (var item in assets) // make sure we only grab valid sprites here
                if (item is Sprite)
                    sprites.Add(item as Sprite);

            return sprites;
        }

        void CreateAnimatorController(List<AnimationClip> animations)
        {
            var path = directoryName + "/" + fileName + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == default)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
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

        void CreateAnimatorOverrideController(List<AnimationClip> animations)
        {
            var path = directoryName + "/" + fileName + ".overrideController";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            var baseController = controller?.runtimeAnimatorController;

            if (controller == default)
            {
                controller = new AnimatorOverrideController();
                AssetDatabase.CreateAsset(controller, path);
                baseController = Settings.baseAnimator;
            }

            if (baseController == default)
            {
                Debug.LogError("Can not make override controller");
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

        void CreateSpriteAtlas(List<Sprite> sprites)
        {
            var path = directoryName + "/" + fileName + ".spriteatlas";
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == default)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, path);
            }

            var texSetting = new SpriteAtlasTextureSettings();
            texSetting.filterMode = FilterMode.Point;
            texSetting.generateMipMaps = false;

            var packSetting = new SpriteAtlasPackingSettings();
            packSetting.padding = 2;
            packSetting.enableRotation = false;
            packSetting.enableTightPacking = true;

            var platformSetting = new TextureImporterPlatformSettings();
            platformSetting.textureCompression = TextureImporterCompression.Uncompressed;

            atlas.SetTextureSettings(texSetting);
            atlas.SetPackingSettings(packSetting);
            atlas.SetPlatformSettings(platformSetting);
            atlas.Add(sprites.ToArray());

            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
        }
    }
}
