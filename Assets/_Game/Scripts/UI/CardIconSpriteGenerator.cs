using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Generates and manages high-contrast procedural icon sprites for Card Draft UI
    /// and Hero Recruitment visuals, ensuring clear readability and distinct visual identity.
    /// </summary>
    public static class CardIconSpriteGenerator
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetSpriteForCard(string cardTitle, string cardType, string heroId = null)
        {
            string key = $"{heroId}_{cardType}_{cardTitle}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite generated = CreateHighContrastIcon(cardTitle, cardType, heroId);
            Cache[key] = generated;
            return generated;
        }

        private static Sprite CreateHighContrastIcon(string title, string type, string heroId)
        {
            int width = 128;
            int height = 128;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color primaryColor = GetPrimaryColor(heroId, type, title);
            Color accentColor = GetAccentColor(heroId, type, title);
            Color darkBg = new Color(0.08f, 0.10f, 0.16f, 1f);

            float radius = width * 0.45f;
            Vector2 center = new Vector2(width / 2f, height / 2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float dist = Vector2.Distance(pos, center);

                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - 4f)
                    {
                        // High-contrast border ring
                        tex.SetPixel(x, y, accentColor);
                    }
                    else if (dist > radius - 8f)
                    {
                        // Inner dark trim
                        tex.SetPixel(x, y, Color.Lerp(darkBg, accentColor, 0.3f));
                    }
                    else
                    {
                        // Core pattern fill
                        float normalizedDist = dist / (radius - 8f);
                        Color c = Color.Lerp(primaryColor, darkBg, normalizedDist * 0.4f);

                        if (Mathf.Abs(x - width / 2f) < 4f || Mathf.Abs(y - height / 2f) < 4f)
                        {
                            c = Color.Lerp(c, accentColor, 0.6f);
                        }

                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private static Color GetPrimaryColor(string heroId, string type, string title)
        {
            string t = (title ?? "").ToLower();
            string h = (heroId ?? "").ToLower();

            if (h.Contains("archer") || t.Contains("arrow") || t.Contains("shot"))
                return new Color(0.18f, 0.55f, 0.22f);
            if (h.Contains("bombardier") || t.Contains("blast") || t.Contains("powder"))
                return new Color(0.35f, 0.35f, 0.40f);
            if (h.Contains("frost") || t.Contains("ice") || t.Contains("freeze"))
                return new Color(0.15f, 0.50f, 0.85f);
            if (h.Contains("fire") || t.Contains("flame") || t.Contains("ember"))
                return new Color(0.85f, 0.22f, 0.10f);
            if (h.Contains("electric") || h.Contains("engineer") || t.Contains("chain") || t.Contains("static"))
                return new Color(0.85f, 0.72f, 0.10f);
            if (h.Contains("sniper") || t.Contains("precision") || t.Contains("deadeye"))
                return new Color(0.45f, 0.22f, 0.65f);
            if (h.Contains("plague") || h.Contains("doctor") || t.Contains("poison") || t.Contains("toxic"))
                return new Color(0.08f, 0.40f, 0.22f);
            if (h.Contains("radiant") || h.Contains("paladin") || t.Contains("holy") || t.Contains("divine") || t.Contains("smite"))
                return new Color(0.88f, 0.85f, 0.90f);
            if (h.Contains("shadow") || h.Contains("assassin") || t.Contains("void") || t.Contains("stealth"))
                return new Color(0.15f, 0.12f, 0.22f);
            if (h.Contains("storm") || h.Contains("druid") || t.Contains("tempest") || t.Contains("gale"))
                return new Color(0.08f, 0.48f, 0.48f);

            if (type == "Add") return new Color(0.10f, 0.60f, 0.35f);
            if (type == "Upgrade") return new Color(0.85f, 0.45f, 0.10f);

            return new Color(0.25f, 0.30f, 0.45f);
        }

        private static Color GetAccentColor(string heroId, string type, string title)
        {
            string t = (title ?? "").ToLower();
            string h = (heroId ?? "").ToLower();

            if (h.Contains("archer") || t.Contains("arrow"))
                return new Color(0.95f, 0.82f, 0.30f);
            if (h.Contains("bombardier") || t.Contains("blast"))
                return new Color(1.00f, 0.55f, 0.10f);
            if (h.Contains("frost") || t.Contains("freeze"))
                return new Color(0.60f, 0.92f, 1.00f);
            if (h.Contains("fire") || t.Contains("flame"))
                return new Color(1.00f, 0.65f, 0.20f);
            if (h.Contains("electric") || t.Contains("chain"))
                return new Color(0.40f, 0.85f, 1.00f);
            if (h.Contains("sniper") || t.Contains("deadeye"))
                return new Color(0.85f, 0.60f, 1.00f);
            if (h.Contains("plague") || h.Contains("doctor") || t.Contains("poison"))
                return new Color(0.30f, 0.95f, 0.45f);
            if (h.Contains("radiant") || h.Contains("paladin") || t.Contains("holy"))
                return new Color(1.00f, 0.85f, 0.20f);
            if (h.Contains("shadow") || h.Contains("assassin") || t.Contains("void"))
                return new Color(0.75f, 0.30f, 1.00f);
            if (h.Contains("storm") || h.Contains("druid") || t.Contains("tempest"))
                return new Color(0.25f, 0.95f, 0.95f);

            return new Color(0.90f, 0.90f, 0.95f);
        }
    }
}
