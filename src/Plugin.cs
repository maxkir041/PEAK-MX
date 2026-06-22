using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PeakMX
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.maxkir041.peakmx";
        public const string Name = "PEAK-MX";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        private static bool _menuOpen;
        private Harmony _harmony;

        private CursorLockMode _savedLock;
        private bool _savedCursor;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Init(Config);
            Localization.Current = (Lang)ModConfig.Language.Value;
            Stats.Init();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Log.LogInfo($"{Name} v{Version} loaded.");
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();

        private void Update()
        {
            if (Input.GetKeyDown(ModConfig.MenuToggleKey.Value))
                ToggleMenu();

            if (_menuOpen)
            {
                // Keep the cursor usable while the overlay is open.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Features.Tick();
        }

        private void ToggleMenu()
        {
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
            }

            // Shown once per session, independently of whether the menu is open.
            Menu.DrawDonateNotice();

            if (_menuOpen)
                Menu.Draw();
        }
    }
}
