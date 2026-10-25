export function sidebarArtworkSettings({state,mutate,openForm,toast}){
 let artwork=null,generation=0;
 openForm('Sidebar artwork',`<form><p>No artwork is installed by default. Upload your own PNG to cover the full desktop sidebar beneath the navigation. Portrait or transparent overlay images work well. Recommended canvas: 512 × 1536 pixels. Upload is fitted without cropping into that canvas, then displayed across the sidebar with a dark readability layer. The saying is separate.</p><label>Choose PNG (under 10 MB)<input id="sidebar-art-file" type="file" accept="image/png"></label><img id="sidebar-art-preview" width="128" height="384" alt="Sidebar artwork preview" ${state.librarySidebarArtwork?'':'hidden'}><p id="sidebar-art-status" role="status"></p><p class="form-error" role="alert"></p><button type="submit">Save artwork</button><button id="remove-sidebar-art" type="button" class="secondary">Remove artwork</button><p class="help">Saved for this library and its paired browser companions. The desktop sidebar is hidden on narrow screens; artwork never blocks controls. Included in library exports and backups.</p></form>`,async()=>{
  if(artwork===null)throw Error('Choose a PNG first.');await mutate('setLibrarySidebarArtwork',{artwork});document.querySelector('#form-dialog').close();toast('Sidebar artwork saved');
 });
 const preview=document.querySelector('#sidebar-art-preview');if(state.librarySidebarArtwork)preview.src='data:image/png;base64,'+state.librarySidebarArtwork;
 document.querySelector('#sidebar-art-file').onchange=async e=>{
  artwork=null;const chosen=++generation,file=e.target.files[0],status=document.querySelector('#sidebar-art-status');if(!file)return;
  let bitmap;
  try{
   if(file.type!=='image/png'||file.size>10*1024*1024)throw Error('Choose a PNG smaller than 10 MB.');bitmap=await createImageBitmap(file);
   if(bitmap.width>8192||bitmap.height>8192)throw Error('Use an image no larger than 8192 pixels on either side.');
   const canvas=document.createElement('canvas');canvas.width=512;canvas.height=1536;const scale=Math.min(512/bitmap.width,1536/bitmap.height);
   canvas.getContext('2d').drawImage(bitmap,(512-bitmap.width*scale)/2,(1536-bitmap.height*scale)/2,bitmap.width*scale,bitmap.height*scale);
   if(chosen!==generation||!status.isConnected)return;const data=canvas.toDataURL('image/png');if(data.length>2796204)throw Error('The resized image is over 2 MB. Try a simpler PNG.');artwork=data.split(',')[1];preview.src=data;preview.hidden=false;status.textContent='Ready to save.';
  }catch(error){if(chosen===generation&&status.isConnected)status.textContent=error.message;}finally{bitmap?.close();}
 };
 document.querySelector('#form-dialog').addEventListener('close',()=>generation++,{once:true});
 document.querySelector('#remove-sidebar-art').onclick=async()=>{try{await mutate('setLibrarySidebarArtwork',{artwork:''});document.querySelector('#form-dialog').close();toast('Sidebar artwork removed');}catch(error){document.querySelector('.form-error').textContent=error.message;}};
}
