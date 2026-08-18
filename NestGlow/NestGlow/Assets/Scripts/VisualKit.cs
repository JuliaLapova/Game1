using UnityEngine;

namespace Nestglow
{
    public sealed class VisualKit
    {
        public Sprite SoftOrb { get; private set; }
        public Sprite CoreOrb { get; private set; }
        public Sprite GlowHalo { get; private set; }
        public Sprite WideGlow { get; private set; }
        public Sprite Ring { get; private set; }
        public Sprite CellPad { get; private set; }
        public Sprite CellWell { get; private set; }
        public Sprite RoundPanel { get; private set; }
        public Sprite Pill { get; private set; }
        public Sprite Star { get; private set; }
        public Sprite Gradient { get; private set; }
        public Sprite Vignette { get; private set; }
        public Sprite Moon { get; private set; }
        public Sprite Hill { get; private set; }
        public Sprite WhitePixel { get; private set; }

        public static VisualKit Create()
        {
            return new VisualKit
            {
                SoftOrb = MakeSoftOrb(160),
                CoreOrb = MakeCoreOrb(96),
                // Мягкий круглый ореол: falloff гасит alpha до углов квадрата
                GlowHalo = MakeGlowHalo(192, 1.8f, 0.62f),
                WideGlow = MakeGlowHalo(256, 1.25f, 0.42f),
                Ring = MakeRing(128, 0.72f, 0.12f),
                CellPad = MakeRoundedRect(112, 0.34f, 0.92f, inset: false),
                CellWell = MakeCellWell(112),
                RoundPanel = MakeRoundedRect(160, 0.22f, 0.98f, inset: false),
                // Hallmark --radius-pill: 999px → почти капсула
                Pill = MakeRoundedRect(192, 0.98f, 0.92f, inset: false),
                Star = MakeStar(64),
                Gradient = MakeNightSky(16, 320),
                Vignette = MakeVignette(256),
                Moon = MakeMoon(128),
                Hill = MakeHill(256, 96),
                WhitePixel = MakeSolid(4)
            };
        }

        static Sprite MakeSoftOrb(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                // стеклянный шар: яркий верх, мягкий низ
                float body = Smoothstep(0.98f, 0.2f, d);
                float shade = Mathf.Lerp(0.55f, 1.15f, Mathf.Clamp01(0.55f - ny * 0.55f - d * 0.25f));
                float rim = Mathf.Exp(-Mathf.Abs(d - 0.82f) * 18f) * 0.55f;
                float a = Mathf.Clamp01(body + rim * 0.35f);
                tex.SetPixel(x, y, new Color(shade, shade, shade, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeCoreOrb(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Exp(-d * d * 5.5f);
                float shade = Mathf.Lerp(1.2f, 0.8f, d);
                tex.SetPixel(x, y, new Color(shade, shade, shade, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeGlowHalo(int size, float falloff, float strength)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                // Жёсткая круглая маска: за пределами круга — полностью прозрачно,
                // иначе мягкий ореол «квадратится» на углах текстуры.
                if (d >= 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Exp(-d * d * falloff) * strength;
                // плавно гасим у края круга
                a *= Smoothstep(1f, 0.72f, d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeRing(int size, float radius, float thickness)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float dist = Mathf.Abs(d - radius);
                float a = Mathf.Exp(-dist * dist / (thickness * thickness)) * Smoothstep(1.05f, 0.9f, d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeRoundedRect(int size, float corner, float fill, bool inset)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            float half = c * fill;
            float rad = Mathf.Max(1f, half * corner);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - c);
                float py = Mathf.Abs(y - c);
                float dx = Mathf.Max(px - (half - rad), 0f);
                float dy = Mathf.Max(py - (half - rad), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) / rad;
                float a = Smoothstep(1.08f, 0.82f, d);
                float edge = Smoothstep(0.75f, 1.05f, d);
                float tone = inset
                    ? Mathf.Lerp(0.55f, 1f, 1f - edge)
                    : Mathf.Lerp(1.05f, 0.65f, edge);
                // лёгкий верхний блик
                float highlight = Mathf.Clamp01((1f - (y / (float)size)) * 0.25f);
                tone += highlight;
                tex.SetPixel(x, y, new Color(tone, tone, tone, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeCellWell(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            float half = c * 0.9f;
            float rad = half * 0.38f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - c);
                float py = Mathf.Abs(y - c);
                float dx = Mathf.Max(px - (half - rad), 0f);
                float dy = Mathf.Max(py - (half - rad), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(0.001f, rad);
                float a = Smoothstep(1.1f, 0.78f, d);

                // колодец: темнее к центру, светлая кромка
                float inward = 1f - Mathf.Clamp01(d);
                float tone = Mathf.Lerp(0.35f, 0.85f, edgeFactor(d));
                tone -= inward * 0.22f;
                float rim = Mathf.Exp(-Mathf.Abs(d - 0.92f) * 14f) * 0.55f;
                tone += rim;
                tex.SetPixel(x, y, new Color(tone, tone, tone, a));
            }
            return ToSprite(tex, size);

            float edgeFactor(float d) => Smoothstep(1.05f, 0.2f, d);
        }

        static Sprite MakeStar(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float ax = Mathf.Abs(nx);
                float ay = Mathf.Abs(ny);
                float cross = Mathf.Max(1f - (ax * 9f + ay * 0.65f), 0f);
                float cross2 = Mathf.Max(1f - (ay * 9f + ax * 0.65f), 0f);
                float a = Mathf.Clamp01(cross + cross2);
                a *= Mathf.Exp(-(nx * nx + ny * ny) * 1.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeNightSky(int width, int height)
        {
            var tex = NewTex(width, height);
            // Hallmark atmospheric: dark paper + subtle vertical shift (no purple mesh blobs)
            Color bottom = NestglowTheme.Paper2;
            Color top = NestglowTheme.Paper;
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color col = Color.Lerp(bottom, top, t);
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, col);
            }
            return ToSprite(tex, width, height);
        }

        static Sprite MakeVignette(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                // овальная виньетка
                float d = Mathf.Sqrt(nx * nx * 0.85f + ny * ny * 1.15f);
                float a = Mathf.SmoothStep(0.35f, 1.15f, d) * 0.78f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeMoon(int size)
        {
            var tex = NewTex(size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float body = Smoothstep(0.95f, 0.2f, d);
                // серп: вычитаем смещённый круг
                float sx = nx - 0.28f;
                float sy = ny + 0.08f;
                float sd = Mathf.Sqrt(sx * sx + sy * sy);
                float cut = Smoothstep(0.75f, 0.35f, sd);
                float a = Mathf.Clamp01(body * (1f - cut * 0.95f));
                float shade = Mathf.Lerp(0.75f, 1.1f, 0.5f - ny * 0.3f);
                tex.SetPixel(x, y, new Color(shade, shade * 0.98f, shade * 0.9f, a));
            }
            return ToSprite(tex, size);
        }

        static Sprite MakeHill(int w, int h)
        {
            var tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float v = y / (float)(h - 1);
                float hill =
                    0.42f
                    + 0.18f * Mathf.Sin(u * Mathf.PI * 1.2f)
                    + 0.12f * Mathf.Sin(u * Mathf.PI * 3.1f + 1.2f)
                    + 0.08f * Mathf.Sin(u * Mathf.PI * 6.5f);
                float a = v < hill ? Smoothstep(hill, hill - 0.08f, v) : 0f;
                float tone = Mathf.Lerp(0.15f, 0.05f, v);
                tex.SetPixel(x, y, new Color(tone, tone * 1.15f, tone * 1.05f, a));
            }
            return ToSprite(tex, w, h);
        }

        static Sprite MakeSolid(int size)
        {
            var tex = NewTex(size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
            return ToSprite(tex, size);
        }

        static Texture2D NewTex(int size) => NewTex(size, size);

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Sprite ToSprite(Texture2D tex, int size) => ToSprite(tex, size, size);

        static Sprite ToSprite(Texture2D tex, int w, int h)
        {
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h), 0, SpriteMeshType.FullRect);
        }

        static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
