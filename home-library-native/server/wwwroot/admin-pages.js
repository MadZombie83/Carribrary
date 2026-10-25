import {icon} from './admin-dashboard.js';
export const pageEscape=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export function pageSummary(state,view,day){
 const people=(state.borrowers||[]).filter(p=>!p.archived),loans=(state.loans||[]).filter(l=>!l.returnedDate),books=(state.books||[]).filter(b=>!b.archived);
 if(view==='borrowers')return [[people.length,'Borrowers'],[new Set(loans.map(l=>l.borrowerId)).size,'Currently borrowing'],[loans.length,'Books on loan']];
 if(view==='loans')return [[loans.length,'On loan'],[loans.filter(l=>l.dueDate&&l.dueDate<day).length,'Overdue'],[loans.filter(l=>!l.dueDate).length,'No due date']];
 if(view==='reading')return [];
 return null; // Catalog retains collection-scoped counts from the existing renderer.
}
export function syncPageHeading(state,view,day){
 const headings={catalog:['Catalog','Browse your collection, find a copy and lend a good story.'],borrowers:['Borrowers','Manage lending contacts and reminder permissions.'],loans:['On loan','See who has each book and bring it back to the shelf.'],reading:['Reading tracker','Household reading lists; borrower personal lists remain private.']};
 const [title,description]=headings[view]||['Your library',''];document.querySelector('#page-title').textContent=title;document.querySelector('#page-description').textContent=description;
 const counts=pageSummary(state,view,day);if(counts)document.querySelector('#stats').innerHTML=counts.map(([number,label])=>`<div class="stat"><strong>${number}</strong><span>${label}</span></div>`).join('');
}
export function borrowerCard(person,loans){
 const initials=person.name.trim().split(/\s+/).slice(0,2).map(part=>Array.from(part)[0]||'').join('').toUpperCase(),count=loans.filter(l=>l.borrowerId===person.id&&!l.returnedDate).length,e=pageEscape;
 return `<article class="person borrower-card"><div class="borrower-card-head"><span class="borrower-card-avatar" aria-hidden="true">${e(initials)}</span><div><span class="copy-id">${e(person.id)}</span><h2>${e(person.name)}</h2></div></div><p class="borrower-contact">${icon('people')}<span>${e(person.contact||'No contact information')}</span></p><p class="borrower-loan-count"><span class="badge ${count?'loan':''}">${count} ${count===1?'book':'books'} on loan</span></p><p class="help">${person.reminderConsent?'Email reminders permitted':'Automatic email reminders off'}</p><div class="record-actions"><button class="secondary" data-edit-borrower="${e(person.id)}">Edit borrower / reminders</button><button class="text danger" data-delete-borrower="${e(person.id)}" aria-label="Delete borrower ${e(person.name)}">Delete borrower</button></div></article>`;
}
export function installWorkspaceStyling(){
 const dialog=document.querySelector('#form-dialog'),heading=document.querySelector('#dialog-title');
 const sync=()=>{const tool={'Book requests':'requests','Lending & holds':'lending-tools'}[heading.textContent];dialog.classList.toggle('workspace-dialog',!!tool);dialog.dataset.workspace=tool||'';
  document.querySelectorAll('.sidebar-nav [data-admin-tool]').forEach(button=>{if(dialog.open&&!dialog.classList.contains('settings-inline-editor')&&button.dataset.adminTool===tool)button.setAttribute('aria-current','page');else button.removeAttribute('aria-current');});
  document.querySelectorAll('.sidebar-nav [data-view]').forEach(button=>{if(!(dialog.open&&tool&&!dialog.classList.contains('settings-inline-editor'))&&button.dataset.view===document.body.dataset.adminView)button.setAttribute('aria-current','page');else button.removeAttribute('aria-current');});
 };
 new MutationObserver(sync).observe(heading,{subtree:true,childList:true,characterData:true});new MutationObserver(sync).observe(dialog,{attributes:true,attributeFilter:['open']});
}
