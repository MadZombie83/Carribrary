// Administrative requests remain on the authenticated, same-origin companion route.
const original=window.fetch.bind(window);
window.fetch=(input,options)=>{
 if(typeof input==='string'&&input.startsWith('/api/'))input='/companion'+input;
 return original(input,options).then(response=>{if(response.status===401&&typeof input==='string'&&input.startsWith('/companion/api/'))location.replace('/companion/');return response;});
};
document.addEventListener('DOMContentLoaded',()=>{
 document.body.classList.add('iphone-companion');
 const card=document.createElement('section');card.id='companion-device-tools';card.className='settings-card companion-device-tools';card.hidden=true;
 card.innerHTML='<p class="eyebrow">THIS COMPANION</p><h2>Your iPhone / browser</h2><p>Manage account, borrowing and renewal request notifications and this device’s approved connection. Other paired devices are unaffected.</p><div class="companion-device-actions"><button class="secondary" id="admin-push">Request notifications</button><button class="secondary" id="companion-signout">Sign out / unpair</button></div><p id="push-status" role="status"></p>';
 document.body.append(card);
 const mount=()=>{
  const page=document.querySelector('#settings-page'),parts=location.hash.split('/');
  const show=page&&!page.hidden&&(!parts[1]||parts[1]==='people-access')&&!parts[2];card.hidden=!show;
  if(show)page.append(card);else document.body.append(card);
 };
 document.addEventListener('carribrary:settings-render',mount);window.addEventListener('hashchange',mount);mount();
 document.querySelector('#iphone-companion').hidden=true;
 document.querySelectorAll('a[href="/help.html"]').forEach(a=>a.href='/companion/help.html');
 card.querySelector('#companion-signout').onclick=async()=>{
  if(!confirm('Unpair this browser and turn off its notifications?'))return;
  const button=card.querySelector('#companion-signout');button.disabled=true;
  try{const response=await fetch('/companion/logout',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'});if(!response.ok)throw Error('Could not unpair this device. Check the connection and try again.');location.replace('/companion/');}
  catch(error){card.querySelector('#push-status').textContent=error.message;button.disabled=false;}
 };
 const install=document.createElement('button');install.type='button';install.id='install-companion';install.className='secondary';install.textContent='Install librarian app';const installStatus=document.createElement('p');installStatus.id='install-status';installStatus.hidden=true;installStatus.setAttribute('role','status');card.prepend(install,installStatus);
 import('/account/pwa-install.js').then(({installPwa})=>installPwa(install,installStatus,{admin:true}));
 import('./push-client.js').then(({pushControls})=>pushControls('/companion',card.querySelector('#admin-push'),card.querySelector('#push-status'))).catch(()=>{card.querySelector('#push-status').textContent='Notifications are unavailable. Your library still works.';});
});
