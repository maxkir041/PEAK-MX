using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakMX
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxkir041.peakmx";
        public const string Name = "PEAK-MX";
        public const string Version = "1.1.2";

        internal static ManualLogSource Log;
        private static bool _menuOpen;
        internal static bool IsMenuOpen => _menuOpen;
        private float _lastMenuToggleAt = -999f;
        private bool _loadedLobbyItems;
        private bool _updateNoticeDismissed;
        private Harmony _harmony;

        private CursorLockMode _savedLock;
        private bool _savedCursor;

        private void Awake()
        {
            Log = Logger;


            ModConfig.Init(Config);
            Localization.Current = (Lang)ModConfig.Language.Value;
#if !DISABLE_UPDATE_CHECKER
            UpdateChecker.Init();
#endif
            VoiceControl.Init();
            MxAhgHost.Init();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnDestroy()
        {
            VoiceControl.Dispose();
            MxAhgHost.Dispose();
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {

            if (!Menu.IsCapturingHotkey &&
                (Input.GetKeyDown(ModConfig.MenuToggleKey.Value) || (_menuOpen && Input.GetKeyDown(KeyCode.Escape))))
                ToggleMenu();


            try
            {
                bool inRoom = PhotonNetwork.InRoom;
                if (inRoom && !_loadedLobbyItems)
                {
                    VoiceControl.Init();
                    GameApi.EnsureItemsLoaded();
                    GameApi.RefreshPlayers();
                    _loadedLobbyItems = true;
                }
                else if (!inRoom)
                {
                    _loadedLobbyItems = false;
                }
            }
            catch { /* Photon not ready */ }

            if (_menuOpen)
            {
                // Keep the cursor usable while the overlay is open.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            AntiCheat.Tick();
            MxAhgHost.Tick();
            GameApi.TickScheduledActions();
            Features.Tick();
            PlayerMeta.Tick();
            QuickActions.Tick(_menuOpen);
        }

        private void ToggleMenu()
        {
            if (Time.realtimeSinceStartup - _lastMenuToggleAt < 0.12f)
                return;
            _lastMenuToggleAt = Time.realtimeSinceStartup;

            _menuOpen = !_menuOpen;
            if (_menuOpen)
            {
                _savedLock = Cursor.lockState;
                _savedCursor = Cursor.visible;
            }
            else
            {
                Cursor.lockState = _savedLock;
                Cursor.visible = _savedCursor;
            }
        }

        private void LateUpdate()
        {
            // The game re-grabs the cursor every frame; reassert here (after its Update).
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnGUI()
        {
            // OnGUI runs after Update/LateUpdate, so this is the most reliable place to
            // keep the cursor free while the overlay is open.
            if (_menuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Event e = Event.current;
                if (!Menu.IsCapturingHotkey && e != null && e.type == EventType.KeyDown && e.keyCode == ModConfig.MenuToggleKey.Value)
                {
                    GUI.FocusControl(null);
                    ToggleMenu();
                    e.Use();
                    return;
                }
            }

            // Shown once per session, independently of whether the menu is open.
            Menu.DrawDonateNotice();
            if (UpdateChecker.UpdateAvailable && !_updateNoticeDismissed)
            {
                float width = Mathf.Min(360f, Screen.width - 24f);
                Rect notice = new Rect(Screen.width - width - 12f, 12f, width, 84f);
                GUI.Box(notice, "PEAK-MX " + UpdateChecker.LatestVersion);
                if (GUI.Button(new Rect(notice.x + 8f, notice.y + 30f, width - 48f, 38f), "GitHub Releases"))
                    Application.OpenURL("https://github.com/maxkir041/PEAK-MX/releases");
                if (GUI.Button(new Rect(notice.xMax - 34f, notice.y + 30f, 26f, 38f), "X"))
                    _updateNoticeDismissed = true;
            }
            Menu.DrawAdminNotice();
            PlayerEsp.Draw();
            ItemEsp.Draw();
            WorldEsp.Draw();

            if (_menuOpen)
            {
                Menu.Draw();
                if (Menu.ConsumeCloseRequest())
                    ToggleMenu();
            }
        }
    }
}
