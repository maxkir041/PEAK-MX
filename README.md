# PEAK-MX

PEAK-MX is a utility, prank, and admin menu mod for **PEAK** by **maxkir041**.

It includes 150+ in-game functions: movement tweaks, stamina and status controls, inventory editing, player administration, achievements, cosmetics, world tools, fun actions, larger-lobby controls, and host protection.

## Installation

1. Install with Thunderstore/r2modman, or install BepInEx for PEAK manually.
2. If installing manually, extract `PEAK-MX.dll` into:

   ```text
   PEAK/BepInEx/plugins
   ```

3. Launch the game.
4. Open the PEAK-MX menu with **Insert** by default.

The menu hotkey can be changed inside the About tab. The menu can also be closed with **Esc** or the close button in the header.

## MX-AHG Companion Agent

MX-AHG is an optional lightweight client agent for protected public lobbies. The host runs PEAK-MX; joining players can install only `MX-AHG.dll` and do not need the full admin menu.

The agent provides a heartbeat and a host-requested report containing PEAK's BepInEx plugin DLL hashes, loaded plugin metadata, and Harmony patch owner IDs. The host can inspect exact differences, request another scan, temporarily trust a player, or enforce missing-agent, mismatched-pack, suspicious-name, and stale-heartbeat policies.

MX-AHG checks only PEAK/BepInEx mod state. It does not collect SteamID, hardware identifiers, personal files, unrelated processes, or browser data. It is designed to catch ordinary mod-based cheats and pack mismatches; it is not a kernel anti-cheat and cannot prove that a fully compromised client is clean.

## What's New in 1.1.2

- Added an Entities section in Inventory: spawn beetles, scorpions, frogs, Scoutmasters, mushroom zombies, bee swarms, and tornadoes.
- Added entity search, spawning in front of a selected player or at the crosshair, batches of up to five, and removal of entities created through this menu.
- Fixed player ESP distances getting stuck around 4 meters and boxes following the wrong position.
- World ESP now follows moving objects and updates their distance every frame.
- Visible ESP markers now stay at their actual screen positions, including near the screen edges.

Frogs require the host. Tornadoes require a map with a tornado route, and some creatures need the map's entity manager.

## Previous Updates (1.1.1)

- Redesigned the menu with a compact Simple mode, a full Advanced mode, and fast section navigation.
- Added a persistent Favorites page for frequently used actions and favorite item filters.
- Added predictable item delivery: PEAK-MX fills the first available main slot and otherwise spawns the item beside the player.
- Added one-click recharging for carried items and backpacks.
- Added petrification tools, arrow tools, and new prank actions.
- Added player ESP with distance labels.
- Added global voice hearing for your client.
- Added local nickname changing and clone appearance tools.
- Added random outfit/color tools, including rapid shuffle.
- Added biome teleport and world object finder for Gloom/Citadel routes, items, luggage, bell towers, campfires, statues, and similar objects.
- Added larger-lobby supply helpers for food and backpacks.
- Redesigned Anti-cheat into clear Action detection, Mod control, and Events pages with optional strict MX-AHG verification.
- Added the Knife Fight lobby prank with a five-second countdown and ritual daggers for every player.
- Added a compact affliction editor and persistent menu position and size.
- Improved movement detection and added warnings for unusually frequent mystical or cursed items.
- Admin protection settings now persist between launches.
- Admin protection events now show an on-screen warning instead of only writing to the menu log.
- Added configurable menu accent colors.
- Added mod update checking from GitHub Releases.
- Added offline acknowledgments for supporters and testers SaLaBiDay and Noado.
- Updated compatibility work for the latest PEAK changes.

## Features

- 150+ in-game functions.
- Simple and Advanced interface modes, fast section navigation, persistent favorite actions/items, and configurable hotkeys for frequent functions.
- Movement speed, jump height, unlimited jumps, fly mode, noclip, long interaction, cinematic free camera, and player ESP.
- Stamina, extra stamina, lantern fuel, item uses, status effects, injuries, hunger, cold, poison, curse, drowsiness, heat, and weight controls.
- Inventory editing, backpack support, item spawning, slot clearing, item charging, gold fill, web fill, and random item fill.
- Entity spawning with search, player/crosshair placement, batches of up to five, and removal of entities created through the menu: beetles, scorpions, frogs, Scoutmasters, mushroom zombies, bee swarms, and tornadoes. Some entities require the host or a compatible map.
- Player tools: teleport, pull, push, explode, spawn return, revive, kill, freeze, heal, mute, kick, session ban, and inventory lock.
- Prank tools for petrification, arrows, explosive chaos, inventory surprises, and other lobby fun.
- Local nickname, clone appearance, and random outfit/color tools.
- Host anti-cheat controls with DLL-set checks, mismatch/no-data kicks, client PEAK-MX tool lock-down, movement checks, session ban list, and action log.
- Optional MX-AHG client-agent reports with heartbeat, nonce-bound full scans, exact DLL/plugin/Harmony differences, temporary trust, and host enforcement controls.
- Larger-lobby controls: configurable player limit, host-only airport kiosk start, join/leave logging, voice routing fix, player UI fixes, food handout, and backpack handout.
- World tools: run timer, time-of-day presets, game speed, ping hand size, biome teleport, world object finder, nearby containers, and finish controls.
- Achievements and cosmetics unlock/reset tools with badge icons and cosmetic previews.
- GitHub release notifications, offline thanks, and custom accent colors in the About tab. Updates are installed manually.
- Interface supports 14 languages.

## Important

Use admin features only in your own lobby or where you are allowed to manage players.

If something breaks, test with a clean profile that only has BepInEx and PEAK-MX enabled before reporting issues.

## Links

- GitHub: https://github.com/maxkir041/PEAK-MX
- Nexus Mods: https://www.nexusmods.com/peak/mods/179
- Steam: https://steamcommunity.com/id/everyng/
- Telegram: https://t.me/maxkir041
- Donate: https://www.donationalerts.com/r/maxkir041

## License

MIT License. Copyright (c) 2026 maxkir041. See [LICENSE](LICENSE).
