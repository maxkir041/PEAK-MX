# PEAK-MX

PEAK-MX is a utility and admin menu mod for **PEAK** by **maxkir041**.

## Author

**maxkir041**

- GitHub: https://github.com/maxkir041/PEAK-MX
- Steam: https://steamcommunity.com/id/everyng/
- Telegram: https://t.me/maxkir041
- Playground: https://users.playground.ru/7293247/
- Donate: https://www.donationalerts.com/r/maxkir041

## Features

Open the menu with **Insert**. The menu also has a close button in the header and can be closed with **Esc**.

- Character modifiers: speed, jump, stamina, climbing, infinite jumps.
- Utility cheats: god mode, fly, noclip, long interaction, cinematic camera.
- Admin panel: player list, teleport, revive, kill, freeze, mute, inventory lock, session ban list, action log.
- Inventory tools: item icons, slot editing, backpack editing, fill/clear all slots, item recharge.
- World tools: run timer hold, time-of-day presets, game speed, ping hand size.
- Badges and cosmetics: lightweight badge list, cosmetic previews, unlock/lock controls for grantable cosmetics.
- Localization: multi-language interface with Russian as the default language.
- Telemetry: anonymous usage and diagnostic events are sent to PEAK-MX services for statistics and maintenance.

## Build

Requires the .NET SDK.

```powershell
dotnet build -c Release
```

The build copies `PEAK-MX.dll` into the configured PEAK BepInEx plugins folder.

If your PEAK install path differs, edit the paths at the top of `PEAK-MX.csproj`.

## License

PEAK-MX source code is released under the MIT License. See [LICENSE](LICENSE).
