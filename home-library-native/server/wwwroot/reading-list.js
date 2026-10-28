// TBR is a view of existing reading records, never a separate destructive list.
export const isToBeRead = entry => !!entry && ['want', 'reading'].includes(entry.status);
export const matchesReadingFilter = (entry, filter) => !filter || (filter === 'tbr' ? isToBeRead(entry) : entry.status === filter);
export function finishReading(entry, today) {
 return {currentPage:entry.totalPages||entry.currentPage||0,totalPages:entry.totalPages||0,copyId: entry.copyId, status: 'finished', startedDate: entry.startedDate || '', finishedDate: today, rating: entry.rating || 0, notes: entry.notes || ''};
}
