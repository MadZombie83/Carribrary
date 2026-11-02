// Shared dashboard drawer behavior. Navigation contents and authorization stay role-specific.
export function installDashboardDrawer(){
 const sidebar=document.querySelector('.admin-sidebar'),toggle=document.querySelector('[data-shell-toggle]'),close=document.querySelector('[data-shell-close]'),backdrop=document.querySelector('[data-shell-backdrop]');
 if(!sidebar||!toggle)return;
 const mobile=matchMedia('(max-width:900px)');let opened=false;
 const set=(value,restore=false)=>{opened=value&&mobile.matches;document.body.classList.toggle('shell-nav-open',opened);toggle.setAttribute('aria-expanded',String(opened));sidebar.inert=mobile.matches&&!opened;backdrop.hidden=!opened;if(opened)sidebar.querySelector('button:not([hidden])')?.focus();else if(restore)toggle.focus();};
 toggle.onclick=()=>set(!opened,true);close.onclick=()=>set(false,true);backdrop.onclick=()=>set(false,true);
 sidebar.addEventListener('click',event=>{if(event.target.closest('button:not([data-shell-close]),a')&&opened)set(false,true);});
 document.addEventListener('keydown',event=>{if(!opened)return;if(event.key==='Escape'){event.preventDefault();set(false,true);}if(event.key==='Tab'){const all=[...sidebar.querySelectorAll('button,a[href],[tabindex="0"]')].filter(e=>!e.disabled&&e.getClientRects().length);const first=all[0],last=all.at(-1);if(event.shiftKey&&document.activeElement===first){event.preventDefault();last?.focus();}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first?.focus();}}});
 mobile.addEventListener('change',()=>set(mobile.matches&&opened));set(false);
}
