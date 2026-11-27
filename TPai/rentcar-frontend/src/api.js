// In development Vite proxies /api to the ASP.NET Core backend (see vite.config.js),
// so requests are same-origin and never depend on a hardcoded port/host.
// Set VITE_API_BASE to an absolute URL when the frontend is hosted separately.
const API_BASE = (import.meta.env.VITE_API_BASE || '/api').replace(/\/+$/, '');

export class ApiError extends Error {
  constructor(message, status = 0) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export function getStoredAuth() {
  try {
    const raw = localStorage.getItem('rentigo-auth');
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function setStoredAuth(auth) {
  if (auth) {
    localStorage.setItem('rentigo-auth', JSON.stringify(auth));
  } else {
    localStorage.removeItem('rentigo-auth');
  }
}

function extractMessage(body) {
  if (!body) return '';

  try {
    const data = JSON.parse(body);

    if (typeof data === 'string') {
      return data;
    }

    if (data && typeof data === 'object') {
      if (data.errors && typeof data.errors === 'object') {
        const messages = Object.values(data.errors).flat().filter(Boolean);
        if (messages.length > 0) {
          return messages.join(' ');
        }
      }
      return data.detail || data.message || data.title || '';
    }
  } catch {
    // Plain text response (e.g. "A category named "SUV" already exists.").
  }

  return body;
}

async function readResponseMessage(res) {
  try {
    return extractMessage(await res.text());
  } catch {
    return '';
  }
}

async function request(path, options = {}) {
  const auth = getStoredAuth();
  const headers = {
    'Content-Type': 'application/json',
    ...(auth?.token ? { Authorization: `Bearer ${auth.token}` } : {}),
    ...options.headers,
  };

  let res;
  try {
    res = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new ApiError(
      `Cannot reach the API server at ${API_BASE}. Make sure the backend is running, then reload the page.`,
      0
    );
  }

  if (!res.ok) {
    const message = await readResponseMessage(res);

    if (res.status === 401) {
      if (auth?.token) {
        setStoredAuth(null);
        throw new ApiError('Your session has expired. Please sign in again.', 401);
      }
      throw new ApiError(message || 'Invalid email or password.', 401);
    }

    if (res.status === 403) {
      throw new ApiError(message || 'You do not have permission to perform this action.', 403);
    }

    if (res.status === 404) {
      throw new ApiError(message || 'The requested item was not found (404).', 404);
    }

    throw new ApiError(message || `Request failed with status ${res.status}`, res.status);
  }

  if (res.status === 204) {
    return null;
  }

  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

export const authApi = {
  login: (data) => request('/Auth/login', { method: 'POST', body: JSON.stringify(data) }),
  register: (data) => request('/Auth/register', { method: 'POST', body: JSON.stringify(data) }),
  logout: () => request('/Auth/logout', { method: 'POST' }),
};

export const categoriesApi = {
  getAll: () => request('/CarCategories'),
  create: (data) => request('/CarCategories', { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => request(`/CarCategories/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => request(`/CarCategories/${id}`, { method: 'DELETE' }),
};

export const carsApi = {
  getAll: () => request('/Cars'),
  create: (data) => request('/Cars', { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => request(`/Cars/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => request(`/Cars/${id}`, { method: 'DELETE' }),
};

export const usersApi = {
  getAll: () => request('/Users'),
  create: (data) => request('/Users', { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => request(`/Users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => request(`/Users/${id}`, { method: 'DELETE' }),
};

export const logsApi = {
  getAll: () => request('/Logs'),
};

export const healthApi = {
  check: () => request('/health'),
};
