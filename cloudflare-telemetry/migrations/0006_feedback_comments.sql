CREATE TABLE IF NOT EXISTS feedback_comments (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ticket_code TEXT NOT NULL,
    install_id TEXT NOT NULL,
    author TEXT NOT NULL DEFAULT 'user',
    message TEXT,
    attachment_name TEXT,
    attachment_type TEXT,
    attachment_size INTEGER,
    attachment_base64 TEXT,
    telegram_file_id TEXT,
    telegram_message_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_feedback_comments_ticket_created_at
    ON feedback_comments(ticket_code, created_at ASC);

CREATE INDEX IF NOT EXISTS idx_feedback_comments_install_created_at
    ON feedback_comments(install_id, created_at DESC);
