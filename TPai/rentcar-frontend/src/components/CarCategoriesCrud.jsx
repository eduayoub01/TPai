import { useState } from 'react';
import { categoriesApi } from '../api';

export default function CarCategoriesCrud({ categories, onRefresh }) {
  const [name, setName] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (submitting) return;

    const trimmed = name.trim();
    if (!trimmed) {
      setError('Please enter a category name.');
      return;
    }

    setError('');
    setSubmitting(true);
    try {
      if (editingId !== null) {
        await categoriesApi.update(editingId, { id: editingId, name: trimmed });
        setEditingId(null);
      } else {
        await categoriesApi.create({ name: trimmed });
      }
      setName('');
      await onRefresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (category) => {
    setEditingId(category.id);
    setName(category.name);
    setError('');
  };

  const handleCancel = () => {
    setEditingId(null);
    setName('');
    setError('');
  };

  const handleDelete = async (category) => {
    const confirmed = window.confirm(
      `Delete category "${category.name}"? This cannot be undone.`
    );
    if (!confirmed) return;

    setError('');
    try {
      await categoriesApi.delete(category.id);
      if (editingId === category.id) {
        setEditingId(null);
        setName('');
      }
      await onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <section className="crud-section">
      <div className="section-header">
        <h2>Car Categories</h2>
        <span className="count-pill">{categories.length}</span>
      </div>
      {error && <p className="error">{error}</p>}

      <form onSubmit={handleSubmit} className="crud-form">
        <input
          type="text"
          placeholder="Category name (e.g. SUV, Sedan)"
          value={name}
          onChange={(e) => {
            setName(e.target.value);
            if (error) setError('');
          }}
          maxLength={100}
          required
        />
        <button type="submit" disabled={submitting}>
          {submitting ? 'Saving...' : editingId !== null ? 'Update' : 'Add'}
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
              <th>ID</th>
              <th>Name</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {categories.length === 0 ? (
              <tr>
                <td colSpan="3" className="empty-state">
                  No categories yet. Add one above.
                </td>
              </tr>
            ) : (
              categories.map((cat) => (
                <tr key={cat.id}>
                  <td>#{cat.id}</td>
                  <td>{cat.name}</td>
                  <td className="actions">
                    <button type="button" onClick={() => handleEdit(cat)} className="btn-sm btn-edit">
                      Edit
                    </button>
                    <button type="button" onClick={() => handleDelete(cat)} className="btn-sm danger">
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
