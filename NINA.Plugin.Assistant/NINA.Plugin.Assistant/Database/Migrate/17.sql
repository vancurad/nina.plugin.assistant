/*
*/

ALTER TABLE project ADD COLUMN overheadObstructionRadius REAL NOT NULL DEFAULT 0.0;

PRAGMA user_version = 17;
