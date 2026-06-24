CREATE TABLE IF NOT EXISTS installs (
    install_id TEXT PRIMARY KEY,
    first_seen_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    mod_version TEXT,
    lang TEXT,
    nick TEXT,
    last_country TEXT,
    last_region TEXT,
    last_city TEXT
);

CREATE TABLE IF NOT EXISTS objects (
    object_key TEXT PRIMARY KEY,
    kind TEXT NOT NULL,
    install_id TEXT,
    request_path TEXT NOT NULL,
    content_type TEXT,
    size_bytes INTEGER NOT NULL,
    country TEXT,
    region TEXT,
    city TEXT,
    colo TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS daily_counters (
    day TEXT NOT NULL,
    metric TEXT NOT NULL,
    value INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (day, metric)
);

CREATE INDEX IF NOT EXISTS idx_installs_last_seen_at
    ON installs(last_seen_at DESC);

CREATE INDEX IF NOT EXISTS idx_objects_kind_created_at
    ON objects(kind, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_objects_install_created_at
    ON objects(install_id, created_at DESC);
