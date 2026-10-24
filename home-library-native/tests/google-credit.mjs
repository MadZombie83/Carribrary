import assert from 'node:assert/strict';import {readFile} from 'node:fs/promises';
const code=await readFile(new URL('../server/wwwroot/cover-credit.js',import.meta.url),'utf8');const {googleCoverLink,googleCredit}=await import('data:text/javascript;base64,'+Buffer.from(code).toString('base64'));
assert.equal(googleCoverLink('http://books.google.com/books/content?id=Volume_1&key=private'),'https://books.google.com/books?id=Volume_1');
for(const url of ['https://books.google.com.evil.example/?id=Volume_1','javascript:alert(1)','https://user:pass@books.google.com/?id=Volume_1','https://books.google.com:444/?id=Volume_1','https://covers.openlibrary.org/b/id/12-M.jpg','https://books.google.com/?id=%22%3E%3Cscript%3E'])assert.equal(googleCredit(url),'');
assert.match(googleCredit('https://books.google.com/books/content?id=Volume_1'),/powered-by-google.png/);assert.match(googleCredit('https://books.google.com/books/content?id=Volume_1'),/Google Books/);
console.log('Google cover attribution/provenance: PASS');
