using System;
using PeakMX;
using UnityEngine;

// Engine-free stubs exercise the production position resolver, not a copy of it.
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public static Vector3 zero => new(0, 0, 0);
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public class Transform { public Vector3 position; }
    public class Camera { public Transform transform = new(); }
}

public class Character
{
    public static Character localCharacter;
    public Transform transform = new();
    public Vector3 Body;
    public bool Unready;
    public Vector3 Center => Unready ? throw new InvalidOperationException("Body not ready") : Body;
}

public class Item
{
    public Vector3 Body;
    public bool Unready;
    public Vector3 Center() => Unready ? throw new InvalidOperationException("Item not ready") : Body;
}

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        _checks++;
    }
    private static float Distance(Vector3 a, Vector3 b) =>
        MathF.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));

    private static void Main()
    {
        var local = new Character { Body = new(10, 2, 3) };
        var remote = new Character { Body = new(35, 2, 3) };
        local.transform.position = new(0, 0, 0);
        remote.transform.position = new(4, 0, 0);
        Character.localCharacter = local;
        Check(EspPositions.TryObserver(null, out Vector3 observer), "Local body must be resolved");
        Check(EspPositions.TryCharacter(remote, out Vector3 target), "Remote body must be resolved");
        Check(Distance(observer, target) == 25, "Distance must be 25m, not the constant 4m between spawn roots");
        remote.Body = new(60, 2, 3);
        Check(EspPositions.TryCharacter(remote, out target) && Distance(observer, target) == 50, "Target movement must update immediately");
        local.Body = new(20, 2, 3);
        Check(EspPositions.TryObserver(null, out observer) && Distance(observer, target) == 40, "Observer movement must update immediately");
        Check(!EspPositions.TryCharacter(null, out _), "Missing characters must not produce origin markers");
        remote.Unready = true;
        Check(!EspPositions.TryCharacter(remote, out _), "Unready characters must not fall back to stale root positions");

        var camera = new Camera();
        camera.transform.position = new(-2, 4, -6);
        Character.localCharacter = null;
        Check(EspPositions.TryObserver(camera, out observer) && observer.x == -2 && observer.z == -6, "Spectator camera fallback");
        Check(!EspPositions.TryObserver(null, out _), "No observer must not produce a fake zero-meter distance");
        Character.localCharacter = new Character { Unready = true };
        Check(EspPositions.TryObserver(camera, out observer) && observer.y == 4, "Camera fallback while the character initializes");

        var item = new Item { Body = new(10, 0, 0) };
        Check(EspPositions.TryItem(item, out target) && target.x == 10, "Resolve actual item center");
        item.Body = new(50, 0, 0);
        Check(EspPositions.TryItem(item, out target) && target.x == 50, "Track moving items without cached coordinates");
        item.Unready = true;
        Check(!EspPositions.TryItem(item, out _), "Unready items must be skipped");
        Check(!EspPositions.TryItem(null, out _), "Destroyed items must be skipped");
        Check(EspPositions.IsFinite(new(-100, 0, 100)), "Negative world coordinates are valid");
        Check(!EspPositions.IsFinite(new(float.NaN, 0, 0)), "Reject NaN X");
        Check(!EspPositions.IsFinite(new(0, float.NaN, 0)), "Reject NaN Y");
        Check(!EspPositions.IsFinite(new(0, 0, float.NaN)), "Reject NaN Z");
        Check(!EspPositions.IsFinite(new(float.PositiveInfinity, 0, 0)), "Reject infinite X");
        Check(!EspPositions.IsFinite(new(0, float.NegativeInfinity, 0)), "Reject infinite Y");
        Check(!EspPositions.IsFinite(new(0, 0, float.PositiveInfinity)), "Reject infinite Z");
        Character.localCharacter = new Character { Body = new(float.NaN, 0, 0) };
        Check(EspPositions.TryObserver(camera, out observer) && observer.x == -2, "Invalid character position uses the camera");
        camera.transform.position = new(float.NaN, 0, 0);
        Check(!EspPositions.TryObserver(camera, out _), "Invalid observer positions must be skipped");
        Console.WriteLine($"PASS: {_checks} ESP position regression checks.");
    }
}
