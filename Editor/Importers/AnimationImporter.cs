using System.Collections.Generic;
using System.Text;
using Aseprite;
using Aseprite.Chunks;
using AsepriteImporter.Settings;
using UnityEditor;
using UnityEngine;

namespace AsepriteImporter.Importers
{
    public class AnimationImporter
    {
        readonly AseFile aseFile = default;

        public AnimationImporter(AseFile aseFile)
            => this.aseFile = aseFile;

        public AseFileAnimationSettings[] GetAnimationImportSettings()
        {
            var animationSettings = new List<AseFileAnimationSettings>();
            var frameTags = aseFile.GetAnimations();

            foreach (var frameTag in frameTags)
            {
                var frames = frameTag.FrameTo - frameTag.FrameFrom + 1;

                var setting = new AseFileAnimationSettings(frameTag.TagName)
                {
                    about = GetAnimationAbout(frameTag),
                    loopTime = true,
                    sprites = new Sprite[frames],
                    frameNumbers = new int[frames]
                };

                var frameFrom = frameTag.FrameFrom;
                var frameTo = frameTag.FrameTo;
                int frameIndex = frameFrom;
                var step = (frameTag.Animation != LoopAnimation.Reverse) ? 1 : -1;
                var i = 0;

                while (frameIndex != frameTo)
                {
                    setting.frameNumbers[i++] = frameIndex;
                    frameIndex += step;
                }

                setting.frameNumbers[i] = frameTo;

                animationSettings.Add(setting);
            }

            return animationSettings.ToArray();
        }

        public AnimationClip[] GenerateAnimations(string parentName, AseFileAnimationSettings[] animationSettings)
        {
            var animations = aseFile.GetAnimations();

            if (animations.Length <= 0) return new AnimationClip[0];

            var animationClips = new AnimationClip[animations.Length];
            var index = 0;

            foreach (var animation in animations)
            {
                var importSettings = GetAnimationSettingFor(animationSettings, animation);

                if (importSettings == default || importSettings.HasInvalidSprites) continue;

                var animationClip = new AnimationClip
                {
                    name = parentName + "_" + animation.TagName,
                    frameRate = 25
                };

                var spriteBinding = new EditorCurveBinding
                {
                    type = typeof(SpriteRenderer),
                    path = "",
                    propertyName = "m_Sprite"
                };

                var length = animation.FrameTo - animation.FrameFrom + 1;
                var from = (animation.Animation != LoopAnimation.Reverse) ? animation.FrameFrom : animation.FrameTo;
                var step = (animation.Animation != LoopAnimation.Reverse) ? 1 : -1;
                int keyIndex = from;
                float time = 0;
                var spriteKeyFrames = new ObjectReferenceKeyframe[length + 1]; // plus last frame to keep the duration

                for (var i = 0; i < length; ++i)
                {
                    if (i >= length) keyIndex = from;

                    var frame = new ObjectReferenceKeyframe
                    {
                        time = time,
                        value = importSettings.sprites[i]
                    };

                    time += aseFile.Frames[keyIndex].FrameDuration / 1000f;

                    keyIndex += step;
                    spriteKeyFrames[i] = frame;
                }

                var frameTime = 1f / animationClip.frameRate;

                var lastFrame = new ObjectReferenceKeyframe
                {
                    time = time - frameTime,
                    value = importSettings.sprites[length - 1]
                };

                spriteKeyFrames[spriteKeyFrames.Length - 1] = lastFrame;

                AnimationUtility.SetObjectReferenceCurve(animationClip, spriteBinding, spriteKeyFrames);

                var settings = AnimationUtility.GetAnimationClipSettings(animationClip);

                switch (animation.Animation)
                {
                    case LoopAnimation.Forward:
                        animationClip.wrapMode = WrapMode.Loop;
                        settings.loopTime = true;
                        break;
                    case LoopAnimation.Reverse:
                        animationClip.wrapMode = WrapMode.Loop;
                        settings.loopTime = true;
                        break;
                    case LoopAnimation.PingPong:
                        animationClip.wrapMode = WrapMode.PingPong;
                        settings.loopTime = true;
                        break;
                }

                if (!importSettings.loopTime)
                {
                    animationClip.wrapMode = WrapMode.Once;
                    settings.loopTime = false;
                }

                AnimationUtility.SetAnimationClipSettings(animationClip, settings);
                animationClips[index++] = animationClip;
            }

            return animationClips;
        }

        public AseFileAnimationSettings GetAnimationSettingFor(AseFileAnimationSettings[] animationSettings, FrameTag animation)
        {
            for (var i = 0; i < animationSettings.Length; ++i)
                if (animationSettings[i].animationName == animation.TagName)
                    return animationSettings[i];

            return default;
        }

        public string GetAnimationAbout(FrameTag animation)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Animation Type:\t{0}", animation.Animation.ToString());
            sb.AppendLine();
            sb.AppendFormat("Animation:\tFrom: {0}; To: {1}", animation.FrameFrom, animation.FrameTo);

            return sb.ToString();
        }
    }
}
