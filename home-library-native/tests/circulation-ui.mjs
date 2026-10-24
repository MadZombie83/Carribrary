import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE || 'playwright');
const browser=await chromium.launch({executablePath:process.env.CHROME_PATH || '/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage({viewport:{width:412,height:915}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{
  window.fakeScan='';window.scannerFormats=[];
  window.BarcodeDetector=class{constructor(o){window.scannerFormats=o.formats}static async getSupportedFormats(){return ['ean_13','ean_8','upc_a','upc_e','code_128','code_39','qr_code']}async detect(){return window.fakeScan?[{rawValue:window.fakeScan}]:[]}};
  Object.defineProperty(navigator.mediaDevices,'getUserMedia',{value:async()=>new MediaStream()});
  Object.defineProperty(navigator.mediaDevices,'enumerateDevices',{value:async()=>[]});
  HTMLMediaElement.prototype.play=async()=>{};
 });
 await authenticateLocalAdmin(page);await page.goto(process.env.LIBRARY_TEST_URL || 'http://localhost:4180');
 await page.waitForSelector('#checkout');
 const state={version:1,nextCopy:4,collections:[{id:'general',name:'General'},{id:'11111111111111111111111111111111',name:'Kids'}],libraryName:'Scanning Test Library',books:[1,2,3].map(i=>({id:'BK-000'+i,isbn:i===2?'0140328726':'9780140328721',title:'Matilda',author:'Roald Dahl',publisher:'',coverUrl:'',collectionId:i===2?'11111111111111111111111111111111':'general'})),borrowers:[{id:'BR-0001',name:'Alice',contact:''},{id:'BR-0002',name:'Bob',contact:''}],loans:[{id:'LN-1',copyId:'BK-0002',borrowerId:'BR-0001',checkoutDate:'2026-10-01',dueDate:'2026-11-01',returnedDate:null},{id:'LN-2',copyId:'BK-0003',borrowerId:'BR-0002',checkoutDate:'2026-10-02',dueDate:'',returnedDate:null}]};
 const result=await page.evaluate(async s=>{const current=await (await fetch('/api/state')).json();return(await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action:'restore',input:s,revision:current.revision})})).status;},state);assert.equal(result,200);
 await page.reload();await page.waitForSelector('.book');await page.locator('#collection').selectOption('general');
 await page.locator('#return').click();await page.locator('[name=copyId]').fill('9780140328721');
 assert.equal(await page.locator('#copy-matches input').count(),2);
 assert.equal(await page.locator('#confirm-circulation').isDisabled(),true);
 await page.screenshot({path:'/tmp/home-library-copy-chooser.png'});
 assert.match(await page.locator('#copy-matches').innerText(),/Alice/);assert.match(await page.locator('#copy-matches').innerText(),/Bob/);
 await page.locator('#copy-matches input[value="BK-0002"]').check();
 assert.equal(await page.locator('#confirm-circulation').isEnabled(),true);
 // Changing input must clear the selection; never return a stale chosen copy.
 await page.locator('[name=copyId]').fill('9780547928227');assert.equal(await page.locator('#confirm-circulation').isDisabled(),true);assert.match(await page.locator('#copy-matches').innerText(),/No checked-out copy/);
 await page.locator('[name=copyId]').fill('BK-0002');await page.locator('#confirm-circulation').click();await page.waitForFunction(()=>!document.querySelector('#form-dialog').open);
 let live=await page.evaluate(async()=> (await(await fetch('/api/state')).json()).state);
 assert.ok(live.loans.find(l=>l.copyId==='BK-0002').returnedDate);assert.equal(live.loans.find(l=>l.copyId==='BK-0003').returnedDate,null);
 await page.locator('#return').click();await page.locator('[name=copyId]').fill('0-14-032872-6');assert.equal(await page.locator('#copy-matches input').count(),1);assert.equal(await page.locator('#confirm-circulation').isEnabled(),true);await page.locator('#close-form').click();
 await page.locator('#checkout').click();await page.locator('[name=copyId]').fill('9780140328721');assert.equal(await page.locator('#copy-matches input').count(),2);assert.equal(await page.locator('#confirm-circulation').isDisabled(),true);
 assert.equal(await page.locator('#copy-matches input[value="BK-0003"]').count(),0);
 await page.locator('[name=copyId]').fill('BK-0003');assert.equal(await page.locator('#confirm-circulation').isDisabled(),true);assert.match(await page.locator('#copy-matches').innerText(),/No available copy/);
 await page.locator('[name=copyId]').fill('Matilda');await page.locator('#copy-matches input[value="BK-0001"]').check();await page.locator('[name=borrowerId]').selectOption('BR-0002');await page.locator('#confirm-circulation').click();await page.waitForFunction(()=>document.querySelector('#dialog-title').textContent.includes('receipt'));
 live=await page.evaluate(async()=> (await(await fetch('/api/state')).json()).state);assert.equal(live.loans.filter(l=>l.copyId==='BK-0001'&&!l.returnedDate).length,1);
 await page.locator('#close-form').click();await page.locator('#return').click();
 await page.evaluate(()=>window.fakeScan='9780140328721');await page.locator('#scan-copy').click();await page.waitForFunction(()=>!document.querySelector('#scan-dialog').open);
 assert.equal(await page.locator('[name=copyId]').inputValue(),'9780140328721');assert.equal(await page.locator('#copy-matches input').count(),2);
 assert.ok(await page.evaluate(()=>window.scannerFormats.includes('ean_13')&&window.scannerFormats.includes('qr_code')));
 await page.evaluate(()=>window.fakeScan='BK-0003');await page.locator('#scan-copy').click();await page.waitForFunction(()=>!document.querySelector('#scan-dialog').open);
 assert.equal(await page.locator('[name=copyId]').inputValue(),'BK-0003');assert.equal(await page.locator('#confirm-circulation').isEnabled(),true);
 await page.screenshot({path:'/tmp/home-library-scanning-mobile.png'});
 assert.deepEqual(errors,[]);console.log('Mobile web UI: PASS (confirmation, ambiguity, selection reset, ISBN equivalence, eligible filtering, checkout/return and receipts)');
}finally{await browser.close();}
