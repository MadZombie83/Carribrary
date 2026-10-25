import {bookAbout} from './book-about.js';
import {googleCredit,googleCoverLink} from './cover-credit.js';
const $=s=>document.querySelector(s);
const escape=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
let books=[],page=1,loaded=false,collection='';
const collator=new Intl.Collator(undefined,{numeric:true,sensitivity:'base'});
function coverSources(book){
 let stored='';try{const url=new URL(book.coverUrl);if(['https:','http:'].includes(url.protocol)){url.protocol='https:';stored=url.href;}}catch{}
 const isbn=String(book.isbn||'').replace(/[\s-]/g,'');
 const fallback=/^(?:\d{9}[\dXx]|\d{13})$/.test(isbn)?`https://covers.openlibrary.org/b/isbn/${encodeURIComponent(isbn)}-M.jpg?default=false`:'';
 return {source:stored||fallback,fallback:stored&&stored!==fallback&&!googleCoverLink(stored)?fallback:''};
}
function coverPlaceholder(){return '<div class="cover cover-placeholder" aria-hidden="true"><span>▤</span><small>No cover</small></div>';}
function coverMarkup(book){const {source,fallback}=coverSources(book);return source?`<img class="cover" src="${escape(source)}" alt="Cover of ${escape(book.title)}" width="68" height="100" loading="lazy" decoding="async" referrerpolicy="no-referrer" ${fallback?`data-fallback="${escape(fallback)}"`:''}>`:coverPlaceholder();}
function render(){
 const q=$('#search').value.trim().toLowerCase(),key=$('#sort').value,size=Number($('#size').value);
 const all=books.filter(b=>!collection||(b.collectionId||'general')===collection).filter(b=>`${b.title} ${b.author} ${b.series} ${b.id} ${b.isbn} ${b.publisher}`.toLowerCase().includes(q)).sort((a,b)=>{
  const av=a[key]||'',bv=b[key]||'';if(!av!==!bv)return av?-1:1;
  return collator.compare(av,bv)||collator.compare(a.title,b.title)||collator.compare(a.id,b.id);
 });
 const pages=Math.max(1,Math.ceil(all.length/size));page=Math.min(pages,Math.max(1,page));
 $('#catalog-summary').textContent=loaded?`${books.length} ${books.length===1?'copy':'copies'} · ${books.filter(b=>!b.availability||b.availability==='available').length} available`:'';
 $('#status').textContent=loaded?`${all.length} matching ${all.length===1?'copy':'copies'}`:'Loading books…';
 $('#books').innerHTML=all.length?all.slice((page-1)*size,page*size).map(b=>`<article><div class="book-heading">${coverMarkup(b)}<div class="book-info"><span class="id">${escape(b.id)}</span><h2>${escape(b.title)}</h2><p>${escape(b.author||'Author not recorded')}</p></div></div>${b.series?`<p>Series: ${escape(b.series)}</p>`:''}<p class="muted">${escape(b.publisher)}<br>ISBN: ${escape(b.isbn)}</p>${googleCredit(b.coverUrl)}<div class="catalog-book-footer"><button class="book-about-action" data-about="${escape(b.id)}">About this book</button><p class="availability" data-availability="${escape(b.availability||'available')}">${b.availability==='on-hold'?'On hold':b.availability==='on-loan'?'On loan':'Available'}</p><button data-request="${escape(b.id)}" ${b.availability&&b.availability!=='available'?'disabled':''}>Request to borrow</button></div></article>`).join(''):loaded?'<div class="catalog-empty"><h2>No matching books</h2><p>Try a different title, author or collection.</p></div>':'';
 $('#books').querySelectorAll('img.cover').forEach(img=>img.onerror=()=>{if(img.dataset.fallback){const fallback=img.dataset.fallback;delete img.dataset.fallback;img.src=fallback;}else{const holder=document.createElement('div');holder.innerHTML=coverPlaceholder();img.replaceWith(holder.firstElementChild);}});
 $('#books').querySelectorAll('[data-about]').forEach(button=>button.onclick=()=>bookAbout(books.find(b=>b.id===button.dataset.about),'about/'+encodeURIComponent(button.dataset.about)));$('#books').querySelectorAll('[data-request]').forEach(button=>button.onclick=()=>openRequest(button.dataset.request));
 $('#page').textContent=`Page ${page} of ${pages}`;$('#prev').disabled=page===1;$('#next').disabled=page===pages;
}
$('#search').oninput=()=>{page=1;render();};for(const id of ['sort','size'])$('#'+id).onchange=()=>{page=1;render();};
$('#prev').onclick=()=>{page--;render();$('#books').scrollIntoView({block:'start'});};$('#next').onclick=()=>{page++;render();$('#books').scrollIntoView({block:'start'});};
try{
 const response=await fetch('books.json',{cache:'no-store',signal:AbortSignal.timeout(15000)});
 if(!response.ok)throw Error();const data=await response.json();books=data.books;const collections=data.collections||[{id:'general',name:'General'}];$('#collection').innerHTML='<option value="">All collections</option>'+collections.map(c=>`<option value="${escape(c.id)}">${escape(c.name)}</option>`).join('');const requested=new URL(location.href).searchParams.get('collection')||'';collection=collections.some(c=>c.id===requested)?requested:'';$('#collection').value=collection;const name=data.libraryName||'Carribrary';$('h1').textContent=name;document.title=name+' · Book list';for(const [id,text]of [['banner-subtitle',data.bannerSubtitle??'A shelf worth sharing'],['encouraging-message',data.encouragingMessage??'A place for every book.']]){const element=$('#'+id);element.textContent=text;element.hidden=!text;}loaded=true;render();
}catch{$('#status').textContent='The catalog is unavailable. The computer may be offline, or the link may have been revoked. Try refreshing later.';}

let requestCopy='',requestAttempt=null;
function openRequest(copy){requestCopy=copy;requestAttempt=null;$('#request-form').reset();$('#request-status').textContent='';$('#submit-request').disabled=false;$('#submit-request').hidden=false;const book=books.find(b=>b.id===copy);$('#request-book').textContent=(book?.title||copy)+' · '+copy;$('#request-dialog').showModal();}
$('#close-request').onclick=()=>$('#request-dialog').close();
$('#request-form').onsubmit=async event=>{
 event.preventDefault();const data=Object.fromEntries(new FormData(event.target));const book=books.find(b=>b.id===requestCopy);const content=JSON.stringify({copyId:requestCopy,expectedIsbn:book?.isbn||'',expectedTitle:book?.title||'',...data});
 if(!requestAttempt||requestAttempt.content!==content)requestAttempt={content,id:crypto.randomUUID()};
 const button=$('#submit-request');button.disabled=true;$('#request-status').textContent='Sending request…';
 try{
  const response=await fetch('requests',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({...JSON.parse(content),requestId:requestAttempt.id}),signal:AbortSignal.timeout(15000)});
  const result=await response.json().catch(()=>({}));if(!response.ok)throw Error(response.status===429?'Too many requests. Please wait a few minutes before trying again.':result.error||'The request could not be sent. Try again later.');
  book.availability='on-hold';render();$('#request-status').textContent='Copy held while awaiting approval. Request received. The library owner will review it and can contact you using the details you provided.';button.hidden=true;
 }catch(error){$('#request-status').textContent=error.name==='TimeoutError'?'Connection timed out. You can retry safely; your request will not be duplicated.':error.message;}
 finally{button.disabled=false;}
};

$("#collection").onchange=()=>{collection=$("#collection").value;const url=new URL(location.href);if(collection)url.searchParams.set("collection",collection);else url.searchParams.delete("collection");history.replaceState(null,"",url);page=1;render();};

const accountLink=document.getElementById('account-link');if(accountLink)accountLink.href='/account/?library='+encodeURIComponent(location.pathname.split('/')[2]||'');
