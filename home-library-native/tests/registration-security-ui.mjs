import {authenticateLocalAdmin} from './local-admin-fixture.mjs';
import assert from 'node:assert/strict';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE||'playwright');
const browser=await chromium.launch({executablePath:'/usr/bin/google-chrome',headless:true,args:['--no-sandbox']});
const adminURL=process.env.LIBRARY_TEST_URL,upstream=process.env.PUBLIC_TEST_URL,origin='https://library.test';
try{
 const admin=await browser.newPage();await authenticateLocalAdmin(admin);await admin.goto(adminURL);await admin.waitForFunction(()=>document.querySelector('#save-status').textContent==='Shared library on your home server');
 const share=await admin.evaluate(async()=> (await(await fetch('/api/sharing',{method:'POST',headers:{'Content-Type':'application/json'},body:'{"action":"enable"}'})).json()).url);const token=new URL(share).pathname.split('/')[2];
 const context=await browser.newContext();await context.route(origin+'/**',async route=>{const u=new URL(route.request().url());await route.fulfill({response:await route.fetch({url:upstream+u.pathname+u.search,headers:{...route.request().headers(),origin}})});});const page=await context.newPage();let cloudflare=0;page.on('request',r=>{if(new URL(r.url()).hostname==='challenges.cloudflare.com')cloudflare++;});
 await page.goto(origin+'/account/?library='+token);await page.locator('#show-register').click();await page.waitForFunction(()=>!document.querySelector('#register [type=submit]').disabled);
 assert.match(await page.locator('#registration-verification').innerText(),/rate limits/);assert.equal(cloudflare,0);
 for(const [field,value] of Object.entries({name:'Security Reader',username:'security.reader',password:'Security test password 232',confirm:'Security test password 232'}))await page.locator(`#register [name=${field}]`).fill(value);
 await page.locator('#register [type=submit]').click();await page.waitForSelector('#check-approval');assert.match(await page.locator('#auth-content').innerText(),/awaiting librarian approval/);
 const post=(path,data)=>context.request.post(upstream+path,{headers:{Origin:origin},data});
 assert.equal((await post('/account/registration-options',{shareToken:'invalid'})).status(),404);
 const options=await post('/account/registration-options',{shareToken:token});assert.deepEqual(await options.json(),{turnstileEnabled:false,siteKey:''});
 for(let i=0;i<4;i++)assert.equal((await post('/account/register',{username:'unused'+i,password:'Security test password 232',name:'Unused',contact:'',shareToken:'invalid'})).status(),404);
 const limited=await post('/account/register',{username:'unused',password:'Security test password 232',name:'Unused',contact:'',shareToken:token});assert.equal(limited.status(),429);assert.match((await limited.json()).error,/Too many attempts/);assert.ok(Number(limited.headers()['retry-after'])>0);
 let logins=0;async function login(username,password){logins++;return post('/account/login',{username,password});}
 const unknown=await login('unknown.reader','wrong'),wrong=await login('security.reader','wrong');assert.equal(unknown.status(),401);assert.deepEqual(await unknown.json(),await wrong.json());
 assert.equal((await login('security.reader','Security test password 232')).status(),200);
 for(let i=0;i<10;i++)assert.equal((await post('/account/reset',{token:'x'.repeat(43),password:'Replacement test password 232'})).status(),401);
 assert.equal((await post('/account/reset',{token:'x'.repeat(43),password:'Replacement test password 232'})).status(),429);assert.equal((await login('security.reader','Security test password 232')).status(),200);
 while(logins<20)assert.equal((await login('unknown.reader','wrong')).status(),401);
 assert.equal((await login('unknown.reader','wrong')).status(),429);
 await context.clearCookies();await page.goto(origin+'/account/');await page.locator('#signin [name=username]').fill('unknown.reader');await page.locator('#signin [name=password]').fill('wrong');await page.locator('#signin [type=submit]').click();await page.waitForFunction(()=>document.querySelector('#auth-message').textContent.includes('Too many attempts'));
 assert.equal((await context.request.get(upstream+'/')).status(),404);assert.equal((await context.request.get(upstream+'/api/state')).status(),404);
 console.log('PASS provider-free signup/approval, no Cloudflare traffic, valid invitation required, separate signup/login/reset limits, Retry-After, generic auth errors and visible 429 feedback');
}finally{await browser.close();}
