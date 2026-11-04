import {installPwa} from '/account/pwa-install.js';
const key='carribrary-household-reader-link',valid=value=>/^[A-Za-z0-9_-]{43}$/.test(value||'');
const remember=document.getElementById('remember-reader'),status=document.getElementById('reader-device-status');
if(remember){
 const token=location.hash.slice(1);
 try{remember.checked=valid(token)&&localStorage.getItem(key)===token;}catch{}
 remember.onchange=()=>{try{if(remember.checked){if(!valid(token))throw Error('Open your current private reading link first.');localStorage.setItem(key,token);status.textContent='Private link saved on this device. Keep this device locked.';}else{localStorage.removeItem(key);status.textContent='Saved private link removed. Keep your original link to return.';}}catch(error){remember.checked=false;status.textContent=error.message||'This browser cannot save a private link.';}};
 document.getElementById('forget-reader').onclick=()=>{remember.checked=false;remember.onchange();};
 installPwa(document.getElementById('install-reader'),document.getElementById('reader-install-status'),{reader:true});
 if(!token)location.replace('/reading/launch');
}
const form=document.getElementById('reader-launch-form');
if(form){
 let saved='';try{saved=localStorage.getItem(key)||'';}catch{}
 if(valid(saved))location.replace('/reading/#'+saved);
 form.onsubmit=event=>{event.preventDefault();const note=document.getElementById('reader-launch-status');try{const url=new URL(form.elements.link.value);if(url.origin!==location.origin||url.pathname!=='/reading/'||!valid(url.hash.slice(1)))throw Error();location.replace('/reading/#'+url.hash.slice(1));}catch{note.textContent='Paste your full personal reading link for this library. Ask the librarian for a replacement if it no longer works.';}};
}
