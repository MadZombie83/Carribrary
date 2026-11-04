// One installation controller for borrower pages and the separate librarian companion.
// Installation never grants access and never copies a share token into the manifest.
export function installPwa(button,status,{admin=false,reader=false}={}){
 if(!button||!status)return;
 let pending=null;const installed=()=>matchMedia('(display-mode: standalone)').matches||navigator.standalone===true;
 const say=text=>{status.hidden=false;status.textContent=text;};
 const show=()=>{button.hidden=installed();if(installed())say('Opened as your Carribrary app.');};show();
 window.addEventListener('beforeinstallprompt',event=>{event.preventDefault();pending=event;show();button.textContent=admin?'Install librarian app':reader?'Install reader app':'Install borrower app';});
 window.addEventListener('appinstalled',()=>{pending=null;button.hidden=true;say('Installed. Open the Carribrary icon from your Home screen or app launcher.');});
 const apple=/iPhone|iPad|iPod/.test(navigator.userAgent)||navigator.platform==='MacIntel'&&navigator.maxTouchPoints>1;
 const worker=admin?'/companion/admin-sw.js':reader?'/reading/reader-sw.js':'/account/borrower-sw.js',scope=admin?'/companion/':reader?'/reading/':'/';
 if(isSecureContext&&'serviceWorker'in navigator)navigator.serviceWorker.register(worker,{scope,updateViaCache:'none'}).catch(()=>say('The app could not prepare installation. Refresh the public HTTPS page and try again. Your library data has not changed.'));
 button.onclick=async()=>{
  if(pending){const prompt=pending;pending=null;try{await prompt.prompt();const choice=await prompt.userChoice;say(choice.outcome==='accepted'?'Installation requested. Look for the library icon in your apps.':'Installation cancelled. You can try again from the browser menu.');}catch{say('The install prompt expired. Refresh and try again, or use the browser menu.');}return;}
  if(!isSecureContext){say('App installation requires the public HTTPS library address with a trusted certificate. A local http:// address on another computer can only make a shortcut. Open the HTTPS address and try again.');return;}
  if(apple){say('On iPhone, open this HTTPS page in Safari → Share → Add to Home Screen → Open as Web App (if shown) → Add. Open that icon'+(admin?', then pair the companion.':'.'));return;}
  try{const response=await fetch(admin?'/companion/manifest.webmanifest':reader?'/reading/manifest.webmanifest':'/account/manifest.webmanifest',{cache:'no-store'});if(!response.ok)throw Error();const manifest=await response.json();if(!manifest.start_url||!manifest.icons?.length)throw Error();}
  catch{say('The app installation information could not load. Refresh this HTTPS page after updating the server, then try again.');return;}
  say('In regular Chrome, use ⋮ → Add to Home screen → Install, or ⋮ → Cast, save and share → Install page as app on desktop. If Chrome offers only Create shortcut, check that this is the public HTTPS address without a certificate warning, not Incognito or an in-app browser. The app may already be installed. Close old tabs and reopen this link after a server update; if it still offers only a shortcut, send the librarian the address and browser version. A shortcut alone does not confirm app installation.');
 };
}
