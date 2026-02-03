import React, { useEffect, useMemo, useState } from 'react';
import { api, redirectUrl } from './api';
import type { UrlListItemResponse } from './types';

function fmt(dt: string) {
  try {
    return new Date(dt).toLocaleString();
  } catch {
    return dt;
  }
}

export default function App() {
  const [items, setItems] = useState<UrlListItemResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // Create form
  const [longUrl, setLongUrl] = useState('');
  const [customCode, setCustomCode] = useState('');

  // Resolve form
  const [resolveCode, setResolveCode] = useState('');
  const [resolvedLong, setResolvedLong] = useState<string | null>(null);

  const hasItems = useMemo(() => items.length > 0, [items]);

  async function refresh() {
    setErr(null);
    setLoading(true);
    try {
      setItems(await api.list());
    } catch (e: any) {
      setErr(e?.message ?? String(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  async function onCreate(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setResolvedLong(null);
    try {
      await api.create({
        longUrl: longUrl.trim(),
        customShortCode: customCode.trim() ? customCode.trim() : undefined
      });
      setLongUrl('');
      setCustomCode('');
      await refresh();
    } catch (e: any) {
      setErr(e?.message ?? String(e));
    }
  }

  async function onDelete(code: string) {
    if (!confirm(`Delete short URL '${code}'?`)) return;
    setErr(null);
    try {
      await api.del(code);
      await refresh();
    } catch (e: any) {
      setErr(e?.message ?? String(e));
    }
  }

  async function onResolve(e: React.FormEvent) {
    e.preventDefault();
    setErr(null);
    setResolvedLong(null);
    try {
      const r = await api.resolve(resolveCode.trim());
      setResolvedLong(r.longUrl);
      await refresh(); // show updated clicks
    } catch (e: any) {
      setErr(e?.message ?? String(e));
    }
  }

  return (
    <div className="page">
      <header className="header">
        <h1>Tiny URL POC</h1>
        <p className="sub">Anonymous client (localStorage). Create, delete, resolve, and track clicks.</p>
      </header>

      {err && <div className="alert">{err}</div>}

      <section className="card">
        <h2>Create short URL</h2>
        <form onSubmit={onCreate} className="form">
          <label>
            Long URL
            <input
              value={longUrl}
              onChange={(e) => setLongUrl(e.target.value)}
              placeholder="https://www.adroit-tt.com"
              required
            />
          </label>

          <label>
            Custom short code (optional)
            <input
              value={customCode}
              onChange={(e) => setCustomCode(e.target.value)}
              placeholder="My_Code (4-32 chars)"
            />
          </label>

          <div className="row">
            <button type="submit">Create</button>
            <button type="button" className="secondary" onClick={refresh} disabled={loading}>
              Refresh
            </button>
          </div>

          <p className="hint">
            If you submit the same Long URL again from this browser, you will get the same short code (cache behavior).
          </p>
        </form>
      </section>

      <section className="card">
        <h2>Resolve short code (increments clicks)</h2>
        <form onSubmit={onResolve} className="form">
          <label>
            Short code
            <input
              value={resolveCode}
              onChange={(e) => setResolveCode(e.target.value)}
              placeholder="e.g. 3rp36a3s"
              required
            />
          </label>

          <div className="row">
            <button type="submit">Get long URL</button>           
          </div>

          {resolvedLong && (
            <div className="result">
              <div className="label">Long URL</div>
              <div className="value">
                <a href={resolvedLong} target="_blank" rel="noreferrer">
                  {resolvedLong}
                </a>
              </div>
            </div>
          )}
        </form>
      </section>

      <section className="card">
        <h2>Your short URLs</h2>
        {!hasItems && !loading && <div className="empty">No URLs created yet.</div>}
        {loading && <div className="empty">Loading...</div>}

        {hasItems && (
          <div className="table">
            <div className="thead">
              <div>Short</div>
              <div>Long</div>
              <div>Clicks</div>
              <div>Created</div>
              <div />
            </div>

            {items.map((x) => (
              <div className="trow" key={x.shortCode}>
                <div className="truncate">
                  <a href={x.longUrl} target="_blank" rel="noreferrer" title={`http://tinyurlpoc.com/${x.shortCode}`}>
                    {x.shortCode}
                  </a>
                </div>
                <div className="truncate">
                  <a href={x.longUrl} target="_blank" rel="noreferrer" title={x.longUrl}>
                    {x.longUrl}
                  </a>
                </div>
                <div>{x.clicks}</div>
                <div>{fmt(x.createdAtUtc)}</div>
                <div className="actions">
                  <button
                    type="button"
                    className="secondary"
                    onClick={() => navigator.clipboard.writeText(redirectUrl(x.shortCode))}
                  >
                    Copy
                  </button>
                  <button type="button" className="danger" onClick={() => onDelete(x.shortCode)}>
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <footer className="footer">
        <div>
          Backend expects header <code>X-Client-Id</code>. This UI generates a UUID and reuses it.
        </div>
      </footer>
    </div>
  );
}
