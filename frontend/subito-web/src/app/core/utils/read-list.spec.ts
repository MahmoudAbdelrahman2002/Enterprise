import { readList, readPage } from './read-list';

describe('readList', () => {
  it('reads a plain array', () => {
    expect(readList<{ id: string }>([{ id: '1' }])).toEqual([{ id: '1' }]);
  });

  it('reads camelCase and PascalCase item lists', () => {
    expect(readList<number>({ items: [1, 2] })).toEqual([1, 2]);
    expect(readList<number>({ Items: [3] })).toEqual([3]);
  });

  it('returns an empty list for missing data', () => {
    expect(readList(null)).toEqual([]);
    expect(readList({})).toEqual([]);
  });
});

describe('readPage', () => {
  it('reads paged totals from either casing', () => {
    expect(readPage<string>({ items: ['a'], totalCount: 9, totalPages: 3 })).toEqual({
      items: ['a'],
      totalCount: 9,
      totalPages: 3,
    });
    expect(readPage<string>({ Items: ['b'], TotalCount: 4, TotalPages: 2 }).totalCount).toBe(4);
  });

  it('treats a plain array as a single page', () => {
    expect(readPage<number>([1, 2])).toEqual({ items: [1, 2], totalCount: 2, totalPages: 1 });
  });
});
