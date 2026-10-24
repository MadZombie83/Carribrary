import assert from 'node:assert/strict';
import {isToBeRead,matchesReadingFilter,finishReading} from '../server/wwwroot/reading-list.js';
for(const status of ['want','reading'])assert(isToBeRead({status}));
for(const status of ['finished','unknown'])assert(!isToBeRead({status}));
assert(!isToBeRead(null));assert(!matchesReadingFilter({status:'finished'},'tbr'));
assert(matchesReadingFilter({status:'finished'},''));assert(matchesReadingFilter({status:'reading'},'reading'));
const old={copyId:'BK-0001',status:'reading',startedDate:'2026-01-01',finishedDate:'',rating:5,notes:'Preserve my notes'};
const done=finishReading(old,'2026-10-19');assert.equal(old.status,'reading');assert.equal(done.status,'finished');assert.equal(done.notes,old.notes);assert.equal(done.rating,5);assert.equal(done.startedDate,old.startedDate);assert.equal(done.finishedDate,'2026-10-19');assert(!isToBeRead(done));assert(matchesReadingFilter(done,'finished'));
console.log('PASS TBR membership, non-destructive completion, finished history and preserved reading details');
