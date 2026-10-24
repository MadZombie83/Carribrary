export function cameraConstraints(deviceId='') {
  return {video:{...(deviceId?{deviceId:{exact:deviceId}}:{facingMode:{ideal:'environment'}}),width:{ideal:1920},height:{ideal:1080}},audio:false};
}
export async function enableAutofocus(track) {
  try {
    const modes=track.getCapabilities?.().focusMode||[];
    const mode=modes.includes('continuous')?'continuous':modes.includes('single-shot')?'single-shot':null;
    if(!mode||!track.applyConstraints)return false;
    const current=track.getConstraints?.()||{};
    await track.applyConstraints({...current,advanced:[...(current.advanced||[]),{focusMode:mode}]});
    return true;
  } catch {return false;}
}

const CAMERA_KEY='home-library-preferred-camera-v1';
export function preferredCamera(storage){
  try{return (storage??globalThis.localStorage).getItem(CAMERA_KEY)||'';}catch{return '';}
}
export function rememberCamera(deviceId,storage){
  if(!deviceId)return false;
  try{const target=storage??globalThis.localStorage;target.setItem(CAMERA_KEY,deviceId);return target.getItem(CAMERA_KEY)===deviceId;}catch{return false;}
}
export async function openCamera(mediaDevices,deviceId=''){
  try{return await mediaDevices.getUserMedia(cameraConstraints(deviceId));}
  catch(error){
    // A removed/renumbered camera should not strand a saved preference.
    // Permission errors are reported without retrying another camera.
    if(!deviceId||!['OverconstrainedError','NotFoundError'].includes(error.name))throw error;
    return await mediaDevices.getUserMedia(cameraConstraints());
  }
}
