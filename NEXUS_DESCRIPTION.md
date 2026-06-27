# PEAK-MX 1.0.12

PEAK-MX is a cheat mod and admin menu for PEAK with 140+ functions: movement tweaks, stamina and status controls, inventory editing, player administration, achievements, cosmetics, world tools, fun actions, larger-lobby controls, and host protection.

## Installation

1. Install BepInEx into your PEAK game folder.
2. Download `PEAK-MX-1.0.12.zip`.
3. Extract `PEAK-MX.dll` into:

   ```text
   PEAK/BepInEx/plugins
   ```

4. If the `plugins` folder does not exist, create it.
5. Launch the game.
6. Open the PEAK-MX menu with **Insert**.

The menu hotkey can now be changed inside the About tab.

## Features

- 140+ in-game functions.
- Movement speed, jump height, unlimited jumps, fly mode, noclip, long interaction, and cinematic free camera.
- Stamina, extra stamina, lantern fuel, item uses, status effects, injuries, hunger, cold, poison, curse, drowsiness, heat, and weight controls.
- Inventory editing, backpack support, item spawning, slot clearing, item charging, gold fill, web fill, and random item fill.
- Player tools: teleport, pull, spawn return, revive, kill, freeze, heal, mute, kick, session ban, and inventory lock.
- Host admin protection with warning-only mode, movement checks, session ban list, and action log.
- Larger-lobby controls: configurable player limit, host-only airport kiosk start, join/leave logging, voice routing fix, and player UI fixes.
- World tools: run timer, time-of-day presets, game speed, ping hand size, nearby containers, finish controls, and cinematic free camera.
- Achievements and cosmetics unlock/reset tools with badge icons and cosmetic previews.
- Fun PEAK-themed actions like balloons, status effects, item chaos, and inventory floods.
- Interface supports 14 languages: English, Russian, Ukrainian, Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, Brazilian Portuguese, German, French, Italian, Polish, and Turkish.

## Version 1.0.12

- Added built-in larger-lobby controls in the admin menu.
- Added configurable player limit up to 30.
- Added host-only airport kiosk start protection.
- Added join/leave logging for larger rooms.
- Added voice routing fix for rooms above the base player count.
- Added player name/waiting UI fixes for larger rooms.
- Added menu hotkey editing directly inside the menu.
- Improved backend privacy for statistics and admin reporting.
- Fixed SteamID display in the private Telegram admin bot for normal telemetry-enabled builds.
- Thunderstore package is built without analytics.

## Important

Use admin features only in your own lobby or where you are allowed to manage players. Some actions depend on lobby state, player state, and what the game currently syncs.

Recreate the lobby after changing the larger-lobby player limit. Photon usually keeps the old limit for an already-created room.

If something breaks, test with a clean profile that only has BepInEx and PEAK-MX enabled before reporting issues.

## Links

- GitHub: https://github.com/maxkir041/PEAK-MX
- Latest GitHub release: https://github.com/maxkir041/PEAK-MX/releases/tag/v1.0.12
- Nexus Mods: https://www.nexusmods.com/peak/mods/179

## License

All rights reserved. Copyright (c) 2026 maxkir041.
