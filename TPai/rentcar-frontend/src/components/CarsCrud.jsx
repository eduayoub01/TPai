import { useState } from 'react';
import { carsApi } from '../api';

export default function CarsCrud({ cars, categories, onRefresh }) {
  const [model, setModel] = useState('');
  const [plateNumber, setPlateNumber] = useState('');
  const [carCategoryId, setCarCategoryId] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (submitting) return;

    const trimmedModel = model.trim();
    const trimmedPlate = plateNumber.trim();
    const categoryIdNum = Number(carCategoryId);

    if (!trimmedModel) {
      setError('Please enter the car model.');
      return;
    }

    if (!trimmedPlate) {
      setError('Please enter the plate number.');
      return;
    }

    if (!categoryIdNum) {
      setError('Please select a category.');
      return;
    }

    setError('');
    setSubmitting(true);
    try {
      if (editingId !== null) {
        await carsApi.update(editingId, {
          id: editingId,
          model: trimmedModel,
          plateNumber: trimmedPlate,
          carCategoryId: categoryIdNum,
        });
        setEditingId(null);
      } else {
        await carsApi.create({
          model: trimmedModel,
          plateNumber: trimmedPlate,
          carCategoryId: categoryIdNum,
        });
      }
      setModel('');
      setPlateNumber('');
      setCarCategoryId('');
      await onRefresh();
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (car) => {
    setEditingId(car.id);
    setModel(car.model);
    setPlateNumber(car.plateNumber);
    setCarCategoryId(String(car.carCategoryId));
    setError('');
  };

  const handleCancel = () => {
    setEditingId(null);
    setModel('');
    setPlateNumber('');
    setCarCategoryId('');
    setError('');
  };

  const handleDelete = async (car) => {
    const confirmed = window.confirm(
      `Delete car "${car.model}" (${car.plateNumber})? This cannot be undone.`
    );
    if (!confirmed) return;

    setError('');
    try {
      await carsApi.delete(car.id);
      if (editingId === car.id) {
        handleCancel();
      }
      await onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const noCategories = categories.length === 0;

  return (
    <section className="crud-section">
      <div className="section-header">
        <h2>Cars</h2>
        <span className="count-pill">{cars.length}</span>
      </div>
      {error && <p className="error">{error}</p>}
      {noCategories && (
        <p className="error">Add a car category first, then you can register cars.</p>
      )}

      <form onSubmit={handleSubmit} className="crud-form">
        <input
          type="text"
          placeholder="Model (e.g. Toyota Corolla)"
          value={model}
          onChange={(e) => {
            setModel(e.target.value);
            if (error) setError('');
          }}
          maxLength={100}
          required
        />
        <input
          type="text"
          placeholder="Plate (e.g. 123-TUN-456)"
          value={plateNumber}
          onChange={(e) => {
            setPlateNumber(e.target.value);
            if (error) setError('');
          }}
          maxLength={20}
          required
        />
        <select
          value={carCategoryId}
          onChange={(e) => {
            setCarCategoryId(e.target.value);
            if (error) setError('');
          }}
          required
        >
          <option value="">-- Select Category --</option>
          {categories.map((cat) => (
            <option key={cat.id} value={cat.id}>
              {cat.name}
            </option>
          ))}
        </select>
        <button type="submit" disabled={submitting || noCategories}>
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
              <th>Model</th>
              <th>Plate</th>
              <th>Category</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {cars.length === 0 ? (
              <tr>
                <td colSpan="5" className="empty-state">
                  No cars yet. Add one above.
                </td>
              </tr>
            ) : (
              cars.map((car) => (
                <tr key={car.id}>
                  <td>#{car.id}</td>
                  <td><strong>{car.model}</strong></td>
                  <td><span className="plate-badge">{car.plateNumber}</span></td>
                  <td><span className="badge">{car.carCategory?.name ?? car.carCategoryId}</span></td>
                  <td className="actions">
                    <button type="button" onClick={() => handleEdit(car)} className="btn-sm btn-edit">
                      Edit
                    </button>
                    <button type="button" onClick={() => handleDelete(car)} className="btn-sm danger">
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
