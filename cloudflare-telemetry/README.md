# PEAK-MX Cloudflare Telemetry

Cloudflare Worker that accepts PEAK-MX telemetry and stores raw payloads in R2 while keeping lightweight indexes in D1.

What it supports right now:

- `GET /api/ping`
- `GET /api/setnick`
- `POST /api/diag`
- `POST /api/crash`
- `POST /api/lobby`
- `GET /api/event`
- `POST /api/event`
- `POST /api/upload`
- `POST /api/actions`
- `POST /api/ingest`

The first six routes are compatible with the mod code that already exists in this repo. The last three routes are the future-proof ingress for "every action" analytics, including raw JSON, NDJSON, or binary upload batches.

## Storage layout

- D1:
  - `installs` - unique installs and latest metadata
  - `objects` - index of stored raw objects
  - `daily_counters` - simple daily route counters
- R2:
  - raw request bodies grouped by kind and day

## Setup

1. Install dependencies:

```bash
npm install
```

2. Create the D1 database:

```bash
npx wrangler d1 create peak-mx-telemetry
```

3. Create the R2 bucket:

```bash
npx wrangler r2 bucket create peak-mx-telemetry-raw
```

4. Put the returned D1 `database_id` into [wrangler.jsonc](./wrangler.jsonc).

5. Apply the migration:

```bash
npx wrangler d1 migrations apply peak-mx-telemetry --local
npx wrangler d1 migrations apply peak-mx-telemetry --remote
```

6. Run locally:

```bash
npm run dev
```

7. Set secrets used by protected admin routes:

```bash
npx wrangler secret put STATS_TOKEN
```

`GET /api/stats` accepts the token as `Authorization: Bearer ...`, `x-admin-token`, or `?admin_token=...`.

8. Deploy:

```bash
npm run deploy
```

## Notes

- `GET /api/ping` returns `{ ok, counted, installs }`, matching the current mod expectation.
- `GET /api/summary` is public and returns aggregate-only counters/charts.
- `GET /api/stats` is private and requires `STATS_TOKEN` or `ADMIN_TOKEN`; it can include recent objects, network metadata, and diagnostics.
- The mod ships with a public client key, so analytics works for every user without a private secret file. You can still set `PING_TOKEN` in the Worker if you want an extra override.
- `POST /api/event` accepts either a single JSON event, `{ events: [...] }`, or a raw uploaded payload.
- `POST /api/upload` stores the request body as-is in R2. Use headers like `x-install-id` and `x-event-kind` when sending bulk action files.
- The Telegram bot remains the preferred place for private reporting and client lookups.
