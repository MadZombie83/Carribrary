import assert from 'node:assert/strict';
import {labelDocument,labelLayouts} from '../server/wwwroot/copy-labels.js';
const book={id:'BK-0001',title:'<script>Never execute</script>',author:'Author',copyUid:'1234567890abcdef1234567890abcdef'};
for(const [key,g] of Object.entries(labelLayouts)){
 assert.ok(g.left*2+3*g.labelWidth+2*g.gap<=g.width+.001);
 assert.ok(g.top*2+g.rows*g.labelHeight<=g.height+.001);
 const doc=labelDocument([book,book],'A & B',key,g.rows*3);
 assert.equal((doc.match(/class="sheet"/g)||[]).length,2);
 assert.equal((doc.match(/<svg /g)||[]).length,2);
 assert.ok(!doc.includes('<script>'));assert.ok(doc.includes('&lt;script&gt;'));assert.ok(doc.includes('A &amp; B'));
 assert.ok(doc.includes('Permanent physical-copy QR label'));
 assert.throws(()=>labelDocument([book],'Library',key,g.rows*3+1));
}
for(const books of [[],Array(501).fill(book),[{...book,archived:true}],[{...book,copyUid:''}]])assert.throws(()=>labelDocument(books,'Library'));
console.log('Copy labels: PASS (sheet geometry, partial sheets, batches, safe text, identity checks)');
