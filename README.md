# TinyURL POC — Quick Start

A small proof-of-concept TinyURL service (backend: .NET 6, frontend: React + Vite).

## Backend — what it does 
1. Accepts long URLs and returns a short code (POST /api/urls).
2. Supports optional custom short codes and enforces uniqueness.
3. Stores mappings in-memory and returns the caller's list of URLs (GET /api/urls).
4. Redirects short codes to long URLs (GET /r/{code}) and tracks clicks (GET /api/urls/{code}/stats).
5. Identifies callers via `X-Client-Id` (no accounts); same client + same long URL → same short code.

### API summary
- POST /api/urls — body: { longUrl, customShortCode? } → create
- DELETE /api/urls/{code} — remove mapping
- GET /api/urls/{code} — get mapping (increments clicks)
- GET /api/urls/{code}/stats — click stats
- GET /api/urls — list mappings for caller (`X-Client-Id`)
- GET /r/{code} — 302 redirect to long URL

### Run backend locally
1. Open a terminal
2. cd backend
3. dotnet restore
4. dotnet run --project TinyUrl.Api

Default API URLs: http://localhost:5000 and https://localhost:5001

---

## Frontend — what it does and how to use it
- Small React UI that creates short URLs, lists your URLs, shows stats, and follows redirects.
- It generates/stores a UUID in `localStorage` and sends it as `X-Client-Id` on each request.

### Run frontend locally
1. Open a terminal
2. cd frontend
3. npm install
4. npm run dev

By default the UI calls the API at `http://localhost:5000`. To change: `VITE_API_BASE=http://localhost:5000 npm run dev`

---

## Notes
- Persistence is in-memory (for simplicity); restarting the backend clears data.
- Concurrency handled via `ConcurrentDictionary` and `Interlocked.Increment` for counters.
- There are unit tests in `backend/TinyUrl.Tests` for service logic.
