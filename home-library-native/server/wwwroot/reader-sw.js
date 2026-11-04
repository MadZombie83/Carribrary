// Network-only. Never store catalog, borrower, credential or personal API responses.
self.addEventListener('install',()=>self.skipWaiting());
self.addEventListener('activate',event=>event.waitUntil(self.clients.claim()));
self.addEventListener('fetch',event=>{
 const request=event.request;if(!new URL(request.url).pathname.startsWith('/reading/'))return;if(request.method!=='GET'||request.mode!=='navigate'||new URL(request.url).origin!==self.location.origin)return;
 event.respondWith(fetch(request,{cache:'no-store'}).catch(()=>new Response('<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Carribrary · Offline</title><body style="font-family:system-ui;padding:2rem;max-width:35rem;margin:auto"><h1>Connect to your library</h1><p>Your library server is not reachable. Check your connection and try again.</p><p>Private loans, contacts and reading notes are not stored for offline use in this borrower app.</p><p><a href="">Try again</a></p></body></html>',{status:503,headers:{'Content-Type':'text/html; charset=utf-8','Cache-Control':'no-store','Content-Security-Policy':"default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'"}})));
});
