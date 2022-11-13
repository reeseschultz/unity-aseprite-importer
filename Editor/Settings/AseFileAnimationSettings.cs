using UnityEngine;

namespace AsepriteImporter.Settings
{
    [System.Serializable]
    public class AseFileAnimationSettings
    {
        public AseFileAnimationSettings() { }

        public AseFileAnimationSettings(string name)
            => animationName = name;

        [SerializeField] public string animationName = default;
        [SerializeField] public bool loopTime = true;
        [SerializeField] public string about = default;
        [SerializeField] public Sprite[] sprites = default;
        [SerializeField] public int[] frameNumbers = default;

        public override string ToString()
            => animationName;

        public bool HasInvalidSprites
        {
            get
            {
                foreach (Sprite sprite in sprites)
                    if (sprite == default)
                        return true;

                return false;
            }
        }
    }
}
