package com.household.homelibrary
import android.Manifest
import android.view.MotionEvent
import java.util.concurrent.TimeUnit
import android.content.pm.PackageManager
import android.hardware.camera2.CameraCharacteristics
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.camera2.interop.Camera2CameraInfo
import androidx.camera.camera2.interop.ExperimentalCamera2Interop
import androidx.camera.core.*
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.barcode.BarcodeScannerOptions
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.common.InputImage
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean

@androidx.annotation.OptIn(markerClass = [ExperimentalCamera2Interop::class, ExperimentalGetImage::class])
@Composable fun Scanner(api:NativeApi,kind:String,onRead:(String)->Unit,onClose:()->Unit){
 val context=LocalContext.current;val lifecycle=LocalLifecycleOwner.current
 var permitted by remember{mutableStateOf(ContextCompat.checkSelfPermission(context,Manifest.permission.CAMERA)==PackageManager.PERMISSION_GRANTED)}
 val permission=rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()){permitted=it}
 var provider by remember{mutableStateOf<ProcessCameraProvider?>(null)}
 var selected by remember{mutableStateOf(api.cameraId)};var expanded by remember{mutableStateOf(false)};var message by remember{mutableStateOf("")}
 val preview=remember{PreviewView(context)};val once=remember{AtomicBoolean(false)};val disposed=remember{AtomicBoolean(false)};var boundCamera by remember{mutableStateOf<Camera?>(null)}
 var scannedCount by remember{mutableIntStateOf(0)}
 var batchCode by remember{mutableStateOf("")};var batchSeen by remember{mutableStateOf(0L)}
 val detector=remember{BarcodeScanning.getClient(BarcodeScannerOptions.Builder().setBarcodeFormats(Barcode.FORMAT_EAN_13,Barcode.FORMAT_EAN_8,Barcode.FORMAT_UPC_A,Barcode.FORMAT_UPC_E,Barcode.FORMAT_CODE_128,Barcode.FORMAT_CODE_39,Barcode.FORMAT_QR_CODE).build())}
 val executor=remember{Executors.newSingleThreadExecutor()}
 LaunchedEffect(permitted){if(permitted){val future=ProcessCameraProvider.getInstance(context);future.addListener({if(!disposed.get())runCatching{provider=future.get()}.onFailure{message="Camera unavailable. Use manual entry."}},ContextCompat.getMainExecutor(context))}}
 DisposableEffect(Unit){onDispose{disposed.set(true);executor.shutdown();detector.close()}}
 DisposableEffect(provider,selected,permitted){
  val current=provider
  if(current!=null&&permitted){
   val cameraPreview=Preview.Builder().build().also{it.surfaceProvider=preview.surfaceProvider}
   val analysis=ImageAnalysis.Builder().setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST).build()
   analysis.setAnalyzer(executor){proxy->val image=proxy.image;if(image==null){proxy.close()}else{
    detector.process(InputImage.fromMediaImage(image,proxy.imageInfo.rotationDegrees)).addOnSuccessListener{codes->
     if(kind=="batch-isbn"&&codes.isEmpty()&&System.currentTimeMillis()-batchSeen>900)batchCode=""
     if(!disposed.get())for(code in codes){val raw=code.rawValue?:continue;val normalized=raw.trim().uppercase().replace(" ","")
      val value=when(kind){"pair"->raw.takeIf{it.startsWith("hl-pair:")};"patron"->raw.trim().takeIf{it.matches(Regex("carribrary:card:v1:[A-Za-z0-9_-]{43}"))};"isbn","batch-isbn"->normalized.takeIf{it.matches(Regex("[0-9]{9}[0-9X]|[0-9]{13}"))};else->normalized.takeIf{validCopyReference(it)||validCirculationISBN(it)||it.matches(Regex("BK-?[0-9]+"))}}
      if(value!=null&&kind=="batch-isbn"){batchSeen=System.currentTimeMillis();if(batchCode!=value){batchCode=value;scannedCount++;onRead(value);message="Queued #$scannedCount: $value. Move to the next book, or remove the barcode briefly before scanning another identical copy."};break}
      if(value!=null&&once.compareAndSet(false,true)){onRead(value);break}
     }
    }.addOnCompleteListener{proxy.close()}
   }}
   try{current.unbindAll();val selector=if(selected.isEmpty())CameraSelector.DEFAULT_BACK_CAMERA else CameraSelector.Builder().addCameraFilter{infos->infos.filter{Camera2CameraInfo.from(it).cameraId==selected}}.build();boundCamera=current.bindToLifecycle(lifecycle,selector,cameraPreview,analysis)}catch(_:Exception){try{boundCamera=current.bindToLifecycle(lifecycle,CameraSelector.DEFAULT_BACK_CAMERA,cameraPreview,analysis);message="Saved camera unavailable; using rear camera."}catch(_:Exception){message="Camera unavailable. Use manual entry."}}
  }
  onDispose{preview.setOnTouchListener(null);current?.unbindAll();boundCamera=null}
 }
 Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Row(Modifier.fillMaxWidth(),horizontalArrangement=Arrangement.SpaceBetween){Text("Scan ${if(kind=="pair")"pairing QR" else if(kind=="patron")"library card" else if(kind=="isbn")"ISBN" else "ISBN or copy label"}",style=MaterialTheme.typography.titleLarge);TextButton(onClick=onClose){Text("Close")}}
  if(!permitted){Text("Camera images are processed on this phone to read barcodes and pairing QR codes. Google ML Kit reports scanner diagnostics. You can close this screen and enter codes manually.");Button(onClick={permission.launch(Manifest.permission.CAMERA)}){Text("Allow camera")}}
  else AndroidView(factory={preview},modifier=Modifier.fillMaxWidth().weight(1f),update={view->view.setOnTouchListener{_,event->if(event.action==MotionEvent.ACTION_UP){val point=view.meteringPointFactory.createPoint(event.x,event.y);boundCamera?.cameraControl?.startFocusAndMetering(FocusMeteringAction.Builder(point).setAutoCancelDuration(5,TimeUnit.SECONDS).build());view.performClick()};true}})
  Text("Tap the preview to focus. Keep the whole barcode sharp and well lit; move the book farther away if needed.")
  Box{OutlinedButton(onClick={expanded=true}){Text("Select camera")};DropdownMenu(expanded=expanded,onDismissRequest={expanded=false}){provider?.availableCameraInfos?.forEach{info->val camera=Camera2CameraInfo.from(info);val id=camera.cameraId;val side=if(camera.getCameraCharacteristic(CameraCharacteristics.LENS_FACING)==CameraCharacteristics.LENS_FACING_BACK)"Rear" else "Front";DropdownMenuItem(text={Text("$side camera $id")},onClick={selected=id;expanded=false;message="Camera selected"})}}}
  OutlinedButton(onClick={if(selected.isNotEmpty()){api.cameraId=selected;message="Default camera saved"}else message="Select a camera first."}){Text("Use selected camera as default")}
  if(message.isNotEmpty())Text(message)
 }
}
