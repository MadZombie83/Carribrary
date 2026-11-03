import {fittedPng} from './appearance.js';
export function libraryIconSettings({mutate,openForm,toast}){
 let icons=null,generation=0,busy=false;
 openForm('Library icon',`<form><p>Choose a PNG, JPEG or WebP under 10 MiB, up to 8192 pixels per side. A square 512 × 512 transparent PNG is recommended. Carribrary fits it without cropping and preserves transparency. Saved PNGs are 192 × 192 and 512 × 512, up to 2 MiB each. Detailed designs are allowed. This brands library banners and browser Home-screen apps; the native Android launcher remains Carribrary.</p><input type="file" id="icon-file" accept="image/png,image/jpeg,image/webp"><img id="icon-preview" width="96" height="96" alt="Icon preview" hidden><p id="icon-message" role="status"></p><p class="form-error" role="alert"></p><button type="submit">Save library icon</button><button type="button" id="reset-library-icon" class="secondary">Use Carribrary icon</button></form>`,async()=>{
  if(busy)throw Error('Wait for the icon to finish loading.');if(!icons)throw Error('Choose a valid image first.');
  await mutate('setLibraryIcon',icons);toast('Library icon saved. Refresh open pages; Home-screen icons may need removing and adding again.');document.querySelector('#form-dialog').close();
 });
 const form=document.querySelector('#form-content form'),message=form.querySelector('#icon-message'),preview=form.querySelector('#icon-preview');
 form.querySelector('#icon-file').onchange=async e=>{
  const selected=++generation;icons=null;busy=true;preview.hidden=true;message.textContent='Preparing icon…';form.querySelector('.form-error').textContent='';
  try{
   const file=e.target.files[0];const [icon192,icon512]=await Promise.all([fittedPng(file,192,192,2097152),fittedPng(file,512,512,2097152)]);
   if(selected!==generation||!form.isConnected)return;icons={icon192,icon512};preview.src='data:image/png;base64,'+icon192;preview.hidden=false;message.textContent='Ready to save: 192 × 192 and 512 × 512 PNGs.';
  }catch(error){if(selected===generation)message.textContent=error.message;}finally{if(selected===generation)busy=false;}
 };
 form.querySelector('#reset-library-icon').onclick=async()=>{try{generation++;busy=false;icons=null;await mutate('setLibraryIcon',{icon192:'',icon512:''});document.querySelector('#form-dialog').close();toast('Carribrary icon restored');}catch(error){form.querySelector('.form-error').textContent=error.message;}};
 document.querySelector('#form-dialog').addEventListener('close',()=>generation++,{once:true});
}
