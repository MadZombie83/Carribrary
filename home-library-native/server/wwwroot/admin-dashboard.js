import {applyLibraryArtwork,artworkSource,applyServerArtworkLayout} from './library-artwork.js';
import {googleCredit} from './cover-credit.js';
// Presentation only: reuse server state and existing actions; never fetch private reader data.
const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export function dashboardSummary(state,today){
 const books=(state.books||[]).filter(b=>!b.archived),ids=new Set(books.map(b=>b.id));
 const loans=(state.loans||[]).filter(l=>!l.returnedDate&&ids.has(l.copyId)),loanIds=new Set(loans.map(l=>l.copyId));
 const holds=(state.holds||[]).filter(h=>ids.has(h.copyId)&&!loanIds.has(h.copyId)),heldIds=new Set(holds.map(h=>h.copyId));
 return {books,loans,holds,available:books.filter(b=>!loanIds.has(b.id)&&!heldIds.has(b.id)).length,requests:(state.requests||[]).filter(r=>['pending','approved'].includes(r.status)),people:(state.borrowers||[]).filter(b=>!b.archived),overdue:loans.filter(l=>l.dueDate&&l.dueDate<today).length};
}
export {icon} from './ui-icons.js';
import {icon} from './ui-icons.js';
const action=(name,label,kind='secondary')=>`<button type="button" class="${kind}" data-dashboard-action="${name}">${label}</button>`;
const sectionIcons={'Recent requests':'requests','Lending & holds':'swap','Quick Actions':'bolt','Borrower snapshot':'people','Activity / System Status':'activity','Catalog preview':'book'};
const heading=(label,tool)=>`<div class="dashboard-card-head"><h2>${icon(sectionIcons[label]||'book')}${label}</h2>${tool?action(tool,'View all →','text'):''}</div>`;
function cover(book){return book.coverUrl?`<img class="dashboard-cover" src="${esc(book.coverUrl)}" alt="" loading="lazy" referrerpolicy="no-referrer">`:'<span class="dashboard-cover placeholder" aria-hidden="true">'+icon('book')+'</span>';}
function requestDate(value){const d=new Date(value);return Number.isNaN(d.getTime())?'Date unavailable':new Intl.DateTimeFormat(undefined,{dateStyle:'medium'}).format(d);}
export function summaryPanels(s){return `${[['book',s.books.length,'Physical copies'],['check',s.available,'Available'],['clock',s.loans.length,'On loan'],['people',s.requests.length,'Active requests']].map(([symbol,n,label],i)=>`<div class="dashboard-kpi kpi-${i}"><span class="kpi-icon">${icon(symbol)}</span><div><strong data-kpi="${i}">${n}</strong><span>${label}</span></div></div>`).join('')}`;}
export function renderDashboard(state,{today,offline,onAction,onBook}){
 const s=dashboardSummary(state,today),target=document.querySelector('#admin-dashboard');
 const labels={pending:'Awaiting approval',approved:'Approved · on hold',fulfilled:'Handed over',declined:'Declined',cancelled:'Cancelled'};
 const recent=[...(state.requests||[])].sort((a,b)=>(b.submittedAt||'').localeCompare(a.submittedAt||'')).slice(0,3);
 const recentHTML=recent.map(r=>{const b=(state.books||[]).find(b=>b.id===r.copyId)||{title:r.copyId};return `<li>${cover(b)}<div><strong>${esc(b.title)}</strong><p class="request-author">${esc(b.author||'Author not recorded')}</p><p>${esc(r.name||'Borrower')} · ${esc(requestDate(r.submittedAt))}</p><span class="badge request-${esc(r.status)}">${esc(labels[r.status]||r.status)}</span>${googleCredit(b.coverUrl||'')}</div>${action('requests','Review','text')}</li>`;}).join('');
 const rows=[['loans','On loan',s.loans.length],['holds','Holds waiting',s.holds.length],['loans','Overdue',s.overdue]];
 target.innerHTML=`<div class="dashboard-heading"><h1>${esc(state.libraryName||'Carribrary')}</h1><p>${esc(state.bannerSubtitle??'A shelf worth sharing')}</p></div>
 <div class="dashboard-kpis" aria-label="Whole-library summary">${summaryPanels(s)}</div>
 <div class="dashboard-upper"><section class="dashboard-card recent-requests">${heading('Recent requests','requests')}<ul class="dashboard-list">${recentHTML||'<li class="dashboard-empty">No requests yet. Share your library to invite readers.</li>'}</ul></section>
 <section class="dashboard-card">${heading('Lending & holds','holds')}<div class="dashboard-lending">${rows.map(([tool,label,n],i)=>action(tool,`<i class="lending-icon lending-${i}">${icon(['clock','hourglass','alert'][i])}</i><span>${label}</span><strong>${n}</strong><span aria-hidden="true">›</span>`)).join('')}</div></section>
 <section class="dashboard-card quick-actions">${heading('Quick Actions')}<div>${action('add',icon('plus')+'<span>Add Book</span>','primary')}${action('holds',icon('people')+'<span>Create Hold</span>')}${action('batch',icon('swap')+'<span>Batch Checkout</span>')}${action('backup',icon('backup')+'<span>Backup Now</span>')}${action('share-qr',icon('requests')+'<span>Share Library</span>')}${action('books-to-buy',icon('book')+'<span>Books to Buy</span>')}</div><p class="help">Choose a backup type before downloading.</p></section></div>
 <div class="dashboard-lower"><section class="dashboard-card">${heading('Borrower snapshot','borrowers')}<ul class="dashboard-list">${s.people.slice(0,3).map(p=>{const n=s.loans.filter(l=>l.borrowerId===p.id).length;return `<li><span class="borrower-avatar" aria-hidden="true">${esc(p.name.slice(0,1))}</span><div><strong>${esc(p.name)}</strong><p>${n} ${n===1?'book':'books'} on loan</p></div>${action('borrowers','View','text')}</li>`;}).join('')||'<li class="dashboard-empty">No borrowers yet. Add someone when you are ready to lend.</li>'}</ul></section>
 <section class="dashboard-card">${heading('Activity / System Status')}<dl class="dashboard-status"><div><dt>Library server</dt><dd class="${offline?'status-offline':''}">${offline?'Unavailable · showing last loaded data':'Connected'}</dd></div><div><dt>Paired devices</dt><dd>${action('devices','Manage devices','text')}</dd></div><div><dt>Automatic backups</dt><dd>${action('setup','Check latest backup','text')}</dd></div><div><dt>Email reminders</dt><dd>${action('reminders','Review settings','text')}</dd></div></dl><p class="help">Review devices, backup history and reminder settings.</p></section></div>
 <section class="dashboard-card dashboard-preview">${heading('Catalog preview','catalog')}<div class="dashboard-books">${s.books.slice(0,4).map(b=>{const l=s.loans.some(l=>l.copyId===b.id),h=s.holds.some(h=>h.copyId===b.id);return `<article class="preview-book">${cover(b)}<div><button type="button" class="text" data-dashboard-book="${esc(b.id)}">${esc(b.title)}</button><span class="muted">${esc(b.author||'Author not recorded')}</span><span class="copy-id">${esc(b.id)}</span><span class="badge ${l?'loan':''}">${l?'On loan':h?'On hold':'Available'}</span>${googleCredit(b.coverUrl||'')}</div></article>`;}).join('')||'<p class="dashboard-empty">Your shelf is ready. Add a book to begin.</p>'}</div></section>`;
 applyLibraryArtwork(target.querySelector('.dashboard-heading'),artworkSource(state.librarySidebarArtwork));
 target.querySelectorAll('[data-dashboard-action]').forEach(b=>b.onclick=()=>onAction(b.dataset.dashboardAction));
 target.querySelectorAll('[data-dashboard-book]').forEach(b=>b.onclick=()=>onBook(b.dataset.dashboardBook));
 target.querySelectorAll('img').forEach(img=>img.onerror=()=>{const fallback=document.createElement('span');fallback.className='dashboard-cover placeholder';fallback.setAttribute('aria-hidden','true');fallback.innerHTML=icon('book');img.replaceWith(fallback);});
}
export function syncAdminShell(state,view){
 applyServerArtworkLayout(document.body,state.libraryServerArtwork);
 applyLibraryArtwork(document.querySelector('.admin-header'),artworkSource(state.librarySidebarArtwork));
 document.body.dataset.adminView=view;
 document.body.dataset.libraryTheme=state.libraryTheme||'classic';
 const name=document.querySelector('[data-sidebar-name]');name.textContent=state.libraryName||'Carribrary';
 document.querySelector('[data-sidebar-subtitle]').textContent=state.bannerSubtitle??'A shelf worth sharing';
 const logo=document.querySelector('[data-sidebar-icon]');logo.replaceChildren();
 if(state.libraryIcon192){const img=document.createElement('img');img.src='data:image/png;base64,'+state.libraryIcon192;img.alt='';logo.append(img);}else logo.innerHTML=icon('book');
 const artwork=document.querySelector('.sidebar-decoration');artwork.hidden=!state.librarySidebarArtwork;if(artwork.dataset.image!==state.librarySidebarArtwork){artwork.dataset.image=state.librarySidebarArtwork||'';artwork.replaceChildren();if(state.librarySidebarArtwork){const img=document.createElement('img');img.src='data:image/png;base64,'+state.librarySidebarArtwork;img.alt='';artwork.append(img);}}
 const quote=document.querySelector('#sidebar-quote');quote.textContent=state.encouragingMessage??'A place for every book.';quote.hidden=!quote.textContent;
 document.querySelectorAll('[data-nav-icon]').forEach(el=>{if(!el.childElementCount)el.innerHTML=icon(el.dataset.navIcon);});
}
