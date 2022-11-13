using UnityEngine;

namespace Aseprite
{
    public abstract class Pixel
    {
        protected Frame Frame = default;
        public abstract Color GetColor();

        public Pixel(Frame frame)
            => Frame = frame;
    }
}
