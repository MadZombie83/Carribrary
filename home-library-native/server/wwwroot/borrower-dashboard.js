import {icon} from './ui-icons.js';
import {installDashboardDrawer} from './dashboard-shell.js';
import {installLibraryTitleFitting} from './title-fit.js';
const escape=x=>String(x??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export function installBorrowerDashboard(){
 installDashboardDrawer();installLibraryTitleFitting();
 const form=document.querySelector('#borrower-search-form');form.onsubmit=event=>{event.preventDefault();document.querySelector('[data-tab=catalog]').click();const input=document.querySelector('#search');input.value=form.elements.query.value;input.dispatchEvent(new Event('input'));input.focus();};
 document.querySelectorAll('[data-nav-icon]').forEach(el=>el.innerHTML=icon(el.dataset.navIcon));
}
export function borrowerOverview(snapshot){
 const target=document.querySelector('#borrower-overview');if(!target)return;
 const loans=(snapshot.loans||[]).filter(l=>!l.returnedDate),requests=[...(snapshot.requests||[]).filter(r=>['pending','approved'].includes(r.status)),...(snapshot.holds||[]).map(h=>({copyId:h.copyId,holdStage:h.stage,status:'approved'}))];
 const title=id=>snapshot.books.find(b=>b.id===id)?.title||id;
 const items=loans.slice().sort((a,b)=>(a.dueDate||'9999').localeCompare(b.dueDate||'9999')).slice(0,3);
 const row=(name,text)=>`<li><strong>${escape(name)}</strong><span>${escape(text)}</span></li>`;
 target.innerHTML=`<section class="dashboard-card"><div class="dashboard-card-head"><h2>${icon('clock')}My loans & due dates</h2><button type="button" class="text" data-overview-loans>View all</button></div><ul>${items.map(l=>row(title(l.copyId),(l.dueDate?'Due '+l.dueDate:'No due date')+(l.returnRequestedAt?' · Return reported — awaiting librarian receipt':''))).join('')||'<li>No books currently borrowed.</li>'}</ul></section><section class="dashboard-card"><div class="dashboard-card-head"><h2>${icon('requests')}My holds & requests</h2></div><ul>${requests.slice(0,3).map(r=>row(title(r.copyId),r.holdStage==='ready'?'Ready for pickup / delivery':r.status==='pending'?'On hold — awaiting approval':'Held until handover')).join('')||'<li>No open requests.</li>'}</ul><p class="help">A hold is not a loan. Borrowing starts when the book is handed over.</p></section>`;
 target.querySelector('[data-overview-loans]').onclick=()=>document.querySelector('[data-tab=loans]').click();
}
