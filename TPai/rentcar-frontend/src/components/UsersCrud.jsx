import { useState } from 'react';
import { usersApi } from '../api';

export default function UsersCrud({ users, onRefresh }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('USER');
  const [editingId, setEditingId] = useState(null);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (submitting) return;

    setError('');
    setSubmitting(true);
    try {
      if (editingId !== null) {
        await usersApi.update(editingId, {
          email: email.trim(),
          role,
          password: password || null,
        });
        setEditingId(null);
      } else {
        await usersApi.create({ email: email.trim(), password, role });
      }
      setEmail('');
      setPassword('');
      setRole('USER');
      await onRefresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (user) => {
    setEditingId(user.id);
    setEmail(user.email);
    setRole(user.role || 'USER');
    setPassword('');
    setError('');
  };

  const handleCancel = () => {
    setEditingId(null);
    setEmail('');
    setPassword('');
    setRole('USER');
    setError('');
  };

  const handleDelete = async (user) => {
    const confirmed = window.confirm(
      `Delete user "${user.email}"? This cannot be undone.`
    );
    if (!confirmed) return;

    setError('');
    try {
      await usersApi.delete(user.id);
      if (editingId === user.id) {
        handleCancel();
      }
      await onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <section className="crud-section users-section">
      <div className="section-header">
        <h2>User Management (Admin Only)</h2>
        <span className="count-pill">{users.length}</span>
      </div>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit} className="crud-form">
        <input
          type="email"
          placeholder="User email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder={editingId ? 'New password (optional)' : 'Password (min 5 chars)'}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required={editingId === null}
          autoComplete="new-password"
        />
        <select value={role} onChange={(e) => setRole(e.target.value)}>
          <option value="USER">USER</option>
          <option value="ADMIN">ADMIN</option>
        </select>
        <button type="submit" disabled={submitting}>
          {submitting ? 'Saving...' : editingId !== null ? 'Update' : 'Add User'}
        </button>
        {editingId !== null && (
          <button type="button" onClick={handleCancel} className="secondary" disabled={submitting}>
            Cancel
          </button>
        )}
      </form>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Email</th>
              <th>Role</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.length === 0 ? (
              <tr>
                <td colSpan="3" className="empty-state">
                  No users found.
                </td>
              </tr>
            ) : (
              users.map((u) => (
                <tr key={u.id}>
                  <td>{u.email}</td>
                  <td>
                    <span className="badge">{u.role}</span>
                  </td>
                  <td className="actions">
                    <button type="button" onClick={() => handleEdit(u)} className="btn-sm btn-edit">
                      Edit
                    </button>
                    <button type="button" onClick={() => handleDelete(u)} className="btn-sm danger">
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
