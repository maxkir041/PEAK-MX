using UnityEngine;

namespace PeakMX
{
    /// <summary>
    /// Custom IMGUI look for PEAK-MX with rounded corners (generated rounded textures + 9-slice).
    /// Built once, lazily, during the first OnGUI pass. Original styling by maxkir041.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Bg = C(0.071f, 0.082f, 0.098f);
        public static readonly Color HeaderBg = C(0.114f, 0.255f, 0.180f);
        public static readonly Color Panel = C(0.133f, 0.149f, 0.176f);
        public static readonly Color PanelLight = C(0.180f, 0.200f, 0.231f);
        public static readonly Color PanelHi = C(0.235f, 0.259f, 0.298f);
        public static readonly Color Accent = C(0.255f, 0.835f, 0.541f);
        public static readonly Color AccentHi = C(0.345f, 0.918f, 0.616f);
        public static readonly Color AccentDim = C(0.157f, 0.392f, 0.282f);
        public static readonly Color Text = C(0.949f, 0.957f, 0.965f);
        public static readonly Color TextDim = C(0.722f, 0.753f, 0.792f);
        public static readonly Color Donate = C(1.000f, 0.620f, 0.231f);
        public static readonly Color DonateHi = C(1.000f, 0.706f, 0.345f);
        public static readonly Color Gold = C(1.000f, 0.851f, 0.400f);
        public static readonly Color DarkOnAccent = C(0.043f, 0.094f, 0.067f);

        public static GUIStyle Window, Title, Subtitle, Section, Label, LabelDim, DonateText, TipText;
        public static GUIStyle NavItem, NavItemActive, Toggle, ToggleOn, DonateBtn, LinkBtn, CloseBtn;
        public static GUIStyle SliderBar, SliderThumb, Panel9, Tooltip, Card, SwitchOn, SwitchOff, RowLabel, RowHover;
        public static Texture2D KnobTex;

        private static bool _built;

        public static void EnsureBuilt()
        {
            if (_built)
                return;
            _built = true;

            Window = Rounded(GUI.skin.box, Bg, 12, Text);
            Window.padding = new RectOffset(0, 0, 0, 0);

            Panel9 = Rounded(GUI.skin.box, Panel, 10, Text);
            Panel9.padding = new RectOffset(10, 10, 8, 8);

            Tooltip = Rounded(GUI.skin.box, C(0.050f, 0.058f, 0.070f, 0.98f), 8, Gold);

            Title = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            Subtitle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = C(0.82f, 0.92f, 0.86f) } };
            Section = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Accent },
                margin = new RectOffset(2, 0, 10, 4),
            };
            Label = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Text }, wordWrap = true };
            LabelDim = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = TextDim }, wordWrap = true };
            DonateText = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Gold } };
            TipText = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, normal = { textColor = Gold }, padding = new RectOffset(10, 10, 6, 6) };

            NavItem = Rounded(GUI.skin.button, Panel, 8, TextDim);
            NavItem.alignment = TextAnchor.MiddleLeft;
            NavItem.fontSize = 13;
            NavItem.padding = new RectOffset(14, 8, 9, 9);
            NavItem.margin = new RectOffset(0, 6, 3, 3);
            SetHover(NavItem, PanelHi, Color.white);

            NavItemActive = Rounded(GUI.skin.button, AccentDim, 8, Color.white);
            NavItemActive.alignment = TextAnchor.MiddleLeft;
            NavItemActive.fontSize = 13;
            NavItemActive.fontStyle = FontStyle.Bold;
            NavItemActive.padding = new RectOffset(14, 8, 9, 9);
            NavItemActive.margin = new RectOffset(0, 6, 3, 3);
            SetHover(NavItemActive, AccentDim, Color.white);

            Toggle = Rounded(GUI.skin.button, PanelLight, 8, Text);
            Toggle.alignment = TextAnchor.MiddleLeft;
            Toggle.fontSize = 13;
            Toggle.padding = new RectOffset(14, 12, 10, 10);
            Toggle.margin = new RectOffset(0, 8, 4, 0);
            SetHover(Toggle, PanelHi, Color.white);

            ToggleOn = Rounded(GUI.skin.button, Accent, 8, DarkOnAccent);
            ToggleOn.alignment = TextAnchor.MiddleLeft;
            ToggleOn.fontSize = 13;
            ToggleOn.fontStyle = FontStyle.Bold;
            ToggleOn.padding = new RectOffset(14, 12, 10, 10);
            ToggleOn.margin = new RectOffset(0, 8, 4, 0);
            SetHover(ToggleOn, AccentHi, DarkOnAccent);

            DonateBtn = Rounded(GUI.skin.button, Donate, 8, Color.white);
            DonateBtn.fontSize = 14;
            DonateBtn.fontStyle = FontStyle.Bold;
            DonateBtn.padding = new RectOffset(14, 14, 9, 9);
            SetHover(DonateBtn, DonateHi, Color.white);

            LinkBtn = Rounded(GUI.skin.button, PanelLight, 7, Text);
            LinkBtn.fontSize = 12;
            LinkBtn.fontStyle = FontStyle.Bold;
            LinkBtn.padding = new RectOffset(12, 12, 7, 7);
            LinkBtn.margin = new RectOffset(0, 6, 2, 2);
            SetHover(LinkBtn, AccentDim, Color.white);

            CloseBtn = new GUIStyle(LinkBtn) { alignment = TextAnchor.MiddleCenter };

            Card = Rounded(GUI.skin.box, C(0.102f, 0.117f, 0.141f), 10, Text);
            Card.padding = new RectOffset(12, 12, 10, 12);
            Card.margin = new RectOffset(0, 0, 0, 10);

            RowHover = Rounded(GUI.skin.box, C(0.180f, 0.200f, 0.231f, 0.55f), 8, Text);

            SwitchOn = Rounded(GUI.skin.box, Accent, 11, Text);
            SwitchOff = Rounded(GUI.skin.box, C(0.255f, 0.278f, 0.318f), 11, Text);
            RowLabel = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = Text } };
            KnobTex = RoundedTex(Color.white, 16);

            SliderBar = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 8, margin = new RectOffset(4, 4, 10, 4),
                border = new RectOffset(4, 4, 4, 4),
                normal = { background = RoundedTex(PanelHi, 4) },
            };
            SliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = 18, fixedWidth = 18, border = new RectOffset(0, 0, 0, 0),
                normal = { background = RoundedTex(Accent, 9) },
                hover = { background = RoundedTex(AccentHi, 9) },
                active = { background = RoundedTex(AccentHi, 9) },
            };
        }

        // ---- builders ----
        private static GUIStyle Rounded(GUIStyle baseStyle, Color fill, int radius, Color textColor)
        {
            var s = new GUIStyle(baseStyle)
            {
                border = new RectOffset(radius, radius, radius, radius),
                normal = { background = RoundedTex(fill, radius), textColor = textColor },
            };
            return s;
        }

        private static void SetHover(GUIStyle s, Color hoverFill, Color hoverText)
        {
            int r = s.border.left;
            s.hover = new GUIStyleState { background = RoundedTex(hoverFill, r), textColor = hoverText };
            s.active = new GUIStyleState { background = RoundedTex(hoverFill, r), textColor = hoverText };
            s.onNormal = new GUIStyleState { background = RoundedTex(hoverFill, r), textColor = hoverText };
        }

        // ---- helpers ----
        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        /// <summary>Flat 1x1 fill texture (for bands and dividers).</summary>
        public static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>Vertical gradient texture (1 x h). Row 0 = bottom color, top row = top color.</summary>
        public static Texture2D GradientTex(Color top, Color bottom, int h)
        {
            if (h < 2) h = 2;
            var t = new Texture2D(1, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
                t.SetPixel(0, y, Color.Lerp(bottom, top, (float)y / (h - 1)));
            t.Apply();
            return t;
        }

        /// <summary>Rounded-corner texture (size 2*radius+1) for use as a 9-sliced background.</summary>
        public static Texture2D RoundedTex(Color col, int radius)
        {
            if (radius < 1) radius = 1;
            int size = radius * 2 + 1;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.filterMode = FilterMode.Bilinear;
            int max = size - 1;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x; else if (x > max - radius) dx = x - (max - radius);
                    if (y < radius) dy = radius - y; else if (y > max - radius) dy = y - (max - radius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - dist + 0.5f); // ~1px anti-aliased edge
                    t.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            }
            t.Apply();
            return t;
        }
    }
}
