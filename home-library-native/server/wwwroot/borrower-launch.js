const status=document.getElementById('launch-status');let path='';try{path=localStorage.getItem('home-library-borrower-catalog')||'';}catch{}
if(!/^\/s\/[A-Za-z0-9_-]{16,128}\/$/.test(path))path='';
try{
 const response=await fetch('/account/me',{credentials:'same-origin',cache:'no-store',signal:AbortSignal.timeout(8000)});
 if(response.ok){location.replace('/account/'+(path?'?library='+encodeURIComponent(path.split('/')[2]):''));}
 else if(response.status!==401){status.textContent='The server could not check your sign-in. Try the sign-in link, or reopen the latest library QR.';}
 else if(path){const catalog=await fetch(path,{method:'HEAD',cache:'no-store',signal:AbortSignal.timeout(8000)});if(catalog.ok)location.replace(path);else status.textContent='This shared library link is unavailable or was replaced. Ask the owner for the latest QR/link, or sign in to your existing account below.';}
 else location.replace('/account/');
}catch{status.textContent='The library server is not reachable. Check your connection and try again. Private loans and reading information are not saved for offline use.';}
