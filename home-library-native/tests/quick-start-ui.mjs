import {openCatalogIfAdmin} from './local-admin-fixture.mjs';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
import {mkdir} from 'node:fs/promises';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage({ignoreHTTPSErrors:true,viewport:{width:1100,height:1000},deviceScaleFactor:1});const errors=[];page.on('pageerror',e=>errors.push(e.message));await authenticateLocalAdmin(page);await page.goto(process.env.LIBRARY_TEST_URL);await openCatalogIfAdmin(page);await page.waitForSelector('#content');
 await page.evaluate(async()=>{for(const [action,input] of [['setLibraryName',{libraryName:'Example Library'}],['addBook',{isbn:'9780140328721',title:'Matilda',author:'Roald Dahl'}],['addBorrower',{name:'Example Borrower'}]]){const s=await(await fetch('/api/state')).json();const response=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action,input,revision:s.revision})});if(!response.ok)throw Error(await response.text());}});
 await page.reload();await openCatalogIfAdmin(page);await page.waitForSelector('.book');await mkdir('home-library-native/server/wwwroot/guide',{recursive:true});
 if(!process.env.GUIDE_VERIFY_ONLY){
 await page.setViewportSize({width:1100,height:920});await page.screenshot({path:'home-library-native/server/wwwroot/guide/catalog.png'});
 await page.setViewportSize({width:1100,height:1000});await page.locator('#add-book').click();await page.locator('#form-dialog').screenshot({path:'home-library-native/server/wwwroot/guide/add-book.png'});await page.locator('#close-form').click();
 await page.locator('#checkout').click();await page.locator('#form-dialog [name=copyId]').fill('BK-0001');await page.locator('#form-dialog [name=borrowerId]').selectOption('BR-0001');await page.locator('#form-dialog').screenshot({path:'home-library-native/server/wwwroot/guide/checkout.png'});await page.locator('#close-form').click();
 }
 await page.goto(process.env.LIBRARY_TEST_URL+'/quick-start.html');await page.waitForSelector('#print-guide');assert.equal(await page.locator('main section').count(),9);for(const img of await page.locator('figure img').all()){await img.scrollIntoViewIfNeeded();await img.evaluate(el=>el.loading='eager');await page.waitForFunction(src=>[...document.images].some(i=>i.getAttribute('src')===src&&i.complete&&i.naturalWidth>0),await img.getAttribute('src'));}
 const fullHelp=await (await page.request.get(process.env.LIBRARY_TEST_URL+'/help.json')).json();const version=fullHelp.release;
 assert.equal(await page.title(),'Carribrary '+version+' quick start');assert.match(await page.locator('header .brand').innerText(),new RegExp(version.replaceAll('.','\\.')));
 assert((await page.locator('#recovery').innerText()).includes('current program-only compatibility is '+version+' only'));
 await mkdir('output/release-guide-2.0.59',{recursive:true});
 for(const [width,height] of [[360,800],[390,844],[430,932],[667,375],[844,390],[768,1024],[1024,768],[1440,1000]]){
  await page.setViewportSize({width,height});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1),false,`Guide overflows ${width}x${height}`);
 }
 await page.setViewportSize({width:390,height:844});await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:'output/release-guide-2.0.59/quick-start-phone.png'});
 assert.deepEqual(errors,[]);
 assert.equal((await page.request.get(process.env.PUBLIC_TEST_URL+'/quick-start.html')).status(),404);
 console.log('PASS illustrated guide routes, mobile layout, print control and current fictional-library screenshots');
}finally{await browser.close();}
