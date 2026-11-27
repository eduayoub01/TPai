# RENTIGO frontend

React 19 + Vite SPA for the RENTIGO fleet management API.

```powershell
npm install
npm run dev      # http://localhost:5173
```

The dev server proxies `/api` to `http://localhost:5176` (override with the
`VITE_API_TARGET` environment variable). See `../README.md` for the full setup,
default credentials and troubleshooting.

## Scripts

| Script            | What it does                                            |
| ----------------- | ------------------------------------------------------- |
| `npm run dev`     | Vite dev server with HMR and the API proxy             |
| `npm run build`   | Production build into `dist/`                           |
| `npm run preview` | Serves `dist/` on <http://localhost:4173> with the proxy |
| `npm run lint`    | Oxlint                                                  |
