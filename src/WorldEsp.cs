using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakMX
{
    internal static class WorldEsp
    {
        private struct Marker
        {
            public string Label;
            public float Distance;
            public Color Color;
            public GameObject Source;
            public Character Character;
            public Item Item;
        }

        private static readonly List<Marker> Markers = new List<Marker>();
        private static readonly HashSet<int> Seen = new HashSet<int>();
        private static readonly GUIContent LabelContent = new GUIContent();
        private static readonly string[] BackItemKeywords = { "backpack", "fannypack", "jetpack", "rocketpack", "rucksack" };
        private static readonly string[] AmuletKeywords = { "amulet", "charm", "talisman", "scout" };
        private static readonly string[] ParentBoundaryKeywords = { "spawner", "manager", "holder", "root" };
        private static readonly string[] ArrowKeywords = { "arrow", "dart", "projectile" };
        private static readonly string[] ThornKeywords = { "thorn", "bramble", "spike" };
        private static readonly string[] SporeKeywords = { "spore", "mushroomcloud" };
        private static readonly string[] WebKeywords = { "web", "spider" };
        private static readonly string[] FlytrapKeywords = { "flytrap", "venus" };
        private static readonly string[] GloomKeywords = { "gloom" };
        private static readonly string[] RavenKeywords = { "raven" };
        private static readonly string[] ScoutmasterKeywords = { "scoutmaster", "scout master" };
        private static readonly string[] HeatKeywords = { "lava", "magma", "kiln" };
        private static readonly string[] PoisonKeywords = { "poison", "toxic", "acid" };
        private static readonly string[] ExplosionKeywords = { "explosion", "explode", "bomb" };
        private static readonly string[] DangerKeywords =
        {
            "arrow", "dart", "projectile", "thorn", "bramble", "spike", "spore", "mushroomcloud",
            "web", "spider", "flytrap", "venus", "gloom", "raven", "scoutmaster", "scout master",
            "lava", "magma", "kiln", "poison", "toxic", "acid", "explosion", "explode", "bomb",
        };
        private static GUIStyle _label;
        private static Texture2D _pixel;
        private static float _nextRefresh;

        public static void Draw()
        {
            try
            {
                if (ModConfig.WorldEsp == null || !ModConfig.WorldEsp.Value)
                    return;
                if (Event.current != null && Event.current.type != EventType.Repaint)
                    return;

                Camera camera = Camera.main ?? Camera.current;
                if (camera == null)
                    return;

                Theme.EnsureBuilt();
                EnsureStyle();
                RefreshMarkers();
                if (!EspPositions.TryObserver(camera, out Vector3 localPosition)) return;
                float maxDistance = Mathf.Max(25f, ModConfig.WorldEspDistance?.Value ?? 600f);
                float maxSqr = maxDistance * maxDistance;

                for (int i = 0; i < Markers.Count; i++)
                {
                    Marker marker = Markers[i];
                    if (!TryMarkerPosition(marker, out Vector3 position)) continue;
                    float distanceSqr = (position - localPosition).sqrMagnitude;
                    if (distanceSqr > maxSqr) continue;
                    float distance = Mathf.Sqrt(distanceSqr);
                    Vector2 screen = ProjectToScreenEdge(camera, position + Vector3.up * 0.25f, out bool offscreen);
                    DrawMarker(screen, $"{marker.Label} {distance:0}m", distance, offscreen, marker.Color);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug("[WorldEsp] " + e.Message);
            }
        }

        private static void RefreshMarkers()
        {
            if (Time.realtimeSinceStartup < _nextRefresh)
                return;

            _nextRefresh = Time.realtimeSinceStartup + 1.1f;
            Markers.Clear();
            Seen.Clear();

            Vector3 local = LocalPosition();
            float maxDistance = Mathf.Max(25f, ModConfig.WorldEspDistance?.Value ?? 600f);
            float maxSqr = maxDistance * maxDistance;

            if (ModConfig.WorldEspBackItems?.Value == true || ModConfig.WorldEspAmulets?.Value == true)
                AddGroundItems(local, maxSqr);
            if (ModConfig.WorldEspCampfires?.Value == true)
                AddCampfires(local, maxSqr);
            if (ModConfig.WorldEspDangers?.Value == true)
                AddDangerObjects(local, maxSqr);

            Markers.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            if (Markers.Count > 80)
                Markers.RemoveRange(80, Markers.Count - 80);
        }

        private static void AddGroundItems(Vector3 local, float maxSqr)
        {
            var items = Item.ALL_ACTIVE_ITEMS;
            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item == null || item.gameObject == null || !item.gameObject.activeInHierarchy)
                    continue;

                try
                {
                    if (item.itemState != ItemState.Ground)
                        continue;
                }
                catch { }

                string name = ItemName(item);
                string haystack = (name + " " + item.name + " " + item.GetType().Name).ToLowerInvariant();
                bool back = ContainsAny(haystack, BackItemKeywords);
                bool amulet = ContainsAny(haystack, AmuletKeywords);
                if ((back && ModConfig.WorldEspBackItems?.Value == true)
                    || (amulet && ModConfig.WorldEspAmulets?.Value == true))
                {
                    AddMarker(item.gameObject, name, ItemPosition(item), local, maxSqr,
                        back ? new Color(0.42f, 0.75f, 1f, 1f) : new Color(1f, 0.78f, 0.35f, 1f));
                }
            }
        }

        private static void AddCampfires(Vector3 local, float maxSqr)
        {
            try
            {
                Campfire[] campfires = UnityEngine.Object.FindObjectsByType<Campfire>(FindObjectsSortMode.None);
                for (int i = 0; i < campfires.Length; i++)
                {
                    Campfire campfire = campfires[i];
                    if (campfire == null || campfire.gameObject == null || !campfire.gameObject.activeInHierarchy)
                        continue;
                    AddMarker(campfire.gameObject, "Campfire", campfire.transform.position, local, maxSqr,
                        new Color(1f, 0.55f, 0.24f, 1f));
                }
            }
            catch { }
        }

        private static void AddDangerObjects(Vector3 local, float maxSqr)
        {
            try
            {
                MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour.gameObject == null || !behaviour.gameObject.activeInHierarchy)
                        continue;

                    string quickText = (behaviour.GetType().Name + " " + behaviour.gameObject.name).ToLowerInvariant();
                    if (!LooksLikeDangerName(quickText))
                        continue;

                    GameObject root = InterestingDangerRoot(behaviour.gameObject);
                    if (root == null)
                        continue;

                    string text = DangerText(root, behaviour);
                    string label = DangerLabel(text);
                    if (string.IsNullOrWhiteSpace(label))
                        continue;

                    AddMarker(root, label, root.transform.position, local, maxSqr,
                        new Color(1f, 0.36f, 0.32f, 1f));
                }
            }
            catch { }
        }

        private static GameObject InterestingDangerRoot(GameObject obj)
        {
            if (obj == null)
                return null;
            try
            {
                Item item = obj.GetComponentInParent<Item>();
                if (item != null)
                    return null;
            }
            catch { }
            try
            {
                Campfire campfire = obj.GetComponentInParent<Campfire>();
                if (campfire != null)
                    return null;
            }
            catch { }

            Transform t = obj.transform;
            for (int i = 0; i < 4 && t.parent != null; i++)
            {
                string n = t.parent.name.ToLowerInvariant();
                if (ContainsAny(n, ParentBoundaryKeywords))
                    break;
                if (LooksLikeDangerName(n))
                    t = t.parent;
                else
                    break;
            }
            return t.gameObject;
        }

        private static string DangerText(GameObject root, Component component)
        {
            string text = "";
            try { text += root.name + " "; } catch { }
            try { text += component.GetType().Name + " "; } catch { }
            try
            {
                Component[] components = root.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                    if (components[i] != null)
                        text += components[i].GetType().Name + " ";
            }
            catch { }
            return text.ToLowerInvariant();
        }

        private static string DangerLabel(string text)
        {
            if (ContainsAny(text, ArrowKeywords)) return "Arrow trap";
            if (ContainsAny(text, ThornKeywords)) return "Thorns";
            if (ContainsAny(text, SporeKeywords)) return "Spores";
            if (ContainsAny(text, WebKeywords)) return "Web";
            if (ContainsAny(text, FlytrapKeywords)) return "Flytrap";
            if (ContainsAny(text, GloomKeywords)) return "Gloom";
            if (ContainsAny(text, RavenKeywords)) return "Raven";
            if (ContainsAny(text, ScoutmasterKeywords)) return "Scoutmaster";
            if (ContainsAny(text, HeatKeywords)) return "Heat";
            if (ContainsAny(text, PoisonKeywords)) return "Poison";
            if (ContainsAny(text, ExplosionKeywords)) return "Explosion";
            return "";
        }

        private static bool LooksLikeDangerName(string value)
        {
            return ContainsAny(value, DangerKeywords);
        }

        private static void AddMarker(GameObject source, string label, Vector3 position, Vector3 local, float maxSqr, Color color)
        {
            if (source == null)
                return;
            int id = source.GetInstanceID();
            if (Seen.Contains(id))
                return;

            Character character = source.GetComponentInParent<Character>();
            Item item = source.GetComponent<Item>();
            if (character != null && !EspPositions.TryCharacter(character, out position)) return;
            if (character == null && item != null && !EspPositions.TryItem(item, out position)) return;

            float sqr = (position - local).sqrMagnitude;
            if (sqr > maxSqr)
                return;

            Seen.Add(id);
            Markers.Add(new Marker
            {
                Label = label,
                Distance = Mathf.Sqrt(sqr),
                Color = color,
                Source = source,
                Character = character,
                Item = item,
            });
        }

        private static bool TryMarkerPosition(Marker marker, out Vector3 position)
        {
            position = Vector3.zero;
            if (marker.Source == null || !marker.Source.activeInHierarchy) return false;
            if (marker.Character != null) return EspPositions.TryCharacter(marker.Character, out position);
            if (marker.Item != null)
            {
                if (marker.Item.itemState != ItemState.Ground) return false;
                return EspPositions.TryItem(marker.Item, out position);
            }
            position = marker.Source.transform.position;
            return EspPositions.IsFinite(position);
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

        private static Vector3 ItemPosition(Item item)
        {
            try { return item.Center(); }
            catch { }
            try { return item.transform.position; }
            catch { return Vector3.zero; }
        }

        private static Vector3 LocalPosition()
        {
            return EspPositions.TryObserver(Camera.main, out Vector3 position) ? position : Vector3.zero;
        }

        private static bool ContainsAny(string value, string[] needles)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            for (int i = 0; i < needles.Length; i++)
                if (!string.IsNullOrWhiteSpace(needles[i])
                    && value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
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

        private static void DrawMarker(Vector2 center, string text, float distance, bool offscreen, Color color)
        {
            float alpha = Mathf.Clamp01(1.15f - distance / Mathf.Max(50f, ModConfig.WorldEspDistance?.Value ?? 600f));
            Color accent = color;
            accent.a = Mathf.Lerp(0.5f, 1f, alpha);
            Color bg = new Color(0.02f, 0.025f, 0.03f, Mathf.Lerp(0.38f, 0.78f, alpha));

            if (ModConfig.WorldEspLines?.Value == true)
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
