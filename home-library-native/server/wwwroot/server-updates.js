// Server release notices are librarian-only. Installers remain a deliberate manual step.
const key='carribrary-dismissed-server-update';
export async function mountServerUpdates(){
 const root=document.createElement('section');root.id='server-update-notice';root.className='server-update-notice dashboard-card';root.hidden=true;root.setAttribute('aria-label','Server update');
 document.querySelector('main')?.prepend(root);
 const tools=document.createElement('details');tools.className='server-update-tools';tools.innerHTML='<summary>Server updates</summary><p>Checks official GitHub releases at most once a day when an administrator opens Carribrary. Preview installations include preview releases; stable installations use stable releases. Nothing is installed automatically.</p><button type="button" class="secondary">Check for updates</button><p role="status" aria-live="polite"></p>';
 const footer=document.querySelector('.product-footer');if(footer)footer.before(tools);else document.querySelector('main')?.append(tools);
 const message=tools.querySelector('[role=status]'),button=tools.querySelector('button');
 async function check(manual=false){
  button.disabled=true;message.textContent='Checking official Carribrary releases…';tools.setAttribute('aria-busy','true');
  try{
   const response=await fetch('/api/updates',{method:manual?'POST':'GET',...(manual?{headers:{'Content-Type':'application/json'},body:'{}'}:{}),cache:'no-store',signal:AbortSignal.timeout(20000)});
   if(!response.ok)throw Error();const s=await response.json();
   const version=/^\d+\.\d+\.\d+(-preview)?$/.test(s.latestVersion)?s.latestVersion:'';
   const url='https://github.com/MadZombie83/Carribrary/releases/tag/v'+version;
   // Only link to the fixed official repository; never trust arbitrary server/remote markup.
   const available=s.available&&version&&(s.releaseUrl===url||s.releaseUrl===url.replace('/v','/'));
   message.textContent=s.error||(available?'Carribrary '+version+' is available.':'No newer release found for this installation.');
   let dismissed='';try{dismissed=localStorage.getItem(key);}catch{}
   root.hidden=!available||(!manual&&dismissed===version);
   root.replaceChildren();if(available){
    const heading=document.createElement('strong');heading.textContent='Carribrary '+version+' is available';
    const text=document.createElement('p');text.textContent='Create and verify a complete backup first. Review the release notes and choose the installer for your server. Updates are installed on the server computer.';
    const link=document.createElement('a');link.href=s.releaseUrl;link.target='_blank';link.rel='noopener noreferrer';link.textContent='Release notes and server downloads';
    const dismiss=document.createElement('button');dismiss.type='button';dismiss.className='secondary';dismiss.textContent='Dismiss this version';dismiss.onclick=()=>{try{localStorage.setItem(key,version);}catch{}root.hidden=true;};root.append(heading,text,link,dismiss);
   }
  }catch{message.textContent='Could not check for updates. Your library still works; try again later.';}finally{button.disabled=false;tools.removeAttribute('aria-busy');}
 }
 button.onclick=()=>check(true);await check();
}
