import assert from 'node:assert/strict';
import {parseCsv,detectMapping,mappedRows} from '../server/wwwroot/catalog-import.js';
const csv=parseCsv('\uFEFFTitle,Author,ISBN13,Book ID,Read Status,Notes\r\n"A, book",Writer,"=""9780140328721""",OLD-5,Finished,"Two\nlines"\r\n');
assert.equal(csv.records[0][0],'A, book');const mapping=detectMapping(csv.headers),rows=mappedRows(csv,mapping,'general');assert.equal(rows[0].fields.isbn,'9780140328721');assert.equal(rows[0].fields.sourceCopyId,'OLD-5');assert.match(rows[0].fields.importNotes,/Read Status: Finished/);assert.match(rows[0].fields.importNotes,/Two\nlines/);assert.equal(rows[0].keepDuplicate,false);
for(const text of ['Title,Title\na,b','Title,Author\na,b,c','Title,Author\n"a,b'])assert.throws(()=>parseCsv(text));
assert.equal(parseCsv('Title;Author\nOne;Writer').records[0][0],'One');assert.equal(parseCsv('Title\tAuthor\nOne\tWriter').records[0][1],'Writer');
assert.equal(parseCsv('Title,Author\n'+Array.from({length:1000},(_,i)=>`Book ${i},Author`).join('\n')).records.length,1000);
assert.throws(()=>parseCsv('Title,Author\n'+Array.from({length:1001},(_,i)=>`Book ${i},Author`).join('\n')));
console.log('PASS CSV quoting/BOM/mapping/ISBN normalization/source preservation, malformed inputs, delimiters and bounded batch size');
