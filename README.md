# TinyURL-style POC (C# .NET 6 + React/TypeScript)

A minimal, runnable proof-of-concept for a TinyURL-like service:
- Create/delete short URLs for long URLs
- Resolve short code -> long URL (increments click count)
- Stats for clicks
- Optional custom short code, otherwise random unique code
- In-memory store (NO EF, SQLite, Redis, etc.)
- **Cache behavior**: same anonymous user + same long URL -> same short code

> **Anonymous user identity**
>
> The backend requires an `X-Client-Id` header (string up to 64 chars).
> The React UI generates a UUID once (stored in `localStorage`) and sends it on every request.
>
> This is how we implement “same URL submitted by same user gets same tinyURL” without accounts.

---

## Backend (.NET 6)

Folder: `backend/`

### Structure (as requested)
- `Models/`
- `Domain/` (interfaces)
- `Infrastructure/` (implementations: repositories + services)
- `Controllers/`
- `TinyUrl.Tests/` (unit tests using Moq, testing services only)

### Endpoints
- `POST /api/urls` body: `{ longUrl, customShortCode? }`
- `DELETE /api/urls/{code}`
- `GET /api/urls/{code}` → `{ shortCode, longUrl }` and increments clicks
- `GET /api/urls/{code}/stats`
- `GET /api/urls` → list for the caller (same `X-Client-Id`)
- `GET /r/{code}` → 302 redirect to the long URL (also increments clicks)

### Run
```bash
cd backend
dotnet restore
dotnet run --project TinyUrl.Api
```

Default URLs depend on your environment; typically:
- `http://localhost:5000` (HTTP)
- `https://localhost:5001` (HTTPS)

---

## Frontend (React + TypeScript + Vite)

Folder: `frontend/`

### Run
```bash
cd frontend
npm install
npm run dev
```

The UI expects the API at `http://localhost:5000` by default.
To override:
```bash
VITE_API_BASE=http://localhost:5000 npm run dev
```

---

## Notes on concurrency

- Storage uses `ConcurrentDictionary` for safe concurrent access.
- Clicks are incremented with `Interlocked.Increment`.
- Short-code uniqueness is enforced at creation time (custom + random).
