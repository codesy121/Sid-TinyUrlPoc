import type {
  CreateUrlRequest,
  CreateUrlResponse,
  ResolveUrlResponse,
  UrlListItemResponse,
  UrlStatsResponse
} from './types';

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

function getClientId(): string {
  const key = 'tinyurl_client_id';
  let v = localStorage.getItem(key);
  if (!v) {
    v = crypto.randomUUID();
    localStorage.setItem(key, v);
  }
  return v;
}

async function http<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      'X-Client-Id': getClientId(),
      ...(init?.headers ?? {})
    }
  });

  if (!res.ok) {
    const maybe = await res.json().catch(() => ({}));
    const msg = maybe?.error || `${res.status} ${res.statusText}`;
    throw new Error(msg);
  }
  if (res.status === 204) return undefined as unknown as T;
  return (await res.json()) as T;
}

export const api = {
  list: () => http<UrlListItemResponse[]>('/api/urls'),
  create: (req: CreateUrlRequest) =>
    http<CreateUrlResponse>('/api/urls', { method: 'POST', body: JSON.stringify(req) }),
  del: (code: string) => http<void>(`/api/urls/${encodeURIComponent(code)}`, { method: 'DELETE' }),
  resolve: (code: string) => http<ResolveUrlResponse>(`/api/urls/${encodeURIComponent(code)}`),
  stats: (code: string) => http<UrlStatsResponse>(`/api/urls/${encodeURIComponent(code)}/stats`)
};

export function redirectUrl(shortCode: string): string {
  return `${API_BASE}/r/${encodeURIComponent(shortCode)}`;
}
