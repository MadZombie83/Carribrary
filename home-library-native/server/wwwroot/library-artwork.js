// Decorative library branding only. Never accepts third-party URLs or private link tokens.
export function artworkSource(encoded){return typeof encoded==='string'&&encoded?'data:image/png;base64,'+encoded:'';}
export function applyLibraryArtwork(target,source){
 if(!target)return;
 const valid=typeof source==='string'&&(/^\/account\/(?:library-artwork|borrower-artwork)\.png\?v=[a-fA-F0-9]{16}$/.test(source)||/^data:image\/png;base64,[A-Za-z0-9+/=\r\n]+$/.test(source));
 const old=target.querySelector(':scope > .library-banner-artwork');
 if(!valid){old?.remove();target.classList.remove('library-artwork-banner');return;}
 if(old?.getAttribute('src')===source)return;
 old?.remove();const img=document.createElement('img');img.className='library-banner-artwork';img.alt='';img.setAttribute('aria-hidden','true');img.decoding='async';img.src=source;img.onerror=()=>{if(img.parentElement===target){img.remove();target.classList.remove('library-artwork-banner');}};target.prepend(img);target.classList.add('library-artwork-banner');
}

// Orientation is selected by CSS, including live rotation and standalone PWA windows.
export function headerArtworkPosition(value={}){
 const number=(n,fallback,min=0,max=100)=>Number.isFinite(n)?Math.max(min,Math.min(max,n)):fallback;
 return {fit:value?.fit==='cover'?'cover':'contain',x:number(value?.x,50),y:number(value?.y,38),overlay:number(value?.overlay,70,40,95)};
}
export function applyBorrowerHeaderLayout(target,layout={}){
 if(!target)return;
 target.classList.add('borrower-artwork-responsive');
 for(const orientation of ['portrait','landscape']){
  const p=headerArtworkPosition(layout?.[orientation]);
  for(const [key,value] of Object.entries({fit:p.fit,x:p.x+'%',y:p.y+'%',overlay:p.overlay+'%'}))target.style.setProperty('--art-'+orientation+'-'+key,value);
 }
}

export function serverArtworkLayout(value={}){
 const bounded=(n,fallback)=>Number.isFinite(n)?Math.max(40,Math.min(95,n)):fallback;
 return {sidebarOverlay:bounded(value?.sidebarOverlay,65),headerOverlay:bounded(value?.headerOverlay,85)};
}
export function applyServerArtworkLayout(target,value={}){
 if(!target)return;
 const p=serverArtworkLayout(value);
 target.style.setProperty('--server-sidebar-overlay',p.sidebarOverlay+'%');
 target.style.setProperty('--server-header-overlay',p.headerOverlay+'%');
}
