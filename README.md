# PEAK-MX

PEAK-MX is a utility and admin menu mod for **PEAK** by **maxkir041**.

It includes 140+ in-game functions: movement tweaks, stamina and status controls, inventory editing, player administration, achievements, cosmetics, world tools, fun actions, larger-lobby controls, and host protection.

## Links

- GitHub: https://github.com/maxkir041/PEAK-MX
- Nexus Mods: https://www.nexusmods.com/peak/mods/179
- Steam: https://steamcommunity.com/id/everyng/
- Telegram: https://t.me/maxkir041
- Playground: https://users.playground.ru/7293247/
- Donate: https://www.donationalerts.com/r/maxkir041

## Installation

1. Install BepInEx for PEAK.
2. Download the latest `PEAK-MX-x.x.x.zip` from GitHub Releases.
3. Extract `PEAK-MX.dll` into:

   ```text
   PEAK/BepInEx/plugins
   ```

4. Launch the game.
5. Open the PEAK-MX menu with **Insert** by default.

The menu hotkey can be changed inside the About tab. The menu can also be closed with **Esc** or the close button in the header.

## Features

- Movement speed, jump height, unlimited jumps, fly mode, noclip, long interaction, and cinematic free camera.
- Stamina, extra stamina, lantern fuel, item uses, status effects, injuries, hunger, cold, poison, curse, drowsiness, heat, and weight controls.
- Inventory editing, backpack support, item spawning, slot clearing, item charging, gold fill, web fill, and random item fill.
- Player tools: teleport, pull, spawn return, revive, kill, freeze, heal, mute, kick, session ban, and inventory lock.
- Host admin protection with warning-only mode, movement checks, session ban list, and action log.
- Larger-lobby controls: configurable player limit, host-only airport kiosk start, join/leave logging, voice routing fix, and player UI fixes.
- World tools: run timer, time-of-day presets, game speed, ping hand size, nearby containers, and finish controls.
- Achievements and cosmetics unlock/reset tools with badge icons and cosmetic previews.
- Interface supports 14 languages: English, Russian, Ukrainian, Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, Brazilian Portuguese, German, French, Italian, Polish, and Turkish.

## Build Variants

- GitHub/Nexus build: normal release build with optional anonymous telemetry for install statistics and maintenance.
- Thunderstore build: compiled with analytics disabled.

## Important

Use admin features only in your own lobby or where you are allowed to manage players. Some actions depend on lobby state, player state, and what the game currently syncs.

If something breaks, test with a clean profile that only has BepInEx and PEAK-MX enabled before reporting issues.

## Build

Requires the .NET SDK.

```powershell
dotnet build -c Release
```

The build copies `PEAK-MX.dll` into the configured PEAK BepInEx plugins folder.


If your PEAK install path differs, edit the paths at the top of `PEAK-MX.csproj`.

## License

All rights reserved. Copyright (c) 2026 maxkir041.
