// Fit whole words first; a narrow sidebar stacks the logo before compromising readability.
export function installLibraryTitleFitting(){
 const canvas=document.createElement('canvas'),context=canvas.getContext('2d');let scheduled=false;
 const targets=[['.sidebar-brand','[data-sidebar-name]','[data-sidebar-icon]',1.48],['.admin-header .brand','#library-name','.mark',1.15]];
 function fit(){scheduled=false;const root=parseFloat(getComputedStyle(document.documentElement).fontSize)||16;
  for(const [containerSelector,labelSelector,logoSelector,scale] of targets){const container=document.querySelector(containerSelector),label=document.querySelector(labelSelector),logo=container?.querySelector(logoSelector);if(!container||!label||container.clientWidth===0)continue;
   const css=getComputedStyle(container),font=getComputedStyle(label),width=container.clientWidth-parseFloat(css.paddingLeft)-parseFloat(css.paddingRight),gap=parseFloat(css.columnGap)||12,beside=width-(logo?.getBoundingClientRect().width||0)-gap;
   const preferred=scale*root,minimum=root,words=label.textContent.trim().split(/\s+/).filter(Boolean);const longest=size=>{context.font=`${font.fontStyle} ${font.fontWeight} ${size}px ${font.fontFamily}`;return Math.max(0,...words.map(word=>context.measureText(word).width));};
   let size=preferred;while(size>minimum&&longest(size)>beside)size=Math.max(minimum,size-.5);
   const stacked=longest(size)>beside;container.toggleAttribute('data-title-stacked',stacked);
   if(stacked){size=preferred;while(size>minimum&&longest(size)>width)size=Math.max(minimum,size-.5);}
   label.style.fontSize=size+'px';const scroll=longest(size)>(stacked?width:beside);label.toggleAttribute('data-title-scroll',scroll);if(scroll){label.tabIndex=0;label.title=label.textContent;}else{label.removeAttribute('tabindex');label.removeAttribute('title');}
  }
 }
 const schedule=()=>{if(!scheduled){scheduled=true;requestAnimationFrame(fit);}};
 const observer=new ResizeObserver(schedule);for(const [selector,label]of targets){const element=document.querySelector(selector);if(element)observer.observe(element);const title=document.querySelector(label);if(title)new MutationObserver(schedule).observe(title,{childList:true,characterData:true,subtree:true});}
 window.addEventListener('resize',schedule);document.fonts?.ready.then(schedule);schedule();
}
