import assert from 'node:assert/strict';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
const admin=process.env.LIBRARY_TEST_URL||'http://localhost:19673',origin=process.env.PUBLIC_TEST_URL||'https://localhost:19677';
try{
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:390,height:844}}),adminPage=await context.newPage();await adminPage.goto(admin);
 const pair=await adminPage.evaluate(async()=> (await(await fetch('/api/browser-admins/pair',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'})).json()));
 assert.equal((await context.request.post(origin+'/companion/enroll',{headers:{Origin:origin},data:{code:pair.code,name:'Label PWA test'}})).status(),200);
 const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));await page.addInitScript(()=>window.print=()=>{});await page.goto(origin+'/companion/');await page.waitForSelector('.book');await page.locator('details').filter({has:page.locator('#copy-labels')}).evaluate(el=>el.open=true);
 assert.equal((await page.request.get(origin+'/companion/copy-labels.js')).status(),200);
 await page.locator('#copy-labels').click();await page.locator('[name=layout]').selectOption('a4');await page.locator('#label-select').click();await page.locator('#form-content [type=submit]').click();await page.waitForFunction(()=>document.querySelector('#copy-label-print')?.contentDocument?.querySelector('svg'));
 await page.addScriptTag({url:origin+'/companion/vendor/zxing-browser-0.2.1.min.js'});
 const result=await page.evaluate(async()=>{
  const frame=document.querySelector('#copy-label-print');const svg=frame.contentDocument.querySelector('svg').cloneNode(true);svg.setAttribute('width','500');svg.setAttribute('height','500');const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(new XMLSerializer().serializeToString(svg));await image.decode();
  const result=await new ZXingBrowser.BrowserQRCodeReader().decodeFromImageElement(image);const s=await(await fetch('/api/state')).json();return {actual:result.getText(),expected:'carribrary:copy:v1:'+s.state.books[0].copyUid};
 });assert.equal(result.actual,result.expected);assert.deepEqual(errors,[]);console.log('iPhone PWA label screen, authenticated module/API routing, A4 print and rendered QR decode: PASS');
}finally{await browser.close();}
