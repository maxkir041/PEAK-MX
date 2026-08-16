using UnityEngine;

namespace PeakMX
{
    /// <summary>Shared IMGUI styles and textures for the overlay.</summary>
    public static class Theme
    {
        public const string DefaultAccentHex = "#41D58A";
        public const string DefaultActionHex = "#599FF4";

        public static readonly Color Bg = C(0.071f, 0.082f, 0.098f);
        public static Color HeaderBg = C(0.114f, 0.255f, 0.180f);
        public static Color HeaderDim = C(0.078f, 0.157f, 0.122f);
        public static readonly Color Panel = C(0.133f, 0.149f, 0.176f);
        public static readonly Color PanelLight = C(0.180f, 0.200f, 0.231f);
        public static readonly Color PanelHi = C(0.235f, 0.259f, 0.298f);
        public static Color Accent = C(0.255f, 0.835f, 0.541f);
        public static Color AccentHi = C(0.345f, 0.918f, 0.616f);
        public static Color AccentDim = C(0.157f, 0.392f, 0.282f);
        public static readonly Color Text = C(0.949f, 0.957f, 0.965f);
        public static readonly Color TextDim = C(0.722f, 0.753f, 0.792f);
        public static Color Donate = C(0.349f, 0.624f, 0.957f);
        public static Color DonateHi = C(0.447f, 0.710f, 1.000f);
        public static readonly Color Gold = C(1.000f, 0.851f, 0.400f);
        public static Color DarkOnAccent = C(0.043f, 0.094f, 0.067f);
        public static Color TextOnAction = Color.white;

        public static GUIStyle Window, Title, Subtitle, Section, Label, LabelDim, DonateText, TipText;
        public static GUIStyle NavItem, NavItemActive, Toggle, ToggleOn, DonateBtn, LinkBtn, CloseBtn, DangerBtn, SuccessBtn;
        public static GUIStyle SliderBar, SliderThumb, Panel9, Tooltip, Card, SwitchOn, SwitchOff, RowLabel, RowHover;
        public static GUIStyle ListItem, ListItemActive, Chip, ChipActive, FoldoutBtn, FoldoutBtnOpen, TextInput, TextArea;
        public static Texture2D KnobTex;
        public static int Version { get; private set; }

        private static bool _built;
        private static string _builtAccentHex;
        private static string _builtActionHex;

        public static void EnsureBuilt()
        {
            string accentHex = NormalizeHex(ModConfig.AccentColorHex?.Value, DefaultAccentHex);
            string actionHex = NormalizeHex(ModConfig.ActionColorHex?.Value, DefaultActionHex);
            bool paletteChanged = !string.Equals(_builtAccentHex, accentHex, System.StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_builtActionHex, actionHex, System.StringComparison.OrdinalIgnoreCase);

            if (_built && !paletteChanged)
                return;

            ApplyPalette(accentHex, actionHex);
            _builtAccentHex = accentHex;
            _builtActionHex = actionHex;
            Version++;
            _built = true;

            Window = Rounded(GUI.skin.box, Bg, 14, Text);
            Window.padding = new RectOffset(0, 0, 0, 0);

            Panel9 = Rounded(GUI.skin.box, Panel, 12, Text);
            Panel9.padding = new RectOffset(12, 12, 10, 10);

            Tooltip = Rounded(GUI.skin.box, C(0.050f, 0.058f, 0.070f, 0.98f), 8, Text);
            Tooltip.alignment = TextAnchor.UpperLeft;
            Tooltip.fontSize = 12;
            Tooltip.wordWrap = true;
            Tooltip.padding = new RectOffset(12, 12, 10, 10);

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
            NavItem.padding = new RectOffset(14, 10, 8, 8);
            NavItem.margin = new RectOffset(0, 2, 2, 2);
            SetHover(NavItem, PanelHi, Color.white);

            NavItemActive = Rounded(GUI.skin.button, AccentDim, 8, Color.white);
            NavItemActive.alignment = TextAnchor.MiddleLeft;
            NavItemActive.fontSize = 13;
            NavItemActive.fontStyle = FontStyle.Bold;
            NavItemActive.padding = new RectOffset(14, 10, 8, 8);
            NavItemActive.margin = new RectOffset(0, 2, 2, 2);
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

            DonateBtn = Rounded(GUI.skin.button, Donate, 8, TextOnAction);
            DonateBtn.fontSize = 14;
            DonateBtn.wordWrap = false;
            DonateBtn.fontStyle = FontStyle.Bold;
            DonateBtn.padding = new RectOffset(14, 14, 7, 7);
            SetHover(DonateBtn, DonateHi, TextOnAction);

            LinkBtn = Rounded(GUI.skin.button, PanelLight, 7, Text);
            LinkBtn.fontSize = 12;
            LinkBtn.fontStyle = FontStyle.Bold;
            LinkBtn.padding = new RectOffset(12, 12, 7, 7);
            LinkBtn.margin = new RectOffset(0, 6, 2, 2);
            SetHover(LinkBtn, AccentDim, Color.white);

            TextInput = Rounded(GUI.skin.textField, PanelLight, 7, Text);
            TextInput.fontSize = 12;
            TextInput.padding = new RectOffset(10, 10, 7, 7);
            TextInput.margin = new RectOffset(0, 6, 2, 2);
            TextInput.wordWrap = false;
            TextInput.clipping = TextClipping.Clip;

            TextArea = Rounded(GUI.skin.textArea, PanelLight, 7, Text);
            TextArea.fontSize = 12;
            TextArea.padding = new RectOffset(10, 10, 8, 8);
            TextArea.margin = new RectOffset(0, 6, 2, 2);
            TextArea.wordWrap = true;

            CloseBtn = new GUIStyle(LinkBtn) { alignment = TextAnchor.MiddleCenter };

            DangerBtn = Rounded(GUI.skin.button, C(0.725f, 0.160f, 0.170f), 7, Color.white);
            DangerBtn.fontSize = 12;
            DangerBtn.fontStyle = FontStyle.Bold;
            DangerBtn.padding = new RectOffset(12, 12, 7, 7);
            DangerBtn.margin = new RectOffset(0, 6, 2, 2);
            SetHover(DangerBtn, C(0.920f, 0.220f, 0.230f), Color.white);

            SuccessBtn = Rounded(GUI.skin.button, AccentDim, 7, Color.white);
            SuccessBtn.fontSize = 12;
            SuccessBtn.fontStyle = FontStyle.Bold;
            SuccessBtn.padding = new RectOffset(12, 12, 7, 7);
            SuccessBtn.margin = new RectOffset(0, 6, 2, 2);
            SetHover(SuccessBtn, Accent, DarkOnAccent);

            FoldoutBtn = Rounded(GUI.skin.button, PanelLight, 9, Text);
            FoldoutBtn.alignment = TextAnchor.MiddleLeft;
            FoldoutBtn.fontSize = 13;
            FoldoutBtn.fontStyle = FontStyle.Bold;
            FoldoutBtn.padding = new RectOffset(12, 12, 8, 8);
            FoldoutBtn.margin = new RectOffset(0, 0, 0, 0);
            SetHover(FoldoutBtn, PanelHi, Color.white);

            FoldoutBtnOpen = Rounded(GUI.skin.button, AccentDim, 9, Color.white);
            FoldoutBtnOpen.alignment = TextAnchor.MiddleLeft;
            FoldoutBtnOpen.fontSize = 13;
            FoldoutBtnOpen.fontStyle = FontStyle.Bold;
            FoldoutBtnOpen.padding = new RectOffset(12, 12, 8, 8);
            FoldoutBtnOpen.margin = new RectOffset(0, 0, 0, 0);
            SetHover(FoldoutBtnOpen, AccentDim, Color.white);

            Card = Rounded(GUI.skin.box, C(0.102f, 0.117f, 0.141f), 12, Text);
            Card.padding = new RectOffset(14, 14, 12, 14);
            Card.margin = new RectOffset(0, 0, 0, 12);

            RowHover = Rounded(GUI.skin.box, C(0.180f, 0.200f, 0.231f, 0.55f), 8, Text);

            SwitchOn = Rounded(GUI.skin.box, Accent, 11, Text);
            SwitchOff = Rounded(GUI.skin.box, C(0.255f, 0.278f, 0.318f), 11, Text);
            RowLabel = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = Text } };

            // Compact list row (item spawner etc.): readable text, small vertical padding so
            // it is NOT clipped at a modest row height. Single line, no wrap.
            ListItem = Rounded(GUI.skin.button, Panel, 7, Text);
            ListItem.alignment = TextAnchor.MiddleLeft;
            ListItem.fontSize = 13;
            ListItem.wordWrap = false;
            ListItem.clipping = TextClipping.Clip;
            ListItem.padding = new RectOffset(12, 10, 5, 5);
            ListItem.margin = new RectOffset(0, 8, 2, 2);
            SetHover(ListItem, PanelHi, Color.white);

            ListItemActive = Rounded(GUI.skin.button, AccentDim, 7, Color.white);
            ListItemActive.alignment = TextAnchor.MiddleLeft;
            ListItemActive.fontSize = 13;
            ListItemActive.fontStyle = FontStyle.Bold;
            ListItemActive.wordWrap = false;
            ListItemActive.clipping = TextClipping.Clip;
            ListItemActive.padding = new RectOffset(12, 10, 5, 5);
            ListItemActive.margin = new RectOffset(0, 8, 2, 2);
            SetHover(ListItemActive, AccentDim, Color.white);

            // Small square chip (slot numbers): centered, compact.
            Chip = Rounded(GUI.skin.button, Panel, 7, TextDim);
            Chip.alignment = TextAnchor.MiddleCenter;
            Chip.fontSize = 13;
            Chip.padding = new RectOffset(0, 0, 4, 4);
            Chip.margin = new RectOffset(0, 6, 0, 0);
            SetHover(Chip, PanelHi, Color.white);

            ChipActive = Rounded(GUI.skin.button, AccentDim, 7, Color.white);
            ChipActive.alignment = TextAnchor.MiddleCenter;
            ChipActive.fontSize = 13;
            ChipActive.fontStyle = FontStyle.Bold;
            ChipActive.padding = new RectOffset(0, 0, 4, 4);
            ChipActive.margin = new RectOffset(0, 6, 0, 0);
            SetHover(ChipActive, AccentDim, Color.white);

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

        private static void ApplyPalette(string accentHex, string actionHex)
        {
            Accent = ColorFromHex(accentHex, C(0.255f, 0.835f, 0.541f));
            AccentHi = Mix(Accent, Color.white, 0.16f);
            AccentDim = Mix(Panel, Accent, 0.42f);
            HeaderBg = Mix(Bg, Accent, 0.30f);
            HeaderDim = Mix(Bg, Accent, 0.16f);
            DarkOnAccent = ContrastText(Accent);

            Donate = ColorFromHex(actionHex, C(0.349f, 0.624f, 0.957f));
            DonateHi = Mix(Donate, Color.white, 0.18f);
            TextOnAction = ContrastText(Donate);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            Color c = Color.Lerp(a, b, Mathf.Clamp01(t));
            c.a = Mathf.Lerp(a.a, b.a, Mathf.Clamp01(t));
            return c;
        }

        private static Color ContrastText(Color color)
        {
            float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            return luminance > 0.54f ? C(0.043f, 0.052f, 0.061f) : Color.white;
        }

        public static Color ColorFromHex(string value, Color fallback)
        {
            string hex = NormalizeHex(value, null);
            if (string.IsNullOrEmpty(hex))
                return fallback;

            try
            {
                byte r = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color(r / 255f, g / 255f, b / 255f, 1f);
            }
            catch
            {
                return fallback;
            }
        }

        public static string ColorToHex(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            return "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        public static string NormalizeHex(string value, string fallback)
        {
            string raw = (value ?? "").Trim();
            if (raw.StartsWith("#"))
                raw = raw.Substring(1);
            if (raw.Length == 3)
                raw = string.Concat(raw[0], raw[0], raw[1], raw[1], raw[2], raw[2]);

            if (raw.Length == 6)
            {
                bool ok = true;
                for (int i = 0; i < raw.Length; i++)
                {
                    char ch = raw[i];
                    if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F')))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    return "#" + raw.ToUpperInvariant();
            }

            return fallback;
        }

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
