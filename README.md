# PEAK-MX

An original quality-of-life and utility mod for **PEAK**, with an ImGui-powered overlay.

> Unofficial, fan-made mod. Not affiliated with or endorsed by the developers/publisher of PEAK.
> Built on third-party libraries (BepInEx, HarmonyLib, DearImGuiInjection, ImGui.NET, …) which
> remain under their own respective licenses.

## Author

**maxkir041**

- GitHub: https://github.com/maxkir041/PEAK-MX
- Steam: https://steamcommunity.com/id/everyng/
- Telegram: https://t.me/maxkir041
- Playground: https://users.playground.ru/7293247/
- ❤️ Donate: https://www.donationalerts.com/r/maxkir041

## Features

Open the menu with **Insert** (configurable).

- **Character:** speed, jump (climb speeds — WIP)
- **Cheats:** god mode, infinite stamina, no fall damage, no weight, lock status (teleport-to-ping, fly — WIP)
- **Inventory:** slot recharge (WIP)
- **World:** expedition-time override (WIP)
- **Badges / Cosmetics:** unlock helpers (WIP)
- **Localization:** English + Russian (more languages registered, translations in progress)

## Build

Requires the **.NET SDK** (the runtime alone is not enough):

```powershell
winget install Microsoft.DotNet.SDK.8
dotnet build -c Release
```

The output `PEAK-MX.dll` goes into `BepInEx/plugins/`. Reference paths to the game and
BepInEx are set at the top of `PEAK-MX.csproj` — adjust them if your install differs.

## Anonymous install counter

PEAK-MX can show an install counter in the **About** tab. To support it, the mod sends a
**single anonymous ping** the first time it runs:

- The only data sent is a **random GUID** generated locally on your machine, plus a "new install" flag.
- **No** Steam ID, username, IP-linked data, or any other personal information is ever collected or sent.
- It can be disabled in the About tab or via `Telemetry.AllowAnonymousStats = false` in the config.
- The endpoint is empty by default (`Stats.Endpoint`), so the counter is **off** until you set your own server URL.

### Minimal server example (Cloudflare Worker + KV)

```js
export default {
  async fetch(req, env) {
    const url = new URL(req.url);
    const isNew = url.searchParams.get("new") === "1";
    let total = parseInt((await env.STATS.get("installs")) || "0", 10);
    if (isNew) { total++; await env.STATS.put("installs", String(total)); }
    return new Response(String(total), { headers: { "content-type": "text/plain" } });
  }
}
```

Then set `Stats.Endpoint` in `src/Stats.cs` to your Worker URL and rebuild.

## License

PEAK-MX is released under the MIT License — see [LICENSE](LICENSE).
Third-party dependencies keep their own licenses.
