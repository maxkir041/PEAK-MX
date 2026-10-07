using UnityEngine;

namespace PeakMX
{
    internal static class EspPositions
    {
        internal static bool TryCharacter(Character character, out Vector3 position)
        {
            position = Vector3.zero;
            if (character == null) return false;
            // Character's root can stay at the spawn point; Center follows the torso.
            try { position = character.Center; return IsFinite(position); }
            catch { return false; }
        }

        internal static bool TryItem(Item item, out Vector3 position)
        {
            position = Vector3.zero;
            if (item == null) return false;
            try { position = item.Center(); return IsFinite(position); }
            catch { return false; }
        }

        internal static bool TryObserver(Camera camera, out Vector3 position)
        {
            if (TryCharacter(Character.localCharacter, out position)) return true;
            if (camera == null) return false;
            position = camera.transform.position;
            return IsFinite(position);
        }

        internal static bool IsFinite(Vector3 position) =>
            !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
            !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
            !float.IsNaN(position.z) && !float.IsInfinity(position.z);
    }
}
