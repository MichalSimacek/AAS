# Test Credentials

## Admin Account (dev environment only)
- **Email:** `admin@localhost`
- **Password:** `Admin123!@#$`
- **Role:** `Admin`

Seeded at app startup via `AdminSeeder`. Used by `backend_test.py`
and for admin panel testing (`/Admin/Collections`, `/Admin/Blog`, etc.).

In production the seed values come from
`Admin:Email` / `Admin:Password` in `appsettings.Production.json` (or env vars).

## Notes
- Login is protected by ASP.NET Core Identity lockout (5 failed attempts → 15 min lockout)
  **plus** a new IP-based rate limiter (policy `auth`: 10 requests / 15 min / IP).
- When running locally, if you hit HTTP `429 Too Many Requests` on login, restart the app
  OR wait for the 15-minute window to reset.
