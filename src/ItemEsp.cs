using System;
using UnityEngine;

namespace PeakMX
{
    internal static class ItemEsp
    {
        private static readonly GUIContent LabelContent = new GUIContent();
        private static GUIStyle _label;
        private static Texture2D _pixel;

        public static void Draw()
        {
            try
            {
                if (!ModConfig.ItemEsp)
                    return;
                if (Event.current != null && Event.current.type != EventType.Repaint)
                    return;

                Camera camera = Camera.main ?? Camera.current;
                if (camera == null)
                    return;

                Theme.EnsureBuilt();
                EnsureStyle();

                if (!EspPositions.TryObserver(camera, out Vector3 localPosition)) return;
                float maxDistance = Mathf.Max(1f, ModConfig.ItemEspDistance);
                float maxDistanceSqr = maxDistance * maxDistance;
                ushort selectedId = GameApi.ItemIdAt(ModConfig.ItemEspSelectedIndex);
                string search = (ModConfig.ItemEspSearch ?? "").Trim().ToLowerInvariant();

                var items = Item.ALL_ACTIVE_ITEMS;
                if (items == null)
                    return;

                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i];
                    if (!ShouldDraw(item, selectedId, search))
                        continue;

                    if (!EspPositions.TryItem(item, out Vector3 position)) continue;
                    float distanceSqr = (position - localPosition).sqrMagnitude;
                    if (distanceSqr > maxDistanceSqr)
                        continue;

                    float distance = Mathf.Sqrt(distanceSqr);
                    Vector2 screen = ProjectToScreenEdge(camera, position + Vector3.up * 0.25f, out bool offscreen);
                    DrawMarker(screen, $"{ItemName(item)} {distance:0}m", distance, offscreen);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[ItemEsp] {e.Message}");
            }
        }

        private static bool ShouldDraw(Item item, ushort selectedId, string search)
        {
            if (item == null || !item.gameObject.activeInHierarchy)
                return false;
            try
            {
                if (item.itemState != ItemState.Ground)
                    return false;
            }
            catch { }

            if (selectedId != ushort.MaxValue && item.itemID != selectedId)
                return false;

            if (string.IsNullOrWhiteSpace(search))
                return true;

            string haystack = (ItemName(item) + " "
                + item.name + " "
                + (item.gameObject != null ? item.gameObject.name : "") + " "
                + (item.UIData != null ? item.UIData.itemName : "") + " "
                + item.itemTags + " "
                + item.GetType().Name).ToLowerInvariant();
            return haystack.IndexOf(search, StringComparison.Ordinal) >= 0;
        }

        private static void DrawMarker(Vector2 center, string text, float distance, bool offscreen)
        {
            float alpha = Mathf.Clamp01(1.15f - distance / Mathf.Max(50f, ModConfig.ItemEspDistance));
            Color accent = Theme.Donate;
            accent.a = Mathf.Lerp(0.5f, 1f, alpha);
            Color bg = new Color(0.02f, 0.025f, 0.03f, Mathf.Lerp(0.38f, 0.78f, alpha));

            if (ModConfig.ItemEspLines)
                DrawLine(new Vector2(Screen.width * 0.5f, Screen.height - 18f), center, accent, 1.25f);

            float marker = offscreen ? 16f : Mathf.Clamp(18f - distance * 0.015f, 7f, 18f);
            DrawRect(new Rect(center.x - marker * 0.5f, center.y - 1f, marker, 2f), accent);
            DrawRect(new Rect(center.x - 1f, center.y - marker * 0.5f, 2f, marker), accent);

            LabelContent.text = text;
            Vector2 size = _label.CalcSize(LabelContent);
            Rect labelRect = new Rect(
                Mathf.Clamp(center.x - size.x * 0.5f - 8f, 4f, Screen.width - size.x - 16f),
                Mathf.Clamp(center.y - 30f, 4f, Screen.height - 24f),
                size.x + 16f,
                22f);
            DrawRect(labelRect, bg);
            DrawRect(new Rect(labelRect.x, labelRect.yMax - 2f, labelRect.width, 2f), accent);
            GUI.Label(labelRect, text, _label);
        }

        private static Vector2 ProjectToScreenEdge(Camera camera, Vector3 world, out bool offscreen)
        {
            Vector3 viewport = camera.WorldToViewportPoint(world);
            bool behind = viewport.z <= 0.01f;
            if (behind)
            {
                viewport.x = 1f - viewport.x;
                viewport.y = 1f - viewport.y;
            }

            offscreen = behind || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
            if (offscreen)
            {
                viewport.x = Mathf.Clamp(viewport.x, 0.04f, 0.96f);
                viewport.y = Mathf.Clamp(viewport.y, 0.06f, 0.94f);
            }
            return new Vector2(viewport.x * Screen.width, (1f - viewport.y) * Screen.height);
        }

        private static string ItemName(Item item)
        {
            try
            {
                string name = item.GetName();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            catch { }

            try
            {
                if (item.UIData != null && !string.IsNullOrWhiteSpace(item.UIData.itemName))
                    return item.UIData.itemName;
            }
            catch { }

            try { return item.gameObject.name; }
            catch { return "Item"; }
        }

        private static void EnsureStyle()
        {
            if (_pixel == null)
                _pixel = Texture2D.whiteTexture;

            if (_label != null)
                return;

            _label = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                clipping = TextClipping.Clip
            };
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = old;
        }

        private static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            Color old = GUI.color;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(a, b);
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, length, width), _pixel);
            GUI.matrix = matrix;
            GUI.color = old;
        }
    }
}
