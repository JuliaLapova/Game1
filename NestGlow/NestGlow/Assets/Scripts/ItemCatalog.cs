using UnityEngine;

namespace Nestglow
{
    /// <summary>Ранги Nestglow — тёплая латунно-медовая шкала → холодное сияние.</summary>
    public static class ItemCatalog
    {
        public const int MaxRank = 8;

        public static readonly string[] Names =
        {
            "?",
            "Искра",
            "Светлячок",
            "Огонёк",
            "Лампа",
            "Фонарь",
            "Маяк",
            "Созвездие",
            "Гнездо света"
        };

        // Hallmark Lumen brass → coral → cool ink (не фиолетовый «AI glow»)
        public static readonly Color[] Colors =
        {
            Color.magenta,
            new Color(1.00f, 0.94f, 0.78f), // искра
            NestglowTheme.Honey,             // светлячок
            NestglowTheme.Accent,            // огонёк
            new Color(1.00f, 0.58f, 0.28f), // лампа
            NestglowTheme.Accent2,           // фонарь
            new Color(0.72f, 0.82f, 0.95f), // маяк
            NestglowTheme.Ink2,              // созвездие
            NestglowTheme.Ink                // гнездо
        };

        public static string GetName(int rank)
        {
            if (rank < 1 || rank > MaxRank) return "?";
            return Names[rank];
        }

        public static Color GetColor(int rank)
        {
            if (rank < 1 || rank > MaxRank) return Color.white;
            return Colors[rank];
        }

        public static Color GetGlowColor(int rank)
        {
            var c = GetColor(rank);
            // мягкий базовый alpha — яркость добирается мерцанием
            return new Color(c.r, c.g, c.b, 0.22f);
        }
    }
}
