# RENTIGO — Fleet Management (ASP.NET Core + React)

A small full-stack car-rental management app:

- **Backend** — ASP.NET Core 10 Web API, EF Core + SQL Server (LocalDB), ASP.NET Identity with JWT authentication, audit logging.
- **Frontend** — React 19 + Vite single-page app (login/register, car categories, cars, admin user management, audit logs, themes).

## Prerequisites

- .NET SDK 10
- Node.js 20+ (tested with Node 24)
- SQL Server LocalDB (installed with Visual Studio; `sqllocaldb info` must list `MSSQLLocalDB`)

## Run the app

Open **two terminals** in the `TPai` folder (the one containing `TPai.csproj`).

**1. Backend**

```powershell
dotnet run
```

The API starts on <http://localhost:5176>. On startup it automatically creates/upgrades the
`RentCarDb` database (EF Core migrations) and seeds the `ADMIN`/`USER` roles plus the default
admin account. Swagger UI: <http://localhost:5176/swagger>.

**2. Frontend**

```powershell
cd rentcar-frontend
npm install        # first time only
npm run dev
```

Open <http://localhost:5173> (or <http://127.0.0.1:5173> — both work).

### Default account

| Email            | Password |
| ---------------- | -------- |
| `admin@ayoub.tn` | `golf7`  |

You can also register a normal `USER` account from the login screen.

## How the frontend talks to the API

The frontend calls the **relative** URL `/api/...`. In development (and in `npm run preview`)
Vite proxies `/api` to the ASP.NET Core API, so requests are same-origin and there is no
hard-coded port or CORS problem:

```
Browser → http://localhost:5173/api/... → (Vite proxy) → http://localhost:5176/api/...
```

If your API runs on a different port, start the frontend with:

```powershell
$env:VITE_API_TARGET = "http://localhost:7199"
npm run dev
```

Or serve the API elsewhere and point the built frontend at it:

```powershell
$env:VITE_API_BASE = "https://api.example.com/api"
npm run build
```

In that case add the frontend origin to `Cors:AllowedOrigins` in `TPai/appsettings.json`.
In `Development`, any loopback (`localhost` / `127.0.0.1`) origin is accepted automatically.

## Useful endpoints

| Method | URL                       | Notes                       |
| ------ | ------------------------- | --------------------------- |
| GET    | `/api/health`             | API health check            |
| POST   | `/api/Auth/login`         | Returns JWT + roles         |
| POST   | `/api/Auth/register`      | Creates a `USER` account    |
| GET    | `/api/CarCategories`      | Requires login              |
| POST   | `/api/CarCategories`      | Requires login              |
| GET    | `/api/Cars`               | Requires login              |
| GET    | `/api/Users`              | Admin only                  |
| GET    | `/api/Logs`               | Admin only                  |

## Troubleshooting

- **`Request failed with status 404` / `Failed to fetch`** — the browser could not reach the
  API. Check that `dotnet run` is still running on <http://localhost:5176> (open
  <http://localhost:5176/api/health>). Restart the Vite dev server after changing
  `vite.config.js` or `.env` files.
- **Port 5176 already in use** — stop the other process, or start the API with
  `dotnet run --urls http://localhost:5199` and set `VITE_API_TARGET=http://localhost:5199`
  for the frontend.
- **Database errors / login fails** — make sure LocalDB is running (`sqllocaldb start MSSQLLocalDB`)
  and that the connection string in `appsettings.json` is correct. Deleting the
  `RentCarDb` database is safe: it is recreated and re-seeded on the next start.
- **`dotnet ef` not found** — only needed if you change the EF model:
  `dotnet tool install --global dotnet-ef`, then `dotnet ef migrations add <Name>`.

## Project layout

```
TPai/
├─ Controllers/      Auth, CarCategories, Cars, Users, Logs
├─ Data/             DbContext + startup seeding/migrations
├─ Migrations/       EF Core migrations
├─ Models/           Entities and DTOs
├─ Repositories/     Repository interfaces + EF implementations
├─ Program.cs        DI, JWT, CORS, error handling, database bootstrap
└─ rentcar-frontend/ React + Vite SPA
```
