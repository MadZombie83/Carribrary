import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const code=await readFile(new URL('../server/wwwroot/library.js',import.meta.url),'utf8');
const library=await import('data:text/javascript;base64,'+Buffer.from(code).toString('base64'));
const {state,cases}=JSON.parse(await readFile(new URL('circulation-fixtures.json',import.meta.url),'utf8'));
const before=JSON.stringify(state);
for(const c of cases)assert.deepEqual(library.circulationCopies(state,c.input,c.returning).map(b=>b.id),c.expected,JSON.stringify(c));
// Returning a selected copy changes subsequent ISBN resolution without touching the other loan.
assert.equal(JSON.stringify(state),before);
state.loans.find(l=>l.copyId==='BK-0002').returnedDate='2026-10-05';
assert.deepEqual(library.circulationCopies(state,'9780140328721',true).map(b=>b.id),['BK-0003']);
assert.deepEqual(library.circulationCopies(state,'9780140328721',false).map(b=>b.id),['BK-0001','BK-0002']);
console.log('Circulation resolution: PASS (ISBN-10/13, exact IDs, multiple/single/no matches, history, archived copies, manual search)');

state.books[0].copyUid='1234567890abcdef1234567890abcdef';
const label=library.copyReference(state.books[0]);
assert.deepEqual(library.circulationCopies(state,label).map(b=>b.id),['BK-0001']);
state.books[0].id='BK-0042';assert.deepEqual(library.circulationCopies(state,label).map(b=>b.id),['BK-0042']);
state.books[0].copyUid='abcdef1234567890abcdef1234567890';
assert.deepEqual(library.circulationCopies(state,label),[],'retired QR never resolves to replacement');
assert.deepEqual(library.circulationCopies(state,'carribrary:copy:v2:1234567890abcdef1234567890abcdef'),[]);
state.books[0].copyUid='1234567890abcdef1234567890abcdef';state.loans.push({copyId:'BK-0042',returnedDate:null});
assert.deepEqual(library.circulationCopies(state,label,true).map(b=>b.id),['BK-0042']);
assert.deepEqual(library.circulationCopies(state,label),[]);
console.log('Permanent label resolution: PASS (renumber, retirement/reuse, unsupported version, eligible returns)');
