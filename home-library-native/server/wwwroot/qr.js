import {qrcodegen} from './qrcodegen.js';
export function qrSvg(text,privatePairing=false){
 const qr=qrcodegen.QrCode.encodeText(text,qrcodegen.QrCode.Ecc.MEDIUM),border=4,size=qr.size+border*2;
 const parts=[];for(let y=0;y<qr.size;y++)for(let x=0;x<qr.size;x++)if(qr.getModule(x,y))parts.push(`M${x+border},${y+border}h1v1h-1z`);
 return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}" role="img" aria-label="${privatePairing?'Private phone pairing QR code':'QR code for the read-only book catalog'}" shape-rendering="crispEdges"><rect width="100%" height="100%" fill="white"/><path d="${parts.join('')}" fill="black"/></svg>`;
}

export async function qrPng(text){
 const qr=qrcodegen.QrCode.encodeText(text,qrcodegen.QrCode.Ecc.MEDIUM),border=4,scale=8;
 const canvas=document.createElement('canvas');canvas.width=canvas.height=(qr.size+border*2)*scale;
 const context=canvas.getContext('2d');context.fillStyle='white';context.fillRect(0,0,canvas.width,canvas.height);context.fillStyle='black';
 for(let y=0;y<qr.size;y++)for(let x=0;x<qr.size;x++)if(qr.getModule(x,y))context.fillRect((x+border)*scale,(y+border)*scale,scale,scale);
 return new Promise((resolve,reject)=>canvas.toBlob(blob=>blob?resolve(blob):reject(Error('Could not create QR image.')),'image/png'));
}
