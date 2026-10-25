import {openAdminTool} from './admin-tool-navigation.mjs';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
import fs from 'node:fs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
const output=process.env.CARRIBRARY_UX_SCREENSHOTS||'/tmp/carribrary-ux';fs.mkdirSync(output,{recursive:true});
try{
 const context=await browser.newContext({ignoreHTTPSErrors:true}),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await authenticateLocalAdmin(page);await page.goto(process.env.LIBRARY_TEST_URL);await page.waitForFunction(()=>document.querySelector('#save-status').textContent==='Shared library on your home server');
 await page.evaluate(async()=>{const s=await(await fetch('/api/state')).json();const input={version:1,nextCopy:13,libraryName:'Meadow House Library',libraryTheme:'purple-blue',bannerSubtitle:'A shelf worth sharing',encouragingMessage:'A good story grows when it is shared.',books:Array.from({length:12},(_,i)=>({id:'BK-'+String(i+1).padStart(4,'0'),isbn:'9780140328721',title:['The Secret Garden','A Journey Home','Stories by the Sea','The Little Orchard'][i%4]+(i>3?' · Volume '+(i+1):''),author:['Frances Hodgson Burnett','Alex Reed','Morgan Vale','Robin Green'][i%4],publisher:'Fixture Press',coverUrl:''})),borrowers:[{id:'BR-0001',name:'Test Robin',contact:'robin@example.com'},{id:'BR-0002',name:'Test Alex',contact:'alex@example.com'}],loans:[]};const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action:'restore',revision:s.revision,input})});if(!r.ok)throw Error(await r.text());});
 await page.reload();await page.waitForFunction(()=>document.querySelector('#library-name').textContent==='Meadow House Library');
 async function action(action,input){const result=await page.evaluate(async({action,input})=>{const snap=await(await fetch('/api/state')).json();const r=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action,input,revision:snap.revision})});return {status:r.status,data:await r.json()};},{action,input});assert.equal(result.status,200,JSON.stringify(result.data));return result.data;}
 await action('checkout',{copyId:'BK-0001',borrowerId:'BR-0001',checkoutDate:'2020-01-01',dueDate:'2020-02-01'});
 await action('createHold',{copyId:'BK-0002',borrowerId:'BR-0002'});
 const sharing=await page.evaluate(async()=>(await(await fetch('/api/sharing',{method:'POST',headers:{'Content-Type':'application/json'},body:'{"action":"enable"}'})).json()));
 const guest=await context.newPage();await guest.route('https://library.test/**',async route=>{const u=new URL(route.request().url());await route.fulfill({response:await route.fetch({url:process.env.PUBLIC_TEST_URL+u.pathname+u.search,headers:{...route.request().headers(),origin:'https://library.test'}})});});
 await guest.goto(sharing.url);await guest.waitForSelector('[data-request=BK-0003]');
 for(const id of ['BK-0003','BK-0004']){await guest.locator(`[data-request=${id}]`).click();await guest.locator('[name=name]').fill('Test Reader');await guest.locator('[name=contact]').fill('reader@example.com');await guest.locator('#submit-request').click();await guest.waitForFunction(()=>document.querySelector('#request-status').textContent.includes('held'));await guest.locator('#close-request').click();}
 const requests=await page.evaluate(async()=>(await(await fetch('/api/state')).json()).state.requests);await action('declineRequest',{requestId:requests.find(r=>r.copyId==='BK-0004').id});
 await guest.close();await page.reload();await page.waitForSelector('#content .book');await page.locator('.sidebar-nav [data-view=dashboard]').click();
 assert.deepEqual(await page.locator('[data-kpi]').allTextContents(),['12','9','1','1']);
 assert.match(await page.locator('.recent-requests').innerText(),/Declined/);assert.match(await page.locator('.recent-requests').innerText(),/Awaiting approval/);
 assert.match(await page.locator('.dashboard-lower').innerText(),/1 book on loan/);
 assert.deepEqual(await page.locator('.quick-actions [data-dashboard-action]').allTextContents(),['Add Book','Create Hold','Batch Checkout','Backup Now']);
 const revisionBefore=await page.evaluate(async()=>(await(await fetch('/api/state')).json()).revision);
 for(const [key,title]of [['add','Add a physical copy'],['holds','Lending & holds'],['batch','Lending & holds'],['backup','Backups']]){
  await page.locator(`.quick-actions [data-dashboard-action=${key}]`).click();await page.waitForFunction(()=>document.querySelector('#form-dialog').open);assert.match(await page.locator('#dialog-title').innerText(),new RegExp(title));
  if(key==='batch')assert(await page.locator('#batch-search, [name=batchSearch]').count());
  if(key==='backup')assert(await page.locator('#form-content').getByText('Export library JSON',{exact:false}).count());
  await page.locator('#close-form').click();
 }
 assert.equal(await page.evaluate(async()=>(await(await fetch('/api/state')).json()).revision),revisionBefore,'Opening dashboard shortcuts must not write library data');
 for(const width of [360,390,768,1280,1440]){
  await page.setViewportSize({width,height:900});await page.evaluate(()=>window.scrollTo(0,0));
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`Horizontal overflow at ${width}`);
  assert.equal(await page.locator('.admin-sidebar').isVisible(),width>900);
  assert.equal(await page.locator('.admin-mobile-nav').isVisible(),width<=900);
  if(width<=900)for(const box of await page.locator('.admin-mobile-nav button').evaluateAll(xs=>xs.map(x=>({w:x.getBoundingClientRect().width,h:x.getBoundingClientRect().height}))))assert(box.w>=44&&box.h>=44);
  await page.screenshot({path:output+`/dashboard-admin-${width}.png`,fullPage:true});
 }
 await page.setViewportSize({width:1440,height:900});const before=await page.locator('.admin-sidebar').boundingBox();await page.evaluate(()=>window.scrollTo(0,700));const after=await page.locator('.admin-sidebar').boundingBox();assert.equal(before.y,after.y);assert.equal(await page.locator('.sidebar-decoration').evaluate(e=>getComputedStyle(e).pointerEvents),'none');
 await page.setViewportSize({width:1280,height:480});assert(await page.locator('.sidebar-scroll').evaluate(e=>e.scrollHeight>e.clientHeight));await openAdminTool(page,'edit-library-name');await page.waitForFunction(()=>document.querySelector('#form-dialog').open);await page.locator('#close-form').click();await page.screenshot({path:output+'/short-screen-admin.png'});
 await page.setViewportSize({width:390,height:844});await page.locator('.admin-mobile-nav [data-view=catalog]').click();await page.locator('#search').fill('Orchard');await page.locator('#sort').selectOption('author');await page.locator('#page-size').selectOption('25');const titles=await page.locator('#content h2').allTextContents();assert(titles.length>0);
 await page.locator('.admin-mobile-nav [data-view=dashboard]').click();await page.locator('.admin-mobile-nav [data-view=catalog]').click();assert.equal(await page.locator('#search').inputValue(),'Orchard');assert.equal(await page.locator('#sort').inputValue(),'author');assert.equal(await page.locator('#page-size').inputValue(),'25');assert.deepEqual(await page.locator('#content h2').allTextContents(),titles);
 await page.locator('#search').fill('');for(const width of [360,390,768,1280,1440]){await page.setViewportSize({width,height:900});await page.screenshot({path:output+`/catalog-admin-${width}.png`,fullPage:true});}await page.setViewportSize({width:390,height:844});
 await page.locator('.admin-mobile-nav [data-view=settings]').click();await openAdminTool(page,'household-readers');await page.waitForSelector('#add-reader');
 await page.locator('.admin-mobile-nav [data-view=settings]').click();await openAdminTool(page,'sidebar-artwork-settings');assert.match(await page.locator('#dialog-title').innerText(),/Sidebar artwork/);await page.locator('#close-form').click();assert.equal(await page.locator('.sidebar-decoration').isVisible(),false);
 await page.setViewportSize({width:390,height:844});await page.locator('.admin-mobile-nav [data-view=dashboard]').click();await page.evaluate(()=>document.documentElement.style.fontSize='200%');await page.screenshot({path:output+'/large-text-admin-390.png',fullPage:true});const overflow=await page.evaluate(()=>[...document.querySelectorAll('body *')].filter(e=>{const r=e.getBoundingClientRect();return r.width&&r.right>innerWidth+1}).map(e=>e.tagName+'.'+e.className+': '+Math.round(e.getBoundingClientRect().right)));assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'Large text must reflow: '+overflow.slice(0,20).join(', '));await page.screenshot({path:output+'/large-text-admin-390.png',fullPage:true});await page.evaluate(()=>document.documentElement.style.fontSize='');
 await page.emulateMedia({reducedMotion:'reduce'});await page.keyboard.press('Tab');assert(await page.evaluate(()=>document.activeElement.tagName!=='BODY'));
 // New UI assets stay inside the existing administrator authorization boundary.
 const anonymous=await browser.newContext();const denied=await anonymous.request.get(process.env.LIBRARY_TEST_URL+'/admin-dashboard.js',{maxRedirects:0});assert.equal(denied.status(),302);assert.equal(denied.headers().location,'/login');assert.equal((await anonymous.request.get(process.env.LIBRARY_TEST_URL+'/api/state')).status(),401);await anonymous.close();
 assert.deepEqual(errors,[]);console.log('PASS dashboard counts/history, no-write shortcuts, all widths, stationary/short sidebar, large text, mobile controls/navigation, retained catalog filters, decoration preference and protected UI assets');
}finally{await browser.close();}
