import {toolRoute,settingsTools,settingsAliases} from '../server/wwwroot/admin-tool-inventory.js';
// Exercise the actual new navigation, rather than clicking hidden compatibility controls.
export async function openAdminTool(page,id){
 id=settingsAliases[id]||id;
 if(id==='requests'||id==='lending-tools'){
  const primary=page.locator('.sidebar-nav [data-admin-tool="'+id+'"]');
  if(await primary.isVisible()){await primary.click();return;}
  const tool=settingsTools.find(t=>t.id===id);await page.evaluate(hash=>location.hash=hash,'#settings/'+tool.category);await page.locator('[data-settings-action="'+id+'"]').click();return;
 }
 const hash=toolRoute(id);if(!hash)throw Error('Missing settings route: '+id);const category='#'+hash.slice(1).split('/').slice(0,2).join('/');
 if(new URL(page.url()).hash!==category)await page.evaluate(hash=>location.hash=hash,category);
 try{await page.locator('[data-settings-action="'+id+'"]').click();}catch(error){const state=await page.evaluate(()=>({hash:location.hash,heading:document.querySelector('.settings-heading h1')?.textContent,form:document.querySelector('#dialog-title')?.textContent,open:document.querySelector('#form-dialog')?.open,closeDisabled:document.querySelector('#close-form')?.disabled,toolCount:document.querySelectorAll('[data-settings-action]').length}));throw Error('Settings navigation failed: '+JSON.stringify(state));}
 if(['household-readers','active-loans','bulk-edit','share-list'].includes(id))return;
 const host=page.locator('#settings-editor-host,#settings-palette-slot');await host.first().waitFor({state:'visible'});
}
