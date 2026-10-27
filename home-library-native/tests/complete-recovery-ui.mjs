import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {mkdir,writeFile,readFile,readdir,rm} from 'node:fs/promises';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});const admin=process.env.LIBRARY_TEST_URL,fixture=process.env.HOME_LIBRARY_DATA,password='Complete recovery private fixture password';
const out='output/complete-recovery-2.0.45';await mkdir(out,{recursive:true});let child,page;
try{
 page=await browser.newPage();await authenticateLocalAdmin(page);
 const current=await(await page.request.get(admin+'/api/state')).json();const added=await page.request.post(admin+'/api/action',{headers:{Origin:admin},data:{action:'addBook',input:{isbn:'9780140328721',title:'Recovered household book',author:'Fixture Author'},revision:current.revision}});assert.equal(added.status(),200);
 const themeState=await(await page.request.get(admin+'/api/state')).json();assert.equal((await page.request.post(admin+'/api/action',{headers:{Origin:admin},data:{action:'setLibraryTheme',input:{libraryTheme:'forest'},revision:themeState.revision}})).status(),200);
 const before=await(await page.request.get(admin+'/api/state')).json();const backup=await page.request.post(admin+'/api/backup/complete',{headers:{Origin:admin},data:{password}});assert.equal(backup.status(),200);const archive=out+'/recovery-fixture.carribrary-backup';await writeFile(archive,await backup.body());
 // Fake only the Flatpak environment/launcher context; real standalone wizard,
 // recovered child server, data protection, authentication and sockets are used.
 child=spawn(process.env.DOTNET_COMMAND,['home-library-native/server/bin/Release/net10.0/HomeLibrary.Server.dll','--complete-recovery','flatpak','--no-browser'],{env:{...process.env,FLATPAK_ID:'com.household.HomeLibrary',HOME_LIBRARY_DATA:fixture},stdio:['ignore','pipe','pipe']});let stderr='';child.stderr.on('data',d=>stderr+=d);
 const url=await new Promise((resolve,reject)=>{let data='';const timer=setTimeout(()=>reject(Error('Wizard did not open: '+stderr)),20000);child.stdout.on('data',chunk=>{data+=chunk;const match=data.match(/RECOVERY-OPEN (http:\/\/localhost:\d+\/#[A-F0-9]+)/);if(match){clearTimeout(timer);resolve(match[1]);}});child.on('exit',()=>{clearTimeout(timer);reject(Error('Wizard exited: '+stderr));});});
 const origin=new URL(url).origin;const outsider=await browser.newContext();assert.equal((await outsider.request.get(origin+'/api/status')).status(),401);
 assert.equal((await outsider.request.post(origin+'/unlock',{headers:{Origin:'https://untrusted.example'},data:{token:new URL(url).hash.slice(1)}})).status(),403);
 assert.equal((await outsider.request.post(origin+'/unlock',{headers:{Origin:origin},data:{token:'incorrect'}})).status(),401);
 const wizard=await browser.newPage({viewport:{width:1440,height:1000}});const errors=[];wizard.on('pageerror',e=>errors.push(e.message));await wizard.goto(url);await wizard.locator('#workspace').waitFor({state:'visible'});assert.equal(new URL(wizard.url()).hash,'');assert.equal((await outsider.request.post(origin+'/unlock',{headers:{Origin:origin},data:{token:new URL(url).hash.slice(1)}})).status(),401);await wizard.reload();await wizard.locator('#workspace').waitFor({state:'visible'});
 assert.equal((await wizard.request.post(origin+'/api/restore',{headers:{Origin:origin},data:{confirmation:'RESTORE LIBRARY',checked:true,warnings:true}})).status(),400);
 await wizard.locator('#archive').setInputFiles(archive);await wizard.locator('#password').fill('Wrong archive password value');await wizard.locator('#upload button').click();await wizard.waitForFunction(()=>document.querySelector('#status').textContent.includes('password'));
 await wizard.locator('#password').fill(password);await wizard.locator('#upload button').click();try{await wizard.waitForFunction(()=>!document.querySelector('#report').hidden);}catch(e){throw Error('Verification report missing: '+await wizard.locator('#status').textContent());}assert.equal(await wizard.locator('#password').inputValue(),'');await wizard.locator('#test').click();await wizard.locator('#test-link a').waitFor();const testUrl=await wizard.locator('#test-link a').getAttribute('href');assert.ok(testUrl.startsWith('http://localhost:'));
 const testContext=await browser.newContext();const restored=await testContext.newPage();await restored.goto(testUrl);await restored.locator('.recovery-test-banner').waitFor();
 const login=await testContext.request.post(testUrl+'/local-auth/login',{headers:{Origin:testUrl},data:{username:'Fixture Administrator',password:'Disposable administrator fixture password 233'}});assert.equal(login.status(),200);await restored.goto(testUrl);await restored.locator('.recovery-test-banner').waitFor();const testState=await(await testContext.request.get(testUrl+'/api/state')).json();assert.deepEqual(testState.state,before.state);
 const cookies=await testContext.cookies();assert.ok(cookies.some(c=>c.name==='carribrary-local-admin-rehearsal'));assert.ok(!cookies.some(c=>c.name==='carribrary-local-admin'));
 assert.equal((await page.request.get(admin+'/api/state')).status(),200,'test sign-in must not invalidate live sign-in');
 assert.deepEqual((await(await page.request.get(admin+'/api/state')).json()),before);await wizard.evaluate(()=>scrollTo(0,0));await wizard.screenshot({path:out+'/wizard-desktop.png',fullPage:true});
 await wizard.setViewportSize({width:390,height:844});assert.ok(await wizard.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await wizard.screenshot({path:out+'/wizard-mobile.png',fullPage:true});
 await wizard.locator('#stop-test').click();await wizard.waitForFunction(()=>document.querySelector('#status').textContent.includes('disposable test is stopped'));
 // A full replacement is rehearsed only on this disposable fixture.
 const stopLive=await page.request.post(admin+'/api/desktop/stop',{headers:{Origin:admin},data:{}});assert.equal(stopLive.status(),200);await page.waitForTimeout(700);
 const restore=await wizard.request.post(origin+'/api/restore',{headers:{Origin:origin},data:{confirmation:'RESTORE LIBRARY',checked:true,warnings:true}});assert.equal(restore.status(),200,await restore.text());const restoredResult=await restore.json();assert.ok(restoredResult.retained.startsWith('/tmp/'));
 await authenticateLocalAdmin(page);const liveAfter=await(await page.request.get(admin+'/api/state')).json();assert.deepEqual(liveAfter.state,before.state);
 const setup=await(await page.request.get(admin+'/api/setup')).json();assert.equal(setup.recoveryReviewRequired,true);assert.equal(setup.publicOrigin,'');
 await page.goto(admin);await page.locator('#complete-recovery-review').waitFor();await page.screenshot({path:out+'/restored-local-review.png',fullPage:true});
 assert.equal((await wizard.request.post(origin+'/api/resume',{headers:{Origin:origin},data:{}})).status(),400);
 assert.equal((await page.request.post(admin+'/api/recovery-review/complete',{headers:{Origin:admin},data:{confirmation:'wrong',checked:true}})).status(),400);
 assert.equal((await page.request.post(admin+'/api/recovery-review/complete',{headers:{Origin:admin},data:{confirmation:'RESUME REMOTE ACCESS',checked:true}})).status(),200);
 await page.request.post(admin+'/api/desktop/stop',{headers:{Origin:admin},data:{}});await page.waitForTimeout(700);
 const resume=await wizard.request.post(origin+'/api/resume',{headers:{Origin:origin},data:{}});assert.equal(resume.status(),200,await resume.text());await authenticateLocalAdmin(page);const resumed=await(await page.request.get(admin+'/api/setup')).json();assert.equal(resumed.recoveryReviewRequired,false);
 // Stop the spawned live fixture; the release runner owns only the original PID.
 await page.request.post(admin+'/api/desktop/stop',{headers:{Origin:admin},data:{}});await page.waitForTimeout(700);
 const status=await(await wizard.request.get(origin+'/api/status')).json();assert.equal(status.flatpak,true);assert.ok(status.workDirectory.startsWith('/tmp/'));
 assert.equal((await wizard.request.post(origin+'/api/restore',{headers:{Origin:origin},data:{confirmation:'wrong',checked:true,warnings:true}})).status(),400);
 await wizard.locator('#close').click();await wizard.waitForFunction(()=>document.querySelector('#status').textContent.includes('tool closed'));await new Promise(resolve=>child.once('exit',resolve));child=null;
 const sessions=(await readdir(status.workDirectory)).filter(n=>n.startsWith('session-'));assert.equal(sessions.length,0);assert.deepEqual(errors,[]);await rm(archive);
 console.log('PASS standalone recovery capability/origin/session/refresh checks, wrong password, bounded archive upload, real isolated child startup, recovered administrator login/catalog, separate cookies, banner, unchanged live state, confirmations, responsive UI, real isolated live replacement/retained data, remote-access quarantine, authenticated local review/resume and cleanup');
}finally{if(page){try{await authenticateLocalAdmin(page);await page.request.post(admin+'/api/desktop/stop',{headers:{Origin:admin},data:{}});}catch{}}if(child)child.kill('SIGTERM');await browser.close();}
