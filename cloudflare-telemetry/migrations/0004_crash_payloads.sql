ALTER TABLE objects ADD COLUMN payload_json TEXT;
ALTER TABLE objects ADD COLUMN error_message TEXT;
ALTER TABLE objects ADD COLUMN error_stack TEXT;
ALTER TABLE objects ADD COLUMN error_fingerprint TEXT;

CREATE INDEX IF NOT EXISTS idx_objects_error_fingerprint
    ON objects(error_fingerprint, created_at DESC);
