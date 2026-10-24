import assert from 'node:assert/strict';
import fs from 'node:fs';
import https from 'node:https';
import crypto from 'node:crypto';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const local=process.env.LIBRARY_TEST_URL,origin=process.env.PUBLIC_TEST_URL,phone=process.env.PHONE_TEST_URL;
assert(local&&origin&&phone&&process.env.HOME_LIBRARY_DATA?.startsWith('/tmp/hl-local-auth-'),'Disposable auth fixture required');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:412,height:915}}),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
const password='Local test administrator password 33';let recovery;
async function post(path,body){return page.evaluate(async({path,body})=>{const r=await fetch(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});return{status:r.status,data:await r.json()};},{path,body});}
function phoneRequest(agent,path,body){return new Promise((resolve,reject)=>{const request=https.request(new URL(path,phone),{agent,method:body?'POST':'GET',headers:body?{'Content-Type':'application/json'}:{}},r=>{let text='';r.on('data',x=>text+=x);r.on('end',()=>resolve({status:r.statusCode,data:text?JSON.parse(text):{},reusedSocket:request.reusedSocket}));});request.on('error',reject);request.end(body?JSON.stringify(body):undefined);});}
try{
 await page.goto(local);await page.waitForURL('**/login');await page.waitForFunction(()=>document.querySelector('#heading').textContent.includes('Set up'));
 assert.equal((await context.request.get(local+'/api/state')).status(),401);
 assert.equal((await context.request.post(local+'/api/devices/pair',{headers:{Origin:local},data:{serverUrl:phone}})).status(),401);
 assert.equal((await context.request.get(local+'/api/setup')).status(),401);
 const health=await(await context.request.get(local+'/local-auth/health')).json();assert.deepEqual(Object.keys(health).sort(),['ready','version']);assert.equal(health.ready,true);
 assert.equal((await context.request.get(local+'/local-auth/status')).status(),200);
 assert.equal((await context.request.post(local+'/local-auth/enroll',{headers:{Origin:'https://evil.invalid'},data:{}})).status(),403);
 assert.equal((await post('/local-auth/enroll',{username:'Admin',password,code:'x'.repeat(43)})).status,401);
 const code=fs.readFileSync(process.env.HOME_LIBRARY_DATA+'/local-admin-setup.txt','utf8').trim();
 await page.locator('[name=username]').fill('Admin');await page.locator('[name=password]').fill(password);await page.locator('[name=confirmPassword]').fill(password);await page.locator('[name=code]').fill(code);await page.locator('#submit').click();await page.locator('#saved-code').waitFor({state:'visible'});recovery=await page.locator('#recovery-code').inputValue();assert.equal(recovery.length,43);await page.locator('#saved').check();await page.locator('#continue').click();await page.waitForSelector('#local-admin-signout');await page.waitForFunction(()=>document.querySelector('#save-status').textContent==='Shared library on your home server');
 const cookie=(await context.cookies(local)).find(c=>c.name==='carribrary-local-admin');assert(cookie.httpOnly&&cookie.sameSite==='Strict');
 let state=(await context.request.get(local+'/api/state'));assert.equal(state.status(),200);state=await state.json();
 assert.equal((await post('/api/action',{action:'addBook',revision:state.revision,input:{title:'Authentication fixture book',author:'Test Author',isbn:'9780140328721'}})).status,200);
 const paired=await post('/api/browser-admins/pair',{});assert.equal(paired.status,200);
 const iphoneContext=await browser.newContext({ignoreHTTPSErrors:true}),iphone=await iphoneContext.newPage();iphone.on('pageerror',e=>errors.push(e.message));await iphone.goto(origin+'/companion/');await iphone.locator('[name=code]').fill(paired.data.code);await iphone.locator('[name=name]').fill('Test iPhone');await iphone.locator('#pair [type=submit]').click();await iphone.waitForSelector('#content .book');
 assert.equal(await iphone.locator('#local-admin-signout').count(),0,'Paired companion gets no added local login');
 const oldBrowserCookie=(await iphoneContext.cookies(origin+'/companion/')).find(c=>c.name==='__Secure-hl-admin');assert(oldBrowserCookie);
 assert.equal((await iphoneContext.request.get(origin+'/companion/api/state')).status(),200,'Proxy validates existing credential on inner request');
 assert.equal((await iphoneContext.request.post(origin+'/companion/enroll',{headers:{Origin:origin},data:{code:paired.data.code,name:'Replay'}})).status(),401);
 const grant=(await post('/api/devices/pair',{serverUrl:phone})).data;
 const keys=crypto.generateKeyPairSync('rsa',{modulusLength:2048});const spki=keys.publicKey.export({type:'spki',format:'der'}).toString('base64');const signature=crypto.sign('sha256',Buffer.from('home-library-pair:v1:'+grant.code+':'+spki),keys.privateKey).toString('base64');
 const anonAgent=new https.Agent({rejectUnauthorized:false});const enrolled=await phoneRequest(anonAgent,'/mobile/pair',{code:grant.code,name:'Test Android',publicKey:spki,signature});assert.equal(enrolled.status,200);
 assert.equal((await phoneRequest(anonAgent,'/mobile/pair',{code:grant.code,name:'Replay',publicKey:spki,signature})).status,401);
 const pem='-----BEGIN CERTIFICATE-----\n'+enrolled.data.certificate.match(/.{1,64}/g).join('\n')+'\n-----END CERTIFICATE-----\n';
 const androidAgent=new https.Agent({keepAlive:true,rejectUnauthorized:false,cert:pem,key:keys.privateKey.export({type:'pkcs8',format:'pem'})});
 assert.equal((await phoneRequest(androidAgent,'/mobile/state')).status,200);
 await page.locator('#local-admin-signout').click();await page.waitForURL('**/login');await page.screenshot({path:process.env.AUTH_LOGIN_SCREENSHOT||'/tmp/carribrary-local-admin-login.png',fullPage:true});
 assert.equal((await context.request.get(local+'/api/state')).status(),401);
 assert.equal((await iphoneContext.request.get(origin+'/companion/api/state')).status(),200,'Local sign-out does not unpair browser');
 assert.equal((await phoneRequest(androidAgent,'/mobile/state')).status,200,'Local sign-out does not unpair Android');
 const privateContext=await browser.newContext();const privatePage=await privateContext.newPage();await privatePage.goto(local);await privatePage.waitForURL('**/login');assert.equal((await privateContext.request.get(local+'/api/state')).status(),401,'Fresh InPrivate equivalent cannot use OS login');await privateContext.close();
 assert.equal((await post('/local-auth/login',{username:'Admin',password:'Incorrect password'})).status,401);
 await page.locator('[name=username]').fill('Admin');await page.locator('[name=password]').fill(password);await page.locator('#submit').click();await page.waitForSelector('#local-admin-signout');
 const devices=(await context.request.get(local+'/api/browser-admins'));const device=(await devices.json()).devices.find(x=>x.name==='Test iPhone');assert(device);
 assert.equal((await post('/api/browser-admins/revoke',{deviceId:device.id})).status,200);
 assert.equal((await iphoneContext.request.get(origin+'/companion/api/state')).status(),401);
 assert.equal((await context.request.get(local+'/api/state',{headers:{'X-Carribrary-Companion':oldBrowserCookie.value,Cookie:''}})).status(),401,'Revoked token also fails inner API authorization');
 assert.equal((await post('/api/devices/revoke',{deviceId:enrolled.data.deviceId})).status,200);
 const revokedAndroid=await phoneRequest(androidAgent,'/mobile/state');assert.equal(revokedAndroid.status,401,'Revoked Android certificate fails on keep-alive connection');assert.equal(revokedAndroid.reusedSocket,true,'Revocation was checked on the original TLS connection');
 assert.equal((await phoneRequest(androidAgent,'/mobile/action',{action:'addBorrower',revision:0,input:{name:'Unauthorized'}})).status,401);
 const newAgent=new https.Agent({rejectUnauthorized:false,cert:pem,key:keys.privateKey.export({type:'pkcs8',format:'pem'})});try{const response=await phoneRequest(newAgent,'/mobile/state');assert.equal(response.status,401);}catch(e){assert.match(e.message,/socket hang up|alert|ECONNRESET|certificate/i);}
 androidAgent.destroy();newAgent.destroy();anonAgent.destroy();
 // Expire the local session through password recovery, preserving catalog and companions.
 const oldLocal=(await context.cookies(local)).find(c=>c.name==='carribrary-local-admin').value;
 await page.goto(local+'/login');await page.waitForSelector('#local-admin-signout'); // already signed in returns to dashboard
 const recovered=await post('/local-auth/recover',{username:'Admin',password:password+' new',code:recovery});assert.equal(recovered.status,200);assert.notEqual(recovered.data.recoveryCode,recovery);
 assert.equal((await context.request.get(local+'/api/state',{headers:{Cookie:'carribrary-local-admin='+oldLocal}})).status(),401);
 assert.equal((await post('/local-auth/recover',{username:'Admin',password,code:recovery})).status,401);
 assert.equal((await context.request.get(local+'/api/state')).status(),200);
 assert.equal((await context.request.get(origin+'/')).status(),404);
 assert.equal((await context.request.get(origin+'/local-auth/status')).status(),404);
 assert.equal((await context.request.post(local+'/api/action',{headers:{Origin:'https://evil.invalid'},data:{}})).status(),403);
 assert.equal((await context.request.get(local+'/api/state',{headers:{'Host':'evil.invalid'}})).status(),403);
 await page.goto(local);await page.waitForSelector('#local-admin-signout');await page.screenshot({path:process.env.AUTH_SCREENSHOT||'/tmp/carribrary-local-admin-dashboard.png',fullPage:true});
 assert.deepEqual(errors,[]);console.log('PASS local enrollment, InPrivate/API denial, CSRF/Host guards, local sign-out/recovery, unchanged paired companions, single-use grants, immediate browser and Android revocation on reused/new connections.');
 const retained=(await post('/api/browser-admins/pair',{})).data;const enrolledBrowser=await iphoneContext.request.post(origin+'/companion/enroll',{headers:{Origin:origin},data:{code:retained.code,name:'Migration survivor'}});assert.equal(enrolledBrowser.status(),200);
 const survivor=(await iphoneContext.cookies(origin+'/companion/')).find(c=>c.name==='__Secure-hl-admin').value;fs.writeFileSync(process.env.HOME_LIBRARY_DATA+'/test-survivor.txt',survivor,{mode:0o600});
 await iphoneContext.close();
}finally{await browser.close();}
