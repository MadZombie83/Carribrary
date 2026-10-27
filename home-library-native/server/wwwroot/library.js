export const STORAGE_KEY='home-library-v1';
export const today=()=>{const d=new Date();return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;};
export const normalizeCode=value=>String(value||'').trim().toUpperCase().replace(/[\s-]/g,'');
export const normalizeCopy=value=>{const code=normalizeCode(value);return /^BK\d+$/.test(code)?'BK-'+code.slice(2).padStart(4,'0'):String(value||'').trim().toUpperCase();};
export function validCode(value){const s=normalizeCode(value);if(/^\d{9}[\dX]$/.test(s))return [...s].reduce((sum,c,i)=>sum+(c==='X'?10:Number(c))*(10-i),0)%11===0;if(/^\d{13}$/.test(s))return [...s].reduce((sum,c,i)=>sum+Number(c)*(i%2?3:1),0)%10===0;return false;}
function dateOK(s){if(!/^\d{4}-\d{2}-\d{2}$/.test(s||''))return false;const d=new Date(s+'T00:00:00Z');return !isNaN(d)&&d.toISOString().slice(0,10)===s;}
const text=(value,max=500)=>String(value||'').trim().slice(0,max);
export function safeCover(value){const s=text(value,2000);if(!s)return '';try{const url=new URL(s);if(!['https:','http:'].includes(url.protocol))throw Error();return url.href;}catch{throw Error('Cover URL must start with https:// or http://.');}}
export function seed(){return {version:1,nextCopy:5,books:[{id:'BK-0001',isbn:'9780141439518',title:'Pride and Prejudice',author:'Jane Austen',publisher:'Penguin Classics',coverUrl:''},{id:'BK-0002',isbn:'9780547928227',title:'The Hobbit',author:'J. R. R. Tolkien',publisher:'Mariner Books',coverUrl:''},{id:'BK-0003',isbn:'9780061120084',title:'To Kill a Mockingbird',author:'Harper Lee',publisher:'Harper Perennial',coverUrl:''},{id:'BK-0004',isbn:'9780141439518',title:'Pride and Prejudice',author:'Jane Austen',publisher:'Penguin Classics',coverUrl:''}],borrowers:[{id:'BR-0001',name:'Sam (sample)',contact:'sam@example.com'},{id:'BR-0002',name:'Alex (sample)',contact:''}],loans:[{id:'LN-sample',copyId:'BK-0002',borrowerId:'BR-0001',checkoutDate:today(),dueDate:'',returnedDate:null}]};}
export function activeLoan(state,id){return state.loans.find(l=>l.copyId===normalizeCopy(id)&&!l.returnedDate);}
// Match ISBN-10 against its ISBN-13 edition, plus title/author for other editions.
function editionKey(value){
 const isbn=normalizeCode(value);
 if(isbn.length!==10)return isbn;
 const prefix='978'+isbn.slice(0,9);
 const sum=[...prefix].reduce((n,c,i)=>n+Number(c)*(i%2?3:1),0);
 return prefix+((10-sum%10)%10);
}
// Resolve an identifier or manual title/author search to eligible physical copies.
export const copyReference=book=>`carribrary:copy:v1:${book.copyUid}`;
export const validCopyReference=value=>/^carribrary:copy:v1:[a-f0-9]{32}$/i.test(String(value||'').trim());
export function circulationCopies(state,value,returning=false){
 const raw=String(value||'').trim();if(!raw)return [];
 const code=normalizeCode(raw),id=normalizeCopy(raw),reference=validCopyReference(raw)?raw.slice(-32).toLowerCase():null;
 const copies=state.books.filter(b=>!b.archived).filter(b=>
  raw.toLowerCase().startsWith('carribrary:')?reference!==null&&b.copyUid===reference:
  /^BK\d+$/.test(code)?b.id===id:
  /^[\dX]+$/.test(code)?validCode(code)&&editionKey(b.isbn)===editionKey(code):
  `${b.title} ${b.author} ${b.series||''}`.toLocaleLowerCase().includes(raw.toLocaleLowerCase()));
 return copies.filter(b=>Boolean(activeLoan(state,b.id))===returning&&(returning||!(state.holds||[]).some(h=>h.copyId===b.id)));
}
const matchText=value=>text(value).normalize('NFKC').toLocaleLowerCase().replace(/\s+/g,' ');
export function duplicateBooks(state,input,excludeId=''){
 const isbn=editionKey(input.isbn),title=matchText(input.title),author=matchText(input.author);
 const existing=state.books.find(b=>b.id===excludeId);
 if(existing&&editionKey(existing.isbn)===isbn&&matchText(existing.title)===title&&matchText(existing.author)===author)return [];
 return state.books.filter(b=>!b.archived&&b.id!==excludeId&&
  ((isbn&&editionKey(b.isbn)===isbn)||(title&&author&&matchText(b.title)===title&&matchText(b.author)===author)));
}
function bookDetails(input){
 const isbn=normalizeCode(input.isbn);
 if(isbn&&!validCode(isbn))throw Error('Enter a valid ISBN-10 or EAN-13 barcode, including its check digit.');
 if(!text(input.title))throw Error('A title is required.');
 return {isbn,title:text(input.title),author:text(input.author),series:text(input.series),publisher:text(input.publisher),coverUrl:safeCover(input.coverUrl),collectionId:input.collectionId||'general'};
}
function checkDuplicates(state,input,excludeId=''){
 const matches=duplicateBooks(state,input,excludeId);
 if(matches.some(b=>!Array.isArray(input.confirmedDuplicates)||!input.confirmedDuplicates.includes(b.id)))
  throw Error('Possible duplicate: '+matches.map(b=>b.id).join(', ')+'. Review and confirm before saving another copy.');
}
export function addBook(state,input){
 const details=bookDetails(input);checkDuplicates(state,{...details,confirmedDuplicates:input.confirmedDuplicates});
 const occupied=new Set([...state.books.map(b=>b.id),...state.loans.map(l=>l.copyId),...(state.requests||[]).map(r=>r.copyId),...(state.readingEntries||[]).map(r=>r.copyId)].map(id=>Number(id.slice(3))));
 let n=state.copyNumberPolicy==='next-number'?Math.max(state.nextCopy,...[...occupied].map(n=>n+1)):1;while(occupied.has(n))n++;
 const book={id:`BK-${String(n).padStart(4,'0')}`,...details};state.books.push(book);state.nextCopy=Math.max(state.nextCopy,n+1);return book;
}
export function updateBook(state,input){
 const book=state.books.find(b=>b.id===normalizeCopy(input.copyId)&&!b.archived);
 if(!book)throw Error('Book copy not found.');
 const details=bookDetails(input);checkDuplicates(state,{...details,confirmedDuplicates:input.confirmedDuplicates},book.id);
 if(!('collectionId' in input))details.collectionId=book.collectionId||'general';Object.assign(book,details);return book;
}
export function sortedBooks(books,key='title',direction='asc'){
 const field=['title','author','series','id'].includes(key)?key:'title';
 const collator=new Intl.Collator(undefined,{numeric:true,sensitivity:'base'});
 return [...books].sort((a,b)=>{
  const av=a[field]||'',bv=b[field]||'';
  // Unrecorded series/authors stay at the end in either direction.
  if(!av!==!bv)return av?-1:1;
  const order=collator.compare(av,bv)||collator.compare(a.title,b.title)||collator.compare(a.id,b.id);
  return direction==='desc'?-order:order;
 });
}
export function pageItems(items,page=1,size=10){
 size=[10,25,50].includes(Number(size))?Number(size):10;
 const pages=Math.max(1,Math.ceil(items.length/size));
 page=Math.min(pages,Math.max(1,Math.floor(Number(page))||1));
 return {items:items.slice((page-1)*size,page*size),page,pages,total:items.length,start:items.length?(page-1)*size+1:0,end:Math.min(page*size,items.length)};
}
export function bookListText(books,name="Carribrary"){
 return `${name} — ${books.length} physical ${books.length===1?'copy':'copies'}\n\n`+books.map(b=>
  `${b.title} — ${b.author||'Author not recorded'}${b.series?' | Series: '+b.series:''} | ${b.id} | ISBN: ${b.isbn}`.replace(/[\r\n]+/g,' ')).join('\n');
}
export function addBorrower(state,input){if(!text(input.name))throw Error('Enter the borrower’s name.');const ids=state.borrowers.map(b=>Number(b.id.slice(3))).filter(Number.isFinite);const b={id:`BR-${String(Math.max(0,...ids)+1).padStart(4,'0')}`,name:text(input.name),contact:text(input.contact)};state.borrowers.push(b);return b;}
export function checkout(state,input){const copyId=normalizeCopy(input.copyId);if(!state.books.some(b=>b.id===copyId&&!b.archived))throw Error('Copy ID not found. Choose a book from the catalog.');if(activeLoan(state,copyId))throw Error('This copy is already on loan. Return it before checking it out again.');if(!state.borrowers.some(b=>b.id===input.borrowerId&&!b.archived))throw Error('Select a borrower.');if(!dateOK(input.checkoutDate)||input.checkoutDate>today())throw Error('Choose a valid checkout date, today or earlier.');if(input.dueDate&&(!dateOK(input.dueDate)||input.dueDate<input.checkoutDate))throw Error('Due date must be on or after the checkout date.');const loan={id:globalThis.crypto.randomUUID(),copyId,borrowerId:input.borrowerId,checkoutDate:input.checkoutDate,dueDate:input.dueDate||'',returnedDate:null};state.loans.push(loan);return loan;}
export function returnBook(state,input){const l=activeLoan(state,input.copyId);if(!l)throw Error('No active loan found for that copy ID.');l.returnedDate=today();return l;}
// Records with returned loans are retained so loan references stay valid.
export function removeBook(state,input){
  const id=normalizeCopy(input.copyId),book=state.books.find(b=>b.id===id&&!b.archived);
  if(!book)throw Error('Book copy not found.');
  if(activeLoan(state,id))throw Error('Return this copy before deleting it.');
  const archived=state.loans.some(l=>l.copyId===id)||(state.requests||[]).some(r=>r.copyId===id)||(state.readingEntries||[]).some(r=>r.copyId===id);
  if(archived)book.archived=true;else state.books=state.books.filter(b=>b.id!==id);
  return {id,name:book.title,archived};
}
export function removeBorrower(state,input){
  const borrower=state.borrowers.find(b=>b.id===input.borrowerId&&!b.archived);
  if(!borrower)throw Error('Borrower not found.');
  if(state.loans.some(l=>l.borrowerId===borrower.id&&!l.returnedDate))throw Error('Return this borrower’s books before deleting them.');
  const archived=state.loans.some(l=>l.borrowerId===borrower.id)||(state.readers||[]).some(r=>r.borrowerId===borrower.id);
  if(archived)borrower.archived=true;else state.borrowers=state.borrowers.filter(b=>b.id!==borrower.id);
  return {id:borrower.id,name:borrower.name,archived};
}
export function validateBackup(s){if(!s||![1,2,3,4,5].includes(s.version)||!Array.isArray(s.books)||!Array.isArray(s.borrowers)||!Array.isArray(s.loans)||!Number.isSafeInteger(s.nextCopy)||s.nextCopy<1)throw Error('This is not a valid Carribrary backup.');const unique=items=>new Set(items.map(x=>x.id)).size===items.length;
s.collections??=[{id:'general',name:'General'}];if(!Array.isArray(s.collections)||!unique(s.collections)||!s.collections.some(c=>c.id==='general')||s.collections.some(c=>typeof c.name!=='string'||!c.name.trim()))throw Error('Backup has invalid collections.');for(const b of s.books)b.collectionId??='general';if(!unique(s.books)||!unique(s.borrowers)||!unique(s.loans))throw Error('Backup contains duplicate record IDs.');for(const b of s.books){if(!/^BK-\d{4,}$/.test(b.id)||((b.isbn!==''||s.version<4)&&!validCode(b.isbn))||typeof b.title!=='string'||!b.title.trim()||['author','publisher','coverUrl'].some(k=>typeof b[k]!=='string'))throw Error('Backup has an invalid book.');if(b.series!==undefined&&typeof b.series!=='string')throw Error('Backup has an invalid series.');safeCover(b.coverUrl);if(!s.collections.some(c=>c.id===b.collectionId))throw Error('Backup has an unknown collection.');if(b.archived!==undefined&&typeof b.archived!=='boolean')throw Error('Backup has an invalid archive flag.');}for(const b of s.borrowers)if(!/^BR-\d{4,}$/.test(b.id)||typeof b.name!=='string'||!b.name.trim()||typeof b.contact!=='string'||(b.archived!==undefined&&typeof b.archived!=='boolean'))throw Error('Backup has an invalid borrower.');const active=new Set();for(const l of s.loans){if(typeof l.id!=='string'||!s.books.some(b=>b.id===l.copyId)||!s.borrowers.some(b=>b.id===l.borrowerId)||!dateOK(l.checkoutDate)||l.checkoutDate>today()||(l.dueDate&&(!dateOK(l.dueDate)||l.dueDate<l.checkoutDate))||(l.returnedDate!==null&&(!dateOK(l.returnedDate)||l.returnedDate<l.checkoutDate||l.returnedDate>today())))throw Error('Backup has an invalid loan.');if(!l.returnedDate){if(s.books.find(b=>b.id===l.copyId).archived||s.borrowers.find(b=>b.id===l.borrowerId).archived)throw Error('Backup has an active loan for an archived record.');if(active.has(l.copyId))throw Error('Backup has two active loans for one copy.');active.add(l.copyId);}}return structuredClone(s);}
export function commit(state,action,input,storage=localStorage){const next=structuredClone(state);const result=action(next,input);storage.setItem(STORAGE_KEY,JSON.stringify(next));return {state:next,result};}
