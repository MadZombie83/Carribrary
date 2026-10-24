// Explicit test enrollment through the real authentication API. Never an auth bypass.
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
export async function authenticateLocalAdmin(page){
 const directory=process.env.HOME_LIBRARY_DATA,url=process.env.LIBRARY_TEST_URL;
 assert(directory?.startsWith('/tmp/hl-circulation-ui-'),'Use a disposable circulation fixture for administrator test enrollment');
 assert(['localhost','127.0.0.1'].includes(new URL(url).hostname),'Test enrollment is loopback only');
 const request=page.context().request,status=await(await request.get(url+'/local-auth/status')).json();
 const input={username:'Fixture Administrator',password:'Disposable administrator fixture password 233'};
 let endpoint='login';if(!status.configured){endpoint='enroll';input.code=(await readFile(directory+'/local-admin-setup.txt','utf8')).trim();}
 const response=await request.post(url+'/local-auth/'+endpoint,{headers:{Origin:url},data:input});
 assert.equal(response.status(),200,'Test administrator must authenticate through the real server');
}

// Navigate through the responsive admin UI; mobile reading/lending tools live under More.
export async function openAdminView(page,view){
 const direct=page.locator(`[data-view=${view}]:visible`).first();
 if(await direct.count()){await direct.click();return;}
 await page.locator('[data-admin-tool=more]:visible').first().click();
 await page.locator(`[data-admin-route=${view}]`).click();
}
