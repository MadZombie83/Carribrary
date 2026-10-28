import {installPwa} from '/account/pwa-install.js';
const savedKey='home-library-borrower-catalog';
const catalogPath=/^\/s\/[A-Za-z0-9_-]{16,128}\/$/;
let path=location.pathname;
if(catalogPath.test(path)){try{localStorage.setItem(savedKey,path);}catch{}}
else{const token=new URL(location.href).searchParams.get('library');if(token&&/^[A-Za-z0-9_-]{16,128}$/.test(token)){try{localStorage.setItem(savedKey,'/s/'+token+'/');}catch{}}}
installPwa(document.getElementById('install-borrower'),document.getElementById('install-status'));
