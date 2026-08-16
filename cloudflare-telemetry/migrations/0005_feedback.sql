CREATE TABLE IF NOT EXISTS feedback_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ticket_code TEXT NOT NULL UNIQUE,
    type TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'new',
    install_id TEXT NOT NULL,
    steam_id TEXT,
    nick TEXT,
    contact TEXT,
    title TEXT,
    message TEXT NOT NULL,
    mod_version TEXT,
    game_version TEXT,
    lang TEXT,
    os TEXT,
    screen TEXT,
    ip TEXT,
    country TEXT,
    region TEXT,
    city TEXT,
    colo TEXT,
    asn INTEGER,
    as_org TEXT,
    user_agent TEXT,
    accept_language TEXT,
    payload_json TEXT,
    admin_reply TEXT,
    admin_note TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    replied_at TEXT,
    closed_at TEXT
);

CREATE INDEX IF NOT EXISTS idx_feedback_status_created_at
    ON feedback_messages(status, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_feedback_install_created_at
    ON feedback_messages(install_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_feedback_created_at
    ON feedback_messages(created_at DESC);
