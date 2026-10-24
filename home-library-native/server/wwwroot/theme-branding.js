import {applyPalette} from './theme-palette.js';
try{
 const response=await fetch('branding',{cache:'no-store',signal:AbortSignal.timeout(10000)});
 if(response.ok){const brand=await response.json();applyPalette(brand.libraryTheme,brand.palette);
  const label=document.querySelector('[data-library-name]');if(label)label.textContent=brand.libraryName;
  const mark=document.querySelector('[data-library-icon]');if(mark){const icon=document.createElement('img');icon.width=icon.height=44;icon.alt='';icon.src='branding-icon.png';mark.replaceChildren(icon);}
 }
}catch{/* A branding failure must never prevent sign-in or reading help. */}
