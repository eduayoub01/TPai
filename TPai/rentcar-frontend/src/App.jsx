import { useCallback, useEffect, useState } from 'react';
import {
  authApi,
  carsApi,
  categoriesApi,
  getStoredAuth,
  logsApi,
  setStoredAuth,
  usersApi,
} from './api';
import AuditLogs from './components/AuditLogs';
import CarCategoriesCrud from './components/CarCategoriesCrud';
import CarsCrud from './components/CarsCrud';
import Login from './components/Login';
import UsersCrud from './components/UsersCrud';
import './App.css';

const ACCENT_COLORS = [
  { name: 'Emerald', value: '#10b981', hover: '#059669', soft: 'rgba(16, 185, 129, 0.14)' },
  { name: 'Indigo', value: '#6366f1', hover: '#4f46e5', soft: 'rgba(99, 102, 241, 0.14)' },
  { name: 'Cyan', value: '#06b6d4', hover: '#0891b2', soft: 'rgba(6, 182, 212, 0.14)' },
  { name: 'Amber', value: '#f59e0b', hover: '#d97706', soft: 'rgba(245, 158, 11, 0.14)' },
  { name: 'Rose', value: '#f43f5e', hover: '#e11d48', soft: 'rgba(244, 63, 94, 0.14)' },
];

function App() {
  const [auth, setAuth] = useState(() => getStoredAuth());
  const [categories, setCategories] = useState([]);
  const [cars, setCars] = useState([]);
  const [users, setUsers] = useState([]);
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(() => Boolean(getStoredAuth()?.token));
  const [apiError, setApiError] = useState('');
  const [theme, setTheme] = useState(() => localStorage.getItem('rentigo-theme') || 'dark');
  const [accentName, setAccentName] = useState(
    () => localStorage.getItem('rentigo-accent') || 'Emerald'
  );

  const isAdmin = auth?.roles?.includes('ADMIN');

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('rentigo-theme', theme);
  }, [theme]);

  useEffect(() => {
    const chosen = ACCENT_COLORS.find((c) => c.name === accentName) || ACCENT_COLORS[0];
    const root = document.documentElement;
    root.style.setProperty('--accent', chosen.value);
    root.style.setProperty('--accent-hover', chosen.hover);
    root.style.setProperty('--accent-soft', chosen.soft);
    localStorage.setItem('rentigo-accent', chosen.name);
  }, [accentName]);

  const toggleTheme = () => {
    setTheme((prev) => (prev === 'dark' ? 'light' : 'dark'));
  };

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore logout network errors
    }
    setStoredAuth(null);
    setAuth(null);
    setCategories([]);
    setCars([]);
    setUsers([]);
    setLogs([]);
  };

  const loadAll = useCallback(async () => {
    if (!auth?.token) return;
    setLoading(true);
    setApiError('');
    try {
      const requests = [categoriesApi.getAll(), carsApi.getAll()];
      if (auth?.roles?.includes('ADMIN')) {
        requests.push(usersApi.getAll(), logsApi.getAll());
      }
      const [catsData, carsData, usersData, logsData] = await Promise.all(requests);
      setCategories(catsData ?? []);
      setCars(carsData ?? []);
      if (usersData) setUsers(usersData);
      if (logsData) setLogs(logsData);
    } catch (err) {
      setApiError(err.message);
      if (err.status === 401) {
        setStoredAuth(null);
        setAuth(null);
      }
    } finally {
      setLoading(false);
    }
  }, [auth]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  return (
    <div className="page-wrapper">
      <div className="ambient-orb orb-1" aria-hidden="true" />
      <div className="ambient-orb orb-2" aria-hidden="true" />

      <nav className="top-navbar">
        <div className="nav-brand">
          <span className="nav-dot" />
          <span>RENTIGO</span>
        </div>

        <div className="nav-controls">
          <div className="color-picker" title="Choose accent color">
            {ACCENT_COLORS.map((c) => (
              <button
                key={c.name}
                type="button"
                className={`color-swatch ${accentName === c.name ? 'active' : ''}`}
                style={{ backgroundColor: c.value }}
                onClick={() => setAccentName(c.name)}
                aria-label={`Use ${c.name} accent color`}
              />
            ))}
          </div>

          <button
            type="button"
            className="theme-toggle"
            onClick={toggleTheme}
            aria-label="Toggle light/dark mode"
          >
            {theme === 'dark' ? '☀️ Light' : '🌙 Dark'}
          </button>

          {auth && (
            <div className="user-pill">
              <span>{auth.email}</span>
              <span className="badge">{auth.roles?.[0] ?? 'USER'}</span>
              <button type="button" className="btn-sm danger" onClick={handleLogout}>
                Logout
              </button>
            </div>
          )}
        </div>
      </nav>

      {!auth ? (
        <Login onLoginSuccess={(data) => setAuth(data)} />
      ) : (
        <div className="app-shell">
          <header className="app-header">
            <div className="brand">
              <div className="brand-mark">R</div>
              <div>
                <h1>RENTIGO</h1>
                <p>Pro Fleet &amp; Category Control Center</p>
              </div>
            </div>
            <div className="stats">
              <div className="stat-card">
                <span className="stat-value">{categories.length}</span>
                <span className="stat-label">Categories</span>
              </div>
              <div className="stat-card">
                <span className="stat-value">{cars.length}</span>
                <span className="stat-label">Vehicles</span>
              </div>
            </div>
          </header>

          {apiError && <p className="error">{apiError}</p>}

          {loading && categories.length === 0 && cars.length === 0 ? (
            <div className="loading-state">Syncing RENTIGO fleet data...</div>
          ) : (
            <>
              <div className="grid">
                <CarCategoriesCrud categories={categories} onRefresh={loadAll} />
                <CarsCrud cars={cars} categories={categories} onRefresh={loadAll} />
              </div>
              {isAdmin && (
                <>
                  <div className="admin-panel">
                    <UsersCrud users={users} onRefresh={loadAll} />
                  </div>
                  <div className="admin-panel">
                    <AuditLogs logs={logs} onRefresh={loadAll} />
                  </div>
                </>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default App;
