import {mkdir} from 'node:fs/promises';
import assert from 'node:assert/strict';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(process.env.LIBRARY_TEST_URL+'/login');assert.equal(await page.evaluate(async()=> (await fetch('/api/updates')).status),401);
 await authenticateLocalAdmin(page);
 let version='2.0.55-preview';
 await page.route('**/api/updates',route=>route.fulfill({json:{currentVersion:'2.0.54-preview',latestVersion:version,releaseUrl:'https://github.com/MadZombie83/Carribrary/releases/tag/v'+version,available:true,error:''}}));
 await page.goto(process.env.LIBRARY_TEST_URL);await page.waitForSelector('#server-update-notice:not([hidden])');
 assert.match(await page.locator('#server-update-notice').innerText(),/complete backup/i);await mkdir('output/borrower-dashboard-2.0.54',{recursive:true});await page.screenshot({path:'output/borrower-dashboard-2.0.54/update-notice-fixture.png'});
 assert.equal(await page.locator('#server-update-notice a').getAttribute('href'),'https://github.com/MadZombie83/Carribrary/releases/tag/v2.0.55-preview');
 await page.getByRole('button',{name:'Dismiss this version',exact:true}).click();await page.reload();await page.waitForSelector('.server-update-tools');assert(!(await page.locator('#server-update-notice').isVisible()));
 await page.locator('.server-update-tools summary').click();await page.getByRole('button',{name:'Check for updates',exact:true}).click();await page.waitForSelector('#server-update-notice:not([hidden])');
 version='2.0.56-preview';await page.reload();await page.waitForSelector('#server-update-notice:not([hidden])');assert.match(await page.locator('#server-update-notice').innerText(),/2.0.56-preview/);
 await page.setViewportSize({width:390,height:844});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
 await page.route('**/api/updates',route=>route.fulfill({json:{latestVersion:'2.0.99',releaseUrl:'https://evil.example',available:true,error:''}}));await page.reload();await page.waitForSelector('.server-update-tools');assert(!(await page.locator('#server-update-notice').isVisible()));
 assert.deepEqual(errors,[]);console.log('PASS administrator authorization, official release link, backup reminder, dismissal, manual recheck, next-version notice and untrusted-link rejection');
}finally{await browser.close();}
