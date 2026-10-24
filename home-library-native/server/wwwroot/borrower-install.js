const savedKey='home-library-borrower-catalog';
const catalogPath=/^\/s\/[A-Za-z0-9_-]{16,128}\/$/;
let path=location.pathname;
if(catalogPath.test(path)){try{localStorage.setItem(savedKey,path);}catch{}}
else{const token=new URL(location.href).searchParams.get('library');if(token&&/^[A-Za-z0-9_-]{16,128}$/.test(token)){try{localStorage.setItem(savedKey,'/s/'+token+'/');}catch{}}}
const button=document.getElementById('install-borrower'),status=document.getElementById('install-status');let promptEvent;
const installed=()=>matchMedia('(display-mode: standalone)').matches||navigator.standalone===true;
const message=text=>{status.hidden=false;status.textContent=text;};
if(installed()){button.hidden=true;message('Opened as your Carribrary borrower app.');}
window.addEventListener('beforeinstallprompt',event=>{event.preventDefault();promptEvent=event;button.hidden=false;button.textContent='Install borrower app';});
window.addEventListener('appinstalled',()=>{promptEvent=null;button.hidden=true;message('Installed. Open the Carribrary icon on your Home screen.');});
button.addEventListener('click',async()=>{
 if(promptEvent){const event=promptEvent;promptEvent=null;try{await event.prompt();const choice=await event.userChoice;message(choice.outcome==='accepted'?'Installation requested. Look for the library icon on your Home screen.':'You can install later using Chrome’s menu → Add to Home screen.');}catch{message('In Chrome, tap ⋮ → Add to Home screen → Install (or Create shortcut).');}}
 else if(!isSecureContext)message('Open the library’s public HTTPS link in Chrome first. Then tap ⋮ → Add to Home screen → Install (or Create shortcut).');
 else if(/iPad|iPhone|iPod/.test(navigator.userAgent))message('On iPhone, open this link in Safari, tap Share → Add to Home Screen, then Add.');
 else message('In Chrome, tap ⋮ → Add to Home screen → Install (or Create shortcut), then confirm. If opened inside another app, choose Open in Chrome first. No APK or private pairing QR is needed.');
});
if(isSecureContext&&'serviceWorker' in navigator){navigator.serviceWorker.register('/account/borrower-sw.js',{scope:'/',updateViaCache:'none'}).catch(()=>{});}
