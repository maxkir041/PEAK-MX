using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
using Zorro.Core.Serizalization;

namespace PeakMX
{
    /// <summary>Game-side helpers used by menu actions.</summary>
    public static class GameApi
    {
        public enum CosmeticCategory
        {
            Skin,
            Accessory,
            Eyes,
            Mouth,
            Outfit,
            Hat,
            Sash,
            Medal,
        }

        public struct AdminListEntry
        {
            public string Key;
            public string Label;

            public AdminListEntry(string key, string label)
            {
                Key = key;
                Label = label;
            }
        }

        public static readonly List<Item> Items = new List<Item>();
        public static readonly List<string> ItemNames = new List<string>();
        public static readonly List<Character> PlayerChars = new List<Character>();
        public static readonly List<string> PlayerNames = new List<string>();
        private static readonly HashSet<int> LocalCosmeticUnlocks = new HashSet<int>();
        private static readonly HashSet<string> SessionBans = new HashSet<string>();
        private static readonly Dictionary<string, string> SessionBanNames = new Dictionary<string, string>();
        private static readonly HashSet<string> InventoryLocks = new HashSet<string>();
        private static readonly Dictionary<string, string> InventoryLockNames = new Dictionary<string, string>();
        private static readonly Dictionary<string, Vector3> FrozenPlayers = new Dictionary<string, Vector3>();
        private static readonly List<string> FrozenKeysScratch = new List<string>();
        private static readonly List<string> InventoryLockKeysScratch = new List<string>();
        private static readonly Dictionary<string, AdminMotionSample> AdminMotion = new Dictionary<string, AdminMotionSample>();
        private static readonly Dictionary<string, float> AdminMotionSuppressedUntil = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> AdminStrikes = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> AdminRareSlotState = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> AdminRareInventorySnapshot = new Dictionary<string, string>();
        private static readonly Dictionary<string, List<float>> AdminRareItemEvents = new Dictionary<string, List<float>>();
        private static readonly HashSet<string> AdminRareTouchedScratch = new HashSet<string>();
        private static readonly List<string> AdminRareCurrentScratch = new List<string>();
        private static readonly Dictionary<string, int> AdminRareCountsScratch = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> AdminAmuletFirstOwnerScratch = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Character> AdminAmuletDuplicateTargetScratch = new Dictionary<string, Character>(StringComparer.Ordinal);
        private static readonly HashSet<string> AdminAmuletDuplicateSnapshot = new HashSet<string>(StringComparer.Ordinal);
        private static readonly List<string> AdminRareRemoveScratch = new List<string>();
        public static readonly List<string> AdminProtectionLog = new List<string>();
        public static string AdminNoticeText { get; private set; }
        public static float AdminNoticeUntil { get; private set; }
        private static float _freezeRpcTimer;
        private static float _inventoryLockTimer;
        private static float _knifeFightAt = -1f;
        private static int _knifeFightCountdownSecond = -1;
        private static bool _itemsEverLoaded;
        private const float AdminRareItemWindowSeconds = 10f;
        private const int AdminRareItemBurstThreshold = 3;
        private static readonly string[] AdminRareKeywords =
        {
            "cursed", "curse", "skull", "pandora", "effigy", "faerie", "bugle", "warp compass",
            "amulet", "medallion", "talisman", "charm", "panacea", "cure-all", "cure all",
            "mystic", "mystical", "arcane", "healing gem",
            "прокля", "череп", "амулет", "медальон", "талисман", "панаце", "лекар", "исцел", "мист",
        };
        private static Customization _customizationDb;
        private static readonly MethodInfo CustomGetData = typeof(CharacterCustomization).GetMethod("GetCustomizationData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetData = typeof(CharacterCustomization).GetMethod("SetCustomizationData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetSkin = typeof(CharacterCustomization).GetMethod("SetCharacterSkinColor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetAccessory = typeof(CharacterCustomization).GetMethod("SetCharacterAccessory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetEyes = typeof(CharacterCustomization).GetMethod("SetCharacterEyes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetMouth = typeof(CharacterCustomization).GetMethod("SetCharacterMouth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetOutfit = typeof(CharacterCustomization).GetMethod("SetCharacterOutfit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetHat = typeof(CharacterCustomization).GetMethod("SetCharacterHat", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetSash = typeof(CharacterCustomization).GetMethod("SetCharacterSash", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly MethodInfo CustomSetMedal = typeof(CharacterCustomization).GetMethod("SetCharacterMedal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        private static readonly FieldInfo RunTimeField = typeof(RunManager).GetField("timeSinceRunStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo CharacterViewField = typeof(Character).GetField("view", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RocketExplodeRpcMethod = typeof(CharacterMovement).GetMethod("RocketExplodeRPC", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly object SlotPrefabAccessorLock = new object();
        private static readonly Dictionary<Type, SlotPrefabAccessor> SlotPrefabAccessors = new Dictionary<Type, SlotPrefabAccessor>();
        private static readonly Segment[] WarpSegments =
        {
            Segment.Beach,
            Segment.Tropics,
            Segment.Alpine,
            Segment.Caldera,
            Segment.TheKiln,
            Segment.Peak,
            Segment.Void,
        };
        private static readonly string[] SegmentNamesEn =
            { "Beach", "Tropics", "Alpine", "Caldera", "The Kiln", "Peak", "Void" };
        private static readonly string[] SegmentNamesRu =
            { "Пляж", "Тропики", "Альпы", "Кальдера", "Печь", "Пик", "Пустота" };

        private struct AdminMotionSample
        {
            public Vector3 Position;
            public float Time;
        }

        private struct WorldObjectScanHit
        {
            public string Label;
            public Vector3 Position;
            public float Distance;
        }

        private struct SlotPrefabAccessor
        {
            public MethodInfo Getter;
            public FieldInfo Field;
        }

        private static Item SlotPrefab(ItemSlot slot)
        {
            if (slot == null)
                return null;

            try
            {
                SlotPrefabAccessor accessor = SlotPrefabAccessorFor(slot.GetType());
                if (accessor.Getter != null)
                    return accessor.Getter.Invoke(slot, null) as Item;
                if (accessor.Field != null)
                    return accessor.Field.GetValue(slot) as Item;
            }
            catch (Exception e) when (
                e is MissingFieldException ||
                e is MissingMethodException ||
                e is TargetInvocationException ||
                e is ArgumentException ||
                e is InvalidOperationException)
            {
                Plugin.Log?.LogDebug($"[GameApi] ItemSlot prefab lookup failed: {e.Message}");
            }

            return null;
        }

        private static SlotPrefabAccessor SlotPrefabAccessorFor(Type type)
        {
            if (type == null)
                return default;

            lock (SlotPrefabAccessorLock)
            {
                if (SlotPrefabAccessors.TryGetValue(type, out SlotPrefabAccessor cached))
                    return cached;

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                SlotPrefabAccessor accessor = new SlotPrefabAccessor
                {
                    Getter =
                        type.GetProperty("prefab", flags)?.GetGetMethod(true) ??
                        type.GetMethod("get_prefab", flags),
                    Field =
                        type.GetField("prefab", flags) ??
                        type.GetField("_prefab", flags),
                };
                SlotPrefabAccessors[type] = accessor;
                return accessor;
            }
        }

        // ---------------- items ----------------
        public static void LoadItems()
        {
            try
            {
                Items.Clear();
                ItemNames.Clear();
                var dbObj = Resources.Load("ItemDatabase", typeof(ItemDatabase)) as ItemDatabase;
                if (dbObj != null)
                {
                    try
                    {
                        if (dbObj.itemLookup != null)
                            foreach (var it in dbObj.itemLookup.Values)
                                Add(it);
                    }
                    catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] LoadItems itemLookup: {e.Message}"); }

                    try
                    {
                        var objects = ((ObjectDatabaseAsset<ItemDatabase, Item>)(object)dbObj).Objects;
                        foreach (var it in objects) Add(it as Item);
                    }
                    catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] LoadItems ObjectDatabaseAsset: {e.Message}"); }

                    try
                    {
                        var objects = ((DatabaseAsset<ItemDatabase, Item>)(object)dbObj).Objects;
                        foreach (var it in objects) Add(it as Item);
                    }
                    catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] LoadItems DatabaseAsset: {e.Message}"); }
                }
                try { foreach (var o in Resources.LoadAll("0_Items", typeof(Item))) Add(o as Item); }
                catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] LoadItems 0_Items: {e.Message}"); }
                try { foreach (var o in Resources.FindObjectsOfTypeAll(typeof(Item))) Add(o as Item); }
                catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] LoadItems all objects: {e.Message}"); }
                _itemsEverLoaded = ItemNames.Count > 0;
                Plugin.Log?.LogInfo($"[GameApi] items loaded: {ItemNames.Count}");
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] LoadItems: {e.Message}"); }
        }

        public static bool EnsureItemsLoaded()
        {
            if (_itemsEverLoaded && ItemNames.Count > 0)
                return false;

            LoadItems();
            return ItemNames.Count > 0;
        }

        private static void Add(Item it)
        {
            if (it == null) return;
            try
            {
                if (!IsInventorySpawnableItem(it))
                    return;
                string n = it.GetName();
                if (!string.IsNullOrEmpty(n) && !HasItemAlready(it, n))
                {
                    Items.Add(it);
                    ItemNames.Add(n);
                }
            }
            catch { }
        }

        private static bool HasItemAlready(Item item, string displayName)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item existing = Items[i];
                if (existing == null)
                    continue;
                try
                {
                    if (existing == item)
                        return true;
                    if (existing.itemID == item.itemID && existing.gameObject != null && item.gameObject != null
                        && string.Equals(existing.gameObject.name, item.gameObject.name, StringComparison.Ordinal))
                        return true;
                }
                catch { }
            }

            return ItemNames.Contains(displayName);
        }

        private static bool IsInventorySpawnableItem(Item item)
        {
            try
            {
                if (item == null || item.UIData == null || item.gameObject == null)
                    return false;
                string prefabName = item.gameObject.name ?? "";
                if (prefabName.IndexOf("BaseConstructable", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
                if (!HasResourcePrefab(item))
                    return false;
                if (!item.UIData.canPocket && !item.UIData.canBackpack)
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CanPocketItem(int itemIndex)
        {
            try
            {
                return itemIndex >= 0 && itemIndex < Items.Count
                    && Items[itemIndex] != null
                    && Items[itemIndex].UIData != null
                    && Items[itemIndex].UIData.canPocket
                    && HasResourcePrefab(Items[itemIndex]);
            }
            catch { return false; }
        }

        private static bool CanBackpackItem(int itemIndex)
        {
            try
            {
                return itemIndex >= 0 && itemIndex < Items.Count
                    && Items[itemIndex] != null
                    && Items[itemIndex].UIData != null
                    && Items[itemIndex].UIData.canBackpack
                    && HasResourcePrefab(Items[itemIndex]);
            }
            catch { return false; }
        }

        private const Item.ItemTags ScoutAmuletTag = (Item.ItemTags)0x200;

        private static bool CanBackSlotItem(int itemIndex)
        {
            try
            {
                return itemIndex >= 0 && itemIndex < Items.Count
                    && Items[itemIndex] is Backpack
                    && HasResourcePrefab(Items[itemIndex]);
            }
            catch { return false; }
        }

        private static bool IsScoutAmuletItem(Item item)
        {
            try
            {
                if (item == null) return false;
                if ((item.itemTags & ScoutAmuletTag) != 0)
                    return true;
                string n = ((item.GetName() ?? "") + " " + (item.name ?? "") + " " + (item.gameObject != null ? item.gameObject.name : "")).ToLowerInvariant();
                return n.IndexOf("amulet", StringComparison.Ordinal) >= 0
                    || n.IndexOf("medallion", StringComparison.Ordinal) >= 0
                    || n.IndexOf("talisman", StringComparison.Ordinal) >= 0
                    || n.IndexOf("charm", StringComparison.Ordinal) >= 0
                    || n.IndexOf("амулет", StringComparison.Ordinal) >= 0
                    || n.IndexOf("медальон", StringComparison.Ordinal) >= 0
                    || n.IndexOf("талисман", StringComparison.Ordinal) >= 0;
            }
            catch { return false; }
        }

        private static bool CanTempSlotItem(int itemIndex)
        {
            try
            {
                return itemIndex >= 0 && itemIndex < Items.Count
                    && Items[itemIndex] != null
                    && (CanPocketItem(itemIndex) || IsScoutAmuletItem(Items[itemIndex]))
                    && HasResourcePrefab(Items[itemIndex]);
            }
            catch { return false; }
        }

        private static bool HasResourcePrefab(Item item)
        {
            try
            {
                if (item == null || item.gameObject == null)
                    return false;
                return HasResourcePrefabName(item.gameObject.name);
            }
            catch { return false; }
        }

        private static bool HasResourcePrefabName(string prefabName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prefabName))
                    return false;
                return Resources.Load("0_Items/" + prefabName, typeof(GameObject)) != null;
            }
            catch { return false; }
        }

        private static int RandomItemIndexForSlot(bool backpack)
        {
            EnsureItemsLoaded();
            var candidates = new List<int>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (backpack ? CanBackpackItem(i) : CanPocketItem(i))
                    candidates.Add(i);
            }
            if (candidates.Count == 0)
                return -1;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public static Texture2D ItemIcon(int index)
        {
            try
            {
                var item = index >= 0 && index < Items.Count ? Items[index] : null;
                if (item == null || item.UIData == null) return null;
                Texture2D icon = item.UIData.icon;
                if (icon != null) return icon;
                return TryTextureMember(item.UIData, "altIcon", "colorBlindIcon", "sprite", "smallIcon", "inventoryIcon");
            }
            catch { return null; }
        }

        public static ushort ItemIdAt(int index)
        {
            try
            {
                return index >= 0 && index < Items.Count && Items[index] != null ? Items[index].itemID : ushort.MaxValue;
            }
            catch { return ushort.MaxValue; }
        }

        private static Texture2D TryTextureMember(object source, params string[] names)
        {
            if (source == null || names == null) return null;
            Type type = source.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                try
                {
                    var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        object value = field.GetValue(source);
                        if (value is Texture2D tex) return tex;
                        if (value is Sprite sprite && sprite.texture != null) return sprite.texture;
                    }
                }
                catch { }

                try
                {
                    var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        object value = prop.GetValue(source, null);
                        if (value is Texture2D tex) return tex;
                        if (value is Sprite sprite && sprite.texture != null) return sprite.texture;
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>Spawn item (by index in Items) into the local player's inventory slot.</summary>
        public static bool SpawnToSlot(int itemIndex, int slot)
        {
            try
            {
                var p = Player.localPlayer;
                if (p == null || p.itemSlots == null) return false;
                if (slot < 0 || slot >= p.itemSlots.Length) return false;
                if (itemIndex < 0 || itemIndex >= Items.Count) return false;
                if (!CanPocketItem(itemIndex)) return false;

                ItemSlot s = p.itemSlots[slot];
                SetSlotItem(s, Items[itemIndex]);
                SyncInventory(p);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnToSlot: {e.Message}"); return false; }
        }

        /// <summary>Clear (remove) an item from the local player's inventory slot.</summary>
        public static bool ClearSlot(int slot)
        {
            try
            {
                var p = Player.localPlayer;
                if (p == null || p.itemSlots == null || slot < 0 || slot >= p.itemSlots.Length) return false;
                ClearItemSlot(p.itemSlots[slot]);
                SyncInventory(p);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ClearSlot: {e.Message}"); return false; }
        }

        private static void SyncInventory(Player p) { SyncInventory(p, (RpcTarget)1); }

        private static void SetSlotItem(ItemSlot slot, Item prefab)
        {
            if (slot == null || prefab == null) return;
            ItemInstanceData data = new ItemInstanceData(Guid.NewGuid());
            if (slot is BackpackSlot backpackSlot)
                backpackSlot.backpackType = prefab is Backpack backpack ? backpack.backpackType : BackpackSlot.BackpackType.None;
            slot.SetItem(prefab, data);
            ItemInstanceDataHandler.AddInstanceData(data);
        }

        private static bool BackpackSlotSupportsStorage(BackpackSlot slot)
        {
            try
            {
                if (slot == null || slot.IsEmpty())
                    return false;
                if (!(SlotPrefab(slot) is Backpack backpack))
                    return false;
                if (slot.backpackType == BackpackSlot.BackpackType.Jetpack || slot.backpackType == BackpackSlot.BackpackType.Rocketpack)
                    return false;
                return backpack.slotCount > 0;
            }
            catch { return false; }
        }

        private static void ClearItemSlot(ItemSlot slot)
        {
            if (slot == null) return;
            slot.EmptyOut();
        }

        // target: 0 = All, 1 = Others. For remote players we sync to All so the change
        // lands on the owner too (they don't run our local write).
        private static void SyncInventory(Player p, RpcTarget target)
        {
            byte[] array = IBinarySerializable.ToManagedArray<InventorySyncData>(
                new InventorySyncData(p.itemSlots, p.backpackSlot, p.tempFullSlot));
            ((MonoBehaviourPun)p).photonView.RPC("SyncInventoryRPC", target, new object[] { array, true });
        }

        /// <summary>Resolve the <see cref="Player"/> that owns a given character.</summary>
        private static Player PlayerOf(Character c)
        {
            try { return c != null ? c.player : null; } catch { return null; }
        }

        private static bool IsLocal(Character c)
        {
            try { return c == null || c.IsLocal || c == Character.localCharacter; }
            catch { return c == null || c == Character.localCharacter; }
        }

        public static bool IsHost()
        {
            try { return PhotonNetwork.IsMasterClient; }
            catch { return PhotonNetwork.IsMasterClient; }
        }

        private static bool RequireHostForRemote(Character target, string action)
        {
            return true;
        }

        private static bool RequireHostAction(string action)
        {
            return true;
        }

        private static void LogHostOnly(string action, Character target)
        {
            string name = target != null ? SafeCharacterName(target) : "";
            string message = string.IsNullOrWhiteSpace(name)
                ? $"{action}: host only"
                : $"{action}: host only for {name}";
            AddAdminLog(message);
            Plugin.Log?.LogWarning("[GameApi] " + message);
        }

        private static CharacterCustomization LocalCustomization()
        {
            try { return Character.localCharacter != null ? Character.localCharacter.refs?.customization : null; }
            catch { return null; }
        }

        private static Customization CustomizationDb()
        {
            try
            {
                if (_customizationDb != null && HasAnyCosmeticOptions(_customizationDb))
                    return _customizationDb;

                try
                {
                    var singleton = Singleton<Customization>.Instance;
                    if (singleton != null && HasAnyCosmeticOptions(singleton))
                    {
                        _customizationDb = singleton;
                        return _customizationDb;
                    }
                }
                catch { }

                var all = Resources.FindObjectsOfTypeAll<Customization>();
                _customizationDb = all
                    .Where(HasAnyCosmeticOptions)
                    .OrderByDescending(TotalCosmeticOptionCount)
                    .FirstOrDefault()
                    ?? all.FirstOrDefault();
                return _customizationDb;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasAnyCosmeticOptions(Customization db)
        {
            return TotalCosmeticOptionCount(db) > 0;
        }

        private static int TotalCosmeticOptionCount(Customization db)
        {
            if (db == null) return 0;
            int count = 0;
            try { count += db.skins?.Length ?? 0; } catch { }
            try { count += db.accessories?.Length ?? 0; } catch { }
            try { count += db.eyes?.Length ?? 0; } catch { }
            try { count += db.mouths?.Length ?? 0; } catch { }
            try { count += db.fits?.Length ?? 0; } catch { }
            try { count += db.hats?.Length ?? 0; } catch { }
            try { count += db.sashes?.Length ?? 0; } catch { }
            try { count += db.medals?.Length ?? 0; } catch { }
            return count;
        }

        /// <summary>Spawn item (by index in Items) into another player's inventory slot.</summary>
        public static bool SpawnToSlotFor(Character target, int itemIndex, int slot)
        {
            try
            {
                if (IsLocal(target))
                    return SpawnToSlot(itemIndex, slot);
                if (!RequireHostForRemote(target, "inventory spawn"))
                    return false;

                var p = PlayerOf(target);
                if (p == null || p.itemSlots == null) return false;
                if (slot < 0 || slot >= p.itemSlots.Length) return false;
                if (itemIndex < 0 || itemIndex >= Items.Count) return false;
                if (!CanPocketItem(itemIndex)) return false;

                ItemSlot s = p.itemSlots[slot];
                SetSlotItem(s, Items[itemIndex]);
                SyncInventory(p, (RpcTarget)0);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnToSlotFor: {e.Message}"); return false; }
        }

        public static string SmartSpawnItem(Character target, int itemIndex)
        {
            try
            {
                EnsureItemsLoaded();
                Character resolved = target ?? Character.localCharacter;
                Player player = TargetPlayer(resolved);
                if (player == null || itemIndex < 0 || itemIndex >= Items.Count || Items[itemIndex] == null)
                    return "failed";
                if (!RequireHostForRemote(resolved, "smart inventory spawn"))
                    return "failed";

                if (CanPocketItem(itemIndex) && player.itemSlots != null)
                {
                    for (int i = 0; i < player.itemSlots.Length; i++)
                    {
                        if (player.itemSlots[i] == null || !player.itemSlots[i].IsEmpty())
                            continue;
                        if (!SpawnToSlotFor(resolved, itemIndex, i))
                            continue;
                        return "slot:" + (i + 1);
                    }
                }

                Vector3 position = SafePosition(resolved) + Vector3.up * 0.7f;
                try { position += ((Component)resolved).transform.forward * 1.1f; } catch { }
                if (SpawnWorldItem(itemIndex, position, Quaternion.identity))
                    return "ground";
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] SmartSpawnItem: {RootExceptionMessage(e)}");
            }
            return "failed";
        }

        /// <summary>Clear (remove) an item from another player's inventory slot.</summary>
        public static bool ClearSlotFor(Character target, int slot)
        {
            try
            {
                if (IsLocal(target))
                    return ClearSlot(slot);
                if (!RequireHostForRemote(target, "inventory clear"))
                    return false;

                var p = PlayerOf(target);
                if (p == null || p.itemSlots == null || slot < 0 || slot >= p.itemSlots.Length) return false;
                ClearItemSlot(p.itemSlots[slot]);
                SyncInventory(p, (RpcTarget)0);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ClearSlotFor: {e.Message}"); return false; }
        }

        public static int SlotCount() { return SlotCountFor(Player.localPlayer); }

        public static int SlotCountFor(Character c) { return SlotCountFor(PlayerOf(c)); }

        private static int SlotCountFor(Player p)
        {
            try { return p != null && p.itemSlots != null ? p.itemSlots.Length : 0; }
            catch { return 0; }
        }

        private static Player TargetPlayer(Character c) => PlayerOf(c) ?? Player.localPlayer;

        public static string DescribeSlot(Character c, int slot)
        {
            try
            {
                var p = TargetPlayer(c);
                if (p == null || p.itemSlots == null || slot < 0 || slot >= p.itemSlots.Length)
                    return "";

                var itemSlot = p.itemSlots[slot];
                var prefab = SlotPrefab(itemSlot);
                if (prefab == null)
                    return "-";

                string name = prefab.GetName();
                return string.IsNullOrWhiteSpace(name) ? prefab.name : name;
            }
            catch
            {
                return "";
            }
        }

        public static string DescribeBackSlot(Character c)
        {
            try
            {
                var p = TargetPlayer(c);
                var slot = p != null ? p.backpackSlot : null;
                if (slot == null || slot.IsEmpty())
                    return "-";

                var prefab = SlotPrefab(slot);
                string name = prefab != null ? prefab.GetName() : "";
                if (string.IsNullOrWhiteSpace(name) && prefab != null)
                    name = prefab.name;
                if (string.IsNullOrWhiteSpace(name))
                    name = slot.GetPrefabName();
                return name;
            }
            catch
            {
                return "";
            }
        }

        public static string DescribeTempSlot(Character c)
        {
            try
            {
                var p = TargetPlayer(c);
                var slot = p != null ? p.tempFullSlot : null;
                if (slot == null || slot.IsEmpty())
                    return "-";

                var prefab = SlotPrefab(slot);
                if (prefab == null)
                    return "-";

                string name = prefab.GetName();
                return string.IsNullOrWhiteSpace(name) ? prefab.name : name;
            }
            catch
            {
                return "";
            }
        }

        public static bool SpawnToBackSlotFor(Character target, int itemIndex)
        {
            try
            {
                if (!RequireHostForRemote(target, "back slot spawn"))
                    return false;
                if (!CanBackSlotItem(itemIndex))
                    return false;
                var p = TargetPlayer(target);
                if (p == null || p.backpackSlot == null)
                    return false;

                SetSlotItem(p.backpackSlot, Items[itemIndex]);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnToBackSlotFor: {e.Message}"); return false; }
        }

        public static bool ClearBackSlotFor(Character target)
        {
            try
            {
                if (!RequireHostForRemote(target, "back slot clear"))
                    return false;
                var p = TargetPlayer(target);
                if (p == null || p.backpackSlot == null)
                    return false;

                ClearItemSlot(p.backpackSlot);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ClearBackSlotFor: {e.Message}"); return false; }
        }

        public static void RechargeBackSlotFor(Character target, float value)
        {
            try
            {
                if (!RequireHostForRemote(target, "back slot recharge"))
                    return;
                var p = TargetPlayer(target);
                if (p == null || p.backpackSlot == null || p.backpackSlot.IsEmpty())
                    return;
                RechargeItemSlotData(p.backpackSlot, value);
                SyncTargetInventory(target);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RechargeBackSlotFor: {e.Message}"); }
        }

        public static bool SpawnToTempSlotFor(Character target, int itemIndex)
        {
            try
            {
                if (!RequireHostForRemote(target, "temporary slot spawn"))
                    return false;
                if (!CanTempSlotItem(itemIndex))
                    return false;
                var p = TargetPlayer(target);
                if (p == null || p.tempFullSlot == null)
                    return false;

                SetSlotItem(p.tempFullSlot, Items[itemIndex]);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnToTempSlotFor: {e.Message}"); return false; }
        }

        public static bool ClearTempSlotFor(Character target)
        {
            try
            {
                if (!RequireHostForRemote(target, "temporary slot clear"))
                    return false;
                var p = TargetPlayer(target);
                if (p == null || p.tempFullSlot == null)
                    return false;

                ClearItemSlot(p.tempFullSlot);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ClearTempSlotFor: {e.Message}"); return false; }
        }

        public static void RechargeTempSlotFor(Character target, float value)
        {
            try
            {
                if (!RequireHostForRemote(target, "temporary slot recharge"))
                    return;
                var p = TargetPlayer(target);
                if (p == null || p.tempFullSlot == null || p.tempFullSlot.IsEmpty())
                    return;
                RechargeItemSlotData(p.tempFullSlot, value);
                SyncTargetInventory(target);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RechargeTempSlotFor: {e.Message}"); }
        }

        public static bool HasBackpack(Character c)
        {
            try
            {
                var p = TargetPlayer(c);
                return p != null && p.backpackSlot != null && !p.backpackSlot.IsEmpty() && p.backpackSlot.data != null;
            }
            catch { return false; }
        }

        public static int BackpackSlotCount(Character c)
        {
            var data = BackpackDataFor(c, false);
            return data != null && data.itemSlots != null ? data.itemSlots.Length : 0;
        }

        public static string DescribeBackpackSlot(Character c, int slot)
        {
            try
            {
                var data = BackpackDataFor(c, false);
                if (data == null || data.itemSlots == null || slot < 0 || slot >= data.itemSlots.Length)
                    return "";

                var itemSlot = data.itemSlots[slot];
                var prefab = SlotPrefab(itemSlot);
                if (prefab == null)
                    return "-";

                string name = prefab.GetName();
                return string.IsNullOrWhiteSpace(name) ? prefab.name : name;
            }
            catch { return ""; }
        }

        public static bool SpawnToBackpackSlotFor(Character target, int itemIndex, int slot)
        {
            try
            {
                if (!RequireHostForRemote(target, "backpack spawn"))
                    return false;
                if (itemIndex < 0 || itemIndex >= Items.Count) return false;
                if (!CanBackpackItem(itemIndex)) return false;
                var data = BackpackDataFor(target, true);
                if (data == null || data.itemSlots == null || slot < 0 || slot >= data.itemSlots.Length) return false;

                SetSlotItem(data.itemSlots[slot], Items[itemIndex]);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnToBackpackSlotFor: {e.Message}"); return false; }
        }

        public static bool ClearBackpackSlotFor(Character target, int slot)
        {
            try
            {
                if (!RequireHostForRemote(target, "backpack clear"))
                    return false;
                var data = BackpackDataFor(target, false);
                if (data == null || data.itemSlots == null || slot < 0 || slot >= data.itemSlots.Length) return false;

                ClearItemSlot(data.itemSlots[slot]);
                SyncTargetInventory(target);
                return true;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ClearBackpackSlotFor: {e.Message}"); return false; }
        }

        public static void RechargeBackpackSlotFor(Character target, int slot, float value)
        {
            try
            {
                if (!RequireHostForRemote(target, "backpack recharge"))
                    return;
                var data = BackpackDataFor(target, false);
                if (data == null || data.itemSlots == null || slot < 0 || slot >= data.itemSlots.Length) return;
                RechargeItemSlotData(data.itemSlots[slot], value);
                SyncTargetInventory(target);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RechargeBackpackSlotFor: {e.Message}"); }
        }

        private static BackpackData BackpackDataFor(Character c, bool create)
        {
            try
            {
                var p = TargetPlayer(c);
                if (p == null || p.backpackSlot == null || p.backpackSlot.IsEmpty() || p.backpackSlot.data == null)
                    return null;
                if (!BackpackSlotSupportsStorage(p.backpackSlot))
                    return null;
                if (p.backpackSlot.data.TryGetDataEntry<BackpackData>(DataEntryKey.BackpackData, out var data))
                    return data;
                return create ? p.backpackSlot.data.RegisterNewEntry<BackpackData>(DataEntryKey.BackpackData) : null;
            }
            catch { return null; }
        }

        private static void SyncTargetInventory(Character c)
        {
            var p = TargetPlayer(c);
            if (p == null) return;
            SyncInventory(p, IsLocal(c) ? (RpcTarget)1 : (RpcTarget)0);
        }

        public static CustomizationOption[] GetCosmeticOptions(CosmeticCategory category)
        {
            try
            {
                var db = CustomizationDb();
                if (db == null) return Array.Empty<CustomizationOption>();
                return category switch
                {
                    CosmeticCategory.Skin => db.skins ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Accessory => db.accessories ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Eyes => db.eyes ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Mouth => db.mouths ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Outfit => db.fits ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Hat => db.hats ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Sash => db.sashes ?? Array.Empty<CustomizationOption>(),
                    CosmeticCategory.Medal => db.medals ?? Array.Empty<CustomizationOption>(),
                    _ => Array.Empty<CustomizationOption>(),
                };
            }
            catch
            {
                return Array.Empty<CustomizationOption>();
            }
        }

        public static int GetCurrentCosmeticIndex(CosmeticCategory category)
        {
            try
            {
                var cc = LocalCustomization();
                if (cc == null) return -1;
                CharacterCustomizationData data = GetCustomizationData(cc);
                return category switch
                {
                    CosmeticCategory.Skin => data.currentSkin,
                    CosmeticCategory.Accessory => data.currentAccessory,
                    CosmeticCategory.Eyes => data.currentEyes,
                    CosmeticCategory.Mouth => data.currentMouth,
                    CosmeticCategory.Outfit => data.currentOutfit,
                    CosmeticCategory.Hat => data.currentHat,
                    CosmeticCategory.Sash => data.currentSash,
                    CosmeticCategory.Medal => data.currentMedal,
                    _ => -1,
                };
            }
            catch
            {
                return -1;
            }
        }

        public static int FindBlankCosmeticIndex(CosmeticCategory category)
        {
            var options = GetCosmeticOptions(category);
            for (int i = 0; i < options.Length; i++)
            {
                try
                {
                    if (options[i] != null && options[i].isBlank)
                        return i;
                }
                catch { }
            }
            return -1;
        }

        public static bool IsCosmeticOptionGrantable(CosmeticCategory category, int index)
        {
            var options = GetCosmeticOptions(category);
            return index >= 0 && index < options.Length && IsCosmeticOptionGrantable(category, options[index]);
        }

        public static bool IsCosmeticOptionSelectable(CosmeticCategory category, int index)
        {
            var options = GetCosmeticOptions(category);
            return index >= 0 && index < options.Length && IsCosmeticOptionSelectable(category, options[index]);
        }

        public static bool IsCosmeticOptionSelectable(CosmeticCategory category, CustomizationOption option)
        {
            return option != null;
        }

        public static bool IsCosmeticOptionGrantable(CosmeticCategory category, CustomizationOption option)
        {
            if (option == null) return false;
            try
            {
                if (option.isBlank) return true;
                if (option.type != ExpectedCosmeticType(category)) return false;
                if (!HasKnownCosmeticRequirement(option)) return false;
                if (!HasCosmeticUnlockRequirement(option)) return false;
                return HasCosmeticVisual(category, option);
            }
            catch
            {
                return false;
            }
        }

        private static Customization.Type ExpectedCosmeticType(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Skin => Customization.Type.Skin,
                CosmeticCategory.Accessory => Customization.Type.Accessory,
                CosmeticCategory.Eyes => Customization.Type.Eyes,
                CosmeticCategory.Mouth => Customization.Type.Mouth,
                CosmeticCategory.Outfit => Customization.Type.Fit,
                CosmeticCategory.Hat => Customization.Type.Hat,
                CosmeticCategory.Sash => Customization.Type.Sash,
                CosmeticCategory.Medal => Customization.Type.Medal,
                _ => Customization.Type.Skin,
            };
        }

        private static bool HasCosmeticVisual(CosmeticCategory category, CustomizationOption option)
        {
            if (option == null) return false;
            if (option.texture != null) return true;
            if (option.fitMesh != null || option.fitMaterial != null || option.fitMaterialShoes != null)
                return true;
            if (option.fitMaterialOverridePants != null || option.fitMaterialOverrideHat != null)
                return true;

            if (category == CosmeticCategory.Skin)
            {
                Color c = option.color;
                return c.a > 0.01f || c.r > 0.01f || c.g > 0.01f || c.b > 0.01f;
            }

            return false;
        }

        private static bool HasKnownCosmeticRequirement(CustomizationOption option)
        {
            string req = option.customRequirement.ToString();
            return string.Equals(req, "None", StringComparison.OrdinalIgnoreCase)
                || string.Equals(req, "Goat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(req, "Crown", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasCosmeticUnlockRequirement(CustomizationOption option)
        {
            if (option == null || option.isBlank) return false;
            if (option.requiresAscent) return true;
            if (option.requiresSteamStat) return true;
            if (option.requiredAchievement != ACHIEVEMENTTYPE.NONE) return true;
            string req = option.customRequirement.ToString();
            return string.Equals(req, "Goat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(req, "Crown", StringComparison.OrdinalIgnoreCase);
        }

        public static bool SetCosmetic(CosmeticCategory category, int index)
        {
            try
            {
                var cc = LocalCustomization();
                var options = GetCosmeticOptions(category);
                if (cc == null || index < 0 || index >= options.Length)
                    return false;
                if (!IsCosmeticOptionSelectable(category, options[index]))
                    return false;

                CharacterCustomizationData data = GetCustomizationData(cc);
                switch (category)
                {
                    case CosmeticCategory.Skin: data.currentSkin = index; InvokeCustomizationSetter(cc, CustomSetSkin, index); break;
                    case CosmeticCategory.Accessory: data.currentAccessory = index; InvokeCustomizationSetter(cc, CustomSetAccessory, index); break;
                    case CosmeticCategory.Eyes: data.currentEyes = index; InvokeCustomizationSetter(cc, CustomSetEyes, index); break;
                    case CosmeticCategory.Mouth: data.currentMouth = index; InvokeCustomizationSetter(cc, CustomSetMouth, index); break;
                    case CosmeticCategory.Outfit: data.currentOutfit = index; InvokeCustomizationSetter(cc, CustomSetOutfit, index); break;
                    case CosmeticCategory.Hat: data.currentHat = index; InvokeCustomizationSetter(cc, CustomSetHat, index); break;
                    case CosmeticCategory.Sash: data.currentSash = index; InvokeCustomizationSetter(cc, CustomSetSash, index); break;
                    case CosmeticCategory.Medal: data.currentMedal = index; InvokeCustomizationSetter(cc, CustomSetMedal, index); break;
                    default: return false;
                }

                data.CorrectValues();
                SetCustomizationData(cc, data);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] SetCosmetic: {e.Message}");
                return false;
            }
        }

        public static bool RandomizeOutfitAndColor(bool track = true)
        {
            bool changed = false;
            try
            {
                var categories = (CosmeticCategory[])Enum.GetValues(typeof(CosmeticCategory));
                for (int i = 0; i < categories.Length; i++)
                {
                    int index = RandomSelectableIndex(categories[i]);
                    if (index >= 0)
                        changed |= SetCosmetic(categories[i], index);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RandomizeOutfitAndColor: {e.Message}"); }
            return changed;
        }

        private static int RandomSelectableIndex(CosmeticCategory category)
        {
            var options = GetCosmeticOptions(category);
            var candidates = new List<int>();
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (IsCosmeticOptionSelectable(category, option))
                    candidates.Add(i);
            }
            if (candidates.Count == 0)
                return -1;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static int RandomGrantableIndex(CosmeticCategory category)
        {
            var options = GetCosmeticOptions(category);
            var candidates = new List<int>();
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null || option.isBlank) continue;
                if (IsCosmeticOptionGrantable(category, option))
                    candidates.Add(i);
            }

            return candidates.Count == 0 ? -1 : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public static bool CloneLocalAppearanceFrom(Character source, bool includeNickname)
        {
            try
            {
                Character me = Character.localCharacter;
                if (source == null || me == null) return false;

                var sourceCustomization = source.refs != null ? source.refs.customization : null;
                var localCustomization = LocalCustomization();
                if (sourceCustomization == null || localCustomization == null) return false;

                var owner = OwnerOf(source) ?? PhotonNetwork.LocalPlayer;
                CharacterCustomizationData data = GetCustomizationDataFor(sourceCustomization, owner);
                ApplyCustomizationData(localCustomization, data);

                if (includeNickname)
                    SetLocalNickname(SafeCharacterName(source), false);

                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] CloneLocalAppearanceFrom: {e.Message}");
                return false;
            }
        }

        private static void ApplyCustomizationData(CharacterCustomization cc, CharacterCustomizationData data)
        {
            data.CorrectValues();
            SetCustomizationData(cc, data);
            InvokeCustomizationSetter(cc, CustomSetSkin, data.currentSkin);
            InvokeCustomizationSetter(cc, CustomSetAccessory, data.currentAccessory);
            InvokeCustomizationSetter(cc, CustomSetEyes, data.currentEyes);
            InvokeCustomizationSetter(cc, CustomSetMouth, data.currentMouth);
            InvokeCustomizationSetter(cc, CustomSetOutfit, data.currentOutfit);
            InvokeCustomizationSetter(cc, CustomSetHat, data.currentHat);
            InvokeCustomizationSetter(cc, CustomSetSash, data.currentSash);
            InvokeCustomizationSetter(cc, CustomSetMedal, data.currentMedal);
        }

        public static string LocalNickname()
        {
            try
            {
                string nick = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.NickName : PhotonNetwork.NickName;
                if (!string.IsNullOrWhiteSpace(nick)) return nick;
                Character me = Character.localCharacter;
                if (me != null && !string.IsNullOrWhiteSpace(me.characterName)) return me.characterName;
            }
            catch { }
            return "";
        }

        public static bool SetLocalNickname(string nickname, bool track = true)
        {
            try
            {
                string clean = CleanNickname(nickname);
                if (string.IsNullOrWhiteSpace(clean)) return false;

                PhotonNetwork.NickName = clean;
                if (PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.NickName = clean;

                RefreshPlayers();
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] SetLocalNickname: {e.Message}");
                return false;
            }
        }

        private static string CleanNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return "";
            string clean = nickname.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
            while (clean.Contains("  "))
                clean = clean.Replace("  ", " ");
            return clean.Length > 32 ? clean.Substring(0, 32) : clean;
        }

        public static bool UnlockCosmeticOption(CustomizationOption option)
        {
            if (option == null) return false;
            LocalCosmeticUnlocks.Add(option.GetInstanceID());

            try
            {
                if (option.requiresAscent)
                {
                    SteamAch.SetMaxAscent(Mathf.Max(0, option.requiredAscent));
                }
            }
            catch { }

            try
            {
                if (option.requiresSteamStat)
                {
                    SteamAch.SetStatValue(option.requiredSteamStat, Mathf.Max(1, option.requiredSteamStatValue));
                }
            }
            catch { }

            try
            {
                if (option.requiredAchievement != ACHIEVEMENTTYPE.NONE)
                {
                    SteamAch.Unlock(option.requiredAchievement);
                }
            }
            catch { }

            try
            {
                string custom = option.customRequirement.ToString();
                if (string.Equals(custom, "Goat", StringComparison.OrdinalIgnoreCase))
                {
                    SteamAch.SetMaxAscent(8);
                }
                else if (string.Equals(custom, "Crown", StringComparison.OrdinalIgnoreCase))
                {
                    SteamAch.UnlockAll();
                }
            }
            catch { }

            try { option.testLocked = false; } catch { }

            return true;
        }

        public static bool LockCosmeticOption(CustomizationOption option)
        {
            if (option == null) return false;

            LocalCosmeticUnlocks.Remove(option.GetInstanceID());

            try
            {
                if (option.requiredAchievement != ACHIEVEMENTTYPE.NONE)
                {
                    SteamAch.Revoke(option.requiredAchievement);
                }
            }
            catch { }

            try
            {
                if (option.requiresSteamStat)
                {
                    SteamAch.SetStatValue(option.requiredSteamStat, 0);
                }
            }
            catch { }

            try
            {
                if (option.requiresAscent)
                {
                    SteamAch.SetMaxAscent(Mathf.Max(0, option.requiredAscent - 1));
                }
            }
            catch { }

            try
            {
                string custom = option.customRequirement.ToString();
                if (string.Equals(custom, "Goat", StringComparison.OrdinalIgnoreCase))
                {
                    SteamAch.SetMaxAscent(0);
                }
            }
            catch { }

            try { option.testLocked = true; } catch { }

            return true;
        }

        public static void UnlockAllCosmeticOptions()
        {
            int count = 0;
            try
            {
                foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
                {
                    var options = GetCosmeticOptions(category);
                    for (int i = 0; i < options.Length; i++)
                    {
                        var option = options[i];
                        if (!IsCosmeticOptionGrantable(category, option)) continue;
                        LocalCosmeticUnlocks.Add(option.GetInstanceID());
                        count++;
                    }
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] UnlockAllCosmeticOptions local: {e.Message}"); }

            try { SteamAch.UnlockAllCosmetics(); } catch { }
        }

        public static bool IsCosmeticLocallyUnlocked(CustomizationOption option)
        {
            try { return option != null && LocalCosmeticUnlocks.Contains(option.GetInstanceID()); }
            catch { return false; }
        }

        private static string SafeCosmeticName(CustomizationOption option)
        {
            try { return option != null && !string.IsNullOrWhiteSpace(option.name) ? option.name : "Unknown"; }
            catch { return "Unknown"; }
        }

        private static CharacterCustomizationData GetCustomizationData(CharacterCustomization cc)
        {
            return GetCustomizationDataFor(cc, PhotonNetwork.LocalPlayer);
        }

        private static CharacterCustomizationData GetCustomizationDataFor(CharacterCustomization cc, Photon.Realtime.Player player)
        {
            if (CustomGetData == null) return default;
            if (player == null) return default;
            object target = CustomGetData.IsStatic ? null : cc;
            object result = CustomGetData.Invoke(target, new object[] { player });
            return result is CharacterCustomizationData data ? data : default;
        }

        private static void SetCustomizationData(CharacterCustomization cc, CharacterCustomizationData data)
        {
            SetCustomizationDataFor(cc, data, PhotonNetwork.LocalPlayer);
        }

        private static void SetCustomizationDataFor(CharacterCustomization cc, CharacterCustomizationData data, Photon.Realtime.Player player)
        {
            if (CustomSetData == null) return;
            if (player == null) return;
            object target = CustomSetData.IsStatic ? null : cc;
            CustomSetData.Invoke(target, new object[] { data, player });
        }

        private static void InvokeCustomizationSetter(CharacterCustomization cc, MethodInfo method, int index)
        {
            if (method == null) return;
            object target = method.IsStatic ? null : cc;
            method.Invoke(target, new object[] { index });
        }

        // ---------------- players ----------------
        public static void RefreshPlayers()
        {
            try
            {
                PlayerChars.Clear();
                PlayerNames.Clear();

                AddPlayerCharacter(Character.localCharacter);
                try { AddPlayerCharacter(Player.localPlayer?.character); } catch { }

                var all = Character.AllCharacters;
                if (all != null)
                    foreach (var c in all)
                        AddPlayerCharacter(c);

                foreach (var c in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
                    AddPlayerCharacter(c);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RefreshPlayers: {e.Message}"); }
        }

        private static void AddPlayerCharacter(Character c)
        {
            if (c == null) return;
            try
            {
                bool local = IsLocal(c);
                if (!local)
                {
                    try { if (c.isBot || c.isZombie || c.isScoutmaster) return; } catch { }
                    PhotonView view = ViewOf(c);
                    if (view == null)
                        return;
                }

                for (int i = 0; i < PlayerChars.Count; i++)
                    if (PlayerChars[i] == c)
                        return;

                PlayerChars.Add(c);
                PlayerNames.Add(SafeCharacterName(c));
            }
            catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] AddPlayerCharacter: {e.Message}"); }
        }

        public static void ReviveSelf() { if (Character.localCharacter != null) RevivePlayer(Character.localCharacter); }
        public static void KillSelf() { if (Character.localCharacter != null) KillPlayer(Character.localCharacter); }

        public static void WarpToSpawn()
        {
            try
            {
                var me = Character.localCharacter;
                var sp = me != null ? me.data.spawnPoint : null;
                if (sp == null) return;
                ((MonoBehaviourPun)me).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { sp.position + new Vector3(0f, 2f, 0f), true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpToSpawn: {e.Message}"); }
        }

        public static void WarpPlayerToSpawn(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "warp player to spawn")) return;
                var sp = c.data != null ? c.data.spawnPoint : null;
                if (sp == null) return;
                SuppressAdminMovementCheck(c);
                ((MonoBehaviourPun)c).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { sp.position + new Vector3(0f, 2f, 0f), true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpPlayerToSpawn: {e.Message}"); }
        }

        /// <summary>Teleport the local character to explicit world coordinates.</summary>
        public static void TeleportToCoords(float x, float y, float z)
        {
            try
            {
                var me = Character.localCharacter;
                if (me == null || me.data.dead) return;
                ((MonoBehaviourPun)me).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { new Vector3(x, y, z), true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] TeleportToCoords: {e.Message}"); }
        }

        public static void KillPlayer(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "kill player")) return;
                var view = ViewOf(c);
                if (view == null) return;

                try
                {
                    view.RPC("RPCA_Die", (RpcTarget)0, new object[0]);
                }
                catch (Exception rpcError)
                {
                    Plugin.Log?.LogWarning($"[GameApi] KillPlayer RPCA_Die failed, falling back to RPCA_SetDead: {RootExceptionMessage(rpcError)}");
                    view.RPC("RPCA_SetDead", (RpcTarget)0, new object[0]);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] KillPlayer: {RootExceptionMessage(e)}"); }
        }

        public static void RevivePlayer(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "revive player")) return;
                Vector3 pos = (c.Ghost != null ? ((Component)c.Ghost).transform.position : c.Head) + new Vector3(0f, 4f, 0f);
                ((MonoBehaviourPun)c).photonView.RPC("RPCA_ReviveAtPosition", (RpcTarget)0, new object[] { pos, false, -1 });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RevivePlayer: {e.Message}"); }
        }

        public static void RevivePlayerHere(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "revive player here")) return;
                Vector3 pos = Character.localCharacter != null ? Character.localCharacter.Head + Vector3.up * 2f : SafePosition(c) + Vector3.up * 3f;
                ((MonoBehaviourPun)c).photonView.RPC("RPCA_ReviveAtPosition", (RpcTarget)0, new object[] { pos, false, -1 });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RevivePlayerHere: {e.Message}"); }
        }

        public static void ReviveAllDeadOnly()
        {
            try
            {
                if (!RequireHostAction("revive dead players")) return;
                RefreshPlayers();
                int affected = 0;
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    Character target = PlayerChars[i];
                    if (target == null || !IsDead(target))
                        continue;
                    RevivePlayer(target);
                    affected++;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ReviveAllDeadOnly: {e.Message}"); }
        }

        public static void AntiStuck(Character c = null)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;
                if (!RequireHostForRemote(target, "anti-stuck")) return;

                HaltCharacter(target);
                try
                {
                    if (target.data != null)
                    {
                        target.data.fallSeconds = 0f;
                        target.data.sinceGrounded = 0f;
                        target.data.sinceJump = 0f;
                    }
                }
                catch { }

                Vector3 pos = SafePosition(target) + Vector3.up * 2.75f;
                SuppressAdminMovementCheck(target);
                try { ((MonoBehaviourPun)target).photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { pos, true }); }
                catch
                {
                    try { target.refs?.ragdoll?.MoveAllRigsInDirection(Vector3.up * 2.75f); } catch { }
                }

                ClearArrows(target, false);
                RemovePhysicalThorns(target, false, false);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] AntiStuck: {e.Message}"); }
        }

        public static void KickPlayer(Character c)
        {
            try
            {
                if (c == null || IsLocal(c)) return;
                var p = PlayerOf(c);
                var view = p != null ? ((MonoBehaviourPun)p).photonView : null;
                var owner = view != null ? view.Owner : null;
                if (view == null || owner == null) return;
                view.RPC("RPC_GetKicked", owner, new object[0]);
                try { PhotonNetwork.CloseConnection(owner); } catch { }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] KickPlayer: {e.Message}"); }
        }

        public static void KickPhotonPlayer(Photon.Realtime.Player player, string reason)
        {
            try
            {
                if (player == null || player.IsLocal)
                    return;

                bool sentGameKick = false;
                RefreshPlayers();
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    Character c = PlayerChars[i];
                    if (c == null) continue;
                    var owner = OwnerOf(c);
                    if (owner == null || owner.ActorNumber != player.ActorNumber)
                        continue;

                    KickPlayer(c);
                    sentGameKick = true;
                    break;
                }

                if (!sentGameKick)
                {
                    try { PhotonNetwork.CloseConnection(player); } catch { }
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] KickPhotonPlayer: {e.Message}"); }
        }

        public static void BanPlayer(Character c)
        {
            try
            {
                if (c == null || IsLocal(c)) return;
                string id = PlayerBanId(c);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    SessionBans.Add(id);
                    SessionBanNames[id] = SafeCharacterName(c);
                    AddAdminLog($"{SafeCharacterName(c)}: session ban");
                }
                KickPlayer(c);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] BanPlayer: {e.Message}"); }
        }

        public static bool IsSessionBanned(Character c)
        {
            string id = PlayerBanId(c);
            return !string.IsNullOrWhiteSpace(id) && SessionBans.Contains(id);
        }

        public static void ToggleSessionBan(Character c)
        {
            try
            {
                if (c == null || IsLocal(c)) return;
                if (!RequireHostForRemote(c, "session ban")) return;
                string id = PlayerBanId(c);
                if (string.IsNullOrWhiteSpace(id)) return;

                if (SessionBans.Contains(id))
                {
                    UnbanSession(id);
                    return;
                }

                SessionBans.Add(id);
                SessionBanNames[id] = SafeCharacterName(c);
                AddAdminLog($"{SafeCharacterName(c)}: session ban");
                KickPlayer(c);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ToggleSessionBan: {e.Message}"); }
        }

        public static void UnbanSession(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            string label = SessionBanNames.TryGetValue(key, out string name) ? name : key;
            SessionBans.Remove(key);
            SessionBanNames.Remove(key);
            AddAdminLog($"{label}: session unban");
        }

        public static List<AdminListEntry> SessionBanEntries()
        {
            var list = new List<AdminListEntry>();
            foreach (string key in SessionBans)
            {
                string label = SessionBanNames.TryGetValue(key, out string name) && !string.IsNullOrWhiteSpace(name) ? name : key;
                list.Add(new AdminListEntry(key, label));
            }
            return list;
        }

        public static void ToggleMute(Character c)
        {
            try
            {
                if (!RequireHostForRemote(c, "mute player")) return;
                var owner = OwnerOf(c);
                if (owner == null || owner == PhotonNetwork.LocalPlayer) return;
                SetMuted(owner, !IsMuted(c));
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ToggleMute: {e.Message}"); }
        }

        public static bool IsMuted(Character c)
        {
            try
            {
                var owner = OwnerOf(c);
                if (owner == null) return false;
                object props = owner.GetType().GetProperty("CustomProperties")?.GetValue(owner, null);
                if (props is System.Collections.IDictionary dict && dict.Contains("mu"))
                    return dict["mu"] is bool muted && muted;
                return false;
            }
            catch { return false; }
        }

        private static void SetMuted(Photon.Realtime.Player player, bool muted)
        {
            if (player == null) return;
            Type tableType = Type.GetType("ExitGames.Client.Photon.Hashtable, Photon3Unity3D");
            if (tableType == null) return;
            object table = Activator.CreateInstance(tableType);
            if (table is System.Collections.IDictionary dict)
                dict["mu"] = muted;
            player.GetType().GetMethod("SetCustomProperties", new[] { tableType })?.Invoke(player, new[] { table });
        }

        public static bool IsFrozen(Character c)
        {
            string key = FreezeKey(c);
            return !string.IsNullOrWhiteSpace(key) && FrozenPlayers.ContainsKey(key);
        }

        public static void ToggleFreeze(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "freeze player")) return;
                string key = FreezeKey(c);
                if (string.IsNullOrWhiteSpace(key)) return;

                if (FrozenPlayers.ContainsKey(key))
                {
                    FrozenPlayers.Remove(key);
                }
                else
                {
                    FrozenPlayers[key] = SafePosition(c);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ToggleFreeze: {e.Message}"); }
        }

        public static void ApplyFrozenPlayers(float deltaTime)
        {
            try
            {
                if (FrozenPlayers.Count == 0) return;
                _freezeRpcTimer += Mathf.Max(0f, deltaTime);
                bool sendRpc = _freezeRpcTimer >= 0.2f;
                if (sendRpc) _freezeRpcTimer = 0f;

                FrozenKeysScratch.Clear();
                foreach (var pair in FrozenPlayers)
                    FrozenKeysScratch.Add(pair.Key);

                for (int i = 0; i < FrozenKeysScratch.Count; i++)
                {
                    string key = FrozenKeysScratch[i];
                    if (!FrozenPlayers.TryGetValue(key, out Vector3 position))
                        continue;

                    Character c = FindFrozenCharacter(key);
                    if (c == null) continue;
                    FreezeCharacter(c, position, sendRpc);
                }

                FrozenKeysScratch.Clear();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ApplyFrozenPlayers: {e.Message}"); }
        }

        public static bool IsInventoryLocked(Character c)
        {
            string key = AdminCharacterKey(c);
            return !string.IsNullOrWhiteSpace(key) && InventoryLocks.Contains(key);
        }

        public static void ToggleInventoryLock(Character c)
        {
            try
            {
                if (c == null) return;
                if (!RequireHostForRemote(c, "inventory lock")) return;
                string key = AdminCharacterKey(c);
                if (string.IsNullOrWhiteSpace(key)) return;
                string name = SafeCharacterName(c);
                if (InventoryLocks.Contains(key))
                {
                    InventoryLocks.Remove(key);
                    InventoryLockNames.Remove(key);
                    AddAdminLog($"{name}: inventory unlock");
                }
                else
                {
                    InventoryLocks.Add(key);
                    InventoryLockNames[key] = name;
                    AddAdminLog($"{name}: inventory lock");
                    DropAllInventory(c, false);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ToggleInventoryLock: {e.Message}"); }
        }

        public static void UnlockInventoryLock(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            string label = InventoryLockNames.TryGetValue(key, out string name) ? name : key;
            InventoryLocks.Remove(key);
            InventoryLockNames.Remove(key);
            AddAdminLog($"{label}: inventory unlock");
        }

        public static List<AdminListEntry> InventoryLockEntries()
        {
            var list = new List<AdminListEntry>();
            foreach (string key in InventoryLocks)
            {
                string label = InventoryLockNames.TryGetValue(key, out string name) && !string.IsNullOrWhiteSpace(name) ? name : key;
                list.Add(new AdminListEntry(key, label));
            }
            return list;
        }

        public static void ApplyInventoryLocks(float deltaTime)
        {
            try
            {
                if (InventoryLocks.Count == 0) return;
                _inventoryLockTimer += Mathf.Max(0f, deltaTime);
                if (_inventoryLockTimer < 0.45f) return;
                _inventoryLockTimer = 0f;

                InventoryLockKeysScratch.Clear();
                foreach (string key in InventoryLocks)
                    InventoryLockKeysScratch.Add(key);

                for (int i = 0; i < InventoryLockKeysScratch.Count; i++)
                {
                    string key = InventoryLockKeysScratch[i];
                    if (!InventoryLocks.Contains(key))
                        continue;

                    Character c = FindFrozenCharacter(key);
                    if (c == null) continue;
                    DropAllInventory(c, false);
                }

                InventoryLockKeysScratch.Clear();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ApplyInventoryLocks: {e.Message}"); }
        }

        private static string FreezeKey(Character c)
        {
            return AdminCharacterKey(c);
        }

        private static string AdminCharacterKey(Character c)
        {
            if (c == null) return "";
            if (IsLocal(c)) return "local";
            return PlayerBanId(c);
        }

        private static Character FindFrozenCharacter(string key)
        {
            if (string.Equals(key, "local", StringComparison.Ordinal))
                return Character.localCharacter;

            var all = Character.AllCharacters;
            if (all == null) return null;
            for (int i = 0; i < all.Count; i++)
            {
                Character c = all[i];
                if (c != null && string.Equals(PlayerBanId(c), key, StringComparison.Ordinal))
                    return c;
            }
            return null;
        }

        private static Vector3 SafePosition(Character c)
        {
            try { return c.Center; }
            catch
            {
                try { return ((Component)c).transform.position; }
                catch { return Vector3.zero; }
            }
        }

        private static void FreezeCharacter(Character c, Vector3 position, bool sendRpc)
        {
            if (c == null) return;
            HaltCharacter(c);

            try
            {
                float dist = Vector3.Distance(c.Center, position);
                if (dist > 0.08f && c.refs?.ragdoll != null)
                    c.refs.ragdoll.MoveAllRigsInDirection(position - c.Center);
            }
            catch { }

            if (!IsLocal(c) && IsHost() && sendRpc)
            {
                try { ((MonoBehaviourPun)c).photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { position, false }); }
                catch { }
            }
        }

        private static void HaltCharacter(Character c)
        {
            try { c.refs?.ragdoll?.HaltBodyVelocity(); } catch { }
            try
            {
                var parts = c.refs?.ragdoll?.partList;
                if (parts == null) return;
                for (int i = 0; i < parts.Count; i++)
                {
                    Rigidbody rig = parts[i] != null ? parts[i].Rig : null;
                    if (rig == null || rig.isKinematic) continue;
                    rig.linearVelocity = Vector3.zero;
                    rig.angularVelocity = Vector3.zero;
                }
            }
            catch { }
        }

        public static void EnforceSessionBans()
        {
            try
            {
                if (!IsHost() || SessionBans.Count == 0) return;
                RefreshPlayers();
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    var c = PlayerChars[i];
                    if (c == null || IsLocal(c)) continue;
                    string id = PlayerBanId(c);
                    if (!string.IsNullOrWhiteSpace(id) && SessionBans.Contains(id))
                        KickPlayer(c);
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] EnforceSessionBans: {e.Message}"); }
        }

        public static void EnforceAdminProtection()
        {
            try
            {
                if (!ModConfig.AdminProtectionEnabled || !IsHost())
                    return;

                if (ModConfig.AdminAutoKickSessionBans)
                    EnforceSessionBans();

                if (ModConfig.AdminDetectExtremeMovement)
                    CheckExtremeMovement();

                if (ModConfig.AdminDetectRareItemBursts)
                    CheckRareItemBursts();
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] EnforceAdminProtection: {e.Message}"); }
        }

        private static void CheckExtremeMovement()
        {
            RefreshPlayers();
            float now = Time.realtimeSinceStartup;
            float maxSpeed = Mathf.Max(5f, ModConfig.AdminMaxSpeed);
            int maxStrikes = Mathf.Clamp(ModConfig.AdminSpeedStrikes, 1, 10);

            for (int i = 0; i < PlayerChars.Count; i++)
            {
                Character c = PlayerChars[i];
                if (c == null || IsLocal(c) || IsFrozen(c)) continue;
                try { if (c.data != null && c.data.dead) continue; } catch { }

                string key = PlayerBanId(c);
                if (string.IsNullOrWhiteSpace(key)) continue;

                Vector3 pos = SafePosition(c);
                if (AdminMotionSuppressedUntil.TryGetValue(key, out float suppressedUntil))
                {
                    if (now < suppressedUntil)
                    {
                        AdminMotion[key] = new AdminMotionSample { Position = pos, Time = now };
                        continue;
                    }
                    AdminMotionSuppressedUntil.Remove(key);
                }
                if (AdminMotion.TryGetValue(key, out var sample))
                {
                    float dt = Mathf.Max(0.001f, now - sample.Time);
                    if (dt > 5f)
                    {
                        AdminMotion[key] = new AdminMotionSample { Position = pos, Time = now };
                        continue;
                    }

                    Vector3 delta = pos - sample.Position;
                    if (ModConfig.AdminIgnoreDownwardMovement && delta.y < 0f)
                        delta.y = 0f;

                    float speed = delta.magnitude / dt;
                    if (speed > maxSpeed)
                    {
                        RegisterAdminStrike(c, key + ":speed", $"{SafeCharacterName(c)}: speed {speed:0.0} m/s", maxStrikes);
                    }
                    else if (AdminStrikes.TryGetValue(key + ":speed", out int strikes) && strikes > 0)
                    {
                        AdminStrikes[key + ":speed"] = strikes - 1;
                    }
                }

                AdminMotion[key] = new AdminMotionSample { Position = pos, Time = now };
            }
        }

        private static void CheckRareItemBursts()
        {
            RefreshPlayers();
            float now = Time.realtimeSinceStartup;
            int maxStrikes = Mathf.Clamp(ModConfig.AdminSpeedStrikes, 1, 10);
            AdminAmuletFirstOwnerScratch.Clear();
            AdminAmuletDuplicateTargetScratch.Clear();

            for (int i = 0; i < PlayerChars.Count; i++)
            {
                Character c = PlayerChars[i];
                if (c == null || IsLocal(c)) continue;
                try { if (c.data != null && c.data.dead) continue; } catch { }

                string playerKey = PlayerBanId(c);
                if (string.IsNullOrWhiteSpace(playerKey)) continue;

                Player player = PlayerOf(c);
                if (player == null) continue;

                AdminRareTouchedScratch.Clear();
                AdminRareCurrentScratch.Clear();
                try
                {
                    if (player.itemSlots != null)
                    {
                        for (int slot = 0; slot < player.itemSlots.Length; slot++)
                            CheckRareItemSlot(c, playerKey, "main:" + slot, player.itemSlots[slot], now, AdminRareTouchedScratch, AdminRareCurrentScratch, maxStrikes);
                    }
                }
                catch { }

                CheckRareItemSlot(c, playerKey, "back", player.backpackSlot, now, AdminRareTouchedScratch, AdminRareCurrentScratch, maxStrikes);
                CheckRareItemSlot(c, playerKey, "temp", player.tempFullSlot, now, AdminRareTouchedScratch, AdminRareCurrentScratch, maxStrikes);

                try
                {
                    BackpackData backpack = BackpackDataFor(c, false);
                    if (backpack?.itemSlots != null)
                    {
                        for (int slot = 0; slot < backpack.itemSlots.Length; slot++)
                            CheckRareItemSlot(c, playerKey, "bag:" + slot, backpack.itemSlots[slot], now, AdminRareTouchedScratch, AdminRareCurrentScratch, maxStrikes);
                    }
                }
                catch { }

                CheckRareInventorySnapshot(c, playerKey, AdminRareCurrentScratch, maxStrikes);
                TrackGlobalAmuletDuplicates(c, playerKey, AdminRareCurrentScratch);
                PruneRareSlotState(playerKey, AdminRareTouchedScratch);
            }

            foreach (KeyValuePair<string, Character> pair in AdminAmuletDuplicateTargetScratch)
            {
                if (AdminAmuletDuplicateSnapshot.Contains(pair.Key))
                    continue;
                Character target = pair.Value;
                string key = PlayerBanId(target);
                RegisterAdminStrike(
                    target,
                    key + ":amulet_duplicate_global:" + pair.Key,
                    $"{SafeCharacterName(target)}: duplicate amulet across players ({RareItemDisplayName(pair.Key)})",
                    1);
            }
            AdminAmuletDuplicateSnapshot.Clear();
            foreach (string fingerprint in AdminAmuletDuplicateTargetScratch.Keys)
                AdminAmuletDuplicateSnapshot.Add(fingerprint);
        }

        private static void CheckRareItemSlot(Character c, string playerKey, string slotName, ItemSlot slot, float now, HashSet<string> touched, List<string> currentRare, int maxStrikes)
        {
            string stateKey = playerKey + "|" + slotName;
            touched.Add(stateKey);

            string fingerprint = RareItemFingerprint(slot);
            if (!string.IsNullOrWhiteSpace(fingerprint))
                currentRare.Add(fingerprint);

            AdminRareSlotState.TryGetValue(stateKey, out string previous);
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                if (!string.IsNullOrWhiteSpace(previous))
                    AdminRareSlotState.Remove(stateKey);
                return;
            }

            if (string.Equals(previous, fingerprint, StringComparison.Ordinal))
                return;

            AdminRareSlotState[stateKey] = fingerprint;
            RegisterRareItemEvent(c, playerKey, fingerprint, now, maxStrikes);
        }

        private static void CheckRareInventorySnapshot(Character c, string playerKey, List<string> currentRare, int maxStrikes)
        {
            if (currentRare == null || currentRare.Count == 0)
            {
                AdminRareInventorySnapshot.Remove(playerKey);
                return;
            }

            currentRare.Sort(StringComparer.Ordinal);
            string snapshot = string.Join("\n", currentRare.ToArray());
            if (AdminRareInventorySnapshot.TryGetValue(playerKey, out string previous)
                && string.Equals(previous, snapshot, StringComparison.Ordinal))
                return;
            AdminRareInventorySnapshot[playerKey] = snapshot;

            AdminRareCountsScratch.Clear();
            for (int i = 0; i < currentRare.Count; i++)
            {
                string fingerprint = currentRare[i];
                AdminRareCountsScratch[fingerprint] = AdminRareCountsScratch.TryGetValue(fingerprint, out int old) ? old + 1 : 1;
            }

            foreach (KeyValuePair<string, int> pair in AdminRareCountsScratch)
            {
                if (pair.Value >= 2 && IsAmuletFingerprint(pair.Key))
                {
                    RegisterAdminStrike(
                        c,
                        playerKey + ":rare_duplicate:" + pair.Key,
                        $"{SafeCharacterName(c)}: duplicate rare item ({RareItemDisplayName(pair.Key)} x{pair.Value})",
                        1);
                }
            }

            if (currentRare.Count >= 5)
            {
                RegisterAdminStrike(
                    c,
                    playerKey + ":rare_total",
                    $"{SafeCharacterName(c)}: too many rare items equipped ({currentRare.Count})",
                    maxStrikes);
            }
        }

        private static void RegisterRareItemEvent(Character c, string playerKey, string fingerprint, float now, int maxStrikes)
        {
            if (!AdminRareItemEvents.TryGetValue(playerKey, out List<float> events))
            {
                events = new List<float>();
                AdminRareItemEvents[playerKey] = events;
            }

            events.Add(now);
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (now - events[i] > AdminRareItemWindowSeconds)
                    events.RemoveAt(i);
            }

            if (events.Count >= AdminRareItemBurstThreshold)
            {
                string itemName = RareItemDisplayName(fingerprint);
                RegisterAdminStrike(
                    c,
                    playerKey + ":rare_items",
                    $"{SafeCharacterName(c)}: rare item burst {events.Count}/{AdminRareItemWindowSeconds:0}s ({itemName})",
                    maxStrikes);
                events.Clear();
            }
        }

        private static void PruneRareSlotState(string playerKey, HashSet<string> touched)
        {
            string prefix = playerKey + "|";
            AdminRareRemoveScratch.Clear();
            foreach (string key in AdminRareSlotState.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal) && !touched.Contains(key))
                    AdminRareRemoveScratch.Add(key);
            }
            for (int i = 0; i < AdminRareRemoveScratch.Count; i++)
                AdminRareSlotState.Remove(AdminRareRemoveScratch[i]);
        }

        private static string RareItemFingerprint(ItemSlot slot)
        {
            try
            {
                if (slot == null || slot.IsEmpty())
                    return "";

                Item item = SlotPrefab(slot);
                if (item == null || !IsAdminRareAuditItem(item))
                    return "";

                string display = item.GetName();
                if (string.IsNullOrWhiteSpace(display))
                    display = item.name;
                string prefab = item.gameObject != null ? item.gameObject.name : item.name;
                string kind = IsScoutAmuletItem(item) ? "A" : "R";
                return kind + "|" + item.itemID + "|" + prefab + "|" + display;
            }
            catch { return ""; }
        }

        private static bool IsAmuletFingerprint(string fingerprint)
        {
            return !string.IsNullOrWhiteSpace(fingerprint)
                && fingerprint.StartsWith("A|", StringComparison.Ordinal);
        }

        private static void TrackGlobalAmuletDuplicates(Character c, string playerKey, List<string> currentRare)
        {
            if (c == null || currentRare == null)
                return;
            for (int i = 0; i < currentRare.Count; i++)
            {
                string fingerprint = currentRare[i];
                if (!IsAmuletFingerprint(fingerprint))
                    continue;
                if (!AdminAmuletFirstOwnerScratch.TryGetValue(fingerprint, out string firstOwner))
                {
                    AdminAmuletFirstOwnerScratch[fingerprint] = playerKey;
                    continue;
                }
                if (!string.Equals(firstOwner, playerKey, StringComparison.Ordinal))
                    AdminAmuletDuplicateTargetScratch[fingerprint] = c;
            }
        }

        private static string RareItemDisplayName(string fingerprint)
        {
            if (string.IsNullOrWhiteSpace(fingerprint))
                return "?";
            string[] parts = fingerprint.Split('|');
            return parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : fingerprint;
        }

        private static bool IsAdminRareAuditItem(Item item)
        {
            try
            {
                if (item == null)
                    return false;
                if ((item.itemTags & (ScoutAmuletTag | Item.ItemTags.Mystical)) != 0)
                    return true;

                string haystack = ((item.GetName() ?? "") + " "
                    + (item.name ?? "") + " "
                    + (item.gameObject != null ? item.gameObject.name : "") + " "
                    + item.GetType().Name).ToLowerInvariant();

                return ContainsAny(haystack, AdminRareKeywords);
            }
            catch { return false; }
        }

        private static bool ContainsAny(string value, string[] needles)
        {
            if (string.IsNullOrWhiteSpace(value) || needles == null)
                return false;
            for (int i = 0; i < needles.Length; i++)
            {
                string needle = needles[i];
                if (!string.IsNullOrWhiteSpace(needle) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static void RegisterAdminStrike(Character c, string key, string message, int maxStrikes)
        {
            int strikes = AdminStrikes.TryGetValue(key, out int old) ? old + 1 : 1;
            AdminStrikes[key] = strikes;
            AddAdminLog($"{message} ({strikes}/{maxStrikes})");

            if (!ModConfig.AdminWarnOnly && strikes >= maxStrikes)
            {
                AddAdminLog($"{SafeCharacterName(c)}: auto-kick by admin protection");
                KickPlayer(c);
                AdminStrikes[key] = 0;
            }
        }

        public static void AddAdminLog(string message, bool showNotice = true)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            AdminProtectionLog.Insert(0, line);
            while (AdminProtectionLog.Count > 18)
                AdminProtectionLog.RemoveAt(AdminProtectionLog.Count - 1);
            if (showNotice)
            {
                AdminNoticeText = message;
                AdminNoticeUntil = Time.realtimeSinceStartup + 6f;
            }
            Plugin.Log?.LogInfo("[AdminProtection] " + message);
        }

        private static Photon.Realtime.Player OwnerOf(Character c)
        {
            if (c == null) return null;
            try
            {
                var view = ViewOf(c);
                if (view != null && view.Owner != null) return view.Owner;
            }
            catch { }
            return null;
        }

        private static PhotonView ViewOf(Character c)
        {
            if (c == null) return null;
            try
            {
                var view = CharacterViewField != null ? CharacterViewField.GetValue(c) as PhotonView : null;
                if (view != null) return view;
            }
            catch { }
            try { return ((MonoBehaviourPun)c).photonView; }
            catch { return null; }
        }

        private static string RootExceptionMessage(Exception e)
        {
            if (e == null) return "";
            while (e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;
            return $"{e.GetType().Name}: {e.Message}";
        }

        private static string PlayerBanId(Character c)
        {
            var owner = OwnerOf(c);
            if (owner == null) return "";
            if (!string.IsNullOrWhiteSpace(owner.UserId)) return "uid:" + owner.UserId;
            if (!string.IsNullOrWhiteSpace(owner.NickName)) return "nick:" + owner.NickName;
            return "actor:" + owner.ActorNumber;
        }

        private static void SuppressAdminMovementCheck(Character c, float seconds = 4f)
        {
            if (c == null || IsLocal(c))
                return;
            string key = PlayerBanId(c);
            if (string.IsNullOrWhiteSpace(key))
                return;
            AdminMotionSuppressedUntil[key] = Time.realtimeSinceStartup + Mathf.Max(1f, seconds);
            AdminMotion.Remove(key);
        }

        /// <summary>Teleport yourself to the player.</summary>
        public static void WarpToPlayer(Character c)
        {
            try
            {
                if (c == null || Character.localCharacter == null) return;
                Vector3 pos = c.Head + new Vector3(0f, 4f, 0f);
                ((MonoBehaviourPun)Character.localCharacter).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { pos, true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpToPlayer: {e.Message}"); }
        }

        /// <summary>Bring the player to you.</summary>
        public static void BringPlayer(Character c)
        {
            try
            {
                if (c == null || Character.localCharacter == null) return;
                if (!RequireHostForRemote(c, "bring player")) return;
                Vector3 pos = Character.localCharacter.Head + new Vector3(0f, 4f, 0f);
                SuppressAdminMovementCheck(c);
                ((MonoBehaviourPun)c).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { pos, true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] BringPlayer: {e.Message}"); }
        }

        public static void PushPlayer(Character c, float power = 14f)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;
                if (!RequireHostForRemote(target, "push player")) return;

                Vector3 direction = LocalAimDirection(target);
                if (direction.y < 0.12f)
                    direction.y = 0.12f;
                direction = (direction.normalized + Vector3.up * 0.5f).normalized;

                float clampedPower = Mathf.Clamp(power, 2f, 60f);
                ApplyImpulse(target, direction * clampedPower);
                ApplyForceAtPositionRpc(target, direction * clampedPower * 18f, SafePosition(target), 2.5f);

                if (!IsLocal(target) && IsHost())
                {
                    Vector3 flat = new Vector3(direction.x, 0f, direction.z);
                    if (flat.sqrMagnitude < 0.01f)
                        flat = ((Component)target).transform.forward;
                    flat.Normalize();
                    Vector3 destination = SafePosition(target)
                        + flat * Mathf.Clamp(power * 0.22f, 1.5f, 9f)
                        + Vector3.up * Mathf.Clamp(power * 0.12f, 1f, 5f);
                    SuppressAdminMovementCheck(target);
                    try { ((MonoBehaviourPun)target).photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { destination, true }); }
                    catch { }
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] PushPlayer: {e.Message}"); }
        }

        public static void ExplodePlayer(Character c, float power = 34f, float radius = 9f)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;
                if (!RequireHostForRemote(target, "explode player")) return;

                Vector3 center = SafePosition(target);
                SpawnExplosionVfx(target, center);
                RefreshPlayers();
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    Character other = PlayerChars[i];
                    if (other == null) continue;
                    Vector3 delta = SafePosition(other) - center;
                    float dist = Mathf.Max(0.35f, delta.magnitude);
                    if (dist > radius) continue;

                    Vector3 direction = (delta.sqrMagnitude > 0.05f ? delta.normalized : LocalAimDirection(other)) + Vector3.up * 0.7f;
                    float scaled = power * Mathf.Clamp01(1f - dist / radius + 0.25f);
                    ApplyImpulse(other, direction.normalized * scaled);
                    ApplyForceAtPositionRpc(other, direction.normalized * scaled * 22f, center, radius);
                }

                Vector3 targetBlast = (LocalAimDirection(target) + Vector3.up * 1.35f + UnityEngine.Random.insideUnitSphere * 0.15f).normalized;
                ApplyImpulse(target, targetBlast * Mathf.Clamp(power * 1.25f, 12f, 90f));
                ApplyForceAtPositionRpc(target, targetBlast * Mathf.Clamp(power * 26f, 250f, 1200f), center, Mathf.Max(2f, radius * 0.5f));
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ExplodePlayer: {e.Message}"); }
        }

        private static Vector3 LocalAimDirection(Character fallbackTarget)
        {
            try
            {
                if (MainCamera.instance != null)
                {
                    Vector3 forward = ((Component)MainCamera.instance).transform.forward;
                    if (forward.sqrMagnitude > 0.01f)
                        return forward.normalized;
                }
            }
            catch { }

            try
            {
                Camera camera = Camera.main;
                if (camera != null && camera.transform.forward.sqrMagnitude > 0.01f)
                    return camera.transform.forward.normalized;
            }
            catch { }

            try
            {
                Character local = Character.localCharacter;
                if (local != null && local.data != null && local.data.lookDirection.sqrMagnitude > 0.01f)
                    return local.data.lookDirection.normalized;
            }
            catch { }

            try { return ((Component)fallbackTarget).transform.forward.normalized; }
            catch { return Vector3.forward; }
        }

        private static void SpawnExplosionVfx(Character target, Vector3 center)
        {
            bool localSpawned = false;
            try
            {
                var movement = target?.refs?.movement;
                if (movement != null && RocketExplodeRpcMethod != null)
                {
                    RocketExplodeRpcMethod.Invoke(movement, null);
                    localSpawned = true;
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[GameApi] local RocketExplodeRPC skipped: {RootExceptionMessage(e)}");
            }

            try
            {
                PhotonView view = ViewOf(target);
                if (view != null)
                {
                    view.RPC("RocketExplodeRPC", RpcTarget.Others, new object[0]);
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[GameApi] RocketExplodeRPC skipped: {RootExceptionMessage(e)}");
            }

            if (localSpawned)
                return;

            try
            {
                GameObject prefab = target?.refs?.movement?.explosionPrefab;
                if (prefab != null)
                {
                    UnityEngine.Object.Instantiate(prefab, center, Quaternion.identity);
                    return;
                }
            }
            catch { }

            try
            {
                GameObject skeletonExplosion = Resources.Load<GameObject>("SkeletonExplosion");
                if (skeletonExplosion == null)
                    return;
                GameObject spawned = UnityEngine.Object.Instantiate(skeletonExplosion, center, Quaternion.identity);
                SkeletonExplosion boom = spawned.GetComponent<SkeletonExplosion>();
                if (boom != null && target != null)
                    boom.Boom(target);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[GameApi] explosion VFX fallback skipped: {RootExceptionMessage(e)}");
            }
        }

        private static void ApplyForceAtPositionRpc(Character c, Vector3 force, Vector3 point, float radius)
        {
            try
            {
                PhotonView view = ViewOf(c);
                if (view != null)
                    view.RPC("RPCA_AddForceAtPosition", RpcTarget.All, new object[] { force, point, Mathf.Max(0.1f, radius) });
            }
            catch { }
        }

        private static void ApplyImpulse(Character c, Vector3 impulse)
        {
            try
            {
                var parts = c.refs?.ragdoll?.partList;
                if (parts == null) return;
                for (int i = 0; i < parts.Count; i++)
                {
                    Rigidbody rig = parts[i] != null ? parts[i].Rig : null;
                    if (rig == null || rig.isKinematic) continue;
                    rig.AddForce(impulse, ForceMode.VelocityChange);
                }
            }
            catch { }
        }

        public static void ReviveAll()
        {
            if (!RequireHostAction("revive all")) return;
            RefreshPlayers();
            foreach (var c in PlayerChars) RevivePlayer(c);
        }

        public static void WarpAllToMe()
        {
            if (!RequireHostAction("bring all")) return;
            RefreshPlayers();
            foreach (var c in PlayerChars) if (c != null && !c.IsLocal) BringPlayer(c);
        }

        /// <summary>Kill every player; optionally skip yourself.</summary>
        public static void KillAll(bool excludeSelf)
        {
            if (!RequireHostAction("kill all")) return;
            RefreshPlayers();
            int affected = 0;
            foreach (var c in PlayerChars)
            {
                if (c == null) continue;
                if (excludeSelf) { try { if (c.IsLocal) continue; } catch { } }
                KillPlayer(c);
                affected++;
            }
        }

        // ---------------- inventory recharge ----------------
        /// <summary>Restore charges/fuel/uses/durability on a held item slot to the given value.</summary>
        public static void RechargeSlot(int slot, float value)
        {
            try
            {
                var p = Player.localPlayer;
                if (p == null || p.itemSlots == null || slot < 0 || slot >= p.itemSlots.Length) return;
                RechargeItemSlotData(p.itemSlots[slot], value);
                SyncInventory(p);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RechargeSlot: {e.Message}"); }
        }

        public static void RechargeSlotFor(Character target, int slot, float value)
        {
            try
            {
                if (IsLocal(target))
                {
                    RechargeSlot(slot, value);
                    return;
                }
                if (!RequireHostForRemote(target, "inventory recharge"))
                    return;

                var p = PlayerOf(target);
                if (p == null || p.itemSlots == null || slot < 0 || slot >= p.itemSlots.Length) return;
                RechargeItemSlotData(p.itemSlots[slot], value);
                SyncInventory(p, (RpcTarget)0);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RechargeSlotFor: {e.Message}"); }
        }

        private static void RechargeItemSlotData(ItemSlot slot, float value)
        {
            var data = slot?.data?.data;
            if (data == null) return;
            foreach (var kv in data)
            {
                int key = (int)kv.Key;
                if (key == 12 && kv.Value is IntItemData ii) ii.Value = (int)value;
                else if (key == 10 && kv.Value is FloatItemData f1) f1.Value = value;
                else if (key == 11 && kv.Value is FloatItemData f2) f2.Value = value;
                else if (key == 2 && kv.Value is OptionableIntItemData oi) oi.Value = (int)value;
            }
        }

        // ---------------- luggage / containers ----------------
        public static readonly List<string> LuggageLabels = new List<string>();
        public static readonly List<Luggage> LuggageObjects = new List<Luggage>();

        /// <summary>Find luggage/containers within 300 m of the local character, sorted by distance.</summary>
        public static void RefreshLuggage()
        {
            try
            {
                LuggageLabels.Clear();
                LuggageObjects.Clear();
                var me = Character.localCharacter;
                if (me == null) return;
                var all = Luggage.ALL_LUGGAGE;
                if (all == null || all.Count == 0) return;

                Vector3 head = me.Head;
                var near = new List<KeyValuePair<Luggage, float>>();
                foreach (var lug in all)
                {
                    if (lug == null) continue;
                    try
                    {
                        float d = Vector3.Distance(head, lug.Center());
                        if (d <= 300f) near.Add(new KeyValuePair<Luggage, float>(lug, d));
                    }
                    catch { }
                }
                near.Sort((a, b) => a.Value.CompareTo(b.Value));
                foreach (var kv in near)
                {
                    LuggageObjects.Add(kv.Key);
                    LuggageLabels.Add($"{kv.Key.displayName ?? "Container"} [{kv.Value:F0}m]");
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RefreshLuggage: {e.Message}"); }
        }

        public static void OpenLuggage(int index)
        {
            try
            {
                if (!RequireHostAction("open container")) return;
                if (index < 0 || index >= LuggageObjects.Count) return;
                var lug = LuggageObjects[index];
                if (lug == null) return;
                var pv = ((Component)lug).GetComponent<PhotonView>();
                if (pv != null) pv.RPC("OpenLuggageRPC", (RpcTarget)0, new object[] { true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] OpenLuggage: {e.Message}"); }
        }

        public static void OpenAllNearbyLuggage()
        {
            if (!RequireHostAction("open all containers")) return;
            for (int i = 0; i < LuggageObjects.Count; i++) OpenLuggage(i);
        }

        public static void WarpToLuggage(int index)
        {
            try
            {
                if (index < 0 || index >= LuggageObjects.Count) return;
                var lug = LuggageObjects[index];
                var me = Character.localCharacter;
                if (lug == null || me == null) return;
                ((MonoBehaviourPun)me).photonView.RPC("WarpPlayerRPC", (RpcTarget)0, new object[] { lug.Center() + new Vector3(0f, 2f, 0f), true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpToLuggage: {e.Message}"); }
        }

        public static int SegmentCount => WarpSegments.Length;

        public static string SegmentLabel(int index, bool russian)
        {
            string[] names = russian ? SegmentNamesRu : SegmentNamesEn;
            return index >= 0 && index < names.Length ? names[index] : index.ToString();
        }

        public static string CurrentSegmentLabel(bool russian)
        {
            try
            {
                Segment current = MapHandler.CurrentSegmentNumber;
                for (int i = 0; i < WarpSegments.Length; i++)
                    if (WarpSegments[i] == current)
                        return SegmentLabel(i, russian);
                return current.ToString();
            }
            catch
            {
                return russian ? "неизвестно" : "unknown";
            }
        }

        public static string SegmentBiomeLabel(int index, bool russian)
        {
            try
            {
                if (index < 0 || index >= WarpSegments.Length)
                    return "";
                var biome = MapHandler.GetBiomeForSegment((int)WarpSegments[index]);
                return biome.ToString();
            }
            catch
            {
                return "";
            }
        }

        public static void WarpToSegmentIndex(int index)
        {
            try
            {
                if (!RequireHostAction("warp to biome")) return;
                if (index < 0 || index >= WarpSegments.Length) return;
                if (WarpSegments[index] == Segment.Peak && WarpLocalToPeakSpawn())
                {
                    return;
                }

                MapHandler.JumpToSegment(WarpSegments[index]);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpToSegmentIndex: {e.Message}"); }
        }

        private static bool WarpLocalToPeakSpawn()
        {
            try
            {
                Character me = Character.localCharacter;
                if (me == null || me.data == null || me.data.dead)
                    return false;

                Vector3 position;
                if (!TryFindPeakSpawn(out position))
                    return false;

                ((MonoBehaviourPun)me).photonView.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { position + Vector3.up * 1.5f, true });
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] WarpLocalToPeakSpawn: {RootExceptionMessage(e)}");
                return false;
            }
        }

        private static bool TryFindPeakSpawn(out Vector3 position)
        {
            position = Vector3.zero;
            try
            {
                if (MapHandler.Exists && Singleton<MapHandler>.Instance != null && Singleton<MapHandler>.Instance.respawnThePeak != null)
                {
                    position = Singleton<MapHandler>.Instance.respawnThePeak.position;
                    return true;
                }
            }
            catch { }

            try
            {
                Transform best = null;
                float bestY = float.MinValue;
                Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform t = transforms[i];
                    if (t == null || t.gameObject == null)
                        continue;
                    string name = t.gameObject.name ?? "";
                    if (name.IndexOf("peak", StringComparison.OrdinalIgnoreCase) < 0 &&
                        name.IndexOf("respawn", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (t.position.y > bestY)
                    {
                        best = t;
                        bestY = t.position.y;
                    }
                }
                if (best != null)
                {
                    position = best.position;
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static readonly List<string> WorldObjectLabels = new List<string>();
        private static readonly List<Vector3> WorldObjectPositions = new List<Vector3>();

        public static void RefreshWorldObjects(string query, float radius)
        {
            try
            {
                WorldObjectLabels.Clear();
                WorldObjectPositions.Clear();

                Character me = Character.localCharacter;
                Vector3 origin = me != null ? SafePosition(me) : Vector3.zero;
                string[] terms = ObjectSearchTerms(query);
                float maxRadius = Mathf.Clamp(radius <= 0f ? 1200f : radius, 50f, 5000f);
                var seen = new HashSet<int>();
                var hits = new List<WorldObjectScanHit>();

                foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (transform == null || transform.gameObject == null || !transform.gameObject.activeInHierarchy)
                        continue;

                    GameObject root = InterestingRoot(transform);
                    if (root == null || !root.activeInHierarchy)
                        continue;
                    int id = root.GetInstanceID();
                    if (seen.Contains(id))
                        continue;

                    string haystack = WorldObjectSearchText(root);
                    if (!MatchesAnyTerm(haystack, terms))
                        continue;

                    Vector3 pos = root.transform.position;
                    float distance = Vector3.Distance(origin, pos);
                    if (distance > maxRadius)
                        continue;

                    seen.Add(id);
                    hits.Add(new WorldObjectScanHit
                    {
                        Label = WorldObjectLabel(root, distance),
                        Position = pos,
                        Distance = distance,
                    });
                }

                hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                int count = Mathf.Min(80, hits.Count);
                for (int i = 0; i < count; i++)
                {
                    WorldObjectLabels.Add(hits[i].Label);
                    WorldObjectPositions.Add(hits[i].Position);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] RefreshWorldObjects: {e.Message}"); }
        }

        public static Vector3 WorldObjectPosition(int index)
        {
            return index >= 0 && index < WorldObjectPositions.Count ? WorldObjectPositions[index] : Vector3.zero;
        }

        public static void WarpToWorldObject(int index)
        {
            try
            {
                if (index < 0 || index >= WorldObjectPositions.Count) return;
                Vector3 pos = WorldObjectPositions[index] + Vector3.up * 2f;
                TeleportToCoords(pos.x, pos.y, pos.z);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] WarpToWorldObject: {e.Message}"); }
        }

        private static string[] ObjectSearchTerms(string query)
        {
            string cleaned = string.IsNullOrWhiteSpace(query)
                ? "bell belltower tower item luggage chest gloom campfire statue altar shrine biome spawn scout raven voice fog"
                : query;
            return cleaned
                .Split(new[] { ' ', ',', ';', '|', '/', '\\', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLowerInvariant())
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToArray();
        }

        private static bool MatchesAnyTerm(string haystack, string[] terms)
        {
            if (string.IsNullOrWhiteSpace(haystack) || terms == null || terms.Length == 0)
                return false;
            for (int i = 0; i < terms.Length; i++)
                if (haystack.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static GameObject InterestingRoot(Transform transform)
        {
            try
            {
                var item = transform.GetComponentInParent<Item>();
                if (item != null && item.gameObject != null) return item.gameObject;
            }
            catch { }
            try
            {
                var luggage = transform.GetComponentInParent<Luggage>();
                if (luggage != null && luggage.gameObject != null) return luggage.gameObject;
            }
            catch { }
            try
            {
                var biome = transform.GetComponentInParent<Biome>();
                if (biome != null && biome.gameObject != null) return biome.gameObject;
            }
            catch { }
            return transform.gameObject;
        }

        private static string WorldObjectSearchText(GameObject root)
        {
            if (root == null)
                return "";

            var parts = new List<string> { root.name };
            try
            {
                var item = root.GetComponent<Item>();
                if (item != null)
                {
                    parts.Add(item.GetName());
                    parts.Add(item.name);
                    if (item.UIData != null)
                        parts.Add(item.UIData.itemName);
                    parts.Add("item");
                }
            }
            catch { }
            try
            {
                var luggage = root.GetComponent<Luggage>();
                if (luggage != null)
                {
                    parts.Add(luggage.displayName);
                    parts.Add("luggage chest container");
                }
            }
            catch { }
            try
            {
                var biome = root.GetComponent<Biome>();
                if (biome != null)
                {
                    parts.Add(biome.biomeType.ToString());
                    parts.Add("biome");
                }
            }
            catch { }
            try
            {
                foreach (Component component in root.GetComponents<Component>())
                    if (component != null)
                        parts.Add(component.GetType().Name);
            }
            catch { }
            return string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();
        }

        private static string WorldObjectLabel(GameObject root, float distance)
        {
            string name = root != null ? root.name : "Object";
            try
            {
                var item = root.GetComponent<Item>();
                if (item != null && !string.IsNullOrWhiteSpace(item.GetName()))
                    name = item.GetName();
            }
            catch { }
            try
            {
                var luggage = root.GetComponent<Luggage>();
                if (luggage != null && !string.IsNullOrWhiteSpace(luggage.displayName))
                    name = luggage.displayName;
            }
            catch { }
            return $"{CleanObjectName(name)} [{distance:F0}m]";
        }

        private static string CleanObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Object";
            string cleaned = value.Replace("(Clone)", "").Replace("_", " ").Trim();
            return cleaned.Length > 80 ? cleaned.Substring(0, 80) : cleaned;
        }

        // ---------------- world / timer ----------------
        public static float ExpeditionTimeSeconds()
        {
            try { return RunTimeSeconds(RunManager.Instance); }
            catch { return 0f; }
        }

        public static float RunTimeSeconds(RunManager rm)
        {
            if (rm == null) return 0f;
            try { return rm.TimeSinceRunStarted; }
            catch
            {
                try { return RunTimeField != null ? (float)RunTimeField.GetValue(rm) : 0f; }
                catch { return 0f; }
            }
        }

        public static void SetRunTimeLocal(RunManager rm, float seconds)
        {
            if (rm == null) return;
            try
            {
                if (RunTimeField != null)
                    RunTimeField.SetValue(rm, Mathf.Max(0f, seconds));
            }
            catch (Exception e) { Plugin.Log?.LogDebug($"[GameApi] SetRunTimeLocal: {e.Message}"); }
        }

        public static void SetExpeditionTime(float seconds)
        {
            try
            {
                if (!RequireHostAction("set run time")) return;
                var rm = RunManager.Instance;
                if (rm == null) return;

                seconds = Mathf.Max(0f, seconds);
                SetRunTimeLocal(rm, seconds);

                if (PhotonNetwork.IsMasterClient)
                    ((MonoBehaviourPun)rm).photonView.RPC("RPC_SyncTime", RpcTarget.All, new object[] { seconds, true });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SetExpeditionTime: {e.Message}"); }
        }

        public static float TimeOfDay()
        {
            try { return DayNightManager.instance != null ? DayNightManager.instance.timeOfDay : 0f; }
            catch { return 0f; }
        }

        public static int DayCount()
        {
            try { return DayNightManager.instance != null ? DayNightManager.instance.dayCount : 0; }
            catch { return 0; }
        }

        public static void SetTimeOfDay(float hour)
        {
            try
            {
                if (!RequireHostAction("set time of day")) return;
                var manager = DayNightManager.instance;
                if (manager == null) return;

                float time = Mathf.Repeat(hour, 24f);
                DayNightManager.SetTimeOfDay(time);
                manager.setTimeOfDay(time);
                manager.UpdateCycle();

                try
                {
                    var field = typeof(DayNightManager).GetField("photonView", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.GetValue(manager) is PhotonView view)
                        view.RPC("RPCA_SyncTime", RpcTarget.All, new object[] { manager.dayCount, time });
                }
                catch { }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SetTimeOfDay: {e.Message}"); }
        }

        public static void ForceWin()
        {
            try
            {
                if (!RequireHostAction("force win")) return;
                var me = Character.localCharacter;
                if (me == null) return;
                ((MonoBehaviourPun)me).photonView.RPC("RPCEndGame_ForceWin", RpcTarget.All, new object[0]);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] ForceWin: {e.Message}"); }
        }

        // ---------------- scoutmaster ----------------
        /// <summary>Spawn the Scoutmaster enemy near a player (host only) and aim it at them.</summary>
        public static void SpawnScoutmaster(Character target)
        {
            try
            {
                if (!RequireHostAction("spawn scoutmaster")) return;
                if (target == null) return;

                Vector3 from = ((Component)target).transform.position
                    + new Vector3(UnityEngine.Random.Range(-10f, 10f), 25f, UnityEngine.Random.Range(-10f, 10f));
                if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 100f)) return;
                Vector3 pos = hit.point + Vector3.up;

                var obj = PhotonNetwork.InstantiateRoomObject("Character_Scoutmaster", pos, Quaternion.identity, 0, null);
                if (obj == null) return;
                var ch = obj.GetComponent<Character>();
                if (ch != null) ch.data.spawnPoint = ((Component)ch).transform;

                var sm = obj.GetComponent<Scoutmaster>();
                if (sm != null)
                    TrySetScoutmasterTarget(obj, sm, target, 15f);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnScoutmaster: {e.Message}"); }
        }

        private static void TrySetScoutmasterTarget(GameObject scoutObject, Scoutmaster scoutmaster, Character target, float forceForSeconds)
        {
            try
            {
                if (scoutmaster == null || target == null)
                    return;

                var targetView = ((MonoBehaviourPun)target).photonView;
                if (targetView == null || targetView.ViewID <= 0)
                    return;

                var scoutView = scoutObject != null ? scoutObject.GetComponent<PhotonView>() : null;
                if (scoutView == null)
                    return;

                scoutmaster.currentTarget = target;
                scoutView.RPC("RPCA_SetCurrentTarget", RpcTarget.All, new object[] { targetView.ViewID, forceForSeconds });
            }
            catch (Exception e)
            {
                Plugin.Log?.LogDebug($"[GameApi] Scoutmaster target skipped: {e.Message}");
            }
        }

        // ---------------- stamina / afflictions (the status bar) ----------------
        // Index matches CharacterAfflictions.STATUSTYPE order.
        public static readonly string[] StatusEn =
            { "Injury", "Hunger", "Cold", "Poison", "Crab", "Curse", "Drowsy", "Weight", "Hot", "Thorns", "Spores", "Web", "Arrows", "Petrify", "Flytrap" };
        public static readonly string[] StatusRu =
            { "Травма", "Голод", "Холод", "Яд", "Краб", "Проклятие", "Сонливость", "Вес", "Жара", "Шипы", "Споры", "Паутина", "Стрелы", "Окаменение", "Мухоловка" };

        private const int ArrowStatusIndex = (int)CharacterAfflictions.STATUSTYPE.Arrow;
        private const int PetrifyStatusIndex = (int)CharacterAfflictions.STATUSTYPE.Petrify;

        private static int StatusCount()
        {
            try { return Mathf.Min(CharacterAfflictions.NumStatusTypes, StatusEn.Length); }
            catch { return StatusEn.Length; }
        }

        private static bool IsStatusIndex(int idx)
        {
            return idx >= 0 && idx < StatusCount();
        }

        private static string StatusLabel(int idx)
        {
            return idx >= 0 && idx < StatusEn.Length ? StatusEn[idx] : idx.ToString();
        }

        /// <summary>Refill a character's stamina to full.</summary>
        public static void FullStamina(Character c)
        {
            try
            {
                if (!RequireHostForRemote(c, "refill stamina")) return;
                if (c != null && c.data != null) { c.data.currentStamina = 1f; c.data.extraStamina = Mathf.Max(c.data.extraStamina, 0f); }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] FullStamina: {e.Message}"); }
        }

        public static void FullExtraStamina(Character c)
        {
            SetExtraStamina(c, 1f);
        }

        public static void SetExtraStamina(Character c, float amount)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;
                if (!RequireHostForRemote(target, "set extra stamina")) return;
                float clamped = Mathf.Clamp01(amount);
                if (IsLocal(target)) target.SetExtraStamina(clamped);
                else if (target.data != null) target.data.extraStamina = clamped;
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SetExtraStamina: {e.Message}"); }
        }

        /// <summary>Set a single status (0..1) on a character's afflictions.</summary>
        public static void SetStatus(Character c, int idx, float amount, bool track = true)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (!RequireHostForRemote(target, "set status")) return;
                var a = target != null && target.refs != null ? target.refs.afflictions : null;
                if (a == null || !IsStatusIndex(idx)) return;
                float clamped = Mathf.Clamp01(amount);
                var type = (CharacterAfflictions.STATUSTYPE)idx;

                if (idx == PetrifyStatusIndex)
                {
                    SetPetrifyAmount(target, Mathf.RoundToInt(clamped * 100f), track);
                    return;
                }

                if (idx == ArrowStatusIndex)
                {
                    if (clamped <= 0.001f) ClearArrows(target, track);
                    else AddArrowPrank(target, Mathf.Clamp(Mathf.CeilToInt(clamped * 8f), 1, 8), track);
                    return;
                }

                PrepareLocalStatusWrite(target, type, clamped);
                if (IsLocal(target))
                {
                    a.SetStatus(type, clamped);
                }
                else
                {
                    float current = CurrentStatusValue(target, type);
                    float deltaValue = clamped - current;
                    if (Mathf.Abs(deltaValue) > 0.0001f)
                    {
                        float[] delta = new float[CharacterAfflictions.NumStatusTypes];
                        delta[idx] = deltaValue;
                        var view = ((MonoBehaviourPun)a).photonView;
                        if (view != null)
                            view.RPC("RPC_ApplyStatusesFromFloatArray", RpcTarget.All, new object[] { delta });
                        else
                            a.SetStatus(type, clamped);
                    }
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SetStatus: {e.Message}"); }
        }

        private static float CurrentStatusValue(Character c, CharacterAfflictions.STATUSTYPE type)
        {
            try
            {
                if ((int)type == PetrifyStatusIndex && c != null && c.data != null)
                    return Mathf.Clamp01(c.data.petrifyAmount / 100f);

                var a = c != null && c.refs != null ? c.refs.afflictions : null;
                return a != null ? a.GetCurrentStatus(type) : 0f;
            }
            catch { return 0f; }
        }

        private static void PrepareLocalStatusWrite(Character target, CharacterAfflictions.STATUSTYPE type, float amount)
        {
            if (!IsLocal(target) || amount <= 0f)
                return;

            ModConfig.LockStatus = false;
            if (ModConfig.NoStatusEffects)
                ModConfig.NoStatusEffects = false;

            switch (type)
            {
                case CharacterAfflictions.STATUSTYPE.Injury: ModConfig.NoInjury = false; break;
                case CharacterAfflictions.STATUSTYPE.Hunger: ModConfig.NoHunger = false; break;
                case CharacterAfflictions.STATUSTYPE.Cold: ModConfig.NoCold = false; break;
                case CharacterAfflictions.STATUSTYPE.Poison: ModConfig.NoPoison = false; break;
                case CharacterAfflictions.STATUSTYPE.Crab: ModConfig.NoCrab = false; break;
                case CharacterAfflictions.STATUSTYPE.Curse: ModConfig.NoCurse = false; break;
                case CharacterAfflictions.STATUSTYPE.Drowsy: ModConfig.NoDrowsy = false; break;
                case CharacterAfflictions.STATUSTYPE.Hot: ModConfig.NoHot = false; break;
                case CharacterAfflictions.STATUSTYPE.Thorns: ModConfig.NoThorns = false; break;
                case CharacterAfflictions.STATUSTYPE.Spores: ModConfig.NoSpores = false; break;
                case CharacterAfflictions.STATUSTYPE.Web: ModConfig.NoWeb = false; break;
                case CharacterAfflictions.STATUSTYPE.Arrow: ModConfig.NoArrows = false; break;
                case CharacterAfflictions.STATUSTYPE.Petrify: ModConfig.NoPetrify = false; break;
                case CharacterAfflictions.STATUSTYPE.FlyTrap: ModConfig.NoFlyTrap = false; break;
            }
        }

        private static void PrepareLocalStatusBatch(Character target)
        {
            if (!IsLocal(target))
                return;

            ModConfig.LockStatus = false;
            ModConfig.NoStatusEffects = false;
            ModConfig.NoInjury = false;
            ModConfig.NoHunger = false;
            ModConfig.NoCold = false;
            ModConfig.NoPoison = false;
            ModConfig.NoCrab = false;
            ModConfig.NoCurse = false;
            ModConfig.NoDrowsy = false;
            ModConfig.NoHot = false;
            ModConfig.NoThorns = false;
            ModConfig.NoSpores = false;
            ModConfig.NoWeb = false;
            ModConfig.NoArrows = false;
            ModConfig.NoPetrify = false;
            ModConfig.NoFlyTrap = false;
        }

        public static void IncreaseStatus(Character c, int idx, float amount)
        {
            AdjustStatus(c, idx, Mathf.Max(0f, amount));
        }

        public static void AdjustStatus(Character c, int idx, float delta)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (!RequireHostForRemote(target, "adjust status")) return;
                var a = target != null && target.refs != null ? target.refs.afflictions : null;
                if (a == null || !IsStatusIndex(idx)) return;
                float current = CurrentStatusValue(target, (CharacterAfflictions.STATUSTYPE)idx);
                float next = Mathf.Clamp01(current + delta);
                SetStatus(target, idx, next, false);
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] AdjustStatus: {e.Message}"); }
        }

        public static void IncreaseCommonStatuses(Character c, float amount)
        {
            if (!RequireHostForRemote(c, "increase common statuses")) return;
            int[] indices = { 0, 1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14 };
            for (int i = 0; i < indices.Length; i++)
                IncreaseStatus(c, indices[i], amount);
        }

        /// <summary>Clear every affliction (full heal) on a character.</summary>
        public static void ClearAllStatus(Character c)
        {
            Character target = c ?? Character.localCharacter;
            if (!RequireHostForRemote(target, "clear status")) return;
            for (int i = 0; i < StatusCount(); i++) SetStatus(target, i, 0f, false);
            RemovePhysicalThorns(target, false, false);
            SetPetrifyAmount(target, 0, false);
        }

        /// <summary>Prank: pile a bunch of nasty afflictions onto a player.</summary>
        public static void PrankAfflict(Character c)
        {
            Character target = c ?? Character.localCharacter;
            if (target == null) return;
            if (!RequireHostForRemote(target, "prank afflict")) return;
            PrepareLocalStatusBatch(target);

            const float amount = 0.10f;
            SetStatusAtLeast(target, 3, amount);  // Poison
            SetStatusAtLeast(target, 5, amount);  // Curse
            SetStatusAtLeast(target, 4, amount);  // Crab
            SetStatusAtLeast(target, 6, amount);  // Drowsy
            SetStatusAtLeast(target, 11, amount); // Web
            SetStatusAtLeast(target, 14, amount); // Flytrap
        }

        private static void SetStatusAtLeast(Character c, int idx, float amount)
        {
            try
            {
                var a = c != null && c.refs != null ? c.refs.afflictions : null;
                float current = a != null && IsStatusIndex(idx)
                    ? CurrentStatusValue(c, (CharacterAfflictions.STATUSTYPE)idx)
                    : 0f;
                SetStatus(c, idx, Mathf.Max(current, Mathf.Clamp01(amount)), false);
            }
            catch
            {
                SetStatus(c, idx, Mathf.Clamp01(amount), false);
            }
        }

        public static void PetrifyPlayer(Character c)
        {
            SetPetrifyAmount(c ?? Character.localCharacter, 100);
        }

        public static void ClearPetrify(Character c)
        {
            SetPetrifyAmount(c ?? Character.localCharacter, 0);
        }

        public static void SetPetrifyAmount(Character c, int amount, bool track = true)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null || target.data == null) return;
                if (!RequireHostForRemote(target, "set petrify")) return;

                int clamped = Mathf.Clamp(amount, 0, 100);
                if (IsLocal(target))
                {
                    target.data.SetPetrify(clamped);
                }
                else
                {
                    var view = ((MonoBehaviourPun)target.data).photonView;
                    if (view != null)
                        view.RPC("RPC_SyncPetrify", RpcTarget.All, new object[] { clamped });
                    else
                        target.data.SetPetrify(clamped);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SetPetrifyAmount: {e.Message}"); }
        }

        public static void AddArrowPrank(Character c, int count = 1, bool track = true)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;

                if (!IsLocal(target))
                {
                    AddAdminLog($"{SafeCharacterName(target)}: arrows can only be added by that player's client");
                    Plugin.Log?.LogWarning("[GameApi] add arrows: target owner only");
                    return;
                }

                var affl = target.refs != null ? target.refs.afflictions : null;
                if (affl == null) return;

                int n = Mathf.Clamp(count, 1, 12);
                for (int i = 0; i < n; i++)
                {
                    Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.35f;
                    affl.AddArrow(SafePosition(target) + Vector3.up * 0.8f + offset, Vector3.up);
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] AddArrowPrank: {e.Message}"); }
        }

        public static void ClearArrows(Character c, bool track = true)
        {
            int removed = RemovePhysicalThorns(c ?? Character.localCharacter, true, track);
        }

        public static void ClearThorns(Character c, bool track = true)
        {
            int removed = RemovePhysicalThorns(c ?? Character.localCharacter, false, track, true);
        }

        private static int RemovePhysicalThorns(Character c, bool arrowsOnly, bool logFailures, bool thornsOnly = false)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return 0;
                if (!RequireHostForRemote(target, arrowsOnly ? "clear arrows" : "clear thorns")) return 0;

                var affl = target.refs != null ? target.refs.afflictions : null;
                var thorns = affl != null ? affl.physicalThorns : null;
                if (affl == null || thorns == null) return 0;

                int removed = 0;
                for (int i = 0; i < thorns.Count; i++)
                {
                    ThornOnMe thorn = thorns[i];
                    if (thorn == null || !thorn.stuckIn) continue;
                    if (arrowsOnly && !thorn.isArrow) continue;
                    if (thornsOnly && !thorn.isThorn) continue;

                    if (IsLocal(target))
                    {
                        affl.RemoveThorn(thorn, removedByPlayer: false);
                    }
                    else
                    {
                        var view = ((MonoBehaviourPun)affl).photonView;
                        if (view != null && view.Owner != null)
                            view.RPC("RemoveThornRPC", view.Owner, new object[] { i, false });
                        else if (view != null)
                            view.RPC("RemoveThornRPC", RpcTarget.All, new object[] { i, false });
                    }

                    removed++;
                }

                return removed;
            }
            catch (Exception e)
            {
                if (logFailures) Plugin.Log?.LogWarning($"[GameApi] RemovePhysicalThorns: {e.Message}");
                return 0;
            }
        }

        /// <summary>Prank: stuff a player's inventory full of the chosen item (capped at slot count).</summary>
        public static void PrankSpawnItems(Character c, int itemIndex, int count)
        {
            EnsureItemsLoaded();
            Character target = c ?? Character.localCharacter;
            if (target == null || itemIndex < 0 || itemIndex >= Items.Count) return;
            if (!RequireHostForRemote(target, "prank spawn items")) return;
            int slots = SlotCountFor(target);
            if (slots <= 0) return;
            int n = Mathf.Clamp(count, 1, slots);
            for (int s = 0; s < n; s++) SpawnToSlotFor(target, itemIndex, s);
        }

        public static int FindItemIndex(params string[] fragments)
        {
            EnsureItemsLoaded();
            if (fragments == null || fragments.Length == 0)
                return -1;

            for (int i = 0; i < ItemNames.Count; i++)
            {
                string haystack = ItemSearchText(i);
                if (string.IsNullOrWhiteSpace(haystack))
                    continue;

                for (int j = 0; j < fragments.Length; j++)
                {
                    string f = fragments[j];
                    if (!string.IsNullOrWhiteSpace(f) && haystack.Contains(f.ToLowerInvariant()))
                        return i;
                }
            }

            return -1;
        }

        public static void ScheduleKnifeFight()
        {
            if (!IsHost())
            {
                AddAdminLog(Localization.Current == Lang.Russian
                    ? "Поножовщину может запустить только хост."
                    : "Only the host can start Knife Fight.");
                return;
            }
            _knifeFightAt = Time.realtimeSinceStartup + 5f;
            _knifeFightCountdownSecond = -1;
            AddAdminLog(Localization.Current == Lang.Russian
                ? "Поножовщина начнётся через 5 секунд."
                : "Knife Fight starts in 5 seconds.");
        }

        public static void TickScheduledActions()
        {
            if (_knifeFightAt < 0f)
                return;

            float remaining = _knifeFightAt - Time.realtimeSinceStartup;
            if (remaining > 0f)
            {
                int second = Mathf.CeilToInt(remaining);
                if (second != _knifeFightCountdownSecond)
                {
                    _knifeFightCountdownSecond = second;
                    AdminNoticeText = Localization.Current == Lang.Russian
                        ? $"Поножовщина: {second}"
                        : $"Knife Fight: {second}";
                    AdminNoticeUntil = Time.realtimeSinceStartup + 1.2f;
                }
                return;
            }

            _knifeFightAt = -1f;
            _knifeFightCountdownSecond = -1;
            if (!IsHost())
                return;

            int itemIndex = FindRitualDaggerIndex();
            if (itemIndex < 0)
            {
                AddAdminLog(Localization.Current == Lang.Russian
                    ? "Поножовщина: ритуальный кинжал не найден в базе предметов."
                    : "Knife Fight: ritual dagger was not found in the item database.");
                return;
            }

            RefreshPlayers();
            int supplied = 0;
            for (int i = 0; i < PlayerChars.Count; i++)
            {
                Character player = PlayerChars[i];
                if (player != null && SlotCountFor(player) >= 3 && SpawnToSlotFor(player, itemIndex, 2))
                    supplied++;
            }
            AddAdminLog(Localization.Current == Lang.Russian
                ? $"Поножовщина началась: кинжал выдан игрокам ({supplied})."
                : $"Knife Fight started: daggers supplied ({supplied}).");
        }

        private static int FindRitualDaggerIndex()
        {
            EnsureItemsLoaded();
            for (int i = 0; i < Items.Count; i++)
            {
                string haystack = ItemSearchText(i);
                if (haystack.IndexOf("ritual", StringComparison.OrdinalIgnoreCase) >= 0
                    && (haystack.IndexOf("dagger", StringComparison.OrdinalIgnoreCase) >= 0
                        || haystack.IndexOf("knife", StringComparison.OrdinalIgnoreCase) >= 0))
                    return i;
            }
            return FindItemIndex("ritual dagger", "sacrificial dagger", "dagger");
        }

        public static int FindGoldenItemIndex()
        {
            EnsureItemsLoaded();
            for (int i = 0; i < Items.Count; i++)
            {
                try
                {
                    if (Items[i] != null && Items[i].itemTags.HasFlag(Item.ItemTags.GoldenIdol))
                        return i;
                }
                catch { }
            }
            return FindItemIndex("gold", "golden", "idol", "coin", "treasure", "valuable");
        }

        public static int FindWebItemIndex()
        {
            int direct = FindItemIndex("web", "spider", "silk", "cocoon");
            if (direct >= 0) return direct;
            return FindItemIndexByComponent("Spider", "Web", "Silk");
        }

        public static int FindBackpackItemIndex()
        {
            EnsureItemsLoaded();
            for (int i = 0; i < Items.Count; i++)
            {
                try
                {
                    if (Items[i] is Backpack backpack && backpack.backpackType == BackpackSlot.BackpackType.Backpack)
                        return i;
                }
                catch { }
            }
            int direct = FindItemIndex("backpack", "rucksack");
            if (direct >= 0) return direct;
            return FindItemIndexByComponent("Backpack", "Pack");
        }

        private static List<int> FindCampfireBackpackItemIndices()
        {
            EnsureItemsLoaded();
            var indices = new List<int>();
            for (int i = 0; i < Items.Count; i++)
            {
                try
                {
                    if (!(Items[i] is Backpack backpack))
                        continue;
                    if (backpack.backpackType == BackpackSlot.BackpackType.Backpack ||
                        backpack.backpackType == BackpackSlot.BackpackType.Fannypack)
                        indices.Add(i);
                }
                catch { }
            }
            if (indices.Count == 0)
            {
                int backpack = FindBackpackItemIndex();
                if (backpack >= 0)
                    indices.Add(backpack);
            }
            return indices;
        }

        private static int FindHotDogItemIndex()
        {
            int direct = FindItemIndex("hot dog", "hotdog", "hot-dog", "sausage");
            return direct >= 0 ? direct : FindItemIndexByComponent("Hotdog", "HotDog", "Sausage");
        }

        private static int FindMarshmallowItemIndex()
        {
            int direct = FindItemIndex("marshmallow", "mallow");
            return direct >= 0 ? direct : FindItemIndexByComponent("Marshmallow", "Mallow");
        }

        public static int FindRandomFoodItemIndex()
        {
            EnsureItemsLoaded();
            var candidates = new List<int>();
            for (int i = 0; i < Items.Count; i++)
            {
                try
                {
                    if (!CanPocketItem(i))
                        continue;

                    Item item = Items[i];
                    bool tagged = item.itemTags.HasFlag(Item.ItemTags.PackagedFood)
                        || item.itemTags.HasFlag(Item.ItemTags.Berry)
                        || item.itemTags.HasFlag(Item.ItemTags.Mushroom)
                        || item.itemTags.HasFlag(Item.ItemTags.GourmandRequirement);
                    string haystack = ItemSearchText(i);
                    bool named = haystack.Contains("food") || haystack.Contains("berry") || haystack.Contains("mushroom")
                        || haystack.Contains("ration") || haystack.Contains("trail") || haystack.Contains("cookie")
                        || haystack.Contains("lunch") || haystack.Contains("egg") || haystack.Contains("fruit");
                    if (tagged || named)
                        candidates.Add(i);
                }
                catch { }
            }

            if (candidates.Count == 0)
                return -1;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public static void SpawnItemSet(Character c, string setId)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (target == null) return;
                if (!RequireHostForRemote(target, "spawn item set")) return;

                string id = (setId ?? "").Trim().ToLowerInvariant();
                int slots = SlotCountFor(target);
                if (slots <= 0) return;

                var picks = new List<int>();
                if (id == "food")
                {
                    for (int i = 0; i < Mathf.Min(slots, 4); i++)
                        picks.Add(FindRandomFoodItemIndex());
                }
                else if (id == "rescue")
                {
                    picks.Add(FindItemIndex("rope", "climbing rope"));
                    picks.Add(FindItemIndex("bandage", "medkit", "medicine", "cure", "heal"));
                    picks.Add(FindItemIndex("flare", "torch", "lantern", "light"));
                }
                else if (id == "light")
                {
                    picks.Add(FindItemIndex("lantern", "torch", "flare", "lamp"));
                    picks.Add(FindItemIndex("battery", "fuel", "oil"));
                    picks.Add(FindItemIndex("flare", "torch"));
                }
                else if (id == "mobility")
                {
                    int back = FindItemIndex("jetpack", "rocketpack", "fannypack", "backpack", "rucksack");
                    if (back >= 0 && CanBackSlotItem(back))
                        SpawnToBackSlotFor(target, back);
                    picks.Add(FindItemIndex("rope", "climbing rope"));
                    picks.Add(FindItemIndex("piton", "hook", "grappling"));
                }
                else
                {
                    return;
                }

                int affected = 0;
                int slot = 0;
                for (int i = 0; i < picks.Count && slot < slots; i++)
                {
                    int item = picks[i];
                    if (item < 0)
                        continue;
                    if (SpawnToSlotFor(target, item, slot))
                        affected++;
                    slot++;
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnItemSet: {e.Message}"); }
        }

        public static void GiveEveryoneFood(int itemsPerPlayer = 2)
        {
            try
            {
                RefreshPlayers();
                int count = Mathf.Clamp(itemsPerPlayer, 1, 6);
                int affected = 0;
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    Character target = PlayerChars[i];
                    if (target == null) continue;
                    int slots = SlotCountFor(target);
                    if (slots <= 0) continue;

                    for (int slot = 0; slot < Mathf.Min(count, slots); slot++)
                    {
                        int food = FindRandomFoodItemIndex();
                        if (food >= 0 && SpawnToSlotFor(target, food, slot))
                            affected++;
                    }
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] GiveEveryoneFood: {e.Message}"); }
        }

        public static void GiveEveryoneBackpacks()
        {
            try
            {
                int backpack = FindBackpackItemIndex();
                if (backpack < 0)
                {
                    Plugin.Log?.LogWarning("[GameApi] Backpack item not found.");
                    return;
                }

                RefreshPlayers();
                int affected = 0;
                for (int i = 0; i < PlayerChars.Count; i++)
                {
                    Character target = PlayerChars[i];
                    if (target == null || target.player == null || target.player.backpackSlot == null)
                        continue;
                    if (!RequireHostForRemote(target, "give backpack")) continue;

                    SetSlotItem(target.player.backpackSlot, Items[backpack]);
                    SyncInventory(target.player, RpcTarget.All);
                    affected++;
                }
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] GiveEveryoneBackpacks: {e.Message}"); }
        }

        public static void SpawnCampfireFoodForLobby()
        {
            try
            {
                if (!RequireHostAction("spawn campfire food")) return;
                int hotDog = FindHotDogItemIndex();
                int marshmallow = FindMarshmallowItemIndex();
                if (hotDog < 0 && marshmallow < 0)
                {
                    Plugin.Log?.LogWarning("[GameApi] Campfire food items not found.");
                    return;
                }

                int playerCount = Mathf.Max(1, LobbyPlayerCount());
                Vector3 center = FindCampfireSpawnCenter();
                int spawned = 0;
                for (int i = 0; i < playerCount; i++)
                {
                    int itemIndex = PickCampfireFoodIndex(hotDog, marshmallow);
                    if (itemIndex >= 0 && SpawnWorldItem(itemIndex, SpreadAround(center, i, playerCount, 1.25f), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)))
                        spawned++;
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnCampfireFoodForLobby: {e.Message}"); }
        }

        public static void SpawnCampfireBackpacksForLobby()
        {
            try
            {
                if (!RequireHostAction("spawn campfire backpacks")) return;
                List<int> backpackIndices = FindCampfireBackpackItemIndices();
                if (backpackIndices.Count == 0)
                {
                    Plugin.Log?.LogWarning("[GameApi] Campfire backpack items not found.");
                    return;
                }

                int playerCount = Mathf.Max(1, LobbyPlayerCount());
                int count = Mathf.Clamp(Mathf.CeilToInt(playerCount / 4f), 1, 8);
                Vector3 center = FindCampfireSpawnCenter() + new Vector3(0f, 0.15f, 0f);
                int spawned = 0;
                for (int i = 0; i < count; i++)
                {
                    int itemIndex = backpackIndices[UnityEngine.Random.Range(0, backpackIndices.Count)];
                    if (SpawnWorldItem(itemIndex, SpreadAround(center, i, count, 1.75f), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f)))
                        spawned++;
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] SpawnCampfireBackpacksForLobby: {e.Message}"); }
        }

        public static void SpawnCampfireSuppliesForLobby()
        {
            SpawnCampfireFoodForLobby();
            SpawnCampfireBackpacksForLobby();
        }

        private static int PickCampfireFoodIndex(int hotDog, int marshmallow)
        {
            if (hotDog >= 0 && marshmallow >= 0)
                return UnityEngine.Random.value < 0.5f ? hotDog : marshmallow;
            return hotDog >= 0 ? hotDog : marshmallow;
        }

        private static int RandomBackSlotItemIndex()
        {
            EnsureItemsLoaded();
            var candidates = new List<int>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (CanBackSlotItem(i))
                    candidates.Add(i);
            }
            if (candidates.Count == 0)
                return -1;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private static int LobbyPlayerCount()
        {
            try
            {
                RefreshPlayers();
                if (PlayerChars.Count > 0)
                    return PlayerChars.Count;
            }
            catch { }

            try
            {
                if (PhotonNetwork.CurrentRoom != null)
                    return PhotonNetwork.CurrentRoom.PlayerCount;
            }
            catch { }

            return 1;
        }

        private static Vector3 FindCampfireSpawnCenter()
        {
            try
            {
                Vector3 reference = Character.localCharacter != null ? SafePosition(Character.localCharacter) : Vector3.zero;
                Campfire[] campfires = UnityEngine.Object.FindObjectsByType<Campfire>(FindObjectsSortMode.None);
                Campfire best = null;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < campfires.Length; i++)
                {
                    Campfire campfire = campfires[i];
                    if (campfire == null) continue;
                    Vector3 position = ((Component)campfire).transform.position;
                    float distance = (position - reference).sqrMagnitude;
                    if (best == null || distance < bestDistance)
                    {
                        best = campfire;
                        bestDistance = distance;
                    }
                }
                if (best != null)
                    return ((Component)best).transform.position + Vector3.up * 0.45f;
            }
            catch { }

            try
            {
                if (Character.localCharacter != null)
                    return SafePosition(Character.localCharacter) + ((Component)Character.localCharacter).transform.forward * 2f + Vector3.up;
            }
            catch { }
            return Vector3.up;
        }

        private static Vector3 SpreadAround(Vector3 center, int index, int count, float radius)
        {
            float angle = count <= 1 ? UnityEngine.Random.Range(0f, Mathf.PI * 2f) : (Mathf.PI * 2f * index / count);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            offset += UnityEngine.Random.insideUnitSphere * 0.25f;
            offset.y = Mathf.Abs(offset.y) * 0.25f;
            return center + offset;
        }

        private static bool SpawnWorldItem(int itemIndex, Vector3 position, Quaternion rotation)
        {
            try
            {
                if (itemIndex < 0 || itemIndex >= Items.Count || Items[itemIndex] == null || Items[itemIndex].gameObject == null)
                    return false;
                if (!HasResourcePrefab(Items[itemIndex]))
                    return false;
                if (!PhotonNetwork.IsMasterClient)
                {
                    Plugin.Log?.LogWarning("[GameApi] World item spawn requires host.");
                    return false;
                }

                string prefabName = Items[itemIndex].gameObject.name;
                GameObject spawned = PhotonNetwork.InstantiateItemRoom(prefabName, position, rotation);
                if (spawned == null)
                    spawned = PhotonNetwork.Instantiate("0_Items/" + prefabName, position, rotation, 0);
                return spawned != null;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning($"[GameApi] SpawnWorldItem: {RootExceptionMessage(e)}");
                return false;
            }
        }

        public static void FillInventoryWithGold(Character c)
        {
            if (!RequireHostForRemote(c ?? Character.localCharacter, "fill inventory with gold")) return;
            int gold = FindGoldenItemIndex();
            if (gold >= 0) FillInventoryWith(c, gold);
            else Plugin.Log?.LogWarning("[GameApi] Gold item not found.");
        }

        public static void FillInventoryWithWebs(Character c)
        {
            if (!RequireHostForRemote(c ?? Character.localCharacter, "fill inventory with webs")) return;
            int web = FindWebItemIndex();
            if (web >= 0)
            {
                FillInventoryWith(c, web);
                return;
            }

            Character target = c ?? Character.localCharacter;
            SetStatusAtLeast(target, 11, 0.18f);
        }

        public static void TieBalloons(Character c, int count)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (!RequireHostForRemote(target, "tie balloons")) return;
                var balloons = target?.refs?.balloons;
                if (balloons == null) return;

                int applied = Mathf.Clamp(count, 1, 8);
                int colorCount = balloons.balloonColors != null && balloons.balloonColors.Length > 0 ? balloons.balloonColors.Length : 1;
                for (int i = 0; i < applied; i++)
                    balloons.TieNewBalloon(UnityEngine.Random.Range(0, colorCount));

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] TieBalloons: {e.Message}"); }
        }

        public static void DropAllInventory(Character c, bool track = true)
        {
            try
            {
                Character target = c ?? Character.localCharacter;
                if (!RequireHostForRemote(target, "drop inventory")) return;
                if (target == null || target.refs?.items == null || target.player == null) return;

                Vector3 pos = SafePosition(target) + Vector3.up * 0.8f;
                int dropped = 0;
                int slots = SlotCountFor(target);
                for (int i = 0; i < slots; i++)
                {
                    ItemSlot slot = target.player.itemSlots != null && i < target.player.itemSlots.Length ? target.player.itemSlots[i] : null;
                    if (slot == null || slot.IsEmpty())
                        continue;
                    if (!CanDropSlot(slot))
                    {
                        ClearSlotFor(target, i);
                        continue;
                    }
                    DropSlotByRpc(target, (byte)i, pos + Vector3.up * (0.35f * dropped));
                    dropped++;
                }

                if (target.player.backpackSlot != null && !target.player.backpackSlot.IsEmpty())
                {
                    if (CanDropSlot(target.player.backpackSlot))
                    {
                        DropSlotByRpc(target, 3, pos + Vector3.up * (0.35f * dropped));
                        dropped++;
                    }
                    else
                    {
                        target.player.backpackSlot.EmptyOut();
                        SyncTargetInventory(target);
                    }
                }

                if (target.player.tempFullSlot != null && !target.player.tempFullSlot.IsEmpty())
                {
                    if (CanDropSlot(target.player.tempFullSlot))
                    {
                        DropSlotByRpc(target, 250, pos + Vector3.up * (0.35f * dropped));
                        dropped++;
                    }
                    else
                    {
                        target.player.tempFullSlot.EmptyOut();
                        SyncTargetInventory(target);
                    }
                }

            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] DropAllInventory: {e.Message}"); }
        }

        private static bool CanDropSlot(ItemSlot slot)
        {
            try
            {
                if (slot == null || slot.IsEmpty())
                    return false;
                Item item = SlotPrefab(slot);
                if (item != null && item.UIData != null && !item.UIData.canDrop)
                    return false;
                return HasResourcePrefabName(slot.GetPrefabName());
            }
            catch
            {
                return false;
            }
        }

        private static void DropSlotByRpc(Character target, byte slot, Vector3 spawnPosition)
        {
            try
            {
                ((MonoBehaviourPun)target.refs.items).photonView.RPC("DropItemFromSlotRPC", RpcTarget.All, new object[] { slot, spawnPosition });
            }
            catch (Exception e) { Plugin.Log?.LogWarning($"[GameApi] DropSlotByRpc {slot}: {e.Message}"); }
        }

        private static string ItemSearchText(int index)
        {
            try
            {
                var item = index >= 0 && index < Items.Count ? Items[index] : null;
                string name = index >= 0 && index < ItemNames.Count ? ItemNames[index] : "";
                if (item == null) return (name ?? "").ToLowerInvariant();
                return string.Join(" ", new[]
                {
                    name,
                    item.name,
                    item.gameObject != null ? item.gameObject.name : "",
                    item.UIData != null ? item.UIData.itemName : "",
                    item.itemTags.ToString(),
                    item.GetType().Name,
                }).ToLowerInvariant();
            }
            catch { return ""; }
        }

        private static int FindItemIndexByComponent(params string[] fragments)
        {
            EnsureItemsLoaded();
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (item == null) continue;
                try
                {
                    var components = item.GetComponentsInChildren<Component>(true);
                    for (int c = 0; c < components.Length; c++)
                    {
                        string name = components[c] != null ? components[c].GetType().Name.ToLowerInvariant() : "";
                        for (int f = 0; f < fragments.Length; f++)
                            if (!string.IsNullOrWhiteSpace(fragments[f]) && name.Contains(fragments[f].ToLowerInvariant()))
                                return i;
                    }
                }
                catch { }
            }
            return -1;
        }

        public static void FillInventoryWith(Character c, int itemIndex)
        {
            EnsureItemsLoaded();
            if (itemIndex < 0 || itemIndex >= Items.Count)
                return;

            Character target = c ?? Character.localCharacter;
            if (!RequireHostForRemote(target, "fill inventory")) return;
            int slots = target != null ? SlotCountFor(target) : SlotCount();
            if (CanPocketItem(itemIndex))
            {
                for (int i = 0; i < slots; i++)
                    SpawnToSlotFor(target, itemIndex, i);
            }
            int backpackSlots = BackpackSlotCount(target);
            if (CanBackpackItem(itemIndex))
            {
                for (int i = 0; i < backpackSlots; i++)
                    SpawnToBackpackSlotFor(target, itemIndex, i);
            }
            if (CanBackSlotItem(itemIndex))
                SpawnToBackSlotFor(target, itemIndex);
            if (IsScoutAmuletItem(Items[itemIndex]))
                SpawnToTempSlotFor(target, itemIndex);
        }

        public static void FillInventoryWithRandom(Character c)
        {
            EnsureItemsLoaded();
            if (Items.Count == 0) return;

            Character target = c ?? Character.localCharacter;
            if (!RequireHostForRemote(target, "fill random inventory")) return;
            int slots = target != null ? SlotCountFor(target) : SlotCount();
            if (slots <= 0) return;

            for (int i = 0; i < slots; i++)
            {
                int itemIndex = RandomItemIndexForSlot(false);
                if (itemIndex >= 0) SpawnToSlotFor(target, itemIndex, i);
            }
            int backpackSlots = BackpackSlotCount(target);
            for (int i = 0; i < backpackSlots; i++)
            {
                int itemIndex = RandomItemIndexForSlot(true);
                if (itemIndex >= 0) SpawnToBackpackSlotFor(target, itemIndex, i);
            }
            int backItem = RandomBackSlotItemIndex();
            if (backItem >= 0) SpawnToBackSlotFor(target, backItem);
        }

        public static void ClearInventory(Character c)
        {
            Character target = c ?? Character.localCharacter;
            if (!RequireHostForRemote(target, "clear inventory")) return;
            int slots = target != null ? SlotCountFor(target) : SlotCount();
            if (slots <= 0) return;

            for (int i = 0; i < slots; i++)
                ClearSlotFor(target, i);
            int backpackSlots = BackpackSlotCount(target);
            for (int i = 0; i < backpackSlots; i++)
                ClearBackpackSlotFor(target, i);
            ClearTempSlotFor(target);
            ClearBackSlotFor(target);
        }

        public static string PlayerDisplayName(Character c)
        {
            return SafeCharacterName(c);
        }

        public static string PlayerModVersion(Character c)
        {
            try
            {
                var owner = OwnerOf(c);
                if (owner?.CustomProperties == null)
                    return "";
                object value;
                return owner.CustomProperties.TryGetValue(PlayerMeta.VersionKey, out value) ? value as string ?? "" : "";
            }
            catch { return ""; }
        }

        public static float DistanceToLocal(Character c)
        {
            try
            {
                Character local = Character.localCharacter;
                if (local == null || c == null)
                    return -1f;
                return Vector3.Distance(SafePosition(local), SafePosition(c));
            }
            catch { return -1f; }
        }

        public static bool IsDead(Character c)
        {
            try { return c != null && c.data != null && c.data.dead; }
            catch { return false; }
        }

        public static string PlayerDetails(Character c, bool russian)
        {
            if (c == null)
                return russian ? "Игрок не выбран." : "No player selected.";

            var lines = new List<string>();
            string name = SafeCharacterName(c);
            bool dead = IsDead(c);
            bool local = IsLocal(c);
            lines.Add(name + (local ? (russian ? " (ты)" : " (you)") : ""));
            lines.Add(dead ? (russian ? "Состояние: мёртв" : "State: dead") : (russian ? "Состояние: жив" : "State: alive"));

            float distance = DistanceToLocal(c);
            if (!local && distance >= 0f)
                lines.Add((russian ? "Дистанция: " : "Distance: ") + distance.ToString("0") + " m");

            string pmx = PlayerModVersion(c);
            lines.Add((russian ? "PEAK-MX: " : "PEAK-MX: ") + (string.IsNullOrWhiteSpace(pmx) ? (russian ? "нет данных" : "no data") : "v" + pmx));

            try
            {
                if (c.data != null)
                {
                    lines.Add((russian ? "Стамина: " : "Stamina: ") +
                        Mathf.RoundToInt(Mathf.Clamp01(c.data.currentStamina) * 100f) + "% + " +
                        Mathf.RoundToInt(Mathf.Max(0f, c.data.extraStamina) * 100f) + "%");
                }
            }
            catch { }

            string statuses = StatusSummary(c, russian);
            if (!string.IsNullOrWhiteSpace(statuses))
                lines.Add((russian ? "Недуги: " : "Afflictions: ") + statuses);

            int slots = SlotCountFor(c);
            if (slots > 0)
            {
                lines.Add(russian ? "Инвентарь:" : "Inventory:");
                for (int i = 0; i < slots; i++)
                    lines.Add("  " + (i + 1) + ". " + DescribeSlot(c, i));
                lines.Add("  " + (russian ? "Спина: " : "Back: ") + DescribeBackSlot(c));
                lines.Add("  " + (russian ? "Амулет/временный: " : "Amulet/temp: ") + DescribeTempSlot(c));
            }

            int backpackSlots = BackpackSlotCount(c);
            if (backpackSlots > 0)
            {
                lines.Add(russian ? "Рюкзак:" : "Backpack:");
                for (int i = 0; i < backpackSlots; i++)
                    lines.Add("  B" + (i + 1) + ". " + DescribeBackpackSlot(c, i));
            }
            else
            {
                lines.Add(russian ? "Рюкзак: нет / не синхронизирован" : "Backpack: none / not synced");
            }

            if (IsMuted(c)) lines.Add(russian ? "Мут: включён" : "Mute: on");
            if (IsFrozen(c)) lines.Add(russian ? "Фриз: включён" : "Freeze: on");
            if (IsInventoryLocked(c)) lines.Add(russian ? "Инвентарь: запрещён" : "Inventory lock: on");
            if (IsSessionBanned(c)) lines.Add(russian ? "Бан: сессионный" : "Ban: session");

            return string.Join("\n", lines.ToArray());
        }

        private static string StatusSummary(Character c, bool russian)
        {
            try
            {
                var a = c != null && c.refs != null ? c.refs.afflictions : null;
                if (a == null) return "";
                var parts = new List<string>();
                string[] names = russian ? StatusRu : StatusEn;
                for (int i = 0; i < StatusCount(); i++)
                {
                    float value = CurrentStatusValue(c, (CharacterAfflictions.STATUSTYPE)i);
                    if (value < 0.01f) continue;
                    string label = i >= 0 && i < names.Length ? names[i] : i.ToString();
                    parts.Add(label + " " + Mathf.RoundToInt(value * 100f) + "%");
                }
                return string.Join(", ", parts.ToArray());
            }
            catch { return ""; }
        }

        private static string SafeItemName(int itemIndex)
        {
            try { return itemIndex >= 0 && itemIndex < ItemNames.Count ? ItemNames[itemIndex] : itemIndex.ToString(); }
            catch { return itemIndex.ToString(); }
        }

        private static string SafeCharacterName(Character c)
        {
            try
            {
                if (c == null) return "Unknown";
                if (!string.IsNullOrWhiteSpace(c.characterName)) return c.characterName;
                var owner = OwnerOf(c)?.NickName;
                return string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner;
            }
            catch { return "Unknown"; }
        }

        private static string SafeLuggageName(Luggage lug)
        {
            try { return lug != null && !string.IsNullOrWhiteSpace(lug.displayName) ? lug.displayName : "Container"; }
            catch { return "Container"; }
        }
    }
}
