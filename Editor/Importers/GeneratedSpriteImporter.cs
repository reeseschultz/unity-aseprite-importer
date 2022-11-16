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
            if (GenerateSpritesSeparatedByLayer())
            {
                // TODO: animation

                return true;
            }

            if (GenerateSprites(filePath, fileName))
            {
                // TODO: animation

                // GeneratorAnimations();

                return true;
            }

            return false;
        }

        void BuildAtlas(string acePath)
        {
            fileName = Path.GetFileNameWithoutExtension(acePath);
            directoryName = Path.GetDirectoryName(acePath) + "/" + fileName;

            if (!AssetDatabase.IsValidFolder(directoryName))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(acePath), fileName);
            }

            filePath = directoryName + "/" + fileName + ".png";

            var atlas = GenerateAtlas(frames);

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

        public Texture2D GenerateAtlas(Texture2D[] sprites)
        {
            var area = size.x * size.y * sprites.Length;

            if (sprites.Length < 4)
            {
                if (size.x <= size.y)
                {
                    cols = sprites.Length;
                    rows = 1;
                }
                else
                {
                    rows = sprites.Length;
                    cols = 1;
                }
            }
            else
            {
                var sqrt = Mathf.Sqrt(area);
                cols = Mathf.CeilToInt(sqrt / size.x);
                rows = Mathf.CeilToInt(sqrt / size.y);
            }

            var width = cols * (size.x + padding * 2);
            var height = rows * (size.y + padding * 2);
            var atlas = Texture2DUtil.CreateTransparentTexture(width, height);
            var index = 0;

            for (var row = 0; row < rows; ++row)
            {
                for (var col = 0; col < cols; ++col)
                {
                    if (index == sprites.Length) break;

                    var sprite = sprites[index++];

                    var rect = new RectInt(
                        col * (size.x + padding * 2) + padding,
                        height - (row + 1) * (size.y + padding * 2) + padding,
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

        bool GenerateSprites(string path, string filename, int? rowOverride = null)
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

            var metaList = CreateMetadata(filename, rowOverride);
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

        List<SpriteMetaData> CreateMetadata(string filename, int? colsForAtlas = null)
        {
            var res = new List<SpriteMetaData>();
            var frame = 0;
            var height = rows * (size.y + padding * 2);
            var done = false;
            var tagCountMap = new Dictionary<string, int>();
            var frameTags = AsepriteFile.GetFrameTags();
            var numRows = rows;
            var numCols = cols;

            if (colsForAtlas.HasValue)
            {
                numRows = 1;
                numCols = colsForAtlas.Value;
            }

            for (var row = 0; row < numRows; ++row)
            {
                for (var col = 0; col < numCols; ++col)
                {
                    if (frame >= frames.Length)
                    {
                        done = true;
                        break;
                    }

                    var rect = new Rect(
                        col * size.x,
                        row * size.y,
                        size.x,
                        size.y
                    );

                    if (!colsForAtlas.HasValue)
                    {
                        rect.x = col * (size.x + padding * 2) + padding;
                        rect.y = height - (row + 1) * (size.y + padding * 2) + padding;
                    }

                    var tag = "Untagged";

                    foreach (var frameTag in frameTags)
                    {
                        if (frame >= frameTag.FrameFrom && frame <= frameTag.FrameTo)
                        {
                            tag = frameTag.TagName;
                            break;
                        }
                    }

                    if (tagCountMap.ContainsKey(tag)) ++tagCountMap[tag];
                    else tagCountMap.TryAdd(tag, 0);

                    var meta = new SpriteMetaData();
                    meta.name = tag + tagCountMap[tag].ToString("D");
                    meta.rect = rect;
                    meta.alignment = Settings.spriteAlignment;
                    meta.pivot = Settings.spritePivot;
                    res.Add(meta);

                    ++frame;
                }

                if (done) break;
            }

            return res;
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

            foreach (var layer in layers)
            {
                var rect = new Rect(0, 0, size.x, size.y);
                var layerDirPath = parentPath + layer.Name;

                if (!AssetDatabase.IsValidFolder(layerDirPath)) AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(parentPath),
                    layer.Name
                );

                // TODO: generate atlases that include multiple rows;
                // something more similar to GenerateSpriteAtlas
                var atlas = AsepriteFile.GetTextureAtlas(layer.Frames);
                var layerFilename = layer.Name;
                var layerFilePath = layerDirPath + "/" + layerFilename + ".png";

                try
                {
                    File.WriteAllBytes(layerFilePath, atlas.EncodeToPNG());
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }

                if (GenerateSprites(layerFilePath, layerFilename, layer.Frames.Count))
                    generatedSprites = true;
            }

            return generatedSprites;
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
