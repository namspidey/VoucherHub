-- Local setup only. Supply the email of an already verified user with psql -v admin_email=...
-- Re-running this script does nothing if the account is already ADMIN.
\set ON_ERROR_STOP on
BEGIN;
SELECT id FROM users WHERE email = lower(btrim(:'admin_email')) FOR UPDATE;
WITH promoted AS (
    UPDATE users
    SET role = 'ADMIN', updated_at = CURRENT_TIMESTAMP
    WHERE email = lower(btrim(:'admin_email')) AND status = 'ACTIVE' AND role = 'USER'
    RETURNING id
)
INSERT INTO audit_logs (id, actor_user_id, action, entity_type, entity_id, details, created_at)
SELECT gen_random_uuid(), id, 'ADMIN_BOOTSTRAPPED', 'User', id,
       '{"source":"local bootstrap script","previousRole":"USER","newRole":"ADMIN"}'::jsonb,
       CURRENT_TIMESTAMP
FROM promoted;
SELECT id, email, role, status FROM users WHERE email = lower(btrim(:'admin_email'));
COMMIT;
