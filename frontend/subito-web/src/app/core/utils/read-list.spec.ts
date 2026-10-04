import { readPage, resolvePage } from './read-list';

describe('Page deletion recovery', () => {
  it('returns to the last valid page when deleting the last record reduces the page count', () => {
    expect(resolvePage(4, readPage({ items: [], totalPages: 2, totalCount: 30 }))).toBe(2);
  });
  it('recovers from an empty page when records changed between counting and loading it', () => {
    expect(resolvePage(3, { items: [], totalPages: 3 })).toBe(2);
    expect(resolvePage(2, { items: [], totalPages: 3 })).toBe(1);
  });
  it('keeps populated pages and terminates recovery at an empty first page', () => {
    expect(resolvePage(2, { items: [1], totalPages: 3 })).toBe(2);
    expect(resolvePage(1, readPage({ items: [], totalCount: 0, totalPages: 0 }))).toBe(1);
  });
});
