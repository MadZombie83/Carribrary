// Decorative library branding only. Never accepts third-party URLs or private link tokens.
export function artworkSource(encoded){return typeof encoded==='string'&&encoded?'data:image/png;base64,'+encoded:'';}
export function applyLibraryArtwork(target,source){
 if(!target)return;
 const valid=typeof source==='string'&&(/^\/account\/library-artwork\.png\?v=[a-fA-F0-9]{16}$/.test(source)||/^data:image\/png;base64,[A-Za-z0-9+/=\r\n]+$/.test(source));
 const old=target.querySelector(':scope > .library-banner-artwork');
 if(!valid){old?.remove();target.classList.remove('library-artwork-banner');return;}
 if(old?.getAttribute('src')===source)return;
 old?.remove();const img=document.createElement('img');img.className='library-banner-artwork';img.alt='';img.setAttribute('aria-hidden','true');img.decoding='async';img.src=source;img.onerror=()=>{if(img.parentElement===target){img.remove();target.classList.remove('library-artwork-banner');}};target.prepend(img);target.classList.add('library-artwork-banner');
}
