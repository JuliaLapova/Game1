using UnityEngine;

namespace Nestglow
{
    /// <summary>
    /// Визуальная система Nestglow по Hallmark:
    /// genre: atmospheric · theme: Lumen Night Foundry · garden honey chord.
    /// </summary>
    public static class NestglowTheme
    {
        // Surfaces (Lumen Night Foundry — cool-violet paper, not pure black)
        public static readonly Color Paper = new Color(0.020f, 0.028f, 0.050f);
        public static readonly Color Paper2 = new Color(0.047f, 0.060f, 0.088f);
        public static readonly Color Paper3 = new Color(0.087f, 0.105f, 0.138f);
        public static readonly Color Rule = new Color(0.162f, 0.180f, 0.216f);

        // Ink
        public static readonly Color Ink = new Color(0.939f, 0.948f, 0.964f);
        public static readonly Color Ink2 = new Color(0.755f, 0.769f, 0.795f);
        public static readonly Color Muted = new Color(0.483f, 0.503f, 0.542f);

        // Accents — molten brass + coral chord (Lumen) + honey (Garden)
        public static readonly Color Accent = new Color(0.96f, 0.70f, 0.32f);   // brass
        public static readonly Color Accent2 = new Color(0.92f, 0.42f, 0.40f);  // coral
        public static readonly Color Honey = new Color(0.90f, 0.72f, 0.28f);
        public static readonly Color AccentInk = Paper;

        public static readonly Color Glow = new Color(0.96f, 0.70f, 0.32f, 0.42f);
        public static readonly Color GlowSoft = new Color(0.96f, 0.70f, 0.32f, 0.12f);
        public static readonly Color PaperEmit = new Color(0.96f, 0.70f, 0.32f, 0.05f);

        // Board wells
        public static readonly Color CellA = new Color(0.070f, 0.082f, 0.115f);
        public static readonly Color CellB = new Color(0.090f, 0.105f, 0.140f);

        // Semantic
        public static readonly Color Danger = new Color(0.95f, 0.55f, 0.48f);
        public static readonly Color Success = Accent;

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
