export default function AuditLogs({ logs, onRefresh }) {
  return (
    <section className="crud-section logs-section">
      <div className="section-header">
        <h2>Audit Logs (Admin Only)</h2>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <span className="count-pill">{logs.length}</span>
          <button type="button" onClick={onRefresh} className="btn-sm secondary">
            Refresh
          </button>
        </div>
      </div>

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Action</th>
              <th>Entity</th>
              <th>User</th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            {logs.length === 0 ? (
              <tr>
                <td colSpan="5" className="empty-state">
                  No audit logs recorded yet.
                </td>
              </tr>
            ) : (
              logs.map((log) => (
                <tr key={log.id}>
                  <td>{new Date(log.timestamp).toLocaleString()}</td>
                  <td>
                    <span className={`badge action-${log.action.toLowerCase()}`}>
                      {log.action}
                    </span>
                  </td>
                  <td>{log.entity}</td>
                  <td>{log.userEmail}</td>
                  <td>{log.details}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}
