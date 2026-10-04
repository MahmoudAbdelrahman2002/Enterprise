/** Accepts a plain list or a paged `{ items }` payload (camelCase or PascalCase). */
export function readList<T>(data: unknown): T[] {
  if (!data) return [];
  if (Array.isArray(data)) return data as T[];
  if (typeof data !== 'object') return [];
  const record = data as Record<string, unknown>;
  const items = record['items'] ?? record['Items'];
  return Array.isArray(items) ? (items as T[]) : [];
}

export function readPage<T>(data: unknown): { items: T[]; totalPages: number; totalCount: number } {
  const items = readList<T>(data);
  if (!data || Array.isArray(data) || typeof data !== 'object') {
    return { items, totalPages: 1, totalCount: items.length };
  }
  const record = data as Record<string, unknown>;
  const totalPages = Number(record['totalPages'] ?? record['TotalPages'] ?? 1) || 1;
  const totalCount = Number(record['totalCount'] ?? record['TotalCount'] ?? items.length);
  return { items, totalPages, totalCount: Number.isFinite(totalCount) ? totalCount : items.length };
}

/** Recover from a deletion between the server's count and page queries as well as an invalid last page. */
export function resolvePage(page: number, result: { items: unknown[]; totalPages: number }): number {
  if (page <= result.totalPages && (page === 1 || result.items.length > 0)) return page;
  return Math.max(1, Math.min(result.totalPages, result.items.length > 0 ? page : page - 1));
}
