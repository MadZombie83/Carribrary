import assert from 'node:assert/strict';
import {readFile,mkdir} from 'node:fs/promises';
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const version=(await readFile('home-library-native/server/SetupStatus.cs','utf8')).match(/Version="([^"]+)"/)[1];
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const page=await browser.newPage({ignoreHTTPSErrors:true,viewport:{width:1280,height:900}}),errors=[];page.on('pageerror',e=>errors.push(e.message));await authenticateLocalAdmin(page);await page.goto(process.env.LIBRARY_TEST_URL);await page.waitForFunction(()=>!!document.body.dataset.adminView);
 const expected=`© 2026 Chezburber Studios · Carribrary ${version}`;
 async function footer(){const el=page.locator('[data-product-footer]').first();await el.scrollIntoViewIfNeeded();assert.equal((await el.innerText()).trim(),expected);assert.equal(await el.isVisible(),true);assert.equal(await el.evaluate(e=>getComputedStyle(e).position),'static');assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,'Horizontal overflow on '+page.url());}
 await footer();await mkdir('output/product-info-2.0.53',{recursive:true});await page.screenshot({path:'output/product-info-2.0.53/server-desktop.png',fullPage:true});
 await page.locator('#help-button').click();await page.locator('#help-search').fill('About Carribrary');const help=await page.locator('#help-topics').innerText();for(const value of [version,'© 2026 Chezburber Studios','Chezburber Studios','Source-Available Proprietary License'])assert(help.includes(value),value);assert(!/must be published|invited test|planned as|feature set is frozen/i.test(help));assert.equal(await page.locator('#form-content [data-product-footer]').count(),1);await page.locator('#close-form').click();
 await page.setViewportSize({width:390,height:844});await footer();await page.screenshot({path:'output/product-info-2.0.53/server-mobile.png',fullPage:true});
 await page.goto(process.env.LIBRARY_TEST_URL+'/help.html');await page.waitForSelector('#help-content section');await footer();await page.locator('#help-search').fill('About Carribrary');assert((await page.locator('#help-content').innerText()).includes(version));
 for(const path of ['/','/account/','/companion/','/kiosk/']){await page.goto(process.env.PUBLIC_TEST_URL+path);await footer();}
 await page.goto(process.env.PUBLIC_TEST_URL+'/');await page.screenshot({path:'output/product-info-2.0.53/welcome-mobile.png',fullPage:true});
 await page.setViewportSize({width:1280,height:900});await footer();assert.deepEqual(errors,[]);console.log('PASS current version/copyright, readable nonfixed desktop/mobile footers, customer-facing About, account/welcome/kiosk/companion pages and Help');
}finally{await browser.close();}
