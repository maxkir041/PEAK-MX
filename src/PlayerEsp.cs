using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakMX
{
    internal static class PlayerEsp
    {
        private static readonly HashSet<int> Seen = new HashSet<int>();
        private static readonly List<Character> Characters = new List<Character>();
        private static readonly GUIContent LabelContent = new GUIContent();
        private static GUIStyle _label;
        private static Texture2D _pixel;
        private static float _nextRefresh;
        private static int _focusInstanceId;

        public static void Focus(Character character)
        {
            try
            {
                _focusInstanceId = character != null ? ((UnityEngine.Object)character).GetInstanceID() : 0;
                if (_focusInstanceId != 0)
                    ModConfig.PlayerEsp = true;
            }
            catch
            {
                _focusInstanceId = 0;
            }
        }

        public static void ClearFocus()
        {
            _focusInstanceId = 0;
        }

        public static void Draw()
        {
            try
            {
                if (!ModConfig.PlayerEsp)
                    return;
                if (Event.current != null && Event.current.type != EventType.Repaint)
                    return;

                Camera camera = Camera.main ?? Camera.current;
                if (camera == null)
                    return;

                Theme.EnsureBuilt();
                EnsureStyle();
                RefreshCharacters();

                if (!EspPositions.TryObserver(camera, out Vector3 localPosition))
                    return;
                float maxDistance = Mathf.Max(1f, ModConfig.PlayerEspDistance);
                float maxDistanceSqr = maxDistance * maxDistance;

                for (int i = 0; i < Characters.Count; i++)
                {
                    Character character = Characters[i];
                    if (character == null || IsLocal(character) || !EspPositions.TryCharacter(character, out Vector3 targetPosition))
                        continue;

                    float distanceSqr = (targetPosition - localPosition).sqrMagnitude;
                    if (distanceSqr > maxDistanceSqr)
                        continue;
                    float distance = Mathf.Sqrt(distanceSqr);

                    Vector3 head = HeadPosition(character, targetPosition);
                    Vector2 point = ProjectToScreenEdge(camera, head, out bool offscreen);
                    Rect box = CharacterBox(camera, character, targetPosition, head, point, offscreen);
                    bool focused = _focusInstanceId != 0 && ((UnityEngine.Object)character).GetInstanceID() == _focusInstanceId;
                    string state = IsDead(character) ? " [dead]" : "";
                    string label = $"{GameApi.PlayerDisplayName(character)} {distance:0}m{state}";
                    if (focused)
                        label = ">> " + label;

                    DrawMarker(point, label, distance, box, offscreen, focused);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[PlayerEsp] {e.Message}");
            }
        }

        private static void RefreshCharacters()
        {
            if (Time.realtimeSinceStartup < _nextRefresh)
                return;

            _nextRefresh = Time.realtimeSinceStartup + 0.75f;
            Characters.Clear();
            Seen.Clear();

            try { GameApi.RefreshPlayers(); } catch { }
            for (int i = 0; i < GameApi.PlayerChars.Count; i++)
                AddCharacter(GameApi.PlayerChars[i]);

            try
            {
                foreach (Character character in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
                    AddCharacter(character);
            }
            catch { }
        }

        private static void AddCharacter(Character character)
        {
            if (character == null)
                return;

            int id = ((UnityEngine.Object)character).GetInstanceID();
            if (Seen.Contains(id))
                return;

            try
            {
                if (character.isBot || character.isZombie || character.isScoutmaster)
                    return;
            }
            catch { }

            Seen.Add(id);
            Characters.Add(character);
        }

        private static void DrawMarker(Vector2 center, string text, float distance, Rect box, bool offscreen, bool focused)
        {
            float alpha = Mathf.Clamp01(1.15f - distance / Mathf.Max(50f, ModConfig.PlayerEspDistance));
            Color accent = focused ? Theme.Donate : Theme.Accent;
            accent.a = Mathf.Lerp(0.55f, 1f, alpha);
            Color bg = new Color(0.02f, 0.025f, 0.03f, Mathf.Lerp(0.42f, 0.82f, alpha));

            if (ModConfig.PlayerEspLines || focused)
                DrawLine(new Vector2(Screen.width * 0.5f, Screen.height - 18f), offscreen ? center : new Vector2(box.center.x, box.yMax), accent, focused ? 2.5f : 1.5f);

            if (ModConfig.PlayerEspBoxes)
            {
                if (offscreen)
                    DrawEdgeMarker(center, accent);
                else
                    DrawBox(box, accent, focused ? 2.5f : 1.5f);
            }

            LabelContent.text = text;
            Vector2 size = _label.CalcSize(LabelContent);
            Rect labelRect = new Rect(
                Mathf.Clamp(center.x - size.x * 0.5f - 8f, 4f, Screen.width - size.x - 16f),
                Mathf.Clamp(center.y - 34f, 4f, Screen.height - 26f),
                size.x + 16f,
                24f);

            DrawRect(labelRect, bg);
            DrawRect(new Rect(labelRect.x, labelRect.yMax - 2f, labelRect.width, 2f), accent);
            GUI.Label(labelRect, text, _label);

            float marker = Mathf.Clamp(24f - distance * 0.02f, 8f, 24f);
            DrawRect(new Rect(center.x - marker * 0.5f, center.y - 1f, marker, 2f), accent);
            DrawRect(new Rect(center.x - 1f, center.y - marker * 0.5f, 2f, marker), accent);
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

        private static Rect CharacterBox(Camera camera, Character character, Vector3 position, Vector3 head, Vector2 fallback, bool offscreen)
        {
            if (offscreen)
                return new Rect(fallback.x - 12f, fallback.y - 12f, 24f, 24f);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            int count = 0;

            try
            {
                var parts = character.refs?.ragdoll?.partList;
                if (parts != null)
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (parts[i] == null)
                            continue;
                        AddPoint(camera.WorldToScreenPoint(((Component)parts[i]).transform.position));
                    }
                }
            }
            catch { }

            AddPoint(camera.WorldToScreenPoint(head + Vector3.up * 0.15f));
            AddPoint(camera.WorldToScreenPoint(position + Vector3.down * 0.8f));

            if (count == 0)
                return new Rect(fallback.x - 16f, fallback.y - 32f, 32f, 64f);

            float width = Mathf.Max(maxX - minX, 28f);
            float height = Mathf.Max(maxY - minY, 42f);
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            return new Rect(cx - width * 0.5f - 4f, cy - height * 0.5f - 4f, width + 8f, height + 8f);

            void AddPoint(Vector3 screen)
            {
                if (screen.z <= 0.01f)
                    return;
                float x = screen.x;
                float y = Screen.height - screen.y;
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
                count++;
            }
        }

        private static void DrawBox(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private static void DrawEdgeMarker(Vector2 center, Color color)
        {
            float size = 10f;
            DrawRect(new Rect(center.x - size, center.y - size, size * 2f, 2f), color);
            DrawRect(new Rect(center.x - size, center.y + size, size * 2f, 2f), color);
            DrawRect(new Rect(center.x - size, center.y - size, 2f, size * 2f), color);
            DrawRect(new Rect(center.x + size, center.y - size, 2f, size * 2f), color);
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

        private static Vector3 HeadPosition(Character character, Vector3 center)
        {
            try
            {
                Vector3 head = character.Head + Vector3.up * 0.25f;
                if (EspPositions.IsFinite(head)) return head;
            }
            catch { }
            return center + Vector3.up * 0.7f;
        }

        private static bool IsLocal(Character character)
        {
            try { return character.IsLocal || character == Character.localCharacter; }
            catch { return character == Character.localCharacter; }
        }

        private static bool IsDead(Character character)
        {
            try { return character.data != null && character.data.dead; }
            catch { return false; }
        }
    }
}
