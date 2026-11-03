package com.household.homelibrary
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import android.util.Base64
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.ByteArrayOutputStream

@Composable fun LibraryBrandIcon(state:JSONObject){val data=state.optString("libraryIcon192");val bitmap=remember(data){if(data.isBlank())null else runCatching{val b=Base64.decode(data,Base64.DEFAULT);BitmapFactory.decodeByteArray(b,0,b.size)}.getOrNull()};if(bitmap!=null)Image(bitmap.asImageBitmap(),"Library icon",Modifier.size(40.dp))}
@Composable fun LibraryBrandingControls(api:NativeApi,onChanged:(JSONObject)->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var browsers by remember{mutableStateOf<JSONObject?>(null)};var pair by remember{mutableStateOf<JSONObject?>(null)}
 fun task(block:suspend()->Unit){scope.launch{busy=true;note="";try{block()}catch(e:Exception){note=e.message?:"Could not save changes"}finally{busy=false}}}
 val choose=rememberLauncherForActivityResult(ActivityResultContracts.GetContent()){uri->if(uri!=null)task{val icons=withContext(Dispatchers.IO){val bytes=context.contentResolver.openInputStream(uri)!!.use{input->val output=ByteArrayOutputStream();val b=ByteArray(8192);while(true){val n=input.read(b);if(n<0)break;require(output.size()+n<=10*1024*1024){"Choose an image under 10 MB."};output.write(b,0,n)};output.toByteArray()};val bounds=BitmapFactory.Options().apply{inJustDecodeBounds=true};BitmapFactory.decodeByteArray(bytes,0,bytes.size,bounds);require(bounds.outWidth>0&&bounds.outHeight>0&&bounds.outWidth.toLong()*bounds.outHeight<=20000000){"Choose a smaller PNG, JPEG or WebP image."};val bitmap=BitmapFactory.decodeByteArray(bytes,0,bytes.size)?:error("Could not read image");val result=JSONObject();for(size in listOf(192,512)){val square=Bitmap.createBitmap(size,size,Bitmap.Config.ARGB_8888);val canvas=Canvas(square);val scale=minOf(size.toFloat()/bitmap.width,size.toFloat()/bitmap.height);val width=bitmap.width*scale;val height=bitmap.height*scale;canvas.drawBitmap(bitmap,null,RectF((size-width)/2,(size-height)/2,(size+width)/2,(size+height)/2),Paint(Paint.FILTER_BITMAP_FLAG));val output=ByteArrayOutputStream();square.compress(Bitmap.CompressFormat.PNG,100,output);square.recycle();require(output.size()<=2097152){"Resized icon exceeds 2 MiB. Choose a smaller source image."};result.put("icon$size",Base64.encodeToString(output.toByteArray(),Base64.NO_WRAP))};bitmap.recycle();result};val fresh=api.refresh();onChanged(api.mutate("setLibraryIcon",icons,fresh.optLong("revision")));note="Library icon saved. Home-screen icons may need removing and adding again. The native Android launcher icon stays Carribrary."}}
 OutlinedButton(enabled=!busy,onClick={choose.launch("image/*")}){Text("Choose library icon")}
 Text("Transparent backgrounds are preserved. Re-upload the original image if an older version saved a white background.")
 TextButton(enabled=!busy,onClick={task{val fresh=api.refresh();onChanged(api.mutate("setLibraryIcon",JSONObject().put("icon192","").put("icon512",""),fresh.optLong("revision")));note="Default library icon restored"}}){Text("Use default library icon")}
 if(note.isNotBlank())Text(note)
}

@Composable fun BrowserAdminPairingDialog(api:NativeApi,onClose:()->Unit){
 val scope=rememberCoroutineScope();var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var browsers by remember{mutableStateOf<JSONObject?>(null)};var pair by remember{mutableStateOf<JSONObject?>(null)}
 fun task(block:suspend()->Unit){scope.launch{busy=true;note="";try{block()}catch(e:Exception){note=e.message?:"Could not update pairing"}finally{busy=false}}}
 LaunchedEffect(Unit){task{browsers=api.browserAdmins()}}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){Text("iPhone companion",style=MaterialTheme.typography.titleLarge);Text("In Safari open your library HTTPS hostname plus /companion/. Share → Add to Home Screen. Open that icon, then paste a private pairing code. Grants full administration for 30 days.");Button(enabled=!busy,onClick={task{pair=api.browserAdmins("pair",JSONObject())}}){Text("Create private pairing code")};pair?.let{p->val qr=remember(p.optString("url")){catalogQr(p.optString("url"))};Image(qr.asImageBitmap(),"Private iPhone pairing QR",Modifier.size(220.dp));SelectionContainer{Text(p.optString("code"))};Text("Single use, expires in 10 minutes. Keep private.")};(browsers?.optJSONArray("devices")?.objects()?:emptyList()).forEach{d->Text(d.optString("name")+if(d.optBoolean("revoked"))" · Revoked / expired" else " · Active");if(!d.optBoolean("revoked"))TextButton(enabled=!busy,onClick={task{api.browserAdmins("revoke",JSONObject().put("deviceId",d.optString("id")));browsers=api.browserAdmins()}}){Text("Revoke browser")}};if(note.isNotBlank())Text(note);TextButton(onClick=onClose){Text("Close")}}}}
}
