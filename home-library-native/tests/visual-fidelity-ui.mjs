import {openCatalogIfAdmin} from './local-admin-fixture.mjs';
import {openAdminTool} from './admin-tool-navigation.mjs';
// Real authenticated UI and disposable state; no installed household records.
import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
import fs from 'node:fs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const output=process.env.CARRIBRARY_UX_SCREENSHOTS||'/tmp/carribrary-fidelity';fs.mkdirSync(output,{recursive:true});
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
try{
 const context=await browser.newContext(),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await authenticateLocalAdmin(page);await page.goto(process.env.LIBRARY_TEST_URL);await openCatalogIfAdmin(page);await page.waitForSelector('#content .empty, #content .book');
 const icon='data:image/png;base64,'+fs.readFileSync(process.env.CARRIBRARY_VISUAL_ICON||new URL('../assets/home-library.png',import.meta.url)).toString('base64');
 await page.evaluate(async ({icon,name})=>{const image=new Image();image.src=icon;await image.decode();const canvas=document.createElement('canvas');canvas.width=canvas.height=192;canvas.getContext('2d').drawImage(image,0,0,192,192);const input={version:1,nextCopy:9,libraryName:name,libraryTheme:'purple-blue',bannerSubtitle:'A shelf worth sharing',encouragingMessage:'A good story grows when it is shared.',libraryIcon192:canvas.toDataURL().split(',')[1],books:Array.from({length:8},(_,i)=>({id:'BK-'+String(i+1).padStart(4,'0'),isbn:'9780140328721',title:['The Secret Garden','A Journey Home','Stories by the Sea','The Little Orchard'][i%4],author:['Frances Hodgson Burnett','Alex Reed','Morgan Vale','Robin Green'][i%4],publisher:'Fixture Press',coverUrl:''})),borrowers:[{id:'BR-0001',name:'Test Robin',contact:'robin@example.com'}],loans:[]};canvas.width=canvas.height=512;canvas.getContext('2d').drawImage(image,0,0,512,512);input.libraryIcon512=canvas.toDataURL().split(',')[1];const snapshot=await(await fetch('/api/state')).json();const response=await fetch('/api/action',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({action:'restore',revision:snapshot.revision,input})});if(!response.ok)throw Error(await response.text());},{icon,name:process.env.CARRIBRARY_VISUAL_NAME||'Meadow House Library'});
 await page.reload();await openCatalogIfAdmin(page);await page.waitForSelector('#content .book');await page.locator('.sidebar-nav [data-view=dashboard]').click();
 assert.deepEqual(await page.locator('[data-kpi]').allTextContents(),['8','8','0','0']);
 for(const [width,height] of [[1920,1080],[1440,900],[768,1024],[390,844]]){
  await page.setViewportSize({width,height});await page.evaluate(()=>window.scrollTo(0,0));
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`overflow at ${width}`);
  assert.equal(await page.locator('.admin-sidebar').isVisible(),width>900);
  if(width>900){assert.equal(await page.locator('.sidebar-brand img').evaluate(e=>getComputedStyle(e).backgroundColor),'rgba(0, 0, 0, 0)');assert.equal(await page.locator('.sidebar-brand img').evaluate(e=>getComputedStyle(e).objectFit),'contain');}
  assert.equal(await page.locator('.sidebar-decoration img').count(),0,'No mandatory decorative image');
  await page.screenshot({path:`${output}/finished-${width}x${height}.png`});
  await page.screenshot({path:`${output}/finished-full-${width}x${height}.png`,fullPage:true});
 }
 await page.setViewportSize({width:1440,height:900});await page.locator('#dashboard-search').fill('Orchard');await page.locator('#dashboard-search').press('Enter');
 assert.equal(await page.locator('#search').inputValue(),'Orchard');assert.match(await page.locator('#content').innerText(),/Little Orchard/);assert(!await page.locator('#content').innerText().then(t=>t.includes('Secret Garden')));
 await page.locator('.sidebar-nav [data-view=dashboard]').click();
 for(const tool of ['requests','lending-tools','devices','export','edit-library-name']){await page.locator(`.sidebar-nav [data-admin-tool=${tool}]`).click();await page.waitForFunction(()=>document.querySelector('#form-dialog').open);await page.locator('#close-form').click();}
 await page.locator('.sidebar-nav [data-view=catalog]').click();await page.locator('#search').fill('');
 for(const theme of ['classic','forest','rose','ocean','purple-blue']){await openAdminTool(page,'palette');await page.locator('#theme').selectOption(theme);await page.waitForFunction(t=>document.body.dataset.libraryTheme===t,theme);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));}
 assert.deepEqual(errors,[]);console.log('PASS reference widths, real counts, transparent logo, no mandatory artwork, dashboard catalog search, operational dialogs, all five themes');
}finally{await browser.close();}
