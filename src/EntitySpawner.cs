using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakMX
{
    internal enum EntityKind { Beetle, Scorpion, Frog, Scoutmaster, Zombie, Bees, Tornado }
    internal enum EntityPlacement { NearPlayer, Crosshair }
    internal enum EntitySpawnStatus
    {
        Ready, Locked, NoRoom, NoPlayer, Unavailable, SceneNotReady, HostRequired,
        NoTornadoPath, NoGround, Limit, Cooldown, Failed, Spawned, Cleared
    }

    internal sealed class EntityDefinition
    {
        internal readonly EntityKind Kind;
        internal readonly string ResourcePath;
        internal GameObject Prefab;

        internal EntityDefinition(EntityKind kind, string resourcePath)
        {
            Kind = kind;
            ResourcePath = resourcePath;
        }

        internal string Name => Kind switch
        {
            EntityKind.Beetle => Localization.Pick(Localization.Current, "Жук", "Beetle", "甲虫", "甲蟲", "Жук", "甲虫", "딱정벌레", "Escarabajo", "Besouro", "Käfer", "Scarabée", "Scarabeo", "Chrząszcz", "Böcek"),
            EntityKind.Scorpion => Localization.Pick(Localization.Current, "Скорпион", "Scorpion", "蝎子", "蠍子", "Скорпіон", "サソリ", "전갈", "Escorpión", "Escorpião", "Skorpion", "Scorpion", "Scorpione", "Skorpion", "Akrep"),
            EntityKind.Frog => Localization.Pick(Localization.Current, "Лягушка", "Frog", "青蛙", "青蛙", "Жаба", "カエル", "개구리", "Rana", "Sapo", "Frosch", "Grenouille", "Rana", "Żaba", "Kurbağa"),
            EntityKind.Scoutmaster => Localization.Pick(Localization.Current, "Скаутмастер", "Scoutmaster", "童子军领队", "童軍領隊", "Скаутмайстер", "スカウトマスター", "스카우트마스터", "Scoutmaster", "Scoutmaster", "Scoutmaster", "Scoutmaster", "Scoutmaster", "Scoutmaster", "Scoutmaster"),
            EntityKind.Zombie => Localization.Pick(Localization.Current, "Грибной зомби", "Mushroom zombie", "蘑菇僵尸", "蘑菇殭屍", "Грибний зомбі", "キノコゾンビ", "버섯 좀비", "Zombi de hongos", "Zumbi de cogumelos", "Pilzzombie", "Zombie champignon", "Zombie fungo", "Grzybowy zombie", "Mantar zombisi"),
            EntityKind.Bees => Localization.Pick(Localization.Current, "Рой пчёл", "Bee swarm", "蜂群", "蜂群", "Рій бджіл", "ハチの群れ", "벌떼", "Enjambre de abejas", "Enxame de abelhas", "Bienenschwarm", "Essaim d'abeilles", "Sciame di api", "Rój pszczół", "Arı sürüsü"),
            _ => Localization.Pick(Localization.Current, "Торнадо", "Tornado", "龙卷风", "龍捲風", "Торнадо", "竜巻", "토네이도", "Tornado", "Tornado", "Tornado", "Tornade", "Tornado", "Tornado", "Hortum")
        };
    }

    internal readonly struct EntitySpawnResult
    {
        internal readonly EntitySpawnStatus Status;
        internal readonly int Count;
        internal readonly int Requested;

        internal EntitySpawnResult(EntitySpawnStatus status, int count = 0, int requested = 0)
        {
            Status = status;
            Count = count;
            Requested = requested;
        }
    }

    internal static class EntitySpawner
    {
        internal const int MaxBatch = 5;
        internal const int MaxAlive = 30;
        // These are network resource paths, not scene objects or player-character prefabs.
        private static readonly EntityDefinition[] Definitions =
        {
            new(EntityKind.Beetle, "0_Items/Beetle"),
            new(EntityKind.Scorpion, "0_Items/Scorpion"),
            new(EntityKind.Frog, "0_Items/Frog"),
            new(EntityKind.Scoutmaster, "Character_Scoutmaster"),
            new(EntityKind.Zombie, "MushroomZombie"),
            new(EntityKind.Bees, "BeeSwarm"),
            new(EntityKind.Tornado, "Tornado")
        };
        private static readonly List<GameObject> Spawned = new();
        private static Room _room;
        private static bool _loaded;
        private static float _nextSpawnAt;

        internal static IReadOnlyList<EntityDefinition> Entities => Definitions;
        internal static int AliveCount { get { EnsureSession(); return Spawned.Count; } }

        internal static void LoadCatalog()
        {
            foreach (EntityDefinition definition in Definitions)
            {
                GameObject prefab = Resources.Load<GameObject>(definition.ResourcePath);
                definition.Prefab = prefab != null && prefab.GetComponent<PhotonView>() != null && MatchesKind(prefab, definition.Kind)
                    ? prefab : null;
            }
            _loaded = true;
        }

        internal static void EnsureCatalog()
        {
            if (!_loaded) LoadCatalog();
        }

        private static bool MatchesKind(GameObject prefab, EntityKind kind) => kind switch
        {
            EntityKind.Beetle => prefab.GetComponent<Beetle>() != null,
            EntityKind.Scorpion => prefab.GetComponent<Scorpion>() != null,
            EntityKind.Frog => prefab.GetComponent<FrogTongue>() != null,
            EntityKind.Scoutmaster => prefab.GetComponent<Scoutmaster>() != null,
            EntityKind.Zombie => prefab.GetComponent<MushroomZombie>()?.isNPCZombie == true,
            EntityKind.Bees => prefab.GetComponent<BeeSwarm>() != null,
            EntityKind.Tornado => prefab.GetComponent<Tornado>() != null,
            _ => false
        };

        internal static EntitySpawnResult Spawn(int index, Character target, EntityPlacement placement, int amount)
        {
            try { return SpawnCore(index, target, placement, amount); }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[EntitySpawner] Spawn: {e.GetBaseException().Message}");
                return new(EntitySpawnStatus.Failed);
            }
        }

        private static EntitySpawnResult SpawnCore(int index, Character target, EntityPlacement placement, int amount)
        {
            EnsureSession();
            EntitySpawnStatus blocked = CommonBlockedStatus();
            if (blocked != EntitySpawnStatus.Ready) return new(blocked);
            if (Time.realtimeSinceStartup < _nextSpawnAt) return new(EntitySpawnStatus.Cooldown);
            int count = Mathf.Clamp(amount, 1, MaxBatch);
            if (Spawned.Count + count > MaxAlive) return new(EntitySpawnStatus.Limit);
            EnsureCatalog();
            if (index < 0 || index >= Definitions.Length || Definitions[index].Prefab == null)
                return new(EntitySpawnStatus.Unavailable);

            EntityDefinition definition = Definitions[index];
            Character resolved = target != null ? target : Character.localCharacter;
            if (resolved == null || GameUtils.instance == null) return new(EntitySpawnStatus.NoPlayer);
            if (definition.Prefab.GetComponent<Mob>() != null && MobManager.instance == null)
                return new(EntitySpawnStatus.SceneNotReady);
            if (definition.Kind == EntityKind.Frog && !PhotonNetwork.IsMasterClient)
                return new(EntitySpawnStatus.HostRequired);
            if (definition.Kind == EntityKind.Zombie && ZombieManager.Instance == null)
                return new(EntitySpawnStatus.SceneNotReady);

            TornadoSpawner tornadoSpawner = null;
            if (definition.Kind == EntityKind.Tornado)
            {
                tornadoSpawner = FindTornadoPath();
                if (tornadoSpawner == null) return new(EntitySpawnStatus.NoTornadoPath);
            }
            if (!TryFindCenter(resolved, placement, out Vector3 center)) return new(EntitySpawnStatus.NoGround);
            var positions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = count == 1 ? Vector3.zero : new Vector3(Mathf.Cos(i * Mathf.PI * 2f / count), 0f, Mathf.Sin(i * Mathf.PI * 2f / count)) * 2f;
                if (!TryGround(center + offset, out Vector3 ground)) return new(EntitySpawnStatus.NoGround);
                positions[i] = ground + Vector3.up * (definition.Kind == EntityKind.Bees ? 1.8f : definition.Kind == EntityKind.Tornado ? 0f : 0.6f);
            }

            _nextSpawnAt = Time.realtimeSinceStartup + 0.5f;
            int created = 0;
            foreach (Vector3 position in positions)
            {
                GameObject entity = null;
                try
                {
                    entity = PhotonNetwork.IsMasterClient
                        ? PhotonNetwork.InstantiateRoomObject(definition.ResourcePath, position, Quaternion.identity)
                        : PhotonNetwork.Instantiate(definition.ResourcePath, position, Quaternion.identity);
                    if (entity == null) break;
                    PhotonView view = entity.GetComponent<PhotonView>();
                    if (view == null || view.ViewID <= 0) throw new InvalidOperationException("Entity has no network view.");
                    Character character = entity.GetComponent<Character>();
                    if (character != null) character.data.spawnPoint = entity.transform;
                    if (definition.Kind == EntityKind.Scoutmaster)
                    {
                        PhotonView targetView = resolved.GetComponent<PhotonView>();
                        if (targetView != null && targetView.ViewID > 0)
                            view.RPC("RPCA_SetCurrentTarget", RpcTarget.All, targetView.ViewID, 15f);
                    }
                    if (definition.Kind == EntityKind.Bees) entity.GetComponent<BeeSwarm>().HiveDestroyed(position);
                    if (definition.Kind == EntityKind.Tornado)
                        view.RPC("RPCA_InitTornado", RpcTarget.AllBuffered, tornadoSpawner.GetComponent<PhotonView>().ViewID);
                    Spawned.Add(entity);
                    created++;
                }
                catch (Exception e)
                {
                    Plugin.Log?.LogWarning($"[EntitySpawner] {definition.ResourcePath}: {e.GetBaseException().Message}");
                    try { if (entity != null) PhotonNetwork.Destroy(entity); }
                    catch (Exception cleanup) { Plugin.Log?.LogWarning($"[EntitySpawner] Cleanup: {cleanup.GetBaseException().Message}"); }
                    break;
                }
            }
            return new(created > 0 ? EntitySpawnStatus.Spawned : EntitySpawnStatus.Failed, created, count);
        }

        internal static EntitySpawnResult ClearSpawned()
        {
            EnsureSession();
            EntitySpawnStatus blocked = CommonBlockedStatus();
            if (blocked != EntitySpawnStatus.Ready) return new(blocked);
            int removed = 0;
            for (int i = Spawned.Count - 1; i >= 0; i--)
            {
                GameObject entity = Spawned[i];
                PhotonView view = entity != null ? entity.GetComponent<PhotonView>() : null;
                if (view == null || (!view.IsMine && !PhotonNetwork.IsMasterClient)) continue;
                try
                {
                    PhotonNetwork.Destroy(entity);
                    Spawned.RemoveAt(i);
                    removed++;
                }
                catch (Exception e) { Plugin.Log?.LogWarning($"[EntitySpawner] Clear: {e.GetBaseException().Message}"); }
            }
            return new(EntitySpawnStatus.Cleared, removed);
        }

        private static EntitySpawnStatus CommonBlockedStatus()
        {
            if (AntiCheat.ClientToolsLocked) return EntitySpawnStatus.Locked;
            if (!PhotonNetwork.InRoom) return EntitySpawnStatus.NoRoom;
            if (Character.localCharacter == null) return EntitySpawnStatus.NoPlayer;
            return EntitySpawnStatus.Ready;
        }

        private static void EnsureSession()
        {
            Room room = PhotonNetwork.CurrentRoom;
            if (!ReferenceEquals(room, _room))
            {
                _room = room;
                Spawned.Clear();
                _nextSpawnAt = 0f;
            }
            for (int i = Spawned.Count - 1; i >= 0; i--)
                if (Spawned[i] == null) Spawned.RemoveAt(i);
        }

        private static TornadoSpawner FindTornadoPath()
        {
            foreach (TornadoSpawner spawner in UnityEngine.Object.FindObjectsByType<TornadoSpawner>(FindObjectsSortMode.None))
            {
                Transform points = spawner.transform.Find("TornadoPoints");
                PhotonView view = spawner.GetComponent<PhotonView>();
                if (points != null && points.childCount > 0 && view != null && view.ViewID > 0) return spawner;
            }
            return null;
        }

        private static bool TryFindCenter(Character target, EntityPlacement placement, out Vector3 center)
        {
            center = Vector3.zero;
            if (placement == EntityPlacement.Crosshair)
            {
                Camera camera = Camera.main;
                if (camera == null || !Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 80f, HelperFunctions.terrainMapMask, QueryTriggerInteraction.Ignore)) return false;
                center = hit.point;
                return true;
            }
            Vector3 forward = target.data.lookDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = target.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            return TryGround(target.Center + forward.normalized * 6f, out center);
        }

        private static bool TryGround(Vector3 position, out Vector3 ground)
        {
            ground = Vector3.zero;
            if (!Physics.Raycast(position + Vector3.up * 8f, Vector3.down, out RaycastHit hit, 40f, HelperFunctions.terrainMapMask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.5f) return false;
            ground = hit.point;
            return true;
        }
    }
}
