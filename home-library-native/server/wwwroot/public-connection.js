import {pageEscape as esc} from './admin-pages.js';
export async function publicConnectionSetup({request,openForm,toast}){
 try{
  if(location.pathname.startsWith('/companion')){openForm('Public sharing setup',`<form><p>Open this tool on the library server computer and sign in as a local administrator. Public sharing setup changes how that computer accepts internet traffic.</p><p>On Windows use Start → Carribrary → Connection setup. On Linux use Carribrary Public Sharing Setup in the application menu or this Settings & Tools page locally.</p></form>`,async()=>{});return;}
  const s=await request('/api/public-connection');
  if(!s.supported){openForm('Public sharing setup',`<form><p>On Windows, open Start → Carribrary → Connection setup → Set up public book-list sharing.</p><p>Open the local Libraries &amp; network tool for multi-library setup.</p></form>`,async()=>{});return;}
  const addresses=s.addresses.map(esc).join(', ')||'No local IPv4 address detected';
  const check=(name,label,checked=false)=>`<label style="display:flex;align-items:flex-start;gap:.6rem"><input style="width:auto;min-height:0;flex-shrink:0;margin-top:.25rem" name="${name}" type="checkbox" ${checked?'checked':''}> <span>${label}</span></label>`;
  openForm('Public sharing setup',`<form id="public-connection-form"><p>Set up your public library address. For additional libraries, use Libraries &amp; network.</p>${s.windows?'<button type="button" class="secondary" id="shared-https-setup">Shared HTTPS</button>':''}<nav aria-label="Public sharing setup steps" style="flex-wrap:wrap">${['Hostname','Restart','Router','Check'].map((v,i)=>`<button type="button" data-connection-step="${i+1}">${i+1}. ${v}</button>`).join('')}</nav><p class="form-error" role="alert"></p><p id="connection-result" role="status">${s.pendingRestart?'Saved changes need a Carribrary restart.':''}</p>
  <section data-connection-panel="1"><h3>Your public hostname</h3><p><a href="https://www.duckdns.org/" target="_blank" rel="noopener noreferrer">Create a free hostname at DuckDNS.org</a>, then return here.</p><label>Public hostname<input name="hostname" value="${esc(s.hostname||'')}" placeholder="mybooks.duckdns.org" autocomplete="off" maxlength="253"></label><label>Optional DuckDNS token<input name="duckDnsToken" type="password" autocomplete="new-password" maxlength="200"></label><p class="help">${s.tokenConfigured?'A token is stored encrypted. Leave blank to keep it.':'Leave blank if your router or another device updates DNS.'} Tokens go only to DuckDNS for this hostname.</p>${check('clearToken','Remove the saved DuckDNS token')}${check('enabled','Enable managed public HTTPS',s.enabled)}${check('allowDownload',`Download / repair verified Caddy ${esc(s.caddyVersion)} from its official GitHub release`)}<details><summary>What gets installed or shared?</summary><p>Caddy runs privately inside Carribrary’s environment; no separate system package is required. Windows needs administrator approval in its hosting assistant. Its release checksum is verified before installation. Certificate authorities receive your hostname, which appears in public certificate records. Router and firewall changes remain under your control. The optional DuckDNS updater runs while this enabled server/proxy is running; its token stays encrypted.</p></details><button type="submit">Save setup and continue</button></section>
  <section data-connection-panel="2" hidden><h3>Finish setup and restart</h3>${s.windows?'<p>Save changes first. Open <a href="carribrary-network://setup">Windows hosting assistant</a> (also in Start → Carribrary → Libraries and network). Review and apply its certificate, firewall and proxy changes. It restarts Carribrary. Existing Carribrary public-proxy tasks are kept for Undo.</p>':''}<p>Linux:</p><ol><li>Flatpak: application menu → Carribrary → <strong>Stop library server</strong>.</li><li>Launch Carribrary again, sign in, and reopen Public sharing setup.</li><li>Choose <strong>3. Router</strong> to continue.</li></ol><p>On Raspberry Pi, restart home-library using your desktop service manager. Existing books, accounts and paired phones are preserved.</p><p>Disabling managed HTTPS stops this proxy immediately. It does not revoke sharing links or stop an external proxy. Re-enabling needs a restart.</p><button type="button" data-connection-step="3" class="secondary">Router instructions →</button></section>
  <section data-connection-panel="3" hidden><h3>Forward two router ports</h3><p>Reserve this server’s local address in the router: <strong>${addresses}</strong>.</p><table><thead><tr><th>External TCP port</th><th>Server TCP port</th></tr></thead><tbody><tr><td>80</td><td>${s.httpPort}</td></tr><tr><td>443</td><td>${s.httpsPort}</td></tr></tbody></table><p>Both rules point to this server. Your public URL stays ordinary HTTPS, without :8443. Router settings remain under your control.</p><p>If a Linux firewall is active, allow incoming TCP ${s.httpPort}/${s.httpsPort} using its GUI. Do not forward administrator4173, internal catalog4174 or discovery7445.</p><details><summary>Android and advanced networks</summary><p>Native Android remote pairing still uses its separate phone port, normally7443. For a second server and its exact port table, use Libraries &amp; network. Do not point the same outside phone port at two computers. If your ISP uses carrier-grade NAT or blocks incoming ports, contact the ISP.</p></details><button type="button" data-connection-step="4" class="secondary">Check connection →</button></section>
  <section data-connection-panel="4" hidden><h3>Check, then share</h3><p id="connection-status" style="white-space:pre-line">Proxy: ${esc(s.proxyStatus)}\nDNS: ${esc(s.dnsStatus)}\nHTTPS: ${esc(s.httpsStatus)}</p><button type="button" class="secondary" id="check-public-https">Check saved HTTPS address</button><p>With phone <strong>Wi-Fi OFF</strong>, open your HTTPS hostname. A local check cannot prove outside access.</p><p>Once it opens, enable Library sharing to create the private catalog link/QR. Keep the server powered on and awake. Flatpak must remain running after desktop login; Pi normally starts at boot.</p><p>Return to Hostname to change settings or repair a missing proxy download. Restore/recovery review pauses public access; review locally before resuming.</p></section></form>`,async data=>{
   const form=document.querySelector('#public-connection-form'),button=form.querySelector('[type=submit]'),enabled=form.elements.enabled.checked;button.disabled=true;document.querySelector('#connection-result').textContent='Saving setup; a verified proxy download can take a few minutes…';
   try{await request('/api/public-connection',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({hostname:data.hostname||'',duckDnsToken:data.duckDnsToken||'',enabled,clearToken:form.elements.clearToken.checked,allowDownload:form.elements.allowDownload.checked})});form.elements.duckDnsToken.value='';document.querySelector('#connection-result').textContent=enabled?'Setup saved. Restart Carribrary to apply it.':'Managed HTTPS stopped. Router rules and external proxies were not changed.';step(enabled?2:1);toast('Public sharing setup saved');}catch(error){document.querySelector('#connection-result').textContent='Setup was not saved. Review the message above; previous saved settings remain.';throw error;}finally{button.disabled=false;}
  });
  const form=document.querySelector('#public-connection-form');
  form.querySelector('#shared-https-setup')?.addEventListener('click',()=>sharedHttpsSetup({request,openForm,toast}));
  function step(number){form.querySelectorAll('[data-connection-panel]').forEach(p=>p.hidden=p.dataset.connectionPanel!==String(number));form.querySelectorAll('nav [data-connection-step]').forEach(b=>{if(b.dataset.connectionStep===String(number))b.setAttribute('aria-current','step');else b.removeAttribute('aria-current');});}
  form.querySelectorAll('[data-connection-step]').forEach(b=>b.onclick=()=>step(b.dataset.connectionStep));step(s.pendingRestart?2:1);
  document.querySelector('#check-public-https').onclick=async()=>{const b=document.querySelector('#check-public-https');b.disabled=true;try{const v=await request('/api/public-connection/check',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'});document.querySelector('#connection-status').textContent=`Proxy: ${v.proxyStatus}\nDNS: ${v.dnsStatus}\nHTTPS: ${v.httpsStatus}`;}catch(e){document.querySelector('#connection-status').textContent=e.message;}finally{b.disabled=false;}};
 }catch(error){toast(error.message.includes('404')?'Open Public sharing setup on the server computer after local administrator sign-in.':error.message);}
}

async function connectFamily({request,openForm,toast}) {
 try {
  const saved=await request('/api/shared-https');
  const detection=saved.supported?await request('/api/shared-https/detect'):{hostname:''};
  openForm('Shared HTTPS',`<form id="shared-https-form">
   <p>Use Carribrary’s existing HTTPS connection for Family Command Center on this Windows PC. Your library keeps its own address.</p>
   ${!saved.supported?'<p>Open this on the main Carribrary Windows server.</p>':`
   <ol>
    <li><a href="http://localhost:8123/settings/network#shared-https" target="_blank" rel="noopener noreferrer">Open Family Command Center setup</a>. Choose <strong>Join an existing HTTPS host</strong> and save your separate DuckDNS hostname.</li>
    <li>Return here and choose <strong>Find Family Command Center</strong>. Review the address, then select <strong>Connect</strong>.</li>
   </ol>
   ${saved.available?'':'<p>Start Carribrary’s managed public HTTPS before connecting another app.</p>'}
   <p role="status" id="shared-https-status"></p><p class="form-error" role="alert"></p>
   <button type="button" class="secondary" id="detect-family-https">Find Family Command Center</button>
   <label>Family Command Center DuckDNS hostname<input name="hostname" required maxlength="253" placeholder="family.duckdns.org" value="${esc(detection.hostname||saved.hostname)}" autocomplete="off"></label>
   ${saved.canUseDuckDns?`<label><input type="checkbox" name="useDuckDns" ${saved.useDuckDns?'checked':''}> Update this hostname using Carribrary’s saved DuckDNS token (same DuckDNS account)</label>`:'<p>Set the FCC hostname to your home’s public IP at DuckDNS, or use your existing DNS updater.</p>'}
   <button type="submit" ${saved.available?'':'disabled'}>Connect</button>
   <button type="button" class="secondary" id="disconnect-family-https" ${saved.hostname?'':'hidden'}>Disconnect Family Command Center</button>
   <button type="button" class="secondary" id="check-family-https">Refresh status</button>
   <a id="open-family-https" target="_blank" rel="noopener noreferrer" hidden>Open public Family Command Center</a>
   `}</form>`,async data=>{
    await apply({hostname:data.hostname.trim(),useDuckDns:form.elements.useDuckDns?.checked??false});
   });
  if(!saved.supported)return;
  const form=document.querySelector('#shared-https-form');
  const status=form.querySelector('#shared-https-status'),open=form.querySelector('#open-family-https');
  const remove=form.querySelector('#disconnect-family-https'),submit=form.querySelector('[type=submit]');
  function show(current) {
   status.textContent=current.liveHostname&&current.liveHostname===current.hostname
    ?`Routing to ${current.liveHostname}. ${current.dnsStatus}. Verify HTTPS from cellular data.`
    :current.hostname?`Saved ${current.hostname}. ${current.proxyStatus}`
    :current.liveHostname?'Disconnect saved. Refresh in a moment.':'No Family Command Center address connected.';
   open.hidden=!current.liveHostname||current.liveHostname!==current.hostname;
   if(!open.hidden)open.href='https://'+current.liveHostname;
   remove.hidden=!current.hostname;
   submit.disabled=!current.available;
  }
  async function apply(input) {
   submit.disabled=true;
   remove.disabled=true;
   try {
    let current=await request('/api/shared-https',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)});
    show(current);
    submit.disabled=true;
    remove.disabled=true;
    for(let attempt=0;attempt<8&&current.liveHostname!==current.hostname&&form.isConnected;attempt++){
     await new Promise(resolve=>setTimeout(resolve,1000));
     try{current=await request('/api/shared-https');}catch{break;}
    }
    show(current);
   } finally { if(!input.hostname)submit.disabled=!saved.available;remove.disabled=false; }
  }
  form.querySelector('#detect-family-https').onclick=async()=>{
   try {
    const detected=await request('/api/shared-https/detect');
    if(detected.hostname)form.elements.hostname.value=detected.hostname;
    status.textContent=detected.hostname?'Family Command Center found. Review its address and connect.':'No prepared Family Command Center found. Save Join an existing HTTPS host in FCC first.';
    submit.disabled=!(await request('/api/shared-https')).available;
   }catch(error){status.textContent=error.message;}
  };
  form.querySelector('#check-family-https').onclick=async()=>{try{show(await request('/api/shared-https'));}catch(error){status.textContent=error.message;}};
  remove.onclick=async()=>{if(!confirm('Disconnect the public Family Command Center address?'))return;try{await apply({hostname:''});}catch(error){status.textContent=error.message;}};
  show(saved);
 }catch(error){toast(error.message);}
}

export async function sharedHttpsSetup(services) {
 const {request,openForm,toast}=services;
 const send=(path,body)=>request('/api/shared-https'+path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
 try{
  const state=await request('/api/shared-https');
  if(!state.supported){
   openForm('Shared HTTPS','<form><p>Open Shared HTTPS on the main Windows server.</p></form>',async()=>{});
   return;
  }
  if(state.available&&new URLSearchParams(location.search).get('shared-https')==='1'&&new URLSearchParams(location.search).get('join')!=='1'){
   history.replaceState(null,'',location.pathname);
   return connectFamily(services);
  }
  const routes=state.sites.map(site=>`<li><a href="https://${esc(site.hostname)}" target="_blank" rel="noopener noreferrer">${esc(site.name)}</a>
    · ${esc(site.hostname)} · ${state.liveSites.includes(site.hostname)?'Routing':'Saved'} <button class="secondary" type="button" data-remove-site="${esc(site.hostname)}">Disconnect</button></li>`).join('');
  openForm('Shared HTTPS',`<form id="shared-https-choice">
   <p>Run two or more apps on this PC with one HTTPS host. Give each app its own public hostname and keep the router rules for the app that already serves HTTPS.</p>
   <p id="shared-choice-status" role="status"></p><p class="form-error" role="alert"></p>
   ${state.canJoin?`<section><h3>Join an existing HTTPS host</h3>
    <p>Use this if another app already serves HTTPS. Carribrary will use the public address below and keep its local dashboard private.</p>
    <label>Carribrary public hostname<input name="joined" value="${esc(state.joinedHostname)}" placeholder="mybooks.duckdns.org" maxlength="253" autocomplete="off"></label>
    <label>DuckDNS token (optional)<input type="password" name="token" autocomplete="new-password" maxlength="200"></label>
    <p>${state.joinedTokenConfigured?'A token is saved. Leave this blank to keep it.':'Leave blank if a router or another updater handles this name.'}</p>
    <button type="button" data-join>Save public address</button>
    ${state.joinedHostname?'<button class="secondary" type="button" data-leave>Leave shared HTTPS</button>':''}
    ${state.joinPending?'<p>Restart Carribrary to use the saved address. In Windows, open Services, select Carribrary, and click Restart.</p>':''}
    <p>In the app that runs Caddy, add Carribrary on public web port <strong>${Number(state.publicPort)}</strong>.</p>
    <a href="http://localhost:8123/settings/network#shared-https" target="_blank" rel="noopener noreferrer">Open Family Command Center setup</a>
    ${state.activeJoinedHostname?'<button class="secondary" type="button" data-check-joined>Check my public address</button>':''}
   </section>`:`<section><h3>Host another app</h3>
    ${state.available?'': '<p>Start Carribrary’s managed public HTTPS first. If you just enabled it, restart Carribrary.</p>'}
    ${routes?`<ul>${routes}</ul>`:'<p>No general app connections saved.</p>'}
    <label>App name<input name="name" maxlength="80" autocomplete="off"></label>
    <label>Public hostname<input name="hostname" maxlength="253" placeholder="another-app.duckdns.org" autocomplete="off"></label>
    <label>Local web port<input name="port" type="number" min="1024" max="65535"></label>
    <label><input name="confirmPublic" type="checkbox"> I want this app’s web interface available over the internet.</label>
    <p>Choose the app’s intended public web port and set up its sign-in before connecting. Point the new hostname to the same public IP.</p>
    <button type="submit" ${state.available?'':'disabled'}>Add app</button>
    <details ${state.hostname?'open':''}><summary>Family Command Center setup shortcut</summary>
    ${state.hostname?`<p>Saved address: <strong>${esc(state.hostname)}</strong></p>`:''}
    <button class="secondary" type="button" data-family>Open Family Command Center connection</button></details>
   </section>`}
  </form>`,async values=>{
   if(state.canJoin)return false;
   await send('/sites',{site:{name:values.name,hostname:values.hostname,port:Number(values.port)},confirmPublic:!!values.confirmPublic});
   toast('App saved. Caddy will apply the updated routes.');
   await sharedHttpsSetup(services);
  });
  const form=document.querySelector('#shared-https-choice');
  const note=form.querySelector('#shared-choice-status');
  const act=(selector,work)=>form.querySelector(selector)?.addEventListener('click',async event=>{
   const button=event.currentTarget;button.disabled=true;
   try{await work(button);}catch(error){note.textContent=error.message;}finally{button.disabled=false;}
  });
  act('[data-family]',()=>connectFamily(services));
  act('[data-join]',async()=>{
   if(!form.elements.joined.value.trim())throw Error('Enter Carribrary’s public hostname.');
   await send('/join',{hostname:form.elements.joined.value.trim(),duckDnsToken:form.elements.token.value});
   toast('Address saved. Restart Carribrary, then connect it in the host app.');
   await sharedHttpsSetup(services);
  });
  act('[data-leave]',async()=>{
   if(!confirm('Disconnect Carribrary from shared HTTPS after removing its route at the host?'))return;
   await send('/join',{hostname:''});await sharedHttpsSetup(services);
  });
  act('[data-check-joined]',async()=>{
   const result=await request('/api/public-connection/check',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'});
   note.textContent=result.httpsStatus;
  });
  form.querySelectorAll('[data-remove-site]').forEach(button=>button.onclick=async()=>{
   if(!confirm('Disconnect this public app address?'))return;
   try{await send('/sites',{remove:button.dataset.removeSite});await sharedHttpsSetup(services);}
   catch(error){note.textContent=error.message;}
  });
 }catch(error){toast(error.message);}
}
