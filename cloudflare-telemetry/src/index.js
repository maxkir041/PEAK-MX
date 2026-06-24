function json(data, status = 200, extraHeaders = {}) {
  return new Response(JSON.stringify(data, null, 2), {
    status,
    headers: {
      "content-type": "application/json; charset=utf-8",
      "cache-control": "no-store",
      ...extraHeaders,
    },
  });
}

function text(message, status = 200, extraHeaders = {}) {
  return new Response(message, {
    status,
    headers: {
      "content-type": "text/plain; charset=utf-8",
      "cache-control": "no-store",
      ...extraHeaders,
    },
  });
}

function corsHeaders() {
  return {
    "access-control-allow-origin": "*",
    "access-control-allow-methods": "GET,POST,OPTIONS",
    "access-control-allow-headers": "content-type,x-ping-token,x-install-id,x-event-kind",
  };
}

const PUBLIC_CLIENT_TOKEN = "peak-mx-public-v1";

function withCors(response) {
  const headers = new Headers(response.headers);
  for (const [key, value] of Object.entries(corsHeaders()))
    headers.set(key, value);
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers,
  });
}

function getCfMeta(request) {
  const cf = request.cf || {};
  return {
    country: cf.country || null,
    region: cf.region || null,
    city: cf.city || null,
    colo: cf.colo || null,
  };
}

function mustGetToken(request, env, bodyToken) {
  const url = new URL(request.url);
  const token =
    request.headers.get("x-ping-token") ||
    bodyToken ||
    url.searchParams.get("t") ||
    "";
  if (token === PUBLIC_CLIENT_TOKEN)
    return true;
  if (env.PING_TOKEN && token === env.PING_TOKEN)
    return true;
  return false;
}

function isoDay(value = new Date()) {
  return value.toISOString().slice(0, 10);
}

function sanitizeObject(value) {
  if (Array.isArray(value))
    return value.map(sanitizeObject);

  if (value && typeof value === "object") {
    const out = {};
    for (const [key, nested] of Object.entries(value)) {
      if (key === "t" || key === "token")
        continue;
      out[key] = sanitizeObject(nested);
    }
    return out;
  }

  return value;
}

function safeInstallFragment(installId) {
  if (!installId)
    return "anonymous";
  return installId.replace(/[^a-zA-Z0-9_-]/g, "").slice(0, 24) || "anonymous";
}

function extensionFor(contentType) {
  const normalized = (contentType || "").toLowerCase();
  if (normalized.includes("application/json"))
    return "json";
  if (normalized.includes("application/x-ndjson") || normalized.includes("application/ndjson"))
    return "ndjson";
  if (normalized.startsWith("text/"))
    return "txt";
  if (normalized.includes("gzip"))
    return "gz";
  return "bin";
}

async function readJson(request) {
  const raw = await request.text();
  if (!raw.trim())
    return { raw, data: {} };
  return { raw, data: JSON.parse(raw) };
}

async function ensureInstall(env, installId, request, patch = {}) {
  const meta = getCfMeta(request);
  await env.DB.prepare(
    `INSERT INTO installs (
        install_id,
        mod_version,
        lang,
        nick,
        last_country,
        last_region,
        last_city,
        last_seen_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
      ON CONFLICT(install_id) DO UPDATE SET
        last_seen_at = CURRENT_TIMESTAMP,
        mod_version = COALESCE(excluded.mod_version, installs.mod_version),
        lang = COALESCE(excluded.lang, installs.lang),
        nick = COALESCE(excluded.nick, installs.nick),
        last_country = COALESCE(excluded.last_country, installs.last_country),
        last_region = COALESCE(excluded.last_region, installs.last_region),
        last_city = COALESCE(excluded.last_city, installs.last_city)`
  ).bind(
    installId,
    patch.modVersion || null,
    patch.lang || null,
    patch.nick || null,
    meta.country,
    meta.region,
    meta.city
  ).run();
}

async function bumpCounter(env, metric) {
  await env.DB.prepare(
    `INSERT INTO daily_counters(day, metric, value)
      VALUES (?, ?, 1)
      ON CONFLICT(day, metric) DO UPDATE SET value = value + 1`
  ).bind(isoDay(), metric).run();
}

async function putObject(env, request, kind, installId, body, contentType) {
  const now = new Date();
  const meta = getCfMeta(request);
  const key = [
    "raw",
    kind,
    now.getUTCFullYear(),
    String(now.getUTCMonth() + 1).padStart(2, "0"),
    String(now.getUTCDate()).padStart(2, "0"),
    `${now.toISOString().replace(/[:.]/g, "-")}-${safeInstallFragment(installId)}-${crypto.randomUUID()}.${extensionFor(contentType)}`
  ].join("/");

  const sizeBytes =
    typeof body === "string"
      ? new TextEncoder().encode(body).byteLength
      : body.byteLength;

  await env.RAW_BUCKET.put(key, body, {
    httpMetadata: {
      contentType: contentType || "application/octet-stream",
    },
    customMetadata: {
      installId: installId || "",
      kind,
      path: new URL(request.url).pathname,
      country: meta.country || "",
      region: meta.region || "",
      city: meta.city || "",
      colo: meta.colo || "",
    },
  });

  await env.DB.prepare(
    `INSERT INTO objects (
        object_key,
        kind,
        install_id,
        request_path,
        content_type,
        size_bytes,
        country,
        region,
        city,
        colo
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
  ).bind(
    key,
    kind,
    installId || null,
    new URL(request.url).pathname,
    contentType || null,
    sizeBytes,
    meta.country,
    meta.region,
    meta.city,
    meta.colo
  ).run();

  return key;
}

async function storeStructured(env, request, kind, installId, payload) {
  const sanitized = sanitizeObject(payload);
  const raw = JSON.stringify(sanitized);
  return putObject(env, request, kind, installId, raw, "application/json; charset=utf-8");
}

async function handlePing(request, env) {
  const url = new URL(request.url);
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = url.searchParams.get("id") || "";
  const modVersion = url.searchParams.get("mod");
  const lang = url.searchParams.get("lang");

  if (!installId)
    return json({ ok: false, error: "missing_install_id" }, 400);

  const existed = await env.DB.prepare(
    "SELECT install_id FROM installs WHERE install_id = ? LIMIT 1"
  ).bind(installId).first();

  await ensureInstall(env, installId, request, { modVersion, lang });
  await bumpCounter(env, "ping");
  await storeStructured(env, request, "ping", installId, {
    id: installId,
    mod: modVersion,
    lang,
  });

  const countRow = await env.DB.prepare(
    "SELECT COUNT(*) AS count FROM installs"
  ).first();

  return json({
    ok: true,
    counted: !existed,
    installs: Number(countRow?.count || 0),
  });
}

async function handleSetNick(request, env) {
  const url = new URL(request.url);
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = url.searchParams.get("id") || "";
  const nick = url.searchParams.get("nick") || "";

  if (!installId || !nick)
    return json({ ok: false, error: "missing_parameters" }, 400);

  await ensureInstall(env, installId, request, { nick });
  await bumpCounter(env, "setnick");
  const key = await storeStructured(env, request, "setnick", installId, {
    id: installId,
    nick,
  });

  return json({ ok: true, stored: key });
}

async function handleStructuredPost(request, env, kind) {
  const { raw, data } = await readJson(request);
  if (!mustGetToken(request, env, data?.t))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = data?.id || "";
  if (!installId)
    return json({ ok: false, error: "missing_install_id" }, 400);

  await ensureInstall(env, installId, request, {
    modVersion: data.mod || data.gameVer || null,
    lang: data.lang || null,
    nick: data.nick || null,
  });
  await bumpCounter(env, kind);

  const contentType = request.headers.get("content-type") || "application/json; charset=utf-8";
  const key = await putObject(env, request, kind, installId, raw || "{}", contentType);

  return json({ ok: true, stored: key });
}

async function handleLegacyEventGet(request, env) {
  const url = new URL(request.url);
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = url.searchParams.get("id") || "";
  if (!installId)
    return json({ ok: false, error: "missing_install_id" }, 400);

  const payload = {
    id: installId,
    type: url.searchParams.get("type") || "generic",
    name: url.searchParams.get("name") || "",
    value: url.searchParams.get("value"),
    ts: new Date().toISOString(),
  };

  await ensureInstall(env, installId, request);
  await bumpCounter(env, "event_get");
  const key = await storeStructured(env, request, "event", installId, payload);

  return json({ ok: true, stored: key });
}

async function handleEventPost(request, env) {
  const contentType = request.headers.get("content-type") || "application/json";

  if (contentType.includes("application/json")) {
    const { raw, data } = await readJson(request);
    const token = Array.isArray(data) ? null : (data?.t || null);
    if (!mustGetToken(request, env, token))
      return json({ ok: false, error: "forbidden" }, 403);

    const events = Array.isArray(data)
      ? data
      : Array.isArray(data?.events)
        ? data.events
        : [data];

    const installId =
      (Array.isArray(data) ? data[0]?.id : data?.id) ||
      request.headers.get("x-install-id") ||
      "";

    if (!installId)
      return json({ ok: false, error: "missing_install_id" }, 400);

    await ensureInstall(env, installId, request);
    await bumpCounter(env, "event_post");
    const key = await putObject(env, request, "event-batch", installId, raw || "[]", contentType);

    return json({
      ok: true,
      accepted: events.filter(Boolean).length,
      stored: key,
    });
  }

  return handleUpload(request, env, "event-upload");
}

async function handleUpload(request, env, forcedKind = null) {
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const url = new URL(request.url);
  const kind =
    forcedKind ||
    request.headers.get("x-event-kind") ||
    url.searchParams.get("kind") ||
    "upload";
  const installId =
    request.headers.get("x-install-id") ||
    url.searchParams.get("id") ||
    "";
  const contentType = request.headers.get("content-type") || "application/octet-stream";
  const body = await request.arrayBuffer();

  if (!body.byteLength)
    return json({ ok: false, error: "empty_body" }, 400);

  if (installId)
    await ensureInstall(env, installId, request);

  await bumpCounter(env, "upload");
  const key = await putObject(env, request, kind, installId, body, contentType);

  return json({
    ok: true,
    stored: key,
    bytes: body.byteLength,
  });
}

async function handleHealth(request, env) {
  const installs = await env.DB.prepare("SELECT COUNT(*) AS count FROM installs").first();
  return json({
    ok: true,
    service: "peak-mx-telemetry",
    installs: Number(installs?.count || 0),
    baseUrl: env.PUBLIC_BASE_URL || null,
    now: new Date().toISOString(),
  });
}

export default {
  async fetch(request, env) {
    try {
      if (request.method === "OPTIONS")
        return withCors(text("", 204, corsHeaders()));

      const url = new URL(request.url);
      const path = url.pathname.replace(/\/+$/, "") || "/";

      let response;
      if (request.method === "GET" && (path === "/" || path === "/healthz"))
        response = await handleHealth(request, env);
      else if (request.method === "GET" && path === "/api/ping")
        response = await handlePing(request, env);
      else if (request.method === "GET" && path === "/api/setnick")
        response = await handleSetNick(request, env);
      else if (request.method === "GET" && path === "/api/event")
        response = await handleLegacyEventGet(request, env);
      else if (request.method === "POST" && path === "/api/diag")
        response = await handleStructuredPost(request, env, "diag");
      else if (request.method === "POST" && path === "/api/crash")
        response = await handleStructuredPost(request, env, "crash");
      else if (request.method === "POST" && path === "/api/lobby")
        response = await handleStructuredPost(request, env, "lobby");
      else if (request.method === "POST" && path === "/api/event")
        response = await handleEventPost(request, env);
      else if (request.method === "POST" && (path === "/api/upload" || path === "/api/actions" || path === "/api/ingest"))
        response = await handleUpload(request, env);
      else
        response = json({ ok: false, error: "not_found" }, 404);

      return withCors(response);
    } catch (error) {
      return withCors(json({
        ok: false,
        error: "internal_error",
        detail: error instanceof Error ? error.message : String(error),
      }, 500));
    }
  },
};
