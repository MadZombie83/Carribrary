export const paletteKeys=['ink','accent','page','surface','soft','line','text','muted'];
export const paletteLabels={ink:'Banner, buttons & links',accent:'Banner accent',page:'Page background',surface:'Cards & dialogs',soft:'Soft highlights',line:'Borders',text:'Main text',muted:'Secondary text'};
export function validPalette(p){return p&&paletteKeys.every(k=>typeof p[k]==='string'&&p[k].length===7&&/^#[\da-f]{6}$/i.test(p[k]));}
export function applyPalette(key,p){
 const root=document.documentElement;root.dataset.theme=key||'purple-blue';
 for(const k of paletteKeys)root.style.removeProperty('--'+k);
 const custom=/^custom-[a-f0-9]{32}$/.test(key||'')&&validPalette(p);root.toggleAttribute('data-custom-palette',!!custom);
 if(custom)for(const k of paletteKeys)root.style.setProperty('--'+k,p[k]);
 const meta=document.querySelector('meta[name="theme-color"]');if(meta)meta.content=getComputedStyle(root).getPropertyValue('--ink').trim();
}
export function currentPalette(){const css=getComputedStyle(document.documentElement);return Object.fromEntries(paletteKeys.map(k=>[k,css.getPropertyValue('--'+k).trim()||({surface:'#ffffff',text:'#27273d',muted:'#55596a'}[k]) ]));}
export function contrast(a,b){const lum=h=>{const rgb=[1,3,5].map(i=>parseInt(h.slice(i,i+2),16)/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4);return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;};return(Math.max(lum(a),lum(b))+.05)/(Math.min(lum(a),lum(b))+.05);}
export function readablePalette(p){return validPalette(p)&&['page','surface','soft'].every(bg=>['text','muted','ink'].every(fg=>contrast(p[fg],p[bg])>=4.5))&&contrast(p.ink,'#ffffff')>=4.5&&contrast(p.ink,p.accent)>=4.5;}
