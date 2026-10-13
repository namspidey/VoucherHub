# Admin campaign API (local)

## Prepare an admin

Register and verify a separate local account first. From `D:\VoucherHub`, replace the email below with that account's email:

```powershell
Get-Content -Raw ./scripts/bootstrap-admin.sql | docker compose exec -T postgres psql -U voucherhub -d voucherhub -v "admin_email=admin@example.com"
```

The script only promotes an existing ACTIVE USER, records `ADMIN_BOOTSTRAPPED` in audit_logs, and does nothing when the user is already ADMIN. It does not create an account or verify an email. Check that the final SELECT returns a row with role ADMIN. This is a manual local setup script, not a public API or production provisioning workflow.

Log in again after promotion to get a new token with the ADMIN role. An old USER token will still receive 403.

## Create a Draft

```http
POST http://localhost:5242/api/admin/campaigns
Authorization: Bearer ADMIN_ACCESS_TOKEN
Content-Type: application/json

{
  "name": "October Flash Sale",
  "description": "First campaign",
  "startsAt": "2030-10-08T18:00:00+07:00",
  "endsAt": "2030-10-08T20:00:00+07:00",
  "maxRedemptionsPerUser": 3
}
```

Use ISO 8601 timestamps with an explicit offset (`+07:00` or `Z`). The example uses a future date; replace it with your intended schedule. Dates are converted to UTC before persistence. Draft creation requires endsAt > startsAt; publish will validate the schedule separately.

Returns 201 with campaign details and a Location header. `status` is always DRAFT and `createdBy` comes from the authenticated user. The quota applies to the entire campaign. The campaign and CAMPAIGN_CREATED audit are committed together.

## Read back

```http
GET http://localhost:5242/api/admin/campaigns/CAMPAIGN_ID
Authorization: Bearer ADMIN_ACCESS_TOKEN
```

Returns 200 or 404. This admin route can read Draft campaigns. Both routes require an ADMIN claim and an ACTIVE ADMIN account in the database. A blocked/demoted account gets 403 even if it still holds a previously issued ADMIN token.

## Draft editing and offers

All endpoints below require the ADMIN Bearer token. Replace `CAMPAIGN_ID` and `OFFER_ID` with returned IDs.

`PUT /api/admin/campaigns/CAMPAIGN_ID` replaces editable fields using the same body as creation. It returns 200 while Draft; after publishing/cancelling it returns 409. Changing endsAt is rejected with 400 if any existing offer expires at or before the new campaign end.

`POST /api/admin/campaigns/CAMPAIGN_ID/offers` creates an offer (201):

```json
{
  "name": "Voucher 50K",
  "description": "First offer",
  "pointsCost": 100,
  "totalQuantity": 20,
  "expiresAt": "2030-10-09T20:00:00+07:00"
}
```

Expiry must be strictly after campaign endsAt. Positive price and quantity are required. IssuedQuantity starts at 0 and cannot be set by the client.

- `PUT /api/admin/campaigns/CAMPAIGN_ID/offers/OFFER_ID`: replace editable offer fields using the same body (200).
- `GET /api/admin/campaigns/CAMPAIGN_ID/offers`: list offers (200).
- `POST /api/admin/campaigns/CAMPAIGN_ID/publish`: no body; transition Draft to Published (200). Requires a valid campaign that has not ended and at least one valid offer. An ongoing campaign may also be published.
- `POST /api/admin/campaigns/CAMPAIGN_ID/cancel`: no body; cancel Draft or Published (200). Repeated cancellation returns the current state without duplicating the audit. Issued vouchers and point balances are not changed.

Writes acquire locks in order actor (FOR SHARE), campaign (FOR UPDATE), then modify offers. All offer mutations and campaign transitions use the same campaign row lock. This serializes edit/publish/cancel requests so a configuration cannot change after publish. Audit includes before/after values and commits with the mutation. Invalid input returns 400, missing campaign/offer 404, invalid status transition 409. No schema migration is required.

## Lifecycle integration test

Integration test against a temporary API at port 5107, with PostgreSQL and Mailpit running:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/test-create-campaign.ps1
```

The test uses a random account and removes its database records in finally. The verification email remains in local Mailpit. It checks anonymous/USER rejection, invalid dates/quota/name, creation/read-back, UTC conversion, audit presence and stale admin token rejection after blocking/demotion. No schema migration is required.
