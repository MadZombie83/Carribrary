import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage(),url=process.env.LIBRARY_TEST_URL;await authenticateLocalAdmin(page);
 await page.goto(url);await page.waitForSelector('#admin-dashboard .quick-actions');assert.equal(await page.locator('body').getAttribute('data-admin-view'),'dashboard');
 await page.locator('[data-view=catalog]:visible').first().click();await page.waitForFunction(()=>document.body.dataset.adminView==='catalog');await page.reload();await page.waitForSelector('#admin-dashboard .quick-actions');
 await page.goto(url+'/#settings/people-access');await page.waitForSelector('#settings-page h1');assert.match(await page.locator('#settings-page h1').innerText(),/People & Access/);
 await page.locator('#local-admin-signout').click();await page.waitForURL('**/login');assert.equal((await page.request.get(url+'/api/state')).status(),401);
 await page.locator('[name=username]').fill('Fixture Administrator');await page.locator('[name=password]').fill('Disposable administrator fixture password 233');await page.locator('#submit').click();await page.waitForSelector('#admin-dashboard .quick-actions');assert.equal(await page.locator('body').getAttribute('data-admin-view'),'dashboard');
 await page.goto(url+'/?stop=1');await page.waitForSelector('#keep-running');await page.locator('#keep-running').click();assert.equal(await page.locator('body').getAttribute('data-admin-view'),'dashboard');
 console.log('PASS local administrator default, reload and real sign-in open Dashboard; explicit Settings deep links and stop confirmation remain; authentication required after sign-out.');
}finally{await browser.close();}
