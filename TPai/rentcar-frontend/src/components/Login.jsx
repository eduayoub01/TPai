import { useState } from 'react';
import { authApi, setStoredAuth } from '../api';

export default function Login({ onLoginSuccess }) {
  const [isRegister, setIsRegister] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      const data = isRegister
        ? await authApi.register({ email: email.trim(), password })
        : await authApi.login({ email: email.trim(), password });
      setStoredAuth(data);
      onLoginSuccess(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="login-wrapper">
      <div className="login-card">
        <div className="login-header">
          <div className="brand-mark">R</div>
          <h2>{isRegister ? 'Create RENTIGO Account' : 'Sign in to RENTIGO'}</h2>
          <p>{isRegister ? 'Register a new user account' : 'Enter your credentials to manage the fleet'}</p>
        </div>

        {error && <p className="error">{error}</p>}

        <form onSubmit={handleSubmit} className="login-form">
          <label>
            <span>Email</span>
            <input
              type="email"
              placeholder="admin@ayoub.tn"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>
          <label>
            <span>Password</span>
            <input
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>
          <button type="submit" disabled={submitting}>
            {submitting ? 'Please wait...' : isRegister ? 'Register' : 'Sign In'}
          </button>
        </form>

        <div className="login-footer">
          <button
            type="button"
            className="link-btn"
            onClick={() => {
              setIsRegister((prev) => !prev);
              setError('');
            }}
          >
            {isRegister ? 'Already have an account? Sign In' : "Don't have an account? Register"}
          </button>
        </div>
      </div>
    </div>
  );
}
