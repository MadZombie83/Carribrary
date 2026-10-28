import assert from 'node:assert/strict';
import {mkdir} from 'node:fs/promises';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
// Persistent disposable profile: an incognito context reports only 'in-incognito', masking installability errors.
const context=await chromium.launchPersistentContext('',{executablePath:'/usr/bin/google-chrome',headless:true,ignoreHTTPSErrors:true,args:['--no-sandbox','--ignore-certificate-errors']});const output='output/notifications-install-2.0.49';await mkdir(output,{recursive:true});
try{
 const p=await context.newPage(),errors=[];p.on('pageerror',e=>errors.push(e.message));await authenticateLocalAdmin(p);const admin=process.env.LIBRARY_TEST_URL,origin=process.env.PUBLIC_TEST_URL;
 const pair=await p.request.post(admin+'/api/browser-admins/pair',{headers:{Origin:admin},data:{}}),code=(await pair.json()).code;
 const share=await p.request.post(admin+'/api/sharing',{headers:{Origin:admin},data:{action:'enable'}});const path=new URL((await share.json()).url).pathname;
 const c=await context.newCDPSession(p);
 async function installable(path,id){await p.goto(origin+path);await p.waitForFunction(async()=>!!(await navigator.serviceWorker.getRegistration(location.pathname.startsWith('/companion/')?'/companion/':'/'))?.active);const manifest=await c.send('Page.getAppManifest');assert.deepEqual(manifest.errors,[]);assert.equal(JSON.parse(manifest.data).id,id);const errors=await c.send('Page.getInstallabilityErrors');assert.deepEqual(errors.installabilityErrors,[],JSON.stringify(errors));for(const icon of JSON.parse(manifest.data).icons){const image=await p.evaluate(async url=>{const i=new Image();i.src=url;await i.decode();return [i.naturalWidth,i.naturalHeight];},origin+icon.src);assert.deepEqual(image,icon.sizes.split('x').map(Number));}}
 await installable('/','/account/borrower-app');assert.equal(await p.locator('#install-borrower').count(),1);await p.screenshot({path:output+'/welcome-install.png',fullPage:true});
 for(const entry of ['/account','/companion',path.slice(0,-1)]){await p.goto(origin+entry);assert.equal(new URL(p.url()).pathname,entry+'/');assert.equal((await c.send('Page.getAppManifest')).errors.length,0);}
 await installable('/companion/','/companion/app');
 await p.evaluate(()=>{const e=new Event('beforeinstallprompt',{cancelable:true});e.prompt=async()=>{window.__prompted=true;};e.userChoice=Promise.resolve({outcome:'accepted'});window.dispatchEvent(e);});await p.locator('#install-companion').click();assert(await p.evaluate(()=>window.__prompted));await p.locator('#install-companion').click();await p.waitForFunction(()=>!document.querySelector('#install-status').hidden);assert.match(await p.locator('#install-status').innerText(),/Chrome|Installation/i);await p.screenshot({path:output+'/companion-install.png',fullPage:true});
 await p.locator('[name=code]').fill(code);await p.locator('#pair [type=submit]').click();await p.waitForSelector('#request-bell');assert.deepEqual((await c.send('Page.getInstallabilityErrors')).installabilityErrors,[]);assert.equal(JSON.parse((await c.send('Page.getAppManifest')).data).id,'/companion/app');
 await installable('/account/','/account/borrower-app');await installable(path,'/account/borrower-app');assert(!JSON.stringify(await c.send('Page.getAppManifest')).includes(path.split('/')[2]));assert.deepEqual(await p.evaluate(()=>caches.keys()),[]);assert.deepEqual(errors,[]);
 console.log('PASS real Chrome non-incognito installability on welcome, borrower, shared catalog, unpaired and paired companion; missing-slash canonical redirects; decoded 192/512 icons; install control; stable distinct identities; no token or private cache');
}finally{await context.close();}
