using System;
using System.Collections.Generic;
using UnityEngine;

// See: http://wwwimages.adobe.com/www.adobe.com/content/dam/Adobe/en/devnet/pdf/pdfs/PDF32000_2008.pdf
// Page 333
namespace Aseprite.Utils
{
    public static class Texture2DBlender
    {
        public static float Multiply(float b, float s)
            => b * s;

        public static float Screen(float b, float s)
            => b + s - b * s;

        public static float Overlay(float b, float s)
            => HardLight(s, b);

        public static float Darken(float b, float s)
            => Mathf.Min(b, s);

        public static float Lighten(float b, float s)
            => Mathf.Max(b, s);

        // Color Dodge & Color Burn:  http://wwwimages.adobe.com/www.adobe.com/content/dam/Adobe/en/devnet/pdf/pdfs/adobe_supplement_iso32000_1.pdf
        public static float ColorDodge(float b, float s)
        {
            if (b == 0) return 0;
            else if (b >= 1 - s) return 1;
            else return b / (1 - s);
        }

        public static float ColorBurn(float b, float s)
        {
            if (b == 1) return 1;
            else if (1 - b >= s) return 0;
            else return 1 - ((1 - b) / s);
        }

        public static float HardLight(float b, float s)
        {
            if (s <= 0.5) return Multiply(b, 2 * s);
            else return Screen(b, 2 * s - 1);
        }

        public static float SoftLight(float b, float s)
        {
            if (s <= 0.5) return b - (1 - 2 * s) * b * (1 - b);
            else return b + (2 * s - 1) * (SoftLightD(b) - b);
        }

        static float SoftLightD(float x)
        {
            if (x <= 0.25) return ((16 * x - 12) * x + 4) * x;
            else return Mathf.Sqrt(x);
        }

        public static float Difference(float b, float s)
            => Mathf.Abs(b - s);

        public static float Exclusion(float b, float s)
            => b + s - 2 * b * s;

        public static void Normal(ref Texture2D baseLayer, Texture2D layer, float opacity)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    b.a = b.a * opacity;

                    c = ((1f - b.a) * a) + b.a * b;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Multiply(ref Texture2D baseLayer, Texture2D layer, float opacity)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = (a.r) * (opacity * (1f - b.a * (1f - b.r)));
                    c.g = (a.g) * (opacity * (1f - b.a * (1f - b.g)));
                    c.b = (a.b) * (opacity * (1f - b.a * (1f - b.b)));
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Screen(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = a + b - a * b;

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Overlay(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    if (a.r < 0.5) c.r = 2f * a.r * b.r;
                    else c.r = 1f - 2f * (1f - b.r) * (1f - a.r);

                    if (a.g < 0.5) c.g = 2f * a.g * b.g;
                    else c.g = 1f - 2f * (1f - b.g) * (1f - a.g);

                    if (a.b < 0.5) c.b = 2f * a.b * b.b;
                    else c.b = 1f - 2f * (1f - b.b) * (1f - a.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Darken(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = Mathf.Min(a.r, b.r);
                    c.g = Mathf.Min(a.g, b.g);
                    c.b = Mathf.Min(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Lighten(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = Lighten(a.r, b.r);
                    c.g = Lighten(a.g, b.g);
                    c.b = Lighten(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void ColorDodge(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = ColorDodge(a.r, b.r);
                    c.g = ColorDodge(a.g, b.g);
                    c.b = ColorDodge(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void ColorBurn(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = ColorBurn(a.r, b.r);
                    c.g = ColorBurn(a.g, b.g);
                    c.b = ColorBurn(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void HardLight(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = HardLight(a.r, b.r);
                    c.g = HardLight(a.g, b.g);
                    c.b = HardLight(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void SoftLight(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = SoftLight(a.r, b.r);
                    c.g = SoftLight(a.g, b.g);
                    c.b = SoftLight(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Difference(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = Difference(a.r, b.r);
                    c.g = Difference(a.g, b.g);
                    c.b = Difference(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Exclusion(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = new Color();

                    c.r = Exclusion(a.r, b.r);
                    c.g = Exclusion(a.g, b.g);
                    c.b = Exclusion(a.b, b.b);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Hue(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);

                    var s = Sat(a);
                    var l = Lum(a);

                    var b = layer.GetPixel(x, y);
                    var c = SetLum(SetSat(b, s), l);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Saturation(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var l = Lum(a);

                    var b = layer.GetPixel(x, y);
                    var s = Sat(b);

                    var c = SetLum(SetSat(a, s), l);

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Color(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = SetLum(b, Lum(a));

                    c = ((1f - b.a) * a) + b.a * c;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Luminosity(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = SetLum(a, Lum(b));

                    c = ((1f - b.a) * a) + b.a * c; ;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Addition(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = a + b;

                    c = ((1f - b.a) * a) + b.a * c; ;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Subtract(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);
                    var c = a - b;

                    c = ((1f - b.a) * a) + b.a * c; ;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        public static void Divide(ref Texture2D baseLayer, Texture2D layer)
        {
            for (var x = 0; x < baseLayer.width; ++x)
            {
                for (var y = 0; y < baseLayer.height; ++y)
                {
                    var a = baseLayer.GetPixel(x, y);
                    var b = layer.GetPixel(x, y);

                    var c = new Color(
                        BlendDivide(a.r, b.r),
                        BlendDivide(a.g, b.g),
                        BlendDivide(a.b, b.b)
                    );

                    c = (1f - b.a) * a + b.a * c; ;
                    c.a = a.a + b.a * (1f - a.a);

                    baseLayer.SetPixel(x, y, c);
                }
            }

            baseLayer.Apply();
        }

        static float BlendDivide(float b, float s)
        {
            if (b == 0) return 0;
            else if (b >= s) return 255;
            else return b / s;
        }

        static double Lum(Color c)
            => 0.3 * c.r + 0.59 * c.g + 0.11 * c.b;

        static Color ClipColor(Color c)
        {
            var l = Lum(c);
            var n = Math.Min(c.r, Math.Min(c.g, c.b));
            var x = Math.Max(c.r, Math.Max(c.g, c.b));

            if (n < 0)
            {
                c.r = (float)(l + (((c.r - l) * l) / (l - n)));
                c.g = (float)(l + (((c.g - l) * l) / (l - n)));
                c.b = (float)(l + (((c.b - l) * l) / (l - n)));
            }
            if (x > 1)
            {
                c.r = (float)(l + (((c.r - l) * (1 - l)) / (x - l)));
                c.g = (float)(l + (((c.g - l) * (1 - l)) / (x - l)));
                c.b = (float)(l + (((c.b - l) * (1 - l)) / (x - l)));
            }

            return c;
        }

        static Color SetLum(Color c, double l)
        {
            var d = l - Lum(c);
            c.r = (float)(c.r + d);
            c.g = (float)(c.g + d);
            c.b = (float)(c.b + d);

            return ClipColor(c);
        }

        static double Sat(Color c)
            => Math.Max(c.r, Math.Max(c.g, c.b)) - Math.Min(c.r, Math.Min(c.g, c.b));

        static double DMax(double x, double y)
            => x > y ? x : y;

        static double DMin(double x, double y)
            => x < y ? x : y;

        static Color SetSat(Color c, double s)
        {
            var cMin = GetMinComponent(c);
            var cMid = GetMidComponent(c);
            var cMax = GetMaxComponent(c);

            var min = GetComponent(c, cMin);
            var mid = GetComponent(c, cMid);
            var max = GetComponent(c, cMax);

            if (max > min)
            {
                var sAsFloat = (float)s;
                mid = ((mid - min) * sAsFloat) / (max - min);
                c = SetComponent(c, cMid, (float)mid);
                max = sAsFloat;
                c = SetComponent(c, cMax, (float)max);
            }
            else
            {
                mid = max = 0;
                c = SetComponent(c, cMax, (float)max);
                c = SetComponent(c, cMid, (float)mid);
            }

            min = 0;
            c = SetComponent(c, cMin, (float)min);

            return c;
        }

        static float GetComponent(Color c, char component)
        {
            switch (component)
            {
                case 'r': return c.r;
                case 'g': return c.g;
                case 'b': return c.b;
            }

            return 0f;
        }

        static Color SetComponent(Color c, char component, float value)
        {
            switch (component)
            {
                case 'r': c.r = value; break;
                case 'g': c.g = value; break;
                case 'b': c.b = value; break;
            }

            return c;
        }

        static char GetMinComponent(Color c)
        {
            var r = new KeyValuePair<char, float>('r', c.r);
            var g = new KeyValuePair<char, float>('g', c.g);
            var b = new KeyValuePair<char, float>('b', c.b);

            return MIN(r, MIN(g, b)).Key;
        }

        static char GetMidComponent(Color c)
        {
            var r = new KeyValuePair<char, float>('r', c.r);
            var g = new KeyValuePair<char, float>('g', c.g);
            var b = new KeyValuePair<char, float>('b', c.b);

            return MID(r, g, b).Key;
        }

        static char GetMaxComponent(Color c)
        {
            var r = new KeyValuePair<char, float>('r', c.r);
            var g = new KeyValuePair<char, float>('g', c.g);
            var b = new KeyValuePair<char, float>('b', c.b);

            return MAX(r, MAX(g, b)).Key;
        }

        static KeyValuePair<char, float> MIN(KeyValuePair<char, float> x, KeyValuePair<char, float> y)
            => x.Value < y.Value ? x : y;

        static KeyValuePair<char, float> MAX(KeyValuePair<char, float> x, KeyValuePair<char, float> y)
            => x.Value > y.Value ? x : y;

        static KeyValuePair<char, float> MID(KeyValuePair<char, float> x, KeyValuePair<char, float> y, KeyValuePair<char, float> z)
        {
            var components = new List<KeyValuePair<char, float>>();

            components.Add(x);
            components.Add(z);
            components.Add(y);

            components.Sort((c1, c2) => { return c1.Value.CompareTo(c2.Value); });

            return components[1];
        }
    }
}
