// In-memory draft protection only. Never persist or log field values, passwords or private notes.
export function installFormWorkflow(dialog){
 let form=null,protect=false,baseline='',submitting=0,returnHash=location.hash;
 const originalClose=dialog.close.bind(dialog);
 const snapshot=()=>form?JSON.stringify([...form.querySelectorAll('input,select,textarea')].filter(e=>!['submit','button','search'].includes(e.type)).map(e=>[e.type,e.name,e.value,e.checked,[...(e.files||[])].map(f=>[f.name,f.size,f.lastModified])])):'';
 const dirty=()=>protect&&!!form?.isConnected&&dialog.open&&snapshot()!==baseline;
 function leave(){if(!dirty()||submitting)return true;if(!window.confirm('Leave without saving? Choose Cancel to keep editing. Choose OK to discard your unsaved changes.'))return false;baseline=snapshot();return true;}
 dialog.close=function(...args){if(leave())originalClose(...args);};
 dialog.addEventListener('carribrary:form-saved',e=>{if(e.target===form){baseline=snapshot();const status=form.querySelector('.form-workflow-status');if(status)status.textContent='';}});
 dialog.addEventListener('cancel',e=>{if(!leave())e.preventDefault();});
 dialog.addEventListener('close',()=>{form=null;baseline='';});
 document.addEventListener('click',e=>{
  if(!dialog.open)return;
  const target=e.target.closest('a,button');if(!target||target.matches('a[target="_blank"]'))return;
  const navigation=!dialog.contains(target)&&target.matches('a[href],[data-view],[data-admin-tool],[data-settings-action],[data-settings-nav],#logout,#signout');
  const cancellation=dialog.contains(target)&&target.matches('button[type=button]')&&/^(Cancel|Cancel changes|Back|Close)$/i.test(target.textContent.trim());
  if((navigation||cancellation)&&((submitting&&navigation)||!leave())){e.preventDefault();e.stopImmediatePropagation();}
 },{capture:true});
 document.addEventListener('submit',e=>{if(dialog.open&&!dialog.contains(e.target)&&(submitting||!leave())){e.preventDefault();e.stopImmediatePropagation();}},{capture:true});
 for(const type of ['beforeinput','keydown'])document.addEventListener(type,e=>{if(submitting&&dialog.contains(e.target)&&e.target.matches('input,select,textarea')&&(type!=='keydown'||e.key!=='Tab'))e.preventDefault();},{capture:true});
 window.addEventListener('hashchange',e=>{if(dirty()&&!submitting&&!leave()){history.replaceState(null,'',location.pathname+location.search+returnHash);e.stopImmediatePropagation();}else returnHash=location.hash;},{capture:true});
 window.addEventListener('beforeunload',e=>{if(dirty()||submitting){e.preventDefault();e.returnValue='';}});
 return {
  track(next){form=next;returnHash=location.hash;queueMicrotask(()=>{if(form===next)baseline=snapshot();});
   const submit=next.querySelector('[type=submit]');protect=!!submit;if(!submit)return;
   let cancel=[...next.querySelectorAll('button[type=button]')].find(b=>/^(Cancel|Close|Back|Keep request)$/i.test(b.textContent.trim()));
   if(!cancel){cancel=document.createElement('button');cancel.type='button';cancel.className='secondary form-cancel';cancel.textContent='Cancel';cancel.onclick=()=>dialog.close();next.append(cancel);}
   if(submit.parentElement===next&&cancel.parentElement===next){const actions=document.createElement('div');actions.className='form-workflow-actions';next.insertBefore(actions,submit);actions.append(submit,cancel);}
   const status=document.createElement('p');status.className='form-workflow-status';status.setAttribute('role','status');status.setAttribute('aria-live','polite');next.append(status);
   const update=()=>queueMicrotask(()=>{if(form!==next||submitting)return;const message=dirty()?'Unsaved changes. Save to apply them.':'';if(status.textContent!==message)status.textContent=message;});next.addEventListener('input',update);next.addEventListener('change',update);
  },
  begin(next){submitting++;next.setAttribute('aria-busy','true');const fields=[...next.querySelectorAll('input,select,textarea')].map(e=>[e,e.getAttribute('aria-disabled')]);for(const [e]of fields)e.setAttribute('aria-disabled','true');const close=dialog.querySelector('#close-form,#close'),cancel=next.querySelector('.form-cancel');const oldClose=close?.disabled;if(close)close.disabled=true;if(cancel)cancel.disabled=true;const status=next.querySelector('.form-workflow-status');if(status)status.textContent='Working… Keep this page open.';return ()=>{submitting=Math.max(0,submitting-1);next.removeAttribute('aria-busy');for(const [e,value]of fields){if(value===null)e.removeAttribute('aria-disabled');else e.setAttribute('aria-disabled',value);}if(close?.isConnected)close.disabled=oldClose;if(cancel?.isConnected)cancel.disabled=false;if(status?.isConnected)status.textContent=form===next&&dirty()?'Unsaved changes. Save to apply them.':'';};},
  saved(next){if(form===next)baseline=snapshot();},
  canLeave:leave
 };
}

// Borrower editors build their forms before opening the shared dialog.
export function observeDialogFormWorkflow(dialog){
 if(!dialog)return;
 const workflow=installFormWorkflow(dialog),showModal=dialog.showModal.bind(dialog);let tracked;
 dialog.showModal=function(){
  const form=dialog.querySelector('form');
  if(form&&form!==tracked){tracked=form;workflow.track(form);const submit=form.onsubmit;
   if(submit)form.onsubmit=async function(event){const finish=workflow.begin(form);try{const result=await submit.call(this,event);if(result!==false&&!form.querySelector('.form-error')?.textContent.trim())workflow.saved(form);return result;}finally{finish();}};
  }
  return showModal();
 };
}

export function markFormSaved(form){form?.dispatchEvent(new CustomEvent('carribrary:form-saved',{bubbles:true}));}
