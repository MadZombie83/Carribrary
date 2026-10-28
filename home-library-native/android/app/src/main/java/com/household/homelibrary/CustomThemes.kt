package com.household.homelibrary

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.ui.platform.LocalContext
import androidx.compose.foundation.Image
import androidx.compose.ui.graphics.asImageBitmap
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.window.Dialog
import org.json.JSONObject

val paletteFields=linkedMapOf("ink" to "Banner, buttons & links","accent" to "Banner accent","page" to "Page background","surface" to "Cards & dialogs","soft" to "Soft highlights","line" to "Borders","text" to "Main text","muted" to "Secondary text")
fun validHex(value:String)=value.matches(Regex("#[a-fA-F0-9]{6}"))
fun paletteColor(value:String)=Color(android.graphics.Color.parseColor(value))
fun paletteContrast(a:String,b:String):Double {
 fun lum(hex:String):Double{val values=listOf(1,3,5).map{hex.substring(it,it+2).toInt(16)/255.0}.map{if(it<=0.04045)it/12.92 else Math.pow((it+0.055)/1.055,2.4)};return values[0]*0.2126+values[1]*0.7152+values[2]*0.0722}
 val x=lum(a);val y=lum(b);return (maxOf(x,y)+0.05)/(minOf(x,y)+0.05)
}
fun readablePalette(p:JSONObject):Boolean=paletteFields.keys.all{validHex(p.optString(it))}&&listOf("page","surface","soft").all{bg->listOf("text","muted","ink").all{fg->paletteContrast(p.getString(fg),p.getString(bg))>=4.5}}&&paletteContrast(p.getString("ink"),"#ffffff")>=4.5&&paletteContrast(p.getString("ink"),p.getString("accent"))>=4.5
fun defaultPalette(theme:String="purple-blue"):JSONObject { val p=JSONObject("""{"ink":"#493276","accent":"#c5dcff","page":"#f2f1fc","surface":"#ffffff","soft":"#e7eafb","line":"#d9d4ec","text":"#27273d","muted":"#55596a"}""")
 val colors=when(theme){
  "classic"->listOf("#153e46","#f7d58b","#f3f5f5","#edf3f3","#d7e1e2")
  "forest"->listOf("#244d36","#d9e7b8","#f0f5ef","#e5efe2","#ccddcc")
  "rose"->listOf("#683a58","#ffd0dc","#fcf2f6","#f7e6ee","#e7d2dc")
  "ocean"->listOf("#174b73","#aee9ef","#eff6fb","#e0f0f8","#cedeea")
  else->null
 }
 if(colors!=null)listOf("ink","accent","page","soft","line").zip(colors).forEach{(key,value)->p.put(key,value)}
 return p
}

@Composable fun CustomThemeDialog(state:JSONObject,theme:String,online:Boolean,busy:Boolean,message:String,onPreset:(String)->Unit,onSave:(JSONObject)->Unit,onPublish:(String)->Unit,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var artworkBusy by remember{mutableStateOf(false)};var artworkNote by remember{mutableStateOf("")}
 val saved=state.optJSONArray("savedThemes")?.objects()?:emptyList()
 fun withArtwork(p:JSONObject)=JSONObject(p.toString()).apply{if(!has("sidebarArtwork")||isNull("sidebarArtwork"))put("sidebarArtwork","" )}
 var draft by remember{mutableStateOf(saved.find{"custom-"+it.optString("id")==theme}?.let{withArtwork(it)}?:defaultPalette(theme).put("sidebarArtwork",""))}
 var selectedColor by remember{mutableStateOf("")}
 fun update(key:String,value:String){draft=JSONObject(draft.toString()).put(key,value)}
 val chooseArtwork=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()){uri->if(uri!=null)scope.launch{artworkBusy=true;artworkNote="";try{val value=withContext(Dispatchers.IO){val bytes=context.contentResolver.openInputStream(uri)!!.use{input->val output=java.io.ByteArrayOutputStream();val buffer=ByteArray(8192);while(true){val n=input.read(buffer);if(n<0)break;require(output.size()+n<=10*1024*1024){"Choose a PNG under 10 MB."};output.write(buffer,0,n)};output.toByteArray()};val options=android.graphics.BitmapFactory.Options().apply{inJustDecodeBounds=true};android.graphics.BitmapFactory.decodeByteArray(bytes,0,bytes.size,options);require(options.outMimeType=="image/png"&&options.outWidth in 1..8192&&options.outHeight in 1..8192&&options.outWidth.toLong()*options.outHeight<=20000000){"Choose a PNG under 20 million pixels."};val image=android.graphics.BitmapFactory.decodeByteArray(bytes,0,bytes.size)?:error("Cannot read image");val canvasBitmap=android.graphics.Bitmap.createBitmap(512,1536,android.graphics.Bitmap.Config.ARGB_8888);try{val scale=minOf(512f/image.width,1536f/image.height);val width=image.width*scale;val height=image.height*scale;android.graphics.Canvas(canvasBitmap).drawBitmap(image,null,android.graphics.RectF((512-width)/2,(1536-height)/2,(512+width)/2,(1536+height)/2),android.graphics.Paint(android.graphics.Paint.FILTER_BITMAP_FLAG));val output=java.io.ByteArrayOutputStream();canvasBitmap.compress(android.graphics.Bitmap.CompressFormat.PNG,100,output);require(output.size()<=2097152){"Try a simpler sidebar image."};android.util.Base64.encodeToString(output.toByteArray(),android.util.Base64.NO_WRAP)}finally{image.recycle();canvasBitmap.recycle()}};update("sidebarArtwork",value);artworkNote="Ready to save with this theme."}catch(e:Exception){artworkNote=e.message?:"Could not prepare PNG"}finally{artworkBusy=false}}}

 Dialog(onDismissRequest={if(!busy&&!artworkBusy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Theme & artwork",style=MaterialTheme.typography.headlineSmall)
  Text("Presets change this phone. Save a custom palette to the server to use across the library. Use your colors for shared library in Settings publishes a selected preset.",style=MaterialTheme.typography.bodySmall)
  libraryThemes.forEach{(key,label)->OutlinedButton(enabled=!busy&&!artworkBusy,onClick={draft=defaultPalette(key).put("sidebarArtwork","");onPreset(key)},modifier=Modifier.fillMaxWidth()){Text((if(theme==key)"✓ " else "")+label)}}
  if(saved.isNotEmpty()){Text("Saved custom themes",style=MaterialTheme.typography.titleMedium);saved.forEach{p->Row{TextButton(enabled=!busy&&!artworkBusy,onClick={draft=withArtwork(p);selectedColor="";onPreset("custom-"+p.getString("id"))},modifier=Modifier.weight(1f)){Text(p.optString("name"))};TextButton(enabled=!busy&&!artworkBusy,onClick={draft=withArtwork(p)}){Text("Edit")}}}}
  Text("Each saved theme includes its sidebar PNG or a no-artwork choice. Applying it to the shared library restores colors and artwork together. New theme clears the artwork draft; use current library artwork to copy the current overlay. Phone preset selection remains local until published.",style=MaterialTheme.typography.bodySmall)
  OutlinedButton(enabled=!busy&&!artworkBusy,onClick={onPublish(theme)}){Text("Apply phone's selected theme to shared library")}
  Text("Apply uses the selected saved theme or preset; save draft edits below to publish them. Built-in presets clear the shared sidebar artwork.",style=MaterialTheme.typography.bodySmall)
  val imagePreview=remember(draft.optString("sidebarArtwork")){runCatching{val bytes=android.util.Base64.decode(draft.optString("sidebarArtwork"),android.util.Base64.DEFAULT);android.graphics.BitmapFactory.decodeByteArray(bytes,0,bytes.size)?.asImageBitmap()}.getOrNull()}
  imagePreview?.let{Image(it,"Sidebar artwork preview",Modifier.width(64.dp).height(192.dp))}
  OutlinedButton(enabled=!busy&&!artworkBusy,onClick={chooseArtwork.launch(arrayOf("image/png"))}){Text("Choose sidebar PNG")}
  if(artworkNote.isNotBlank())Text(artworkNote)
  Text(if(draft.optString("sidebarArtwork").isBlank())"Sidebar: no artwork" else "Sidebar: saved PNG included")
  OutlinedButton(enabled=!busy&&!artworkBusy,onClick={update("sidebarArtwork",state.optString("librarySidebarArtwork"))}){Text("Use current library artwork")}
  TextButton(enabled=!busy&&!artworkBusy,onClick={update("sidebarArtwork","")}){Text("No artwork for this theme")}
  HorizontalDivider();Text("Custom palette",style=MaterialTheme.typography.titleLarge)
  OutlinedTextField(draft.optString("name"),{if(it.length<=60)update("name",it)},label={Text("Theme name")},modifier=Modifier.fillMaxWidth(),enabled=!busy&&!artworkBusy,singleLine=true)
  paletteFields.forEach{(key,label)->OutlinedButton(enabled=!busy&&!artworkBusy,onClick={selectedColor=key},modifier=Modifier.fillMaxWidth()){Box(Modifier.size(24.dp).background(paletteColor(draft.optString(key))));Spacer(Modifier.width(8.dp));Text("$label · ${draft.optString(key)}",modifier=Modifier.weight(1f))}}
  val preview=libraryColors("custom-preview",draft.toString())
  MaterialTheme(colorScheme=preview){OutlinedCard(colors=CardDefaults.outlinedCardColors(containerColor=preview.surface)){Column(Modifier.padding(12.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){Surface(color=preview.primary,contentColor=preview.onPrimary,modifier=Modifier.fillMaxWidth()){Text("Library preview",Modifier.padding(12.dp))};Text("Book title · Author",color=preview.onSurface);Text("Secondary text",color=preview.onSurfaceVariant);Button(onClick={}){Text("Check out")}}}}
  val readable=readablePalette(draft)
  Text(if(readable)"Text contrast passes (at least 4.5:1)." else "Increase text contrast on page, cards and highlights. Banner needs readable white and accent text.",color=if(readable)MaterialTheme.colorScheme.onSurfaceVariant else MaterialTheme.colorScheme.error)
  Button(enabled=online&&!busy&&!artworkBusy&&readable&&draft.optString("name").isNotBlank(),onClick={onSave(JSONObject(draft.toString()))},modifier=Modifier.fillMaxWidth()){Text(if(draft.has("id"))"Update & use theme" else "Save & use theme")}
  TextButton(enabled=!busy&&!artworkBusy,onClick={selectedColor="";draft=JSONObject(draft.toString()).apply{remove("id");put("name","");put("sidebarArtwork","")}}){Text("New theme")}
  if(!online)Text("Connect to the server to save a custom theme.")
  if(message.isNotBlank())Text(message,color=MaterialTheme.colorScheme.error)
  TextButton(enabled=!busy&&!artworkBusy,onClick=onClose){Text("Done")}
 }}}
 if(selectedColor.isNotBlank())FullColorPicker(paletteFields.getValue(selectedColor),draft.getString(selectedColor),onSave={update(selectedColor,it);selectedColor=""},onClose={selectedColor=""})
}
@Composable private fun FullColorPicker(label:String,initial:String,onSave:(String)->Unit,onClose:()->Unit){
 val initialHsv=remember(initial){FloatArray(3).also{android.graphics.Color.colorToHSV(android.graphics.Color.parseColor(initial),it)}}
 var hue by remember{mutableStateOf(initialHsv[0])};var saturation by remember{mutableStateOf(initialHsv[1])};var brightness by remember{mutableStateOf(initialHsv[2])};var hex by remember{mutableStateOf(initial)}
 fun sliders(){hex=String.format("#%06X",android.graphics.Color.HSVToColor(floatArrayOf(hue,saturation,brightness)) and 0xffffff)}
 AlertDialog(onDismissRequest=onClose,title={Text(label)},text={Column(Modifier.verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(8.dp)){
  Box(Modifier.fillMaxWidth().height(64.dp).background(if(validHex(hex))paletteColor(hex) else Color.Transparent))
  Text("Hue");Slider(hue,{hue=it;sliders()},modifier=Modifier.semantics{contentDescription="$label hue"},valueRange=0f..360f)
  Text("Saturation");Slider(saturation,{saturation=it;sliders()},modifier=Modifier.semantics{contentDescription="$label saturation"})
  Text("Brightness");Slider(brightness,{brightness=it;sliders()},modifier=Modifier.semantics{contentDescription="$label brightness"})
  OutlinedTextField(hex,{value->hex=value.take(7);if(validHex(hex)){val hsv=FloatArray(3);android.graphics.Color.colorToHSV(android.graphics.Color.parseColor(hex),hsv);hue=hsv[0];saturation=hsv[1];brightness=hsv[2]}},label={Text("Hex color #RRGGBB")},isError=!validHex(hex),singleLine=true)
 }},confirmButton={TextButton(enabled=validHex(hex),onClick={onSave(hex)}){Text("Use color")}},dismissButton={TextButton(onClick=onClose){Text("Cancel")}})
}
