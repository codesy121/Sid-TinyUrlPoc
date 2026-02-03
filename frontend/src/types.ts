export type CreateUrlRequest = {
  longUrl: string;
  customShortCode?: string;
};

export type CreateUrlResponse = {
  shortCode: string;
  shortUrl: string;
  longUrl: string;
  createdAtUtc: string;
};

export type UrlListItemResponse = {
  shortCode: string;
  shortUrl: string;
  longUrl: string;
  clicks: number;
  createdAtUtc: string;
};

export type ResolveUrlResponse = {
  shortCode: string;
  longUrl: string;
};

export type UrlStatsResponse = {
  shortCode: string;
  longUrl: string;
  clicks: number;
  createdAtUtc: string;
  lastAccessedAtUtc?: string | null;
};
