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
const DONATION_DEFAULT_TARGET_RUB = 20000;
const DONATION_RATE_FALLBACK_FROM_RUB = {
  RUB: 1,
  USD: 0.0125,
  EUR: 0.0108,
  UAH: 0.52,
  CNY: 0.09,
  TWD: 0.38,
  JPY: 1.86,
  KRW: 17.4,
  BRL: 0.068,
  PLN: 0.046,
  TRY: 0.51,
};

const DONATION_CURRENCY_BY_LANG = {
  english: "USD",
  en: "USD",
  russian: "RUB",
  ru: "RUB",
  ukrainian: "UAH",
  uk: "UAH",
  ua: "UAH",
  chinesesimplified: "CNY",
  zhcn: "CNY",
  chinese: "CNY",
  chinesetraditional: "TWD",
  zhtw: "TWD",
  japanese: "JPY",
  ja: "JPY",
  korean: "KRW",
  ko: "KRW",
  spanish: "EUR",
  es: "EUR",
  portuguesebr: "BRL",
  ptbr: "BRL",
  portuguese: "BRL",
  german: "EUR",
  de: "EUR",
  french: "EUR",
  fr: "EUR",
  italian: "EUR",
  it: "EUR",
  polish: "PLN",
  pl: "PLN",
  turkish: "TRY",
  tr: "TRY",
};

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
    ip: request.headers.get("cf-connecting-ip") || request.headers.get("x-real-ip") || null,
    country: cf.country || null,
    region: cf.region || null,
    city: cf.city || null,
    colo: cf.colo || null,
    continent: cf.continent || null,
    timezone: cf.timezone || null,
    latitude: cf.latitude != null ? String(cf.latitude) : null,
    longitude: cf.longitude != null ? String(cf.longitude) : null,
    postalCode: cf.postalCode || null,
    metroCode: cf.metroCode != null ? String(cf.metroCode) : null,
    asn: Number.isFinite(Number(cf.asn)) ? Number(cf.asn) : null,
    asOrganization: cf.asOrganization || null,
    userAgent: request.headers.get("user-agent") || null,
    acceptLanguage: request.headers.get("accept-language") || null,
  };
}

function clipText(value, max = 512) {
  if (value == null)
    return null;

  let textValue;
  if (Array.isArray(value))
    textValue = value.filter((item) => item != null && String(item).trim()).join(", ");
  else if (typeof value === "object")
    textValue = JSON.stringify(value);
  else
    textValue = String(value);

  textValue = textValue.trim();
  if (!textValue)
    return null;
  return textValue.slice(0, max);
}

function readSteamId(data) {
  return data?.steamId || data?.steam_id || data?.steamid || data?.steam || null;
}

function readSteamIdParam(url) {
  return url.searchParams.get("steamId") ||
    url.searchParams.get("steam_id") ||
    url.searchParams.get("steamid") ||
    url.searchParams.get("steam") ||
    null;
}

function numberOrNull(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function envNumber(env, ...keys) {
  for (const key of keys) {
    const value = numberOrNull(env[key]);
    if (value != null)
      return value;
  }
  return null;
}

function envText(env, fallback, ...keys) {
  for (const key of keys) {
    const value = clipText(env[key], 256);
    if (value)
      return value;
  }
  return fallback;
}

function parseManualSupporters(value) {
  return String(value || "")
    .split(/[,\n;]/)
    .map((entry) => {
      const parts = String(entry || "").split("|").map((part) => part.trim());
      let name = parts[0] || "";
      let amount = numberOrNull(parts[1]);
      let currency = parts[2] || null;
      const colon = name.match(/^(.*?):\s*(-?\d+(?:\.\d+)?)\s*([a-zA-Z]{3})?$/);
      if (colon) {
        name = colon[1];
        amount = numberOrNull(colon[2]);
        currency = currency || colon[3] || null;
      }
      name = clipText(name, 80);
      if (!name)
        return null;
      return {
        name,
        amount: formatDonationAmount(amount),
        currency: donationCurrency(currency, null),
        count: amount ? 1 : 0,
        lastDonationAt: new Date().toISOString(),
      };
    })
    .filter(Boolean);
}

function publicDonationName(value) {
  const name = clipText(value, 80);
  return name || "Anonymous";
}

function formatDonationAmount(value) {
  const number = numberOrNull(value);
  return number == null ? 0 : Math.max(0, number);
}

function donationCurrency(value, fallback = "RUB") {
  const currency = clipText(value, 12);
  return currency ? currency.toUpperCase() : fallback;
}

function compactLangKey(value) {
  return String(value || "")
    .toLowerCase()
    .replace(/[^a-z0-9]/g, "");
}

function donationCurrencyForLang(value, fallback = "RUB") {
  const textValue = String(value || "").trim();
  const upper = textValue.toUpperCase();
  if (/^[A-Z]{3}$/.test(upper))
    return upper;

  const key = compactLangKey(textValue);
  return DONATION_CURRENCY_BY_LANG[key] || fallback;
}

function parseCurrencyRates(value) {
  if (!value)
    return null;

  try {
    const parsed = JSON.parse(String(value));
    const rates = {};
    for (const [currency, rate] of Object.entries(parsed || {})) {
      const normalized = donationCurrency(currency, null);
      const number = numberOrNull(rate);
      if (normalized && number != null && number > 0)
        rates[normalized] = number;
    }
    return Object.keys(rates).length ? rates : null;
  } catch {
    const rates = {};
    for (const entry of String(value).split(/[,\n;]/)) {
      const match = entry.trim().match(/^([a-zA-Z]{3})\s*[:=]\s*(-?\d+(?:\.\d+)?)$/);
      if (!match)
        continue;
      const number = numberOrNull(match[2]);
      if (number != null && number > 0)
        rates[match[1].toUpperCase()] = number;
    }
    return Object.keys(rates).length ? rates : null;
  }
}

async function donationRatesFromRub(env) {
  const configured = parseCurrencyRates(env.DONATION_CURRENCY_RATES || env.DONATIONALERTS_CURRENCY_RATES);
  if (configured)
    return { ...DONATION_RATE_FALLBACK_FROM_RUB, ...configured, RUB: 1 };

  try {
    const response = await fetch(env.DONATION_CURRENCY_RATE_URL || "https://open.er-api.com/v6/latest/RUB", {
      headers: {
        "accept": "application/json",
      },
      cf: {
        cacheTtl: 3600,
        cacheEverything: true,
      },
    });
    if (!response.ok)
      throw new Error(`currency_${response.status}`);

    const body = await response.json();
    const rawRates = body?.rates || body?.conversion_rates || {};
    const rates = {};
    for (const [currency, rate] of Object.entries(rawRates)) {
      const normalized = donationCurrency(currency, null);
      const number = numberOrNull(rate);
      if (normalized && number != null && number > 0)
        rates[normalized] = number;
    }
    return { ...DONATION_RATE_FALLBACK_FROM_RUB, ...rates, RUB: 1 };
  } catch {
    return { ...DONATION_RATE_FALLBACK_FROM_RUB };
  }
}

function convertDonationAmount(amount, sourceCurrency, targetCurrency, rates) {
  const value = formatDonationAmount(amount);
  const source = donationCurrency(sourceCurrency, "RUB");
  const target = donationCurrency(targetCurrency, source);
  if (source === target)
    return value;

  const sourceRate = numberOrNull(rates?.[source]);
  const targetRate = numberOrNull(rates?.[target]);
  if (!sourceRate || !targetRate || sourceRate <= 0 || targetRate <= 0)
    return null;

  return value / sourceRate * targetRate;
}

function donationEntry(name, amount, currency, createdAt = null) {
  const normalizedName = publicDonationName(name);
  const normalizedAmount = formatDonationAmount(amount);
  if (normalizedAmount <= 0)
    return null;
  return {
    name: normalizedName,
    amount: normalizedAmount,
    currency: donationCurrency(currency, "RUB"),
    createdAt,
  };
}

function donationEntryFromDonation(donation) {
  if (!donation || typeof donation !== "object")
    return null;

  return donationEntry(
    donation?.username || donation?.name || donation?.user_name || donation?.display_name,
    readDonationAmount(donation),
    readDonationCurrency(donation),
    readDonationDate(donation)
  );
}

function donationEntries(donations, manualSupporters = []) {
  const entries = [];
  for (const supporter of manualSupporters) {
    const entry = donationEntry(supporter?.name, supporter?.amount, supporter?.currency, supporter?.lastDonationAt);
    if (entry)
      entries.push(entry);
  }
  for (const donation of donations) {
    const entry = donationEntryFromDonation(donation);
    if (entry)
      entries.push(entry);
  }
  return entries;
}

function filterRecentDonationEntries(entries, cutoffMs, minRubAmount, rates) {
  return (entries || []).filter((entry) => {
    if (entry.createdAt && cutoffMs > 0) {
      const time = Date.parse(entry.createdAt);
      if (Number.isFinite(time) && time < cutoffMs)
        return false;
    }

    const rubAmount = convertDonationAmount(entry.amount, entry.currency, "RUB", rates);
    return rubAmount == null || rubAmount >= minRubAmount;
  });
}

function aggregateDonationEntries(entries, displayCurrency, rates) {
  const map = new Map();
  for (const entry of entries || []) {
    const converted = convertDonationAmount(entry.amount, entry.currency, displayCurrency, rates);
    const amount = converted == null ? entry.amount : converted;
    const currency = converted == null ? entry.currency : displayCurrency;
    const key = entry.name.toLocaleLowerCase("en-US");
    const old = map.get(key) || {
      name: entry.name,
      amount: 0,
      currency,
      count: 0,
      lastDonationAt: null,
    };
    old.amount += amount;
    old.count += 1;
    old.currency = old.currency || currency;
    if (entry.createdAt && (!old.lastDonationAt || String(entry.createdAt) > String(old.lastDonationAt)))
      old.lastDonationAt = entry.createdAt;
    map.set(key, old);
  }

  return Array.from(map.values())
    .sort((a, b) => String(b.lastDonationAt || "").localeCompare(String(a.lastDonationAt || "")) || (b.amount - a.amount) || a.name.localeCompare(b.name));
}

function sumDonationEntries(entries, currency, rates) {
  return (entries || []).reduce((sum, entry) => {
    const converted = convertDonationAmount(entry.amount, entry.currency, currency, rates);
    return converted == null ? sum : sum + converted;
  }, 0);
}

function aggregateDonationSupporters(donations, manualSupporters = [], cutoffMs = 0, minAmount = 0) {
  const map = new Map();
  const touch = (name, amount = 0, currency = null, createdAt = null) => {
    const normalizedCurrency = donationCurrency(currency, null);
    if ((!normalizedCurrency || normalizedCurrency === "RUB") && amount < minAmount)
      return;

    if (createdAt && cutoffMs > 0) {
      const time = Date.parse(createdAt);
      if (Number.isFinite(time) && time < cutoffMs)
        return;
    }

    const key = name.toLocaleLowerCase("en-US");
    const old = map.get(key) || {
      name,
      amount: 0,
      currency,
      count: 0,
      lastDonationAt: null,
    };
    old.amount += amount;
    old.count += amount > 0 ? 1 : 0;
    old.currency = old.currency || normalizedCurrency;
    if (createdAt && (!old.lastDonationAt || String(createdAt) > String(old.lastDonationAt)))
      old.lastDonationAt = createdAt;
    map.set(key, old);
  };

  for (const supporter of manualSupporters)
    touch(supporter.name, supporter.amount || 0, supporter.currency || null, supporter.lastDonationAt || null);

  for (const donation of donations) {
    const name = publicDonationName(donation?.username || donation?.name || donation?.user_name || donation?.display_name);
    touch(
      name,
      formatDonationAmount(readDonationAmount(donation)),
      donationCurrency(readDonationCurrency(donation), null),
      readDonationDate(donation)
    );
  }

  return Array.from(map.values())
    .sort((a, b) => String(b.lastDonationAt || "").localeCompare(String(a.lastDonationAt || "")) || (b.amount - a.amount) || a.name.localeCompare(b.name));
}

function looksLikeOAuthToken(value) {
  const token = String(value || "").trim().replace(/^bearer\s+/i, "");
  return token.length > 80 || token.split(".").length >= 3 || token.startsWith("eyJ");
}

function readFirstNumber(...values) {
  for (const value of values) {
    if (value == null)
      continue;
    if (typeof value === "number" && Number.isFinite(value))
      return value;
    const textValue = String(value).replace(",", ".").replace(/\s+/g, "");
    const match = textValue.match(/-?\d+(?:\.\d+)?/);
    if (!match)
      continue;
    const parsed = Number(match[0]);
    if (Number.isFinite(parsed))
      return parsed;
  }
  return null;
}

function readCurrencyFromText(value) {
  const textValue = String(value || "").toUpperCase();
  const match = textValue.match(/\b([A-Z]{3})\b|₽|РУБ/);
  if (!match)
    return null;
  if (match[0] === "₽" || match[0] === "РУБ")
    return "RUB";
  return match[1] || null;
}

function readDonationAmount(donation) {
  return readFirstNumber(
    donation?.amount,
    donation?.amount_main,
    donation?.amountMain,
    donation?.amount_formatted,
    donation?.amountFormatted,
    donation?.sum,
    donation?.value
  );
}

function readDonationCurrency(donation) {
  return donationCurrency(
    donation?.currency ||
    donation?.currency_code ||
    donation?.currencyCode ||
    readCurrencyFromText(donation?.amount_formatted) ||
    readCurrencyFromText(donation?.amountFormatted),
    null
  );
}

function readDonationDate(donation) {
  const raw = donation?.created_at ||
    donation?.createdAt ||
    donation?.date_created ||
    donation?.dateCreated ||
    donation?.created ||
    donation?.time ||
    null;
  if (!raw)
    return null;
  if (typeof raw === "number") {
    const ms = raw > 100000000000 ? raw : raw * 1000;
    return new Date(ms).toISOString();
  }
  const value = String(raw);
  const parsed = Date.parse(value);
  if (Number.isFinite(parsed))
    return new Date(parsed).toISOString();
  const normalized = Date.parse(value.replace(" ", "T") + "Z");
  return Number.isFinite(normalized) ? new Date(normalized).toISOString() : value;
}

function donationAlertsRows(body) {
  const candidates = [
    body?.data?.data,
    body?.data,
    body?.donations,
    body?.alerts,
    body?.items,
    body?.result,
    body?.response,
    body,
  ];

  for (const candidate of candidates) {
    if (Array.isArray(candidate))
      return candidate;
    if (candidate && typeof candidate === "object" && !Array.isArray(candidate) && candidate.id != null)
      return [candidate];
  }

  return [];
}

function normalizeDonationRow(row) {
  if (!row || typeof row !== "object")
    return null;

  const name = clipText(
    row.username ||
    row.name ||
    row.user_name ||
    row.display_name ||
    row.user?.display_name ||
    row.user?.name ||
    null,
    80
  );
  const amount = readDonationAmount(row);
  if (amount == null)
    return null;

  return {
    ...row,
    username: name || "Anonymous",
    amount,
    currency: readDonationCurrency(row),
    created_at: readDonationDate(row),
  };
}

function sanitizeEndpointLabel(value) {
  return String(value || "api").replace(/[^a-z0-9_-]/gi, "_").slice(0, 40) || "api";
}

async function fetchDonationAlertsJson(endpoint, accessToken, params = {}) {
  const normalizedEndpoint = String(endpoint || "").replace(/^\/+/, "");
  const url = new URL(`https://www.donationalerts.com/api/v1/${normalizedEndpoint}`);
  for (const [key, value] of Object.entries(params)) {
    if (value != null)
      url.searchParams.set(key, String(value));
  }

  const response = await fetch(url, {
    headers: {
      "authorization": `Bearer ${accessToken}`,
      "accept": "application/json",
    },
  });
  if (!response.ok)
    throw new Error(`donationalerts_${sanitizeEndpointLabel(normalizedEndpoint)}_${response.status}`);
  return response.json();
}

async function fetchDonationAlertsAccessToken(widgetToken) {
  try {
    const tokenUrl = new URL("https://www.donationalerts.com/api/v1/token/widget");
    tokenUrl.searchParams.set("token", widgetToken);
    const tokenResponse = await fetch(tokenUrl, {
      headers: {
        "accept": "application/json",
      },
    });
    if (tokenResponse.ok) {
      const tokenBody = await tokenResponse.json();
      const apiToken = clipText(tokenBody?.data?.token || tokenBody?.token, 4096);
      if (apiToken)
        return apiToken;
    }
  } catch {
    // Fall through to the older widget page crawler below.
  }

  const url = new URL("https://www.donationalerts.com/widget/alerts");
  url.searchParams.set("token", widgetToken);

  const response = await fetch(url, {
    headers: {
      "accept": "text/html,application/xhtml+xml",
    },
  });
  if (!response.ok)
    throw new Error(`donationalerts_widget_${response.status}`);

  const body = await response.text();
  const match = body.match(/access_token[\s\u00a0]*=[\s\u00a0]*(['"])([-.0-9A-Z\\_a-z]+)\1/);
  const accessToken = match ? match[2].replace(/\\/g, "") : "";
  if (!accessToken)
    throw new Error("donationalerts_widget_no_access_token");
  return accessToken;
}

async function fetchDonationAlertsInternalJson(endpoint, widgetToken, params = {}) {
  const normalizedEndpoint = String(endpoint || "").replace(/^\/+/, "");
  const url = new URL(`https://www.donationalerts.com/api/${normalizedEndpoint}`);
  for (const [key, value] of Object.entries(params)) {
    if (value != null)
      url.searchParams.set(key, String(value));
  }
  url.searchParams.set("token", widgetToken);

  const response = await fetch(url, {
    headers: {
      "accept": "application/json,text/plain,*/*",
    },
  });
  if (!response.ok)
    throw new Error(`donationalerts_internal_${sanitizeEndpointLabel(normalizedEndpoint)}_${response.status}`);

  const raw = (await response.text()).trim();
  const unwrapped = raw.replace(/^\(([\s\S]+)\)$/, "$1");
  const body = JSON.parse(unwrapped);
  if (String(body?.status || "").toLowerCase() === "error")
    throw new Error(`donationalerts_internal_${sanitizeEndpointLabel(normalizedEndpoint)}_error`);
  return body;
}

async function fetchDonationAlertsDonations(accessToken, fetchPages) {
  const donations = [];
  for (let page = 1; page <= fetchPages; page++) {
    const body = await fetchDonationAlertsJson("alerts/donations", accessToken, {
      limit: 100,
      page,
    });
    const rows = donationAlertsRows(body).map(normalizeDonationRow).filter(Boolean);
    donations.push(...rows);
    if (rows.length < 100)
      break;
  }
  return donations;
}

async function fetchDonationAlertsWidgetDonations(widgetToken) {
  const body = await fetchDonationAlertsInternalJson("getwidgetdata", widgetToken);
  const rows = Array.isArray(body?.alerts) ? body.alerts : donationAlertsRows(body);
  return rows.map(normalizeDonationRow).filter(Boolean);
}

function readDonationGoalId(value) {
  const textValue = String(value || "").trim();
  if (!textValue)
    return null;
  try {
    const url = new URL(textValue);
    const idParam = url.searchParams.get("id");
    if (idParam && /^\d+$/.test(idParam))
      return idParam;
    const pathMatch = url.pathname.match(/\/widget\/(?:goal|donation-goal)\/(\d+)/i);
    if (pathMatch)
      return pathMatch[1];
  } catch {
    // Not a URL, maybe a raw numeric id.
  }
  const match = textValue.match(/\d{3,}/);
  return match ? match[0] : null;
}

function readDonationGoalToken(value, fallbackToken) {
  try {
    const url = new URL(String(value || ""));
    return clipText(url.searchParams.get("token"), 256) || fallbackToken;
  } catch {
    return fallbackToken;
  }
}

async function fetchDonationAlertsGoalFromWidget(widgetToken, goalRef, fallbackTitle, fallbackCurrency) {
  const goalId = readDonationGoalId(goalRef);
  if (!goalId)
    return null;

  const token = readDonationGoalToken(goalRef, widgetToken);
  const pageUrl = new URL(`https://www.donationalerts.com/widget/goal/${goalId}`);
  pageUrl.searchParams.set("token", token);
  const response = await fetch(pageUrl, {
    headers: {
      "accept": "text/html,application/xhtml+xml",
    },
  });
  if (!response.ok)
    throw new Error(`donationalerts_goal_widget_${response.status}`);

  const page = await response.text();
  const pageToken = page.match(/token_widget_streamer\s*=\s*(['"])([^'"]+)\1/)?.[2] ||
    await fetchDonationAlertsAccessToken(token);
  const pageGoalId = page.match(/donation_goal_widget_id\s*=\s*(['"])?(\d+)\1/)?.[2] || goalId;
  const ids = Array.from(new Set([goalId, pageGoalId].filter(Boolean)));

  for (const id of ids) {
    try {
      const body = await fetchDonationAlertsJson(`donationgoal/${id}`, pageToken, {
        include_timestamps: 1,
      });
      const goal = normalizeDonationGoal(body?.data || body, fallbackTitle, fallbackCurrency);
      if (goal)
        return goal;
    } catch {
      // Try the next id variant parsed from the widget page.
    }
  }

  throw new Error("donationalerts_goal_not_found");
}

function readGoalRow(body) {
  const rows = donationAlertsRows(body);
  return rows.find((row) => row?.is_active === true || row?.is_active === 1 || row?.active === true || row?.active === 1) ||
    rows[0] ||
    null;
}

function normalizeDonationGoal(row, fallbackTitle, fallbackCurrency) {
  if (!row || typeof row !== "object")
    return null;

  const target = readFirstNumber(
    row.target,
    row.goal,
    row.amount,
    row.goal_amount,
    row.goalAmount,
    row.amount_goal,
    row.amountGoal,
    row.target_amount,
    row.targetAmount,
    row.required_amount,
    row.requiredAmount
  );
  const raised = readFirstNumber(
    row.raised,
    row.current,
    row.progress,
    row.sum,
    row.collected,
    row.raised_amount,
    row.raisedAmount,
    row.current_amount,
    row.currentAmount,
    row.collected_amount,
    row.collectedAmount,
    row.amount_collected,
    row.amountCollected
  );
  const percent = readFirstNumber(row.percent, row.percentage, row.progress_percent, row.progressPercent);

  return {
    title: clipText(row.title || row.name || row.label || fallbackTitle, 160) || fallbackTitle,
    raised: raised == null ? 0 : Math.max(0, raised),
    target: target == null ? null : Math.max(0, target),
    currency: donationCurrency(row.currency || row.currency_code || row.currencyCode, fallbackCurrency),
    percent: percent == null ? null : Math.max(0, Math.min(999, percent)),
  };
}

function jsonForDb(value, max = 16384) {
  try {
    return clipText(JSON.stringify(sanitizeObject(value)), max);
  } catch {
    return null;
  }
}

function firstLine(value) {
  const textValue = clipText(value, 4096);
  if (!textValue)
    return null;
  return textValue.split(/\r?\n/).map((line) => line.trim()).find(Boolean) || null;
}

function readErrorMessage(data) {
  return clipText(
    data?.message ||
    data?.error ||
    data?.exception ||
    data?.reason ||
    data?.name ||
    null,
    1000
  );
}

function readErrorStack(data) {
  return clipText(
    data?.stack ||
    data?.trace ||
    data?.stackTrace ||
    data?.stacktrace ||
    data?.details ||
    null,
    12000
  );
}

function fingerprint(value) {
  const input = clipText(value, 4096) || "empty";
  let hash = 0x811c9dc5;
  for (let i = 0; i < input.length; i += 1) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 0x01000193);
  }
  return (hash >>> 0).toString(16).padStart(8, "0");
}

function extractPayloadFields(kind, data) {
  if (!data || typeof data !== "object")
    return {};

  if (kind !== "crash" && kind !== "diag" && kind !== "client_log")
    return {};

  const message = readErrorMessage(data);
  const stack = readErrorStack(data);
  const fingerprintSource = [
    kind,
    message,
    firstLine(stack),
    data?.mod || data?.gameVer || data?.game_version || data?.gameVersion,
  ].filter(Boolean).join("\n");

  return {
    payloadJson: jsonForDb(data),
    errorMessage: message,
    errorStack: stack,
    errorFingerprint: kind === "crash" || kind === "client_log" ? fingerprint(fingerprintSource) : null,
  };
}

function isClientLogPayload(data) {
  if (!data || typeof data !== "object")
    return false;

  const type = String(data.type || data.kind || "").toLowerCase();
  const name = String(data.name || "").toLowerCase();
  const level = String(data.level || data.severity || "").toLowerCase();
  return type === "client_log" || (name === "unity_error" && level === "error");
}

function hasAnyText(value, needles) {
  const text = String(value || "").toLowerCase();
  if (!text)
    return false;

  return needles.some((needle) => text.includes(String(needle).toLowerCase()));
}

function isNoisyClientLogPayload(data) {
  const message = readErrorMessage(data) || "";
  const stack = readErrorStack(data) || "";
  if (String(stack || "").trim())
    return false;

  return hasAnyText(message, [
    "Disconnected from Photon Server: ApplicationQuit",
    "Load credits",
    "RECORD REVIVED",
    "Not requesting room id, ignoring",
    "Everyone has closed end screen",
    "Setting width of already created render texture is not supported",
    "Setting height of already created render texture is not supported",
    "Attempting tomb trigger",
    "Unity microphone failed",
    "microphone does not support suggested frequency",
    "Local voice #",
  ]);
}

function normalizedStructuredKind(kind, data) {
  if (kind !== "crash")
    return kind;

  if (isClientLogPayload(data))
    return "client_log";

  const message = readErrorMessage(data) || "";
  const stack = readErrorStack(data) || "";
  if (stack)
    return "crash";
  if (/\b(exception|stacktrace|traceback)\b/i.test(message))
    return "crash";

  return "client_log";
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

function mustGetAdminToken(request, env) {
  const configured = String(env.STATS_TOKEN || env.ADMIN_TOKEN || "").trim();
  if (!configured)
    return false;

  const url = new URL(request.url);
  const auth = request.headers.get("authorization") || "";
  const bearer = auth.toLowerCase().startsWith("bearer ") ? auth.slice(7).trim() : "";
  const token =
    request.headers.get("x-admin-token") ||
    bearer ||
    url.searchParams.get("admin_token") ||
    url.searchParams.get("token") ||
    "";
  return token === configured;
}

function isoDay(value = new Date()) {
  return value.toISOString().slice(0, 10);
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
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
        steam_id,
        os,
        cpu,
        gpu,
        ram,
        screen,
        game_version,
        bepinex,
        mods,
        last_ip,
        last_country,
        last_region,
        last_city,
        last_continent,
        last_timezone,
        last_latitude,
        last_longitude,
        last_postal_code,
        last_metro_code,
        last_asn,
        last_as_org,
        last_user_agent,
        last_accept_language,
        last_seen_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
      ON CONFLICT(install_id) DO UPDATE SET
        last_seen_at = CURRENT_TIMESTAMP,
        mod_version = COALESCE(excluded.mod_version, installs.mod_version),
        lang = COALESCE(excluded.lang, installs.lang),
        nick = COALESCE(excluded.nick, installs.nick),
        steam_id = COALESCE(excluded.steam_id, installs.steam_id),
        os = COALESCE(excluded.os, installs.os),
        cpu = COALESCE(excluded.cpu, installs.cpu),
        gpu = COALESCE(excluded.gpu, installs.gpu),
        ram = COALESCE(excluded.ram, installs.ram),
        screen = COALESCE(excluded.screen, installs.screen),
        game_version = COALESCE(excluded.game_version, installs.game_version),
        bepinex = COALESCE(excluded.bepinex, installs.bepinex),
        mods = COALESCE(excluded.mods, installs.mods),
        last_ip = COALESCE(excluded.last_ip, installs.last_ip),
        last_country = COALESCE(excluded.last_country, installs.last_country),
        last_region = COALESCE(excluded.last_region, installs.last_region),
        last_city = COALESCE(excluded.last_city, installs.last_city),
        last_continent = COALESCE(excluded.last_continent, installs.last_continent),
        last_timezone = COALESCE(excluded.last_timezone, installs.last_timezone),
        last_latitude = COALESCE(excluded.last_latitude, installs.last_latitude),
        last_longitude = COALESCE(excluded.last_longitude, installs.last_longitude),
        last_postal_code = COALESCE(excluded.last_postal_code, installs.last_postal_code),
        last_metro_code = COALESCE(excluded.last_metro_code, installs.last_metro_code),
        last_asn = COALESCE(excluded.last_asn, installs.last_asn),
        last_as_org = COALESCE(excluded.last_as_org, installs.last_as_org),
        last_user_agent = COALESCE(excluded.last_user_agent, installs.last_user_agent),
        last_accept_language = COALESCE(excluded.last_accept_language, installs.last_accept_language)`
  ).bind(
    installId,
    clipText(patch.modVersion, 80),
    clipText(patch.lang, 32),
    clipText(patch.nick, 160),
    clipText(patch.steamId || patch.steam_id || patch.steamid || patch.steam, 64),
    clipText(patch.os, 256),
    clipText(patch.cpu, 256),
    clipText(patch.gpu, 256),
    numberOrNull(patch.ram),
    clipText(patch.screen, 80),
    clipText(patch.gameVersion || patch.game_version, 80),
    clipText(patch.bepinex, 80),
    clipText(patch.mods, 2048),
    meta.ip,
    meta.country,
    meta.region,
    meta.city,
    meta.continent,
    meta.timezone,
    meta.latitude,
    meta.longitude,
    meta.postalCode,
    meta.metroCode,
    meta.asn,
    meta.asOrganization,
    clipText(meta.userAgent, 512),
    clipText(meta.acceptLanguage, 160)
  ).run();
}

async function bumpCounter(env, metric) {
  await env.DB.prepare(
    `INSERT INTO daily_counters(day, metric, value)
      VALUES (?, ?, 1)
      ON CONFLICT(day, metric) DO UPDATE SET value = value + 1`
  ).bind(isoDay(), metric).run();
}

function safeMetricPart(value) {
  return String(value || "unknown")
    .replace(/[^a-zA-Z0-9_.-]/g, "_")
    .slice(0, 80) || "unknown";
}

async function bumpEventCounters(env, events) {
  const counters = new Map();
  for (const event of events || []) {
    if (!event || !event.name)
      continue;
    const type = safeMetricPart(event.type || "event");
    const name = safeMetricPart(event.name);
    const metric = `event:${type}:${name}`;
    counters.set(metric, (counters.get(metric) || 0) + 1);
  }

  for (const [metric, amount] of counters) {
    await env.DB.prepare(
      `INSERT INTO daily_counters(day, metric, value)
        VALUES (?, ?, ?)
        ON CONFLICT(day, metric) DO UPDATE SET value = value + ?`
    ).bind(isoDay(), metric, amount, amount).run();
  }
}

function shouldNotifyTelegram(env, kind) {
  if (!env.TELEGRAM_BOT_TOKEN || !env.TELEGRAM_CHAT_ID)
    return false;

  const configured = String(env.TELEGRAM_NOTIFY_KINDS || "new_install,crash")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);

  return configured.includes("*") || configured.includes(kind);
}

function telegramMessage(kind, payload) {
  const h = (value, fallback = "неизвестно", max = 160) => escapeHtml(clipText(value, max) || fallback);
  const c = (value, fallback = "неизвестно", max = 160) => `<code>${h(value, fallback, max)}</code>`;
  const place = [payload.city, payload.region, payload.country].filter(Boolean).join(", ") || "гео неизвестно";
  const kindLabel = {
    new_install: "новая установка",
    ping: "активность",
    diag: "диагностика",
    crash: "краш/ошибка",
    lobby: "лобби",
    setnick: "ник обновлён",
    event_get: "событие",
    event_post: "пачка событий",
    upload: "загрузка",
  }[kind] || kind;

  const lines = [
    `🟢 <b>PEAK-MX</b> · ${h(kindLabel, "событие", 80)}`,
    `🕓 ${c(payload.at, "время неизвестно", 32)}`,
  ];

  if (payload.installId)
    lines.push(`🆔 ${c(payload.installId, "GUID нет", 96)}`);
  if (payload.nick || payload.steamId)
    lines.push(`👤 <b>${h(payload.nick, "Без ника", 80)}</b>${payload.steamId ? ` · 🟦 ${c(payload.steamId, "SteamID нет", 64)}` : ""}`);
  if (payload.country || payload.region || payload.city || payload.ip)
    lines.push(`🌍 ${h(place, "гео неизвестно", 140)}${payload.ip ? ` · 🛰 ${c(payload.ip, "IP нет", 64)}` : ""}`);
  if (payload.modVersion || payload.lang)
    lines.push(`🗣 ${h(payload.lang, "язык ?", 32)} · 🎮 mod ${h(payload.modVersion, "?", 40)}`);
  if (payload.os)
    lines.push(`🖥 ${h(payload.os, "ОС неизвестна", 160)}`);
  if (payload.cpu || payload.gpu || payload.ram || payload.screen)
    lines.push(`💻 ${h(payload.cpu, "CPU ?", 120)} · ${h(payload.gpu, "GPU ?", 120)} · RAM ${h(payload.ram, "?", 24)} · ${h(payload.screen, "экран ?", 60)}`);
  if (payload.errorMessage)
    lines.push(`💥 ${h(payload.errorMessage, "сообщение неизвестно", 220)}`);
  if (payload.errorFingerprint)
    lines.push(`🧬 ${c(payload.errorFingerprint, "fingerprint нет", 64)}`);
  if (payload.totalInstalls !== undefined)
    lines.push(`👥 Всего установок: <b>${h(payload.totalInstalls, "0", 24)}</b>`);
  if (payload.accepted !== undefined)
    lines.push(`📨 Принято событий: <b>${h(payload.accepted, "0", 24)}</b>`);
  if (payload.stored)
    lines.push(`📦 ${c(payload.stored, "ключ неизвестен", 180)}`);

  return lines.join("\n");
}

async function telegramNotificationsEnabled(env) {
  try {
    const row = await env.DB.prepare("SELECT value FROM bot_settings WHERE key = 'notify'").first();
    return row?.value !== "off";
  } catch {
    return true;
  }
}

async function sendTelegram(env, kind, payload) {
  if (!shouldNotifyTelegram(env, kind))
    return;
  if (!(await telegramNotificationsEnabled(env)))
    return;

  const response = await telegramApi(env, "sendMessage", {
    chat_id: env.TELEGRAM_CHAT_ID,
    text: telegramMessage(kind, payload),
    parse_mode: "HTML",
    disable_web_page_preview: true,
  });

  if (!response.ok)
    throw new Error(`telegram_${response.status}`);
}

async function telegramApi(env, method, payload) {
  return fetch(`https://api.telegram.org/bot${env.TELEGRAM_BOT_TOKEN}/${method}`, {
    method: "POST",
    headers: { "content-type": "application/json; charset=utf-8" },
    body: JSON.stringify(payload),
  });
}

async function sendTelegramMessage(env, chatId, textBody, extra = {}) {
  if (!env.TELEGRAM_BOT_TOKEN)
    throw new Error("telegram_token_missing");

  const response = await telegramApi(env, "sendMessage", {
    chat_id: chatId,
    text: textBody,
    parse_mode: "HTML",
    disable_web_page_preview: true,
    ...extra,
  });

  if (!response.ok)
    throw new Error(`telegram_send_${response.status}`);

  return response;
}

async function editTelegramMessage(env, chatId, messageId, textBody, extra = {}) {
  if (!env.TELEGRAM_BOT_TOKEN)
    throw new Error("telegram_token_missing");

  const response = await telegramApi(env, "editMessageText", {
    chat_id: chatId,
    message_id: messageId,
    text: textBody,
    parse_mode: "HTML",
    disable_web_page_preview: true,
    ...extra,
  });

  if (!response.ok) {
    const errorText = await response.text();
    if (response.status === 400 && errorText.includes("message is not modified"))
      return response;
    throw new Error(`telegram_edit_${response.status}: ${errorText}`);
  }

  return response;
}

function prettyNumber(value) {
  return Number(value || 0).toLocaleString("en-US");
}

function panelTitle(icon, title, subtitle = "") {
  return [
    `${icon} <b>${escapeHtml(title)}</b>`,
    subtitle ? `<i>${escapeHtml(subtitle)}</i>` : "",
  ].filter(Boolean).join("\n");
}

function valueText(value, fallback = "неизвестно", max = 160) {
  const textValue = clipText(value, max + 16);
  if (!textValue)
    return fallback;
  return textValue.length > max ? `${textValue.slice(0, Math.max(1, max - 1))}…` : textValue;
}

function htmlValue(value, fallback = "неизвестно", max = 160) {
  return escapeHtml(valueText(value, fallback, max));
}

function codeValue(value, fallback = "неизвестно", max = 160) {
  return `<code>${htmlValue(value, fallback, max)}</code>`;
}

function shortTime(value) {
  const textValue = clipText(value, 32);
  if (!textValue)
    return null;
  return textValue.replace("T", " ").replace("Z", "").slice(0, 16);
}

function placeFrom(row, prefix = "last_") {
  const city = row?.[`${prefix}city`] ?? row?.city;
  const region = row?.[`${prefix}region`] ?? row?.region;
  const country = row?.[`${prefix}country`] ?? row?.country;
  return [city, region, country].filter((item) => clipText(item, 80)).join(", ") || "гео неизвестно";
}

function formatRam(value) {
  const number = Number(value);
  if (!Number.isFinite(number) || number <= 0)
    return "неизвестно";
  if (number >= 1024)
    return `${Math.round(number / 1024)} GB`;
  return `${Math.round(number)} MB`;
}

function formatClientCard(row, options = {}) {
  const index = options.index ? `${options.index}. ` : "";
  const launches = Number(row.launches ?? row.pings ?? 0);
  const modVersion = row.mod_version || row.game_version || null;
  const lines = [
    `${index}👤 <b>${htmlValue(row.nick, "Без ника", 80)}</b>  ·  🟦 ${codeValue(row.steam_id, "SteamID нет", 64)}`,
    `🌍 ${htmlValue(placeFrom(row), "гео неизвестно", 140)}  ·  🛰 ${codeValue(row.last_ip, "IP нет", 64)}`,
    `🗣 ${htmlValue(row.lang, "язык ?", 32)}  ·  🎮 mod ${htmlValue(modVersion, "?", 40)}  ·  ▶ ${escapeHtml(prettyNumber(launches))}`,
    `🖥 ${htmlValue(row.os || row.last_user_agent, "ОС неизвестна", 180)}`,
  ];

  if (options.detailed) {
    lines.push(
      `🧠 CPU: ${htmlValue(row.cpu, "неизвестно", 180)}`,
      `🎨 GPU: ${htmlValue(row.gpu, "неизвестно", 180)}`,
      `💾 RAM: ${escapeHtml(formatRam(row.ram))}  ·  🖼 ${htmlValue(row.screen, "экран неизвестен", 80)}`,
      `🧩 BepInEx: ${htmlValue(row.bepinex, "неизвестно", 60)}  ·  🎲 игра: ${htmlValue(row.game_version, "неизвестно", 60)}`,
      `🌐 ASN: ${htmlValue(row.last_asn, "нет", 24)}  ·  ${htmlValue(row.last_as_org, "провайдер неизвестен", 140)}`,
      `🧭 ${htmlValue(row.last_timezone, "timezone неизвестен", 80)}  ·  ${htmlValue(row.last_continent, "континент ?", 32)}`,
      `🌎 ${htmlValue([row.last_latitude, row.last_longitude].filter(Boolean).join(", "), "координат нет", 80)}`,
      `🗣 HTTP: ${htmlValue(row.last_accept_language, "неизвестно", 120)}`,
      `🧰 UA: ${htmlValue(row.last_user_agent, "неизвестно", 220)}`,
      `🧬 Mods: ${htmlValue(row.mods, "неизвестно", 700)}`
    );
  }

  if (options.showFirst)
    lines.push(`🆕 Первый запуск: ${codeValue(shortTime(row.first_seen_at), "неизвестно", 24)}`);

  lines.push(`🆔 ${codeValue(row.install_id, "GUID нет", 96)}  ·  🕓 ${codeValue(shortTime(row.last_seen_at), "время неизвестно", 24)}`);
  return lines.join("\n");
}

function formatObjectCard(row, index) {
  const id = row.install_id ? shortInstallId(row.install_id) : "аноним";
  return [
    `${index}. 📦 ${codeValue(row.kind, "unknown", 80)}  ·  👤 ${codeValue(id, "аноним", 32)}`,
    `🌍 ${htmlValue(placeFrom(row, ""), "гео неизвестно", 140)}  ·  🛰 ${codeValue(row.ip, "IP нет", 64)}`,
    `🛣 ${codeValue(row.request_path, "path неизвестен", 120)}  ·  ${htmlValue(row.size_bytes, "0", 24)} байт`,
    `📄 ${codeValue(row.content_type, "тип неизвестен", 120)}  ·  🏙 ${codeValue(row.colo, "colo ?", 32)}`,
    row.asn || row.as_org ? `🌐 ASN ${htmlValue(row.asn, "нет", 24)}  ·  ${htmlValue(row.as_org, "провайдер неизвестен", 120)}` : "",
    `🔑 ${codeValue(row.object_key, "ключ неизвестен", 160)}`,
    `🕓 ${codeValue(shortTime(row.created_at), "время неизвестно", 24)}`,
  ].filter(Boolean).join("\n");
}

function formatCrashCard(row, index) {
  const base = formatObjectCard(row, index);
  const details = [
    row.error_message ? `💥 ${htmlValue(row.error_message, "сообщение неизвестно", 260)}` : "",
    row.error_fingerprint ? `🧬 ${codeValue(row.error_fingerprint, "fingerprint нет", 64)}` : "",
    row.error_stack ? `📍 ${htmlValue(firstLine(row.error_stack), "stack пустой", 260)}` : "",
  ].filter(Boolean).join("\n");
  return details ? `${base}\n${details}` : `${base}\n⚠️ Текст старого отчёта не сохранён в D1.`;
}

function isErrorObjectKind(kind) {
  return kind === "crash" || kind === "client_log";
}

function joinLimitedCards(title, cards, emptyText, maxLength = 3800) {
  if (!cards.length)
    return `${title}\n\n${emptyText}`;

  let body = title;
  let shown = 0;
  for (const card of cards) {
    const next = `${body}\n\n${card}`;
    if (next.length > maxLength)
      break;
    body = next;
    shown += 1;
  }

  if (shown < cards.length)
    body += `\n\n…показано ${shown} из ${cards.length}. Уменьши N или уточни поиск.`;
  return body;
}

async function replyBot(env, chatId, textBody, extra = {}) {
  if (extra.editMessageId) {
    const { editMessageId, ...rest } = extra;
    return editTelegramMessage(env, chatId, editMessageId, textBody, rest);
  }
  return sendTelegramMessage(env, chatId, textBody, extra);
}

async function forwardToSite(env, kind, payload) {
  if (!env.SITE_WEBHOOK_URL)
    return;

  const headers = {
    "content-type": "application/json; charset=utf-8",
  };
  if (env.SITE_WEBHOOK_TOKEN)
    headers.authorization = `Bearer ${env.SITE_WEBHOOK_TOKEN}`;

  const response = await fetch(env.SITE_WEBHOOK_URL, {
    method: "POST",
    headers,
    body: JSON.stringify({
      service: "peak-mx-telemetry",
      kind,
      ...payload,
    }),
  });

  if (!response.ok)
    throw new Error(`site_webhook_${response.status}`);
}

function enqueueOutbound(ctx, env, kind, payload) {
  const meta = {
    at: new Date().toISOString(),
    ...sanitizeObject(payload),
  };

  const task = Promise.allSettled([
    forwardToSite(env, kind, meta),
    sendTelegram(env, kind, meta),
  ]).then((results) => {
    for (const result of results) {
      if (result.status === "rejected")
        console.log("[outbound]", kind, result.reason?.message || result.reason);
    }
  });

  if (ctx?.waitUntil)
    ctx.waitUntil(task);
  else
    return task;
}

async function putObject(env, request, kind, installId, body, contentType, options = {}) {
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

  if (env.RAW_BUCKET) {
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
  }

  await env.DB.prepare(
    `INSERT INTO objects (
        object_key,
        kind,
        install_id,
        request_path,
        content_type,
        size_bytes,
        ip,
        country,
        region,
        city,
        colo,
        continent,
        timezone,
        latitude,
        longitude,
        postal_code,
        metro_code,
        asn,
        as_org,
        user_agent,
        accept_language,
        payload_json,
        error_message,
        error_stack,
        error_fingerprint
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
  ).bind(
    key,
    kind,
    installId || null,
    new URL(request.url).pathname,
    contentType || null,
    sizeBytes,
    meta.ip,
    meta.country,
    meta.region,
    meta.city,
    meta.colo,
    meta.continent,
    meta.timezone,
    meta.latitude,
    meta.longitude,
    meta.postalCode,
    meta.metroCode,
    meta.asn,
    meta.asOrganization,
    clipText(meta.userAgent, 512),
    clipText(meta.acceptLanguage, 160),
    clipText(options.payloadJson, 16384),
    clipText(options.errorMessage, 1000),
    clipText(options.errorStack, 12000),
    clipText(options.errorFingerprint, 64)
  ).run();

  return key;
}

async function storeStructured(env, request, kind, installId, payload) {
  const sanitized = sanitizeObject(payload);
  const raw = JSON.stringify(sanitized);
  return putObject(env, request, kind, installId, raw, "application/json; charset=utf-8");
}

async function handlePing(request, env, ctx) {
  const url = new URL(request.url);
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = url.searchParams.get("id") || "";
  const modVersion = url.searchParams.get("mod");
  const lang = url.searchParams.get("lang");
  const steamId = readSteamIdParam(url);

  if (!installId)
    return json({ ok: false, error: "missing_install_id" }, 400);

  const existed = await env.DB.prepare(
    "SELECT install_id FROM installs WHERE install_id = ? LIMIT 1"
  ).bind(installId).first();

  await ensureInstall(env, installId, request, { modVersion, lang, steamId });
  await bumpCounter(env, "ping");
  const key = await storeStructured(env, request, "ping", installId, {
    id: installId,
    mod: modVersion,
    lang,
    steamId,
  });

  const countRow = await env.DB.prepare(
    "SELECT COUNT(*) AS count FROM installs"
  ).first();
  const totalInstalls = Number(countRow?.count || 0);

  enqueueOutbound(ctx, env, existed ? "ping" : "new_install", {
    installId,
    modVersion,
    lang,
    steamId,
    stored: key,
    totalInstalls,
    ...getCfMeta(request),
  });

  return json({
    ok: true,
    counted: !existed,
    installs: totalInstalls,
  });
}

async function handleSetNick(request, env, ctx) {
  const url = new URL(request.url);
  if (!mustGetToken(request, env))
    return json({ ok: false, error: "forbidden" }, 403);

  const installId = url.searchParams.get("id") || "";
  const nick = url.searchParams.get("nick") || "";
  const steamId = readSteamIdParam(url);

  if (!installId || !nick)
    return json({ ok: false, error: "missing_parameters" }, 400);

  await ensureInstall(env, installId, request, { nick, steamId });
  await bumpCounter(env, "setnick");
  const key = await storeStructured(env, request, "setnick", installId, {
    id: installId,
    nick,
    steamId,
  });
  enqueueOutbound(ctx, env, "setnick", {
    installId,
    nick,
    steamId,
    stored: key,
    ...getCfMeta(request),
  });

  return json({ ok: true, stored: key });
}

async function handleStructuredPost(request, env, ctx, kind) {
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
    steamId: readSteamId(data),
    os: data.os || null,
    cpu: data.cpu || null,
    gpu: data.gpu || null,
    ram: data.ram || null,
    screen: data.screen || null,
    gameVersion: data.gameVer || data.game_version || data.gameVersion || null,
    bepinex: data.bepinex || data.bepInEx || null,
    mods: data.mods || null,
  });
  const storedKind = normalizedStructuredKind(kind, data);
  if (storedKind === "client_log" && isNoisyClientLogPayload(data)) {
    await bumpCounter(env, "client_log_ignored");
    return json({ ok: true, ignored: true, kind: "client_noise" });
  }

  await bumpCounter(env, storedKind);

  const contentType = request.headers.get("content-type") || "application/json; charset=utf-8";
  const savedPayload = extractPayloadFields(storedKind, data);
  const key = await putObject(env, request, storedKind, installId, raw || "{}", contentType, savedPayload);
  enqueueOutbound(ctx, env, storedKind, {
    installId,
    modVersion: data.mod || data.gameVer || null,
    lang: data.lang || null,
    nick: data.nick || null,
    steamId: readSteamId(data),
    os: data.os || null,
    cpu: data.cpu || null,
    gpu: data.gpu || null,
    ram: data.ram || null,
    screen: data.screen || null,
    errorMessage: savedPayload.errorMessage || null,
    errorFingerprint: savedPayload.errorFingerprint || null,
    stored: key,
    ...getCfMeta(request),
  });

  return json({ ok: true, stored: key, kind: storedKind });
}

async function handleLegacyEventGet(request, env, ctx) {
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
  await bumpEventCounters(env, [payload]);
  const key = await storeStructured(env, request, "event", installId, payload);
  enqueueOutbound(ctx, env, "event_get", {
    installId,
    eventType: payload.type,
    eventName: payload.name,
    stored: key,
    ...getCfMeta(request),
  });

  return json({ ok: true, stored: key });
}

async function handleEventPost(request, env, ctx) {
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

    if (!Array.isArray(data) && isClientLogPayload(data)) {
      if (isNoisyClientLogPayload(data)) {
        await bumpCounter(env, "client_log_ignored");
        return json({ ok: true, ignored: true, kind: "client_noise" });
      }

      await ensureInstall(env, installId, request, {
        modVersion: data.mod || data.gameVer || null,
        lang: data.lang || null,
        nick: data.nick || null,
        steamId: readSteamId(data),
        os: data.os || null,
      });
      await bumpCounter(env, "client_log");
      const savedPayload = extractPayloadFields("client_log", data);
      const key = await putObject(env, request, "client_log", installId, raw || "{}", contentType, savedPayload);
      enqueueOutbound(ctx, env, "client_log", {
        installId,
        modVersion: data.mod || data.gameVer || null,
        lang: data.lang || null,
        nick: data.nick || null,
        steamId: readSteamId(data),
        os: data.os || null,
        errorMessage: savedPayload.errorMessage || null,
        errorFingerprint: savedPayload.errorFingerprint || null,
        stored: key,
        ...getCfMeta(request),
      });

      return json({ ok: true, stored: key, kind: "client_log" });
    }

    await ensureInstall(env, installId, request);
    await bumpCounter(env, "event_post");
    await bumpEventCounters(env, events);
    const key = await putObject(env, request, "event-batch", installId, raw || "[]", contentType);
    const accepted = events.filter(Boolean).length;
    enqueueOutbound(ctx, env, "event_post", {
      installId,
      accepted,
      stored: key,
      ...getCfMeta(request),
    });

    return json({
      ok: true,
      accepted,
      stored: key,
    });
  }

  return handleUpload(request, env, ctx, "event-upload");
}

async function handleUpload(request, env, ctx, forcedKind = null) {
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
  enqueueOutbound(ctx, env, kind, {
    installId,
    bytes: body.byteLength,
    stored: key,
    ...getCfMeta(request),
  });

  return json({
    ok: true,
    stored: key,
    bytes: body.byteLength,
  });
}

async function handleStats(request, env) {
  if (!mustGetAdminToken(request, env))
    return handlePublicSummary(request, env);

  const url = new URL(request.url);
  const limit = Math.max(1, Math.min(100, Number(url.searchParams.get("limit") || 20)));
  const [installs, online, active24h, active30d, launches, ret1, ret7, objects, counters, byDay, countries, cheats, recent, osRows, cpuRows, gpuRows, ramRows, bepinexRows, gameRows, providers, asns, timezones] = await Promise.all([
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-5 minutes')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-1 day')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-30 days')").first(),
    env.DB.prepare("SELECT COALESCE(SUM(value), 0) AS count FROM daily_counters WHERE metric = 'ping'").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE julianday(last_seen_at) - julianday(first_seen_at) >= 1").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE julianday(last_seen_at) - julianday(first_seen_at) >= 7").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM objects").first(),
    env.DB.prepare(
      `SELECT day, metric, value
        FROM daily_counters
        ORDER BY day DESC, metric ASC
        LIMIT 120`
    ).all(),
    env.DB.prepare(
      `SELECT day AS date, SUM(value) AS count
        FROM daily_counters
        WHERE metric = 'ping'
        GROUP BY day
        ORDER BY day ASC
        LIMIT 60`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_country, '??') AS cc, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_country, '??')
        ORDER BY count DESC
        LIMIT 25`
    ).all(),
    env.DB.prepare(
      `SELECT metric, SUM(value) AS count
        FROM daily_counters
        WHERE metric LIKE 'event:%'
        GROUP BY metric
        ORDER BY count DESC
        LIMIT 25`
    ).all(),
    env.DB.prepare(
      `SELECT kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, continent, timezone, asn, as_org, user_agent, accept_language, error_fingerprint, error_message, created_at
        FROM objects
        ORDER BY created_at DESC
        LIMIT ?`
    ).bind(limit).all(),
    env.DB.prepare(
      `SELECT COALESCE(os, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(os, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(cpu, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(cpu, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(gpu, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(gpu, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT
          CASE
            WHEN ram IS NULL THEN 'unknown'
            WHEN ram >= 32768 THEN '32 GB+'
            WHEN ram >= 16384 THEN '16-31 GB'
            WHEN ram >= 8192 THEN '8-15 GB'
            ELSE '<8 GB'
          END AS name,
          COUNT(*) AS count
        FROM installs
        GROUP BY name
        ORDER BY count DESC`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(bepinex, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(bepinex, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(game_version, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(game_version, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_as_org, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_as_org, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_asn, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_asn, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_timezone, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_timezone, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
  ]);

  const total = Number(installs?.count || 0);
  const dau = Number(active24h?.count || 0);
  const mau = Number(active30d?.count || 0);

  return json({
    ok: true,
    service: "peak-mx-telemetry",
    total,
    online: Number(online?.count || 0),
    dau,
    mau,
    launches: Number(launches?.count || 0),
    ret1: Number(ret1?.count || 0),
    ret7: Number(ret7?.count || 0),
    byDay: byDay?.results || [],
    countries: countries?.results || [],
    cheats: (cheats?.results || []).map((row) => ({
      name: String(row.metric || "").replace(/^event:[^:]+:/, ""),
      count: row.count,
    })),
    updated: new Date().toISOString(),
    now: new Date().toISOString(),
    totals: {
      installs: total,
      active24h: dau,
      active30d: mau,
      objects: Number(objects?.count || 0),
    },
    counters: counters?.results || [],
    systems: {
      os: osRows?.results || [],
      cpu: cpuRows?.results || [],
      gpu: gpuRows?.results || [],
      ram: ramRows?.results || [],
      bepinex: bepinexRows?.results || [],
      gameVersions: gameRows?.results || [],
    },
    network: {
      providers: providers?.results || [],
      asn: asns?.results || [],
      timezones: timezones?.results || [],
    },
    recent: (recent?.results || []).map((row) => ({
      ...row,
      install_id: row.install_id ? `${String(row.install_id).slice(0, 8)}...` : null,
    })),
  });
}

async function handlePublicSummary(request, env) {
  const [installs, online, active24h, active30d, launches, ret1, ret7, byDay, countries, cheats] = await Promise.all([
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-5 minutes')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-1 day')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-30 days')").first(),
    env.DB.prepare("SELECT COALESCE(SUM(value), 0) AS count FROM daily_counters WHERE metric = 'ping'").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE julianday(last_seen_at) - julianday(first_seen_at) >= 1").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE julianday(last_seen_at) - julianday(first_seen_at) >= 7").first(),
    env.DB.prepare(
      `SELECT day AS date, SUM(value) AS count
        FROM daily_counters
        WHERE metric = 'ping'
        GROUP BY day
        ORDER BY day ASC
        LIMIT 60`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_country, '??') AS cc, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_country, '??')
        ORDER BY count DESC
        LIMIT 25`
    ).all(),
    env.DB.prepare(
      `SELECT metric, SUM(value) AS count
        FROM daily_counters
        WHERE metric LIKE 'event:%'
        GROUP BY metric
        ORDER BY count DESC
        LIMIT 25`
    ).all(),
  ]);

  const total = Number(installs?.count || 0);
  const dau = Number(active24h?.count || 0);
  const mau = Number(active30d?.count || 0);

  return json({
    ok: true,
    service: "peak-mx-telemetry",
    public: true,
    total,
    online: Number(online?.count || 0),
    dau,
    mau,
    launches: Number(launches?.count || 0),
    ret1: Number(ret1?.count || 0),
    ret7: Number(ret7?.count || 0),
    byDay: byDay?.results || [],
    countries: countries?.results || [],
    cheats: (cheats?.results || []).map((row) => ({
      name: String(row.metric || "").replace(/^event:[^:]+:/, ""),
      count: row.count,
    })),
    totals: {
      installs: total,
      active24h: dau,
      active30d: mau,
    },
    updated: new Date().toISOString(),
    now: new Date().toISOString(),
  });
}

async function handleDonations(request, env) {
  const url = new URL(request.url);
  const goalTitle = envText(env, "PEAK-MX support", "DONATION_GOAL_TITLE", "DONATIONALERTS_GOAL_TITLE");
  const configuredGoalTarget = envNumber(env, "DONATION_GOAL_TARGET", "DONATIONALERTS_GOAL_TARGET");
  const manualRaised = envNumber(env, "DONATION_GOAL_RAISED", "DONATIONALERTS_GOAL_RAISED");
  const defaultCurrency = donationCurrency(envText(env, "RUB", "DONATION_GOAL_CURRENCY", "DONATIONALERTS_GOAL_CURRENCY"), "RUB");
  const manualSupporters = parseManualSupporters(env.DONATION_SUPPORTERS || env.DONATIONALERTS_SUPPORTERS);
  const goalRef = envText(env, "", "DONATIONALERTS_GOAL_URL", "DONATION_GOAL_URL", "DONATIONALERTS_GOAL_ID", "DONATION_GOAL_ID");
  const configuredToken = String(env.DONATIONALERTS_TOKEN || env.DONATION_ALERTS_TOKEN || "").trim();
  const oauthToken = String(
    env.DONATIONALERTS_OAUTH_TOKEN ||
    env.DONATION_ALERTS_OAUTH_TOKEN ||
    (looksLikeOAuthToken(configuredToken) ? configuredToken : "")
  ).trim().replace(/^bearer\s+/i, "");
  const widgetToken = String(
    env.DONATIONALERTS_WIDGET_TOKEN ||
    env.DONATION_ALERTS_WIDGET_TOKEN ||
    (!looksLikeOAuthToken(configuredToken) ? configuredToken : "")
  ).trim();
  const requestedLang = url.searchParams.get("lang") || url.searchParams.get("language") || url.searchParams.get("locale") || "";
  const displayCurrency = donationCurrencyForLang(url.searchParams.get("currency") || requestedLang, defaultCurrency);
  const limit = Math.max(1, Math.min(100, Number(url.searchParams.get("limit") || 100)));
  const fetchPages = Math.max(1, Math.min(10, Number(env.DONATION_FETCH_PAGES || env.DONATIONALERTS_FETCH_PAGES || 5)));
  const visibleDays = Math.max(1, Math.min(365, Number(env.DONATION_VISIBLE_DAYS || env.DONATIONALERTS_VISIBLE_DAYS || 60)));
  const minPublicAmount = Math.max(0, envNumber(env, "DONATION_MIN_PUBLIC_AMOUNT", "DONATIONALERTS_MIN_PUBLIC_AMOUNT") ?? 100);
  const defaultTargetRub = Math.max(1, envNumber(env, "DONATION_DEFAULT_TARGET_RUB", "DONATIONALERTS_DEFAULT_TARGET_RUB") ?? DONATION_DEFAULT_TARGET_RUB);
  const cutoffMs = Date.now() - visibleDays * 24 * 60 * 60 * 1000;
  const rates = await donationRatesFromRub(env);

  let donations = [];
  let donationGoal = null;
  let source = oauthToken ? "donationalerts_oauth" : (widgetToken ? "donationalerts_widget" : "manual");
  const fetchErrors = [];

  const tryFetchGoal = async (accessToken, recordError = false) => {
    try {
      const body = await fetchDonationAlertsJson("donationgoal", accessToken, {
        is_active: 0,
        include_timestamps: 1,
      });
      return normalizeDonationGoal(readGoalRow(body), goalTitle, defaultCurrency);
    } catch (error) {
      if (recordError)
        fetchErrors.push(error instanceof Error ? error.message : String(error));
      return null;
    }
  };

  if (oauthToken) {
    try {
      donations = await fetchDonationAlertsDonations(oauthToken, fetchPages);
      donationGoal = await tryFetchGoal(oauthToken, true);
      source = "donationalerts_oauth";
    } catch (error) {
      fetchErrors.push(error instanceof Error ? error.message : String(error));
      donations = [];
    }
  }

  if (widgetToken && donations.length <= 0) {
    try {
      let accessToken = "";
      try {
        accessToken = await fetchDonationAlertsAccessToken(widgetToken);
        donations = await fetchDonationAlertsDonations(accessToken, fetchPages);
      } catch {
        donations = await fetchDonationAlertsWidgetDonations(widgetToken);
      }

      if (goalRef) {
        try {
          donationGoal = await fetchDonationAlertsGoalFromWidget(widgetToken, goalRef, goalTitle, defaultCurrency);
        } catch (error) {
          fetchErrors.push(error instanceof Error ? error.message : String(error));
        }
      } else if (accessToken) {
        donationGoal = await tryFetchGoal(accessToken, false);
      }
      source = "donationalerts_widget";
    } catch (error) {
      fetchErrors.push(error instanceof Error ? error.message : String(error));
      source = "manual";
      donations = [];
    }
  }

  const recentEntries = filterRecentDonationEntries(donationEntries(donations, manualSupporters), cutoffMs, minPublicAmount, rates);
  const supporters = aggregateDonationEntries(recentEntries, displayCurrency, rates)
    .slice(0, limit)
    .map((supporter) => ({
      name: supporter.name,
      amount: Number(supporter.amount.toFixed(2)),
      currency: supporter.currency || displayCurrency,
      count: supporter.count,
      lastDonationAt: supporter.lastDonationAt,
    }));

  const baseGoalCurrency = configuredGoalTarget != null
    ? defaultCurrency
    : (donationGoal?.target ? donationCurrency(donationGoal?.currency || defaultCurrency, defaultCurrency) : "RUB");
  const baseGoalTarget = configuredGoalTarget != null
    ? configuredGoalTarget
    : (donationGoal?.target && donationGoal.target > 0 ? donationGoal.target : defaultTargetRub);
  const baseRaised = manualRaised != null
    ? manualRaised
    : (donationGoal?.target && donationGoal?.raised > 0 ? donationGoal.raised : sumDonationEntries(recentEntries, baseGoalCurrency, rates));
  const convertedTarget = convertDonationAmount(baseGoalTarget, baseGoalCurrency, displayCurrency, rates);
  const convertedRaised = convertDonationAmount(baseRaised, baseGoalCurrency, displayCurrency, rates);
  const goalTarget = convertedTarget == null ? baseGoalTarget : convertedTarget;
  const raised = convertedRaised == null ? baseRaised : convertedRaised;
  const goalCurrency = convertedTarget == null || convertedRaised == null ? baseGoalCurrency : displayCurrency;
  const percent = goalTarget && goalTarget > 0 ? Math.max(0, Math.min(999, raised / goalTarget * 100)) : null;

  const response = {
    ok: true,
    service: "peak-mx-telemetry",
    public: true,
    source,
    goal: {
      title: donationGoal?.title || goalTitle,
      raised: Number((raised || 0).toFixed(2)),
      target: Number((goalTarget || 0).toFixed(2)),
      currency: goalCurrency,
      percent: percent == null ? null : Number(percent.toFixed(1)),
    },
    supporters,
    hiddenOlderThanDays: visibleDays,
    minPublicAmount,
    updated: new Date().toISOString(),
  };
  if (fetchErrors.length > 0)
    response.warning = fetchErrors.slice(-2).join("; ");

  return json(response, 200, {
    "cache-control": "public, max-age=300",
  });
}

function adminIds(env) {
  return String(env.TELEGRAM_ADMIN_IDS || env.TELEGRAM_CHAT_ID || "")
    .split(",")
    .map((id) => id.trim())
    .filter(Boolean);
}

function isTelegramAdmin(env, message) {
  const allowed = new Set(adminIds(env));
  if (!allowed.size)
    return false;

  const fromId = message?.from?.id != null ? String(message.from.id) : "";
  const chatId = message?.chat?.id != null ? String(message.chat.id) : "";
  return allowed.has(fromId) || allowed.has(chatId);
}

function botKeyboard() {
  return {
    inline_keyboard: [
      [
        { text: "📊 Сводка", callback_data: "/stats" },
        { text: "🧑 Клиенты", callback_data: "/last 10" },
      ],
      [
        { text: "🔥 Топ действий", callback_data: "/top" },
        { text: "🌍 Страны", callback_data: "/countries" },
      ],
      [
        { text: "🌐 Языки", callback_data: "/langs" },
        { text: "🏷️ Версии", callback_data: "/versions" },
      ],
      [
        { text: "🕘 Последнее", callback_data: "/recent" },
        { text: "💥 Ошибки", callback_data: "/errors 10" },
      ],
      [
        { text: "🧯 Группы крашей", callback_data: "/crashgroups 10" },
        { text: "📅 Сегодня", callback_data: "/today" },
      ],
      [
        { text: "📤 CSV", callback_data: "/export" },
        { text: "📦 Объекты", callback_data: "/objects 10" },
      ],
      [
        { text: "🛣 Пути", callback_data: "/paths" },
        { text: "🧩 Типы", callback_data: "/types" },
      ],
      [
        { text: "🌐 IP/Colo", callback_data: "/network" },
        { text: "📆 Дни", callback_data: "/days" },
      ],
      [
        { text: "💻 Системы", callback_data: "/systems" },
        { text: "🧬 Моды", callback_data: "/mods" },
      ],
      [
        { text: "🔔 Уведомления", callback_data: "/notify" },
        { text: "💚 Статус", callback_data: "/health" },
      ],
      [
        { text: "❔ Помощь", callback_data: "/help" },
      ],
    ],
  };
}

async function botStats(env) {
  const [installs, online, active24h, active30d, launches, objects, top, countries, recent, latestInstalls, crashes, versions, langs, today] = await Promise.all([
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-5 minutes')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-1 day')").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM installs WHERE last_seen_at >= datetime('now', '-30 days')").first(),
    env.DB.prepare("SELECT COALESCE(SUM(value), 0) AS count FROM daily_counters WHERE metric = 'ping'").first(),
    env.DB.prepare("SELECT COUNT(*) AS count FROM objects").first(),
    env.DB.prepare(
      `SELECT metric, SUM(value) AS count
        FROM daily_counters
        WHERE metric LIKE 'event:%'
        GROUP BY metric
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_country, '??') AS cc, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_country, '??')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT object_key, kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, asn, as_org, error_message, error_stack, error_fingerprint, created_at
        FROM objects
        ORDER BY created_at DESC
        LIMIT 8`
    ).all(),
    env.DB.prepare(
      `SELECT i.install_id, i.nick, i.steam_id, i.mod_version, i.lang, i.os, i.last_ip, i.last_country, i.last_region, i.last_city, i.first_seen_at, i.last_seen_at,
          (SELECT COUNT(*) FROM objects o WHERE o.install_id = i.install_id AND o.kind = 'ping') AS launches
        FROM installs i
        ORDER BY last_seen_at DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT object_key, kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, asn, as_org, error_message, error_stack, error_fingerprint, created_at
        FROM objects
        WHERE kind IN ('crash', 'client_log')
        ORDER BY created_at DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(mod_version, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(mod_version, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(lang, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(lang, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT metric, value
        FROM daily_counters
        WHERE day = ?
        ORDER BY value DESC, metric ASC
        LIMIT 20`
    ).bind(isoDay()).all(),
  ]);

  return {
    installs: Number(installs?.count || 0),
    online: Number(online?.count || 0),
    active24h: Number(active24h?.count || 0),
    active30d: Number(active30d?.count || 0),
    launches: Number(launches?.count || 0),
    objects: Number(objects?.count || 0),
    top: top?.results || [],
    countries: countries?.results || [],
    recent: recent?.results || [],
    latestInstalls: latestInstalls?.results || [],
    crashes: crashes?.results || [],
    versions: versions?.results || [],
    langs: langs?.results || [],
    today: today?.results || [],
  };
}

function formatBotStats(data) {
  const topAction = data.top[0]
    ? `${String(data.top[0].metric || "").replace(/^event:[^:]+:/, "")} (${data.top[0].count})`
    : "нет";
  const topCountry = data.countries[0]
    ? `${data.countries[0].cc || "??"} (${data.countries[0].count})`
    : "нет";

  return [
    panelTitle("🟢", "PEAK-MX: админ-панель", "живые данные Worker"),
    "",
    `👥 Установки: <b>${escapeHtml(prettyNumber(data.installs))}</b>`,
    `🟢 Онлайн 5 мин: <b>${escapeHtml(prettyNumber(data.online))}</b>`,
    `☀️ Активны 24ч: <b>${escapeHtml(prettyNumber(data.active24h))}</b>`,
    `📅 Активны 30д: <b>${escapeHtml(prettyNumber(data.active30d))}</b>`,
    `🚀 Запуски: <b>${escapeHtml(prettyNumber(data.launches))}</b>`,
    `📦 События/объекты: <b>${escapeHtml(prettyNumber(data.objects))}</b>`,
    `🔥 Топ действие: <code>${escapeHtml(topAction)}</code>`,
    `🌍 Топ страна: <code>${escapeHtml(topCountry)}</code>`,
    `💥 Последние ошибки: <b>${escapeHtml(data.crashes.length)}</b>`,
    "",
    `🕘 Обновлено: <code>${escapeHtml(new Date().toISOString())}</code>`,
  ].join("\n");
}

function formatTop(data) {
  const rows = data.top.map((row, index) => {
    const name = String(row.metric || "").replace(/^event:[^:]+:/, "");
    return `${index + 1}. <code>${escapeHtml(name)}</code> — <b>${escapeHtml(row.count)}</b>`;
  });
  return `${panelTitle("🔥", "Топ действий PEAK-MX")}\n\n${rows.length ? rows.join("\n") : "Данных пока нет."}`;
}

function formatCountries(data) {
  const rows = data.countries.map((row, index) =>
    `${index + 1}. <code>${escapeHtml(row.cc || "??")}</code> — <b>${escapeHtml(row.count)}</b>`
  );
  return `${panelTitle("🌍", "Топ стран")}\n\n${rows.length ? rows.join("\n") : "Данных пока нет."}`;
}

function formatRecent(data) {
  const cards = data.recent.map((row, index) =>
    isErrorObjectKind(row.kind) ? formatCrashCard(row, index + 1) : formatObjectCard(row, index + 1)
  );
  return joinLimitedCards(panelTitle("🕘", "Последние события"), cards, "Данных пока нет.");
}

function shortInstallId(id) {
  return id ? `${String(id).slice(0, 8)}...` : "аноним";
}

function formatInstalls(data) {
  const cards = data.latestInstalls.map((row, index) => formatClientCard(row, { index: index + 1 }));
  return joinLimitedCards(panelTitle("🧑", "Последние установки / активные клиенты"), cards, "Данных пока нет.");
}

function formatCrashes(data) {
  const cards = data.crashes.map((row, index) => formatCrashCard(row, index + 1));
  return joinLimitedCards(panelTitle("💥", "Последние краши/ошибки"), cards, "Крашей и ошибок пока нет.");
}

function formatPairs(title, rows) {
  const lines = rows.map((row, index) =>
    `${index + 1}. ${codeValue(row.name || row.metric, "unknown", 120)} - <b>${escapeHtml(row.count ?? row.value ?? 0)}</b>`
  );
  return `${panelTitle("📌", title)}\n\n${lines.length ? lines.join("\n") : "Данных пока нет."}`;
}

function formatToday(data) {
  const rows = data.today.map((row, index) =>
    `${index + 1}. <code>${escapeHtml(row.metric)}</code> — <b>${escapeHtml(row.value)}</b>`
  );
  return `${panelTitle("📅", "Счётчики за сегодня")}\n\n${rows.length ? rows.join("\n") : "Сегодня данных пока нет."}`;
}

function formatHelp() {
  return [
    panelTitle("🟢", "PEAK-MX — админ-бот", "всё, что видит Worker"),
    "",
    "<b>Команды:</b>",
    "<code>/stats</code> — общая статистика",
    "<code>/last [N]</code> — последние установки",
    "<code>/find &lt;запрос&gt;</code> — поиск по нику/IP/стране/SteamID/GUID",
    "<code>/info &lt;GUID или ник&gt;</code> — полная карточка клиента",
    "<code>/errors [N]</code> — последние краши/ошибки",
    "<code>/crashgroups [N]</code> — группировка крашей по fingerprint/размеру",
    "<code>/notify [on|off]</code> — уведомления о новых событиях",
    "<code>/export</code> — выгрузка клиентов в CSV",
    "<code>/export events</code> — выгрузка событий в CSV",
    "",
    "<b>Дополнительно:</b>",
    "<code>/top</code> — самые используемые функции",
    "<code>/countries</code> — установки по странам",
    "<code>/langs</code> — языки клиентов",
    "<code>/versions</code> — версии мода",
    "<code>/recent</code> — последние сохранённые события",
    "<code>/objects [N]</code> — подробные строки objects",
    "<code>/clientevents &lt;GUID|ник&gt; [N]</code> — события одного клиента",
    "<code>/types</code> — типы сохранённых данных",
    "<code>/paths</code> — использование endpoint'ов",
    "<code>/network</code> — IP/страны/города/colo",
    "<code>/systems</code> — ОС, CPU, GPU, RAM, BepInEx и версии игры",
    "<code>/mods</code> — последние списки модов клиентов",
    "<code>/days</code> — дневная история",
    "<code>/today</code> — сырые счётчики за сегодня",
    "<code>/health</code> — состояние Worker",
  ].join("\n");
}

function parseLimit(parts, fallback = 10, max = 50) {
  const raw = Number(parts[1] || fallback);
  if (!Number.isFinite(raw))
    return fallback;
  return Math.max(1, Math.min(max, Math.floor(raw)));
}

function csvEscape(value) {
  const textValue = String(value ?? "");
  if (/[",\r\n]/.test(textValue))
    return `"${textValue.replace(/"/g, '""')}"`;
  return textValue;
}

async function getBotSetting(env, key, fallback = null) {
  try {
    const row = await env.DB.prepare("SELECT value FROM bot_settings WHERE key = ?").bind(key).first();
    return row?.value ?? fallback;
  } catch {
    return fallback;
  }
}

async function setBotSetting(env, key, value) {
  await env.DB.prepare(
    `INSERT INTO bot_settings(key, value)
      VALUES (?, ?)
      ON CONFLICT(key) DO UPDATE SET value = excluded.value`
  ).bind(key, value).run();
}

async function formatLastInstalls(env, limit) {
  const rows = await env.DB.prepare(
    `SELECT i.install_id, i.nick, i.steam_id, i.mod_version, i.lang, i.os, i.game_version,
        i.last_ip, i.last_country, i.last_region, i.last_city, i.first_seen_at, i.last_seen_at,
        (SELECT COUNT(*) FROM objects o WHERE o.install_id = i.install_id AND o.kind = 'ping') AS launches
      FROM installs i
      ORDER BY i.last_seen_at DESC
      LIMIT ?`
  ).bind(limit).all();

  const cards = (rows?.results || []).map((row, index) => formatClientCard(row, { index: index + 1 }));

  return joinLimitedCards(`🕓 <b>Последние ${escapeHtml(String(limit))}</b>`, cards, "Установок пока нет.");
}

async function formatFind(env, query) {
  const q = `%${query}%`;
  const rows = await env.DB.prepare(
    `SELECT i.install_id, i.nick, i.steam_id, i.mod_version, i.lang, i.os, i.game_version,
        i.last_ip, i.last_country, i.last_region, i.last_city, i.last_asn, i.last_as_org, i.last_timezone, i.last_user_agent, i.last_seen_at,
        (SELECT COUNT(*) FROM objects o WHERE o.install_id = i.install_id AND o.kind = 'ping') AS launches
      FROM installs i
      WHERE i.install_id LIKE ?
        OR COALESCE(i.nick, '') LIKE ?
        OR COALESCE(i.steam_id, '') LIKE ?
        OR COALESCE(i.last_ip, '') LIKE ?
        OR COALESCE(i.last_country, '') LIKE ?
        OR COALESCE(i.last_region, '') LIKE ?
        OR COALESCE(i.last_city, '') LIKE ?
        OR COALESCE(i.mod_version, '') LIKE ?
        OR COALESCE(i.lang, '') LIKE ?
        OR COALESCE(i.os, '') LIKE ?
        OR COALESCE(i.cpu, '') LIKE ?
        OR COALESCE(i.gpu, '') LIKE ?
        OR COALESCE(i.last_as_org, '') LIKE ?
        OR COALESCE(i.last_user_agent, '') LIKE ?
      ORDER BY i.last_seen_at DESC
      LIMIT 15`
  ).bind(q, q, q, q, q, q, q, q, q, q, q, q, q, q).all();

  const cards = (rows?.results || []).map((row, index) => formatClientCard(row, { index: index + 1 }));

  return joinLimitedCards(`🔎 <b>Поиск:</b> <code>${escapeHtml(query)}</code>`, cards, "Ничего не найдено.");
}

async function formatInfo(env, query) {
  const row = await env.DB.prepare(
    `SELECT i.*,
        (SELECT COUNT(*) FROM objects o WHERE o.install_id = i.install_id AND o.kind = 'ping') AS launches
      FROM installs i
      WHERE i.install_id = ?
        OR i.nick = ?
        OR i.steam_id = ?
        OR i.install_id LIKE ?
        OR COALESCE(i.nick, '') LIKE ?
        OR COALESCE(i.steam_id, '') LIKE ?
      ORDER BY i.last_seen_at DESC
      LIMIT 1`
  ).bind(query, query, query, `%${query}%`, `%${query}%`, `%${query}%`).first();

  if (!row)
    return `<b>Клиент не найден:</b> <code>${escapeHtml(query)}</code>`;

  const [counts, recent, totals] = await Promise.all([
    env.DB.prepare(
      `SELECT kind, COUNT(*) AS count
        FROM objects
        WHERE install_id = ?
        GROUP BY kind
        ORDER BY count DESC`
    ).bind(row.install_id).all(),
    env.DB.prepare(
      `SELECT object_key, kind, request_path, content_type, size_bytes, ip, country, region, city, colo, asn, as_org, error_message, error_stack, error_fingerprint, created_at
        FROM objects
        WHERE install_id = ?
        ORDER BY created_at DESC
        LIMIT 8`
    ).bind(row.install_id).all(),
    env.DB.prepare(
      `SELECT COUNT(*) AS count, COALESCE(SUM(size_bytes), 0) AS bytes
        FROM objects
        WHERE install_id = ?`
    ).bind(row.install_id).first(),
  ]);

  const countLines = (counts?.results || []).map((item) => `• ${codeValue(item.kind, "unknown", 80)}: <b>${escapeHtml(item.count)}</b>`).join("\n") || "Событий пока нет.";
  const recentCards = (recent?.results || []).map((item, index) =>
    isErrorObjectKind(item.kind) ? formatCrashCard(item, index + 1) : formatObjectCard(item, index + 1)
  );

  const header = [
    panelTitle("🪪", "Карточка клиента"),
    "",
    formatClientCard(row, { detailed: true, showFirst: true }),
    "",
    `📊 Всего объектов: <b>${escapeHtml(prettyNumber(totals?.count || 0))}</b>  ·  ${escapeHtml(prettyNumber(totals?.bytes || 0))} байт`,
    "",
    "<b>События:</b>",
    countLines,
    "",
    "<b>Последнее:</b>",
  ].join("\n");
  return joinLimitedCards(header, recentCards, "Событий пока нет.");
}

async function formatCrashGroups(env, limit) {
  const rows = await env.DB.prepare(
    `SELECT
        COALESCE(error_fingerprint, 'legacy-size-' || size_bytes) AS fingerprint,
        COALESCE(MAX(error_message), 'старый crash-report без текста') AS message,
        COALESCE(MAX(firstLine), '') AS stack_first_line,
        COUNT(*) AS count,
        COUNT(DISTINCT install_id) AS clients,
        MIN(created_at) AS first_at,
        MAX(created_at) AS last_at,
        MIN(size_bytes) AS min_bytes,
        MAX(size_bytes) AS max_bytes
      FROM (
        SELECT
          error_fingerprint,
          error_message,
          CASE
            WHEN error_stack IS NULL THEN NULL
            WHEN instr(error_stack, char(10)) > 0 THEN substr(error_stack, 1, instr(error_stack, char(10)) - 1)
            ELSE error_stack
          END AS firstLine,
          install_id,
          created_at,
          size_bytes
        FROM objects
        WHERE kind = 'crash'
          AND error_message IS NOT NULL
      )
      GROUP BY COALESCE(error_fingerprint, 'legacy-size-' || size_bytes)
      ORDER BY count DESC, last_at DESC
      LIMIT ?`
  ).bind(limit).all();

  const cards = (rows?.results || []).map((row, index) => [
    `${index + 1}. 💥 <b>${htmlValue(row.message, "сообщение неизвестно", 220)}</b>`,
    `🧬 ${codeValue(row.fingerprint, "fingerprint нет", 80)}  ·  🔁 ${escapeHtml(row.count)}  ·  👥 ${escapeHtml(row.clients)}`,
    `📦 ${escapeHtml(row.min_bytes)}-${escapeHtml(row.max_bytes)} байт  ·  🕓 ${codeValue(shortTime(row.first_at), "?", 24)} → ${codeValue(shortTime(row.last_at), "?", 24)}`,
    row.stack_first_line ? `📍 ${htmlValue(row.stack_first_line, "stack пустой", 260)}` : "",
  ].filter(Boolean).join("\n"));

  return joinLimitedCards(panelTitle("🧯", "Группы крашей"), cards, "Крашей пока нет.");
}

async function formatErrors(env, limit) {
  const rows = await env.DB.prepare(
    `SELECT object_key, kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, asn, as_org, error_message, error_stack, error_fingerprint, created_at
      FROM objects
      WHERE kind IN ('crash', 'client_log', 'diag')
      ORDER BY created_at DESC
      LIMIT ?`
  ).bind(limit).all();

  const cards = (rows?.results || []).map((row, index) =>
    isErrorObjectKind(row.kind) ? formatCrashCard(row, index + 1) : formatObjectCard(row, index + 1)
  );

  return joinLimitedCards(panelTitle("💥", "Последние краши/ошибки"), cards, "Ошибок пока нет.");
}

async function formatObjects(env, limit) {
  const rows = await env.DB.prepare(
    `SELECT object_key, kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, continent, timezone, asn, as_org, user_agent, accept_language, error_message, error_stack, error_fingerprint, created_at
      FROM objects
      ORDER BY created_at DESC
      LIMIT ?`
  ).bind(limit).all();

  const cards = (rows?.results || []).map((row, index) => {
    const card = isErrorObjectKind(row.kind) ? formatCrashCard(row, index + 1) : formatObjectCard(row, index + 1);
    const extra = [
      row.timezone || row.continent ? `🧭 ${htmlValue(row.timezone, "timezone ?", 80)}  ·  ${htmlValue(row.continent, "континент ?", 32)}` : "",
      row.accept_language ? `🗣 HTTP: ${htmlValue(row.accept_language, "неизвестно", 120)}` : "",
      row.user_agent ? `🧰 UA: ${htmlValue(row.user_agent, "неизвестно", 180)}` : "",
    ].filter(Boolean).join("\n");
    return extra ? `${card}\n${extra}` : card;
  });

  return joinLimitedCards(panelTitle("📦", "Сохранённые объекты"), cards, "Объектов пока нет.");
}

async function formatClientEvents(env, query, limit) {
  const client = await env.DB.prepare(
    `SELECT i.install_id, i.nick, i.steam_id, i.mod_version, i.lang, i.os, i.last_ip, i.last_country, i.last_region, i.last_city, i.last_seen_at,
        (SELECT COUNT(*) FROM objects o WHERE o.install_id = i.install_id AND o.kind = 'ping') AS launches
      FROM installs i
      WHERE i.install_id = ?
        OR i.nick = ?
        OR i.steam_id = ?
        OR i.install_id LIKE ?
        OR COALESCE(i.nick, '') LIKE ?
        OR COALESCE(i.steam_id, '') LIKE ?
      ORDER BY i.last_seen_at DESC
      LIMIT 1`
  ).bind(query, query, query, `%${query}%`, `%${query}%`, `%${query}%`).first();

  if (!client)
    return `<b>Клиент не найден:</b> <code>${escapeHtml(query)}</code>`;

  const rows = await env.DB.prepare(
    `SELECT object_key, kind, request_path, content_type, size_bytes, ip, country, region, city, colo, asn, as_org, error_message, error_stack, error_fingerprint, created_at
      FROM objects
      WHERE install_id = ?
      ORDER BY created_at DESC
      LIMIT ?`
  ).bind(client.install_id, limit).all();

  const cards = (rows?.results || []).map((row, index) => {
    const objectRow = { install_id: client.install_id, ...row };
    return isErrorObjectKind(row.kind) ? formatCrashCard(objectRow, index + 1) : formatObjectCard(objectRow, index + 1);
  });

  return joinLimitedCards(
    `${panelTitle("🧾", "События клиента")}\n${formatClientCard(client)}`,
    cards,
    "У клиента пока нет событий."
  );
}

async function formatTypes(env) {
  const rows = await env.DB.prepare(
    `SELECT kind AS name, COUNT(*) AS count, SUM(size_bytes) AS bytes
      FROM objects
      GROUP BY kind
      ORDER BY count DESC
      LIMIT 30`
  ).all();

  const lines = (rows?.results || []).map((row, index) =>
    `${index + 1}. <code>${escapeHtml(row.name)}</code> — <b>${escapeHtml(row.count)}</b> · ${escapeHtml(row.bytes || 0)} байт`
  );
  return `${panelTitle("🧩", "Типы объектов")}\n\n${lines.length ? lines.join("\n") : "Данных пока нет."}`;
}

async function formatPaths(env) {
  const rows = await env.DB.prepare(
    `SELECT request_path AS name, COUNT(*) AS count, SUM(size_bytes) AS bytes
      FROM objects
      GROUP BY request_path
      ORDER BY count DESC
      LIMIT 30`
  ).all();

  const lines = (rows?.results || []).map((row, index) =>
    `${index + 1}. <code>${escapeHtml(row.name)}</code> — <b>${escapeHtml(row.count)}</b> · ${escapeHtml(row.bytes || 0)} байт`
  );
  return `${panelTitle("🛣", "Использование endpoint'ов")}\n\n${lines.length ? lines.join("\n") : "Данных пока нет."}`;
}

async function formatNetwork(env) {
  const [ips, countries, colos, cities, asns, orgs, timezones, continents, agents] = await Promise.all([
    env.DB.prepare(
      `SELECT COALESCE(last_ip, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_ip, 'unknown')
        ORDER BY count DESC
        LIMIT 15`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_country, '??') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_country, '??')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(colo, 'unknown') AS name, COUNT(*) AS count
        FROM objects
        GROUP BY COALESCE(colo, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(city, 'unknown') AS name, COUNT(*) AS count
        FROM objects
        GROUP BY COALESCE(city, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_asn, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_asn, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_as_org, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_as_org, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_timezone, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_timezone, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_continent, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_continent, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(last_user_agent, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(last_user_agent, 'unknown')
        ORDER BY count DESC
        LIMIT 10`
    ).all(),
  ]);

  const block = (title, rows) => {
    const maxRows = title === "User-Agent" ? 2 : 4;
    const lines = (rows?.results || []).slice(0, maxRows).map((row, index) =>
      `${index + 1}. ${codeValue(row.name, "unknown", 80)} - <b>${escapeHtml(row.count)}</b>`
    );
    return `<b>${escapeHtml(title)}</b>\n${lines.length ? lines.join("\n") : "Данных пока нет."}`;
  };

  return [
    panelTitle("🌐", "Сеть и география"),
    "",
    block("Топ IP", ips),
    "",
    block("Страны", countries),
    "",
    block("Cloudflare colo", colos),
    "",
    block("Города", cities),
    "",
    block("ASN", asns),
    "",
    block("Провайдеры", orgs),
    "",
    block("Timezone", timezones),
    "",
    block("Континенты", continents),
    "",
    block("User-Agent", agents),
  ].join("\n");
}

async function formatSystems(env) {
  const [osRows, cpuRows, gpuRows, ramRows, screenRows, bepinexRows, gameRows] = await Promise.all([
    env.DB.prepare(
      `SELECT COALESCE(os, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(os, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(cpu, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(cpu, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(gpu, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(gpu, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
    env.DB.prepare(
      `SELECT
          CASE
            WHEN ram IS NULL THEN 'unknown'
            WHEN ram >= 32768 THEN '32 GB+'
            WHEN ram >= 16384 THEN '16-31 GB'
            WHEN ram >= 8192 THEN '8-15 GB'
            ELSE '<8 GB'
          END AS name,
          COUNT(*) AS count
        FROM installs
        GROUP BY name
        ORDER BY count DESC`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(screen, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(screen, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(bepinex, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(bepinex, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
    env.DB.prepare(
      `SELECT COALESCE(game_version, 'unknown') AS name, COUNT(*) AS count
        FROM installs
        GROUP BY COALESCE(game_version, 'unknown')
        ORDER BY count DESC
        LIMIT 5`
    ).all(),
  ]);

  const block = (title, rows) => {
    const lines = (rows?.results || []).map((row, index) =>
      `${index + 1}. ${codeValue(row.name, "unknown", 100)} - <b>${escapeHtml(row.count)}</b>`
    );
    return `<b>${escapeHtml(title)}</b>\n${lines.length ? lines.join("\n") : "Данных пока нет."}`;
  };

  return [
    panelTitle("💻", "Системы клиентов"),
    "",
    block("ОС", osRows),
    "",
    block("CPU", cpuRows),
    "",
    block("GPU", gpuRows),
    "",
    block("RAM", ramRows),
    "",
    block("Экраны", screenRows),
    "",
    block("BepInEx", bepinexRows),
    "",
    block("Версии игры", gameRows),
  ].join("\n");
}

async function formatMods(env) {
  const rows = await env.DB.prepare(
    `SELECT install_id, nick, steam_id, mod_version, lang, os, mods, last_ip, last_country, last_region, last_city, last_seen_at
      FROM installs
      WHERE mods IS NOT NULL AND TRIM(mods) <> ''
      ORDER BY last_seen_at DESC
      LIMIT 20`
  ).all();

  const cards = (rows?.results || []).map((row, index) => [
    formatClientCard({ ...row, launches: row.launches ?? 0 }, { index: index + 1 }),
    `🧬 ${htmlValue(row.mods, "модов нет", 900)}`,
  ].join("\n"));

  return joinLimitedCards(panelTitle("🧬", "Моды клиентов", "последние диагностические списки"), cards, "Списков модов пока нет.");
}

async function formatDays(env) {
  const rows = await env.DB.prepare(
    `SELECT day, metric, value
      FROM daily_counters
      ORDER BY day DESC, value DESC, metric ASC
      LIMIT 60`
  ).all();

  const grouped = new Map();
  for (const row of rows?.results || []) {
    if (!grouped.has(row.day))
      grouped.set(row.day, []);
    grouped.get(row.day).push(`${escapeHtml(row.metric)}=${escapeHtml(row.value)}`);
  }

  const lines = [...grouped.entries()].map(([day, values]) =>
    `<b>${escapeHtml(day)}</b>\n${values.slice(0, 10).join("\n")}`
  );

  return `${panelTitle("📆", "Дневная история")}\n\n${lines.length ? lines.join("\n\n") : "Счётчиков пока нет."}`;
}

async function handleNotifyCommand(env, chatId, parts) {
  const value = (parts[1] || "").toLowerCase();
  if (value === "on" || value === "off") {
    await setBotSetting(env, "notify", value);
    return sendTelegramMessage(env, chatId, `🔔 Уведомления теперь: <b>${value === "on" ? "включены" : "выключены"}</b>.`, { reply_markup: botKeyboard() });
  }

  const current = await getBotSetting(env, "notify", "on");
  return sendTelegramMessage(env, chatId, `🔔 Уведомления: <b>${current === "on" ? "включены" : "выключены"}</b>\n\nИспользуй <code>/notify on</code> или <code>/notify off</code>.`, { reply_markup: botKeyboard() });
}

async function handleExportCommand(env, chatId, mode = "") {
  mode = String(mode || "").toLowerCase();
  const exportEvents = mode === "events" || mode === "objects";
  const rows = exportEvents
    ? await env.DB.prepare(
      `SELECT object_key, kind, install_id, request_path, content_type, size_bytes, ip, country, region, city, colo, continent, timezone, latitude, longitude, postal_code, metro_code, asn, as_org, user_agent, accept_language, error_fingerprint, error_message, error_stack, payload_json, created_at
        FROM objects
        ORDER BY created_at DESC`
    ).all()
    : await env.DB.prepare(
      `SELECT install_id, nick, steam_id, mod_version, lang, os, cpu, gpu, ram, screen, game_version, bepinex, mods,
          last_ip, last_country, last_region, last_city, last_continent, last_timezone, last_latitude, last_longitude,
          last_postal_code, last_metro_code, last_asn, last_as_org, last_user_agent, last_accept_language, first_seen_at, last_seen_at
        FROM installs
        ORDER BY last_seen_at DESC`
    ).all();

  const header = exportEvents
    ? ["object_key", "kind", "install_id", "request_path", "content_type", "size_bytes", "ip", "country", "region", "city", "colo", "continent", "timezone", "latitude", "longitude", "postal_code", "metro_code", "asn", "as_org", "user_agent", "accept_language", "error_fingerprint", "error_message", "error_stack", "payload_json", "created_at"]
    : ["install_id", "nick", "steam_id", "mod_version", "lang", "os", "cpu", "gpu", "ram", "screen", "game_version", "bepinex", "mods", "last_ip", "last_country", "last_region", "last_city", "last_continent", "last_timezone", "last_latitude", "last_longitude", "last_postal_code", "last_metro_code", "last_asn", "last_as_org", "last_user_agent", "last_accept_language", "first_seen_at", "last_seen_at"];
  const csvRows = [header.join(",")];
  for (const row of rows?.results || [])
    csvRows.push(header.map((key) => csvEscape(row[key])).join(","));

  const form = new FormData();
  form.append("chat_id", String(chatId));
  form.append("caption", exportEvents ? "PEAK-MX: экспорт событий" : "PEAK-MX: экспорт клиентов");
  form.append("document", new Blob([csvRows.join("\r\n")], { type: "text/csv;charset=utf-8" }), `peak-mx-${exportEvents ? "events" : "clients"}-${isoDay()}.csv`);

  const response = await fetch(`https://api.telegram.org/bot${env.TELEGRAM_BOT_TOKEN}/sendDocument`, {
    method: "POST",
    body: form,
  });

  if (!response.ok)
    throw new Error(`telegram_export_${response.status}`);
}

async function handleBotCommand(env, chatId, command, options = {}) {
  const parts = String(command || "/stats").trim().split(/\s+/);
  const normalized = parts[0].toLowerCase().replace(/@.+$/, "");
  const replyOptions = { reply_markup: botKeyboard(), ...options };

  if (normalized === "/start" || normalized === "/help")
    return replyBot(env, chatId, formatHelp(), replyOptions);
  if (normalized === "/notify")
    return handleNotifyCommand(env, chatId, parts);
  if (normalized === "/export")
    return handleExportCommand(env, chatId, parts[1] || "");

  const data = await botStats(env);

  if (normalized === "/stats")
    return replyBot(env, chatId, formatBotStats(data), replyOptions);
  if (normalized === "/installs" || normalized === "/last")
    return replyBot(env, chatId, await formatLastInstalls(env, parseLimit(parts, 10, 50)), replyOptions);
  if (normalized === "/find")
    return replyBot(env, chatId, parts.slice(1).join(" ").trim() ? await formatFind(env, parts.slice(1).join(" ").trim()) : "🔎 <b>Поиск</b>\n\nИспользуй <code>/find запрос</code>\nИщет по нику, IP, стране, городу, SteamID, GUID, версии и языку.", replyOptions);
  if (normalized === "/info")
    return replyBot(env, chatId, parts.slice(1).join(" ").trim() ? await formatInfo(env, parts.slice(1).join(" ").trim()) : "Использование: <code>/info GUID_или_ник</code>", replyOptions);
  if (normalized === "/errors" || normalized === "/crashes")
    return replyBot(env, chatId, await formatErrors(env, parseLimit(parts, 10, 50)), replyOptions);
  if (normalized === "/crashgroups")
    return replyBot(env, chatId, await formatCrashGroups(env, parseLimit(parts, 10, 30)), replyOptions);
  if (normalized === "/top")
    return replyBot(env, chatId, formatTop(data), replyOptions);
  if (normalized === "/countries")
    return replyBot(env, chatId, formatCountries(data), replyOptions);
  if (normalized === "/langs")
    return replyBot(env, chatId, formatPairs("Языки", data.langs), replyOptions);
  if (normalized === "/versions")
    return replyBot(env, chatId, formatPairs("Версии", data.versions), replyOptions);
  if (normalized === "/recent")
    return replyBot(env, chatId, formatRecent(data), replyOptions);
  if (normalized === "/objects")
    return replyBot(env, chatId, await formatObjects(env, parseLimit(parts, 10, 30)), replyOptions);
  if (normalized === "/clientevents")
    return replyBot(env, chatId, parts.slice(1).join(" ").trim() ? await formatClientEvents(env, parts[1], parseLimit(["", parts[2]], 10, 30)) : "Использование: <code>/clientevents GUID_или_ник [N]</code>", replyOptions);
  if (normalized === "/types")
    return replyBot(env, chatId, await formatTypes(env), replyOptions);
  if (normalized === "/paths")
    return replyBot(env, chatId, await formatPaths(env), replyOptions);
  if (normalized === "/network")
    return replyBot(env, chatId, await formatNetwork(env), replyOptions);
  if (normalized === "/systems")
    return replyBot(env, chatId, await formatSystems(env), replyOptions);
  if (normalized === "/mods")
    return replyBot(env, chatId, await formatMods(env), replyOptions);
  if (normalized === "/days")
    return replyBot(env, chatId, await formatDays(env), replyOptions);
  if (normalized === "/crashes")
    return replyBot(env, chatId, formatCrashes(data), replyOptions);
  if (normalized === "/today")
    return replyBot(env, chatId, formatToday(data), replyOptions);
  if (normalized === "/health")
    return replyBot(env, chatId, `💚 <b>Worker живой</b>\n<code>${escapeHtml(new Date().toISOString())}</code>`, replyOptions);

  return replyBot(env, chatId, `Неизвестная команда: <code>${escapeHtml(normalized)}</code>\n\n${formatHelp()}`, replyOptions);
}

async function handleTelegramWebhook(request, env, ctx) {
  if (!env.TELEGRAM_BOT_TOKEN)
    return json({ ok: false, error: "telegram_not_configured" }, 500);

  if (env.TELEGRAM_WEBHOOK_SECRET) {
    const got = request.headers.get("x-telegram-bot-api-secret-token") || "";
    if (got !== env.TELEGRAM_WEBHOOK_SECRET)
      return json({ ok: false, error: "forbidden" }, 403);
  }

  const update = await request.json();
  const message = update.message || update.edited_message || update.callback_query?.message;
  const chatId = message?.chat?.id;

  if (!chatId)
    return json({ ok: true, ignored: "no_chat" });

  if (!isTelegramAdmin(env, update.message || update.edited_message || update.callback_query)) {
    ctx?.waitUntil?.(sendTelegramMessage(env, chatId, "Access denied."));
    return json({ ok: true, ignored: "not_admin" });
  }

  if (update.callback_query?.id) {
    ctx?.waitUntil?.(telegramApi(env, "answerCallbackQuery", {
      callback_query_id: update.callback_query.id,
    }));
  }

  const command = update.callback_query?.data || update.message?.text || update.edited_message?.text || "/stats";
  const options = update.callback_query?.message?.message_id
    ? { editMessageId: update.callback_query.message.message_id }
    : {};
  ctx?.waitUntil?.(handleBotCommand(env, chatId, command, options));
  return json({ ok: true });
}

async function handleTelegramSetup(request, env) {
  const url = new URL(request.url);
  if (!env.TELEGRAM_BOT_TOKEN || !env.TELEGRAM_WEBHOOK_SECRET)
    return json({ ok: false, error: "telegram_not_configured" }, 500);
  if (url.searchParams.get("secret") !== env.TELEGRAM_WEBHOOK_SECRET)
    return json({ ok: false, error: "forbidden" }, 403);

  const webhookUrl = `${url.origin}/api/telegram/webhook`;
  const response = await telegramApi(env, "setWebhook", {
    url: webhookUrl,
    secret_token: env.TELEGRAM_WEBHOOK_SECRET,
    drop_pending_updates: true,
    allowed_updates: ["message", "edited_message", "callback_query"],
  });
  const body = await response.json();
  return json(body, response.ok ? 200 : 500);
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
  async fetch(request, env, ctx) {
    try {
      if (request.method === "OPTIONS")
        return withCors(text("", 204, corsHeaders()));

      const url = new URL(request.url);
      const path = url.pathname.replace(/\/+$/, "") || "/";

      let response;
      if (request.method === "GET" && (path === "/" || path === "/healthz"))
        response = await handleHealth(request, env);
      else if (request.method === "GET" && path === "/api/stats")
        response = await handleStats(request, env);
      else if (request.method === "GET" && path === "/api/summary")
        response = await handlePublicSummary(request, env);
      else if (request.method === "GET" && path === "/api/donations")
        response = await handleDonations(request, env);
      else if (request.method === "POST" && path === "/api/telegram/webhook")
        response = await handleTelegramWebhook(request, env, ctx);
      else if (request.method === "POST" && path === "/api/telegram/setup")
        response = await handleTelegramSetup(request, env);
      else if (request.method === "GET" && path === "/api/ping")
        response = await handlePing(request, env, ctx);
      else if (request.method === "GET" && path === "/api/setnick")
        response = await handleSetNick(request, env, ctx);
      else if (request.method === "GET" && path === "/api/event")
        response = await handleLegacyEventGet(request, env, ctx);
      else if (request.method === "POST" && path === "/api/diag")
        response = await handleStructuredPost(request, env, ctx, "diag");
      else if (request.method === "POST" && path === "/api/crash")
        response = await handleStructuredPost(request, env, ctx, "crash");
      else if (request.method === "POST" && path === "/api/lobby")
        response = await handleStructuredPost(request, env, ctx, "lobby");
      else if (request.method === "POST" && path === "/api/event")
        response = await handleEventPost(request, env, ctx);
      else if (request.method === "POST" && (path === "/api/upload" || path === "/api/actions" || path === "/api/ingest"))
        response = await handleUpload(request, env, ctx);
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
