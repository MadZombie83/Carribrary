import {openCatalogIfAdmin} from './local-admin-fixture.mjs';
import {openAdminTool} from './admin-tool-navigation.mjs';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
import {readFile,mkdir,readdir} from 'node:fs/promises';
import {execFileSync} from 'node:child_process';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const admin=process.env.LIBRARY_TEST_URL,publicUrl=process.env.PUBLIC_TEST_URL;
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
const password='Complete backup test password 230';
try{
 const page=await browser.newPage(),errors=[];page.on('pageerror',error=>errors.push(error.message));await authenticateLocalAdmin(page);await page.goto(admin);await openCatalogIfAdmin(page);await page.waitForSelector('#content');
 const sharing=await page.evaluate(async()=>{for(const [action,input] of [['addBook',{isbn:'9780140328721',title:'Backup test book',author:'Author'}],['addBorrower',{name:'Backup test borrower',contact:'borrower@example.com'}]]){const s=await(await fetch('/api/state')).json();if(action==='addBook'&&s.state.books.length||action==='addBorrower'&&s.state.borrowers.length)continue;const response=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action,input,revision:s.revision})});if(!response.ok)throw Error(await response.text());}return(await(await fetch('/api/sharing',{method:'POST',headers:{'Content-Type':'application/json'},body:'{"action":"enable"}'})).json()).url;});
 const publicContext=await browser.newContext({ignoreHTTPSErrors:true});const token=new URL(sharing).pathname.split('/').filter(Boolean).at(-1);
 const registration=await publicContext.request.post(publicUrl+'/account/register',{headers:{Origin:publicUrl},data:{shareToken:token,username:'backupreader',password:'Borrower test password 230',name:'Backup Reader',contact:'reader@example.com'}});assert.equal(registration.status(),200);
 assert.equal((await publicContext.request.post(publicUrl+'/companion/api/backup/complete',{headers:{Origin:publicUrl},data:{password}})).status(),401);
 assert.equal((await publicContext.request.post(publicUrl+'/api/backup/complete',{headers:{Origin:publicUrl},data:{password}})).status(),405);
 assert.equal((await page.request.post(admin+'/api/backup/complete',{headers:{Origin:'https://untrusted.example'},data:{password}})).status(),403);
 assert.equal((await page.request.post(admin+'/api/backup/complete',{headers:{Origin:admin},data:{password:'short'}})).status(),400);
 const before=await page.evaluate(async()=> (await(await fetch('/api/state')).json()).state);
 await openAdminTool(page,'export');await page.locator('[name=password]').fill(password);await page.locator('[name=confirmation]').fill('different backup password');await page.locator('#create-complete-backup').click();await page.waitForFunction(()=>document.querySelector('#form-dialog .form-error').textContent.includes('do not match'));
 await page.locator('[name=confirmation]').fill(password);const [download]=await Promise.all([page.waitForEvent('download'),page.locator('#create-complete-backup').click()]);await mkdir('.build-tools/complete230-downloads',{recursive:true});const archive='.build-tools/complete230-downloads/server.carribrary-backup';await download.saveAs(archive);
 assert.equal((await readFile(archive)).subarray(0,8).toString(),'CARRBK01');
 const destination='.build-tools/complete230-downloads/recovered-'+Date.now();execFileSync(process.env.DOTNET_COMMAND,['home-library-native/server/bin/Release/net10.0/HomeLibrary.Server.dll','--unpack-complete-backup',archive,destination],{input:password+'\n',env:{...process.env,DOTNET_ROOT:process.env.DOTNET_ROOT}});
 const manifest=JSON.parse(await readFile(destination+'/manifest.json','utf8'));assert.ok(manifest.files.some(f=>f.path==='data/borrower-accounts.sqlite'));assert.ok(manifest.files.some(f=>f.path.startsWith('data/keyring/')));assert.ok(manifest.files.some(f=>f.path==='data/server.protected'));
 const counts=JSON.parse(execFileSync('python3',['-c',"import sqlite3,json,sys,pathlib;p=pathlib.Path(sys.argv[1]);db=sqlite3.connect(p/'library.sqlite');s=json.loads(db.execute('select data from library').fetchone()[0]);a=sqlite3.connect(p/'borrower-accounts.sqlite');print(json.dumps({'books':len(s['books']),'accounts':a.execute('select count(*) from accounts').fetchone()[0]}))",destination+'/data']).toString());assert.deepEqual(counts,{books:1,accounts:1});
 const after=await page.evaluate(async()=> (await(await fetch('/api/state')).json()).state);assert.deepEqual(after.books.map(b=>[b.id,b.copyUid]),before.books.map(b=>[b.id,b.copyUid]));
 await openAdminTool(page,'export');const [jsonDownload]=await Promise.all([page.waitForEvent('download'),page.locator('#export-library-json').click()]);const exported=JSON.parse(await readFile(await jsonDownload.path(),'utf8'));assert.equal(exported.version,7);assert.equal(exported.accounts,undefined);assert.equal(exported.books.length,1);await page.locator('#close-form').click();
 const pair=await page.evaluate(async()=> (await(await fetch('/api/browser-admins/pair',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'})).json()));
 const companionContext=await browser.newContext({ignoreHTTPSErrors:true});assert.equal((await companionContext.request.post(publicUrl+'/companion/enroll',{headers:{Origin:publicUrl},data:{code:pair.code,name:'Backup PWA test'}})).status(),200);
 const companion=await companionContext.newPage();companion.on('pageerror',e=>errors.push(e.message));await companion.goto(publicUrl+'/companion/');await companion.waitForSelector('#content');
 await openAdminTool(companion,'export');await companion.locator('[name=password]').fill(password);await companion.locator('[name=confirmation]').fill(password);const [pwaDownload]=await Promise.all([companion.waitForEvent('download'),companion.locator('#create-complete-backup').click()]);assert.equal((await readFile(await pwaDownload.path())).subarray(0,8).toString(),'CARRBK01');
 if(process.env.COMPLETE_BACKUP_TEST_DATA){await page.waitForTimeout(200);assert.equal((await readdir(process.env.COMPLETE_BACKUP_TEST_DATA)).filter(n=>n.startsWith('.complete-backup-')).length,0);}
 assert.deepEqual(errors,[]);console.log('PASS complete-backup server/PWA downloads, password checks, authorization, offline recovery, account/keyring integrity, JSON export and temporary-file cleanup');
}finally{await browser.close();}
