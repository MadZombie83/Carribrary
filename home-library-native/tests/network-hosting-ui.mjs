import assert from 'node:assert/strict';
import {mkdir} from 'node:fs/promises';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import {openAdminTool} from './admin-tool-navigation.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox','--ignore-certificate-errors']});
const out='output/network-hosting-2.0.60';await mkdir(out,{recursive:true});
try{
 const context=await browser.newContext({ignoreHTTPSErrors:true,viewport:{width:1440,height:1000}}),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));const base=process.env.LIBRARY_TEST_URL,publicBase=process.env.PUBLIC_TEST_URL;
 const anonymous=await browser.newContext();assert.equal((await anonymous.request.get(base+'/api/network-hosting')).status(),401);assert.equal((await anonymous.request.get(publicBase+'/api/network-hosting')).status(),404);await anonymous.close();
 await authenticateLocalAdmin(page);await page.goto(base);await page.waitForSelector('#admin-dashboard');await openAdminTool(page,'network-hosting');await page.waitForSelector('#network-hosting-form');assert.equal(await page.locator('dialog:modal').count(),0);
 await page.locator('[data-host-mode=create]').click();await page.locator('[name=libraryName]').fill('Neighborhood test library');await page.locator('#network-hosting-form [type=submit]').click();await page.waitForSelector('.hosting-library');
 const snapshot=await(await page.request.get(base+'/api/network-hosting')).json(),library=snapshot.hosting.libraries[0];assert(library.initialized);assert.equal(library.name,'Neighborhood test library');assert(!JSON.stringify(snapshot).includes('protectedToken'));
 await page.screenshot({path:out+'/libraries-desktop.png',fullPage:true});
 page.once('dialog',d=>d.accept());await page.locator('[data-host-toggle]').click();await page.waitForFunction(()=>document.querySelector('[data-host-toggle]')?.textContent==='Start');
 await page.locator('[data-host-toggle]').click();await page.waitForFunction(()=>document.querySelector('[data-host-toggle]')?.textContent==='Pause');
 await page.setViewportSize({width:390,height:844});await page.locator('[data-host-mode=other]').click();await page.locator('[data-host-mode=prepare]').click();assert.equal(await page.locator('[name=externalPhonePort]').inputValue(),'7444');assert.equal(await page.locator('[name=duckDnsToken]').getAttribute('type'),'password');assert(await page.locator('[name=agree]').isVisible());assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));await page.screenshot({path:out+'/connection-mobile.png',fullPage:true});
 await page.route('**/api/setup',route=>route.fulfill({status:503,contentType:'application/json',body:'{"error":"Offline fixture"}'}));await openAdminTool(page,'complete-recovery');await page.waitForSelector('#form-content h3');assert.match(await page.locator('#form-content').innerText(),/server is not responding/i);assert.equal(await page.locator('a[href="carribrary-recovery:"]').count(),0);await page.unroute('**/api/setup');
 // Household installation uses a separate scoped app and never places the personal capability in its manifest.
 const reader=await context.newPage();const token='A'.repeat(43);await reader.goto(publicBase+'/reading/#'+token);await reader.locator('#reader-device summary').click();assert(await reader.locator('#install-reader').isVisible());assert.equal(await reader.locator('#remember-reader').isChecked(),false);
 const manifest=await(await context.request.get(publicBase+'/reading/manifest.webmanifest')).json();assert.equal(manifest.start_url,'/reading/launch');assert.equal(manifest.scope,'/reading/');assert(!JSON.stringify(manifest).includes(token));
 await reader.locator('#remember-reader').check();await reader.reload();await reader.locator('#reader-device summary').click();assert(await reader.locator('#remember-reader').isChecked());await reader.goto(publicBase+'/reading/launch');await reader.waitForURL('**/reading/#'+token);await reader.locator('#reader-device summary').click();await reader.locator('#forget-reader').click();assert.equal(await reader.evaluate(()=>localStorage.getItem('carribrary-household-reader-link')),null);
 await reader.goto(publicBase+'/reading/launch');await reader.locator('[name=link]').fill('https://different.invalid/reading/#'+token);await reader.locator('[type=submit]').click();assert.match(await reader.locator('#reader-launch-status').innerText(),/this library/);
 assert.deepEqual(errors,[]);console.log('PASS network hosting local authorization, real child creation/pause/resume, responsive wizard and private household PWA installation/forget flow');
}finally{await browser.close();}
