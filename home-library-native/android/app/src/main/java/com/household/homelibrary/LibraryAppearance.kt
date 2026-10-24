package com.household.homelibrary

import android.content.Intent
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import androidx.compose.foundation.Image
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.core.content.FileProvider
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.File
import java.io.ByteArrayOutputStream
import java.net.URL
import java.text.Collator
import java.util.Locale

val libraryThemes=linkedMapOf("purple-blue" to "Purple & blue","classic" to "Teal & gold","forest" to "Forest & sage","rose" to "Rose & plum","ocean" to "Ocean blue")
fun libraryColors(theme:String):ColorScheme {
 val (primary,secondary,background)=when(theme){
  "classic"->Triple(0xFF153E46,0xFF805D12,0xFFF5F6EF)
  "forest"->Triple(0xFF244D36,0xFF526435,0xFFF0F5EF)
  "rose"->Triple(0xFF683A58,0xFF93506A,0xFFFCF2F6)
  "ocean"->Triple(0xFF174B73,0xFF24676D,0xFFEFF6FB)
  else->Triple(0xFF493276,0xFF365EA2,0xFFF2F1FC)
 }
 return lightColorScheme(primary=Color(primary),secondary=Color(secondary),background=Color(background),surface=Color(background),surfaceVariant=Color(secondary).copy(alpha=0.12f),onSurfaceVariant=Color(primary),primaryContainer=Color(primary).copy(alpha=0.12f),secondaryContainer=Color(secondary).copy(alpha=0.14f))
}
val sortLabels=linkedMapOf("title" to "Title","author" to "Author","series" to "Series","id" to "Catalog number")
fun sortedCatalog(books:List<JSONObject>,sort:String,descending:Boolean):List<JSONObject> {
 val collator=Collator.getInstance(Locale.getDefault()).apply{strength=Collator.PRIMARY}
 return books.sortedWith(Comparator{a,b->
  val av=a.optString(sort).trim();val bv=b.optString(sort).trim()
  if(av.isBlank()!=bv.isBlank())return@Comparator if(av.isBlank())1 else -1
  val compared=if(sort=="id")compareValues(av.removePrefix("BK-").toLongOrNull()?:Long.MAX_VALUE,bv.removePrefix("BK-").toLongOrNull()?:Long.MAX_VALUE) else collator.compare(av,bv)
  (if(descending)-compared else compared).takeIf{it!=0}?:collator.compare(a.optString("title"),b.optString("title")).takeIf{it!=0}?:a.optString("id").compareTo(b.optString("id"))
 })
}
private val covers=object:LruCache<String,Bitmap>(8*1024*1024){override fun sizeOf(key:String,value:Bitmap)=value.byteCount}
@Composable fun BookCover(book:JSONObject){
 val title=book.optString("title");val raw=book.optString("coverUrl").ifBlank{book.optString("isbn").takeIf{it.isNotBlank()}?.let{"https://covers.openlibrary.org/b/isbn/$it-M.jpg?default=false"}?:""}
 val url=if(raw.startsWith("http://"))"https://"+raw.removePrefix("http://")else raw
 // Recreate image state when a row changes identity or its cover URL changes.
 // produceState keys restart the producer but do not reset its remembered value.
 val context=androidx.compose.ui.platform.LocalContext.current
 val googleLink=runCatching{val uri=android.net.Uri.parse(url);val id=uri.getQueryParameter("id")?:"";if(uri.host=="books.google.com"&&id.matches(Regex("[A-Za-z0-9_-]{1,100}")))"https://books.google.com/books?id=$id" else ""}.getOrDefault("")
 Column{
  key(book.optString("id"),url){CoverImage(title,url)}
  if(googleLink.isNotEmpty())TextButton(onClick={context.startActivity(android.content.Intent(android.content.Intent.ACTION_VIEW,android.net.Uri.parse(googleLink)))},contentPadding=PaddingValues(2.dp)){
   Column{Image(androidx.compose.ui.res.painterResource(R.drawable.powered_by_google),"Powered by Google",Modifier.width(62.dp).height(30.dp));Text("Google Books",style=MaterialTheme.typography.labelSmall)}
  }
 }
}
@Composable private fun CoverImage(title:String,url:String){
 val bitmap by produceState<Bitmap?>(covers.get(url),url){
  if(value==null&&url.startsWith("https://"))value=withContext(Dispatchers.IO){runCatching{
   val connection=URL(url).openConnection().apply{connectTimeout=5000;readTimeout=7000}
   val data=connection.getInputStream().use{input->val output=ByteArrayOutputStream();val buffer=ByteArray(8192);var count=input.read(buffer);while(count!=-1){require(output.size()+count<=2*1024*1024);output.write(buffer,0,count);count=input.read(buffer)};output.toByteArray()}
   require(data.size<=2*1024*1024)
   val bounds=BitmapFactory.Options().apply{inJustDecodeBounds=true};BitmapFactory.decodeByteArray(data,0,data.size,bounds)
   require(bounds.outWidth>0&&bounds.outHeight>0)
   var scale=1;while(bounds.outWidth/scale>256||bounds.outHeight/scale>384)scale*=2
   BitmapFactory.decodeByteArray(data,0,data.size,BitmapFactory.Options().apply{inSampleSize=scale})?.also{covers.put(url,it)}
  }.getOrNull()}
 }
 if(bitmap!=null)Image(bitmap!!.asImageBitmap(),"Cover of $title",Modifier.width(72.dp).height(104.dp),contentScale=ContentScale.Fit)
 else Surface(Modifier.width(72.dp).height(104.dp),color=MaterialTheme.colorScheme.secondaryContainer,shape=MaterialTheme.shapes.small){Column(Modifier.padding(8.dp),verticalArrangement=Arrangement.Center){Text("▤",style=MaterialTheme.typography.headlineMedium);Text("No cover",style=MaterialTheme.typography.labelSmall)}}
}
fun catalogQr(url:String):Bitmap {
 val matrix=QRCodeWriter().encode(url,BarcodeFormat.QR_CODE,768,768,mapOf(EncodeHintType.MARGIN to 4,EncodeHintType.CHARACTER_SET to "UTF-8"))
 val pixels=IntArray(matrix.width*matrix.height){i->if(matrix[i%matrix.width,i/matrix.width])android.graphics.Color.BLACK else android.graphics.Color.WHITE}
 return Bitmap.createBitmap(pixels,matrix.width,matrix.height,Bitmap.Config.ARGB_8888)
}
@Composable fun ShareLibraryDialog(api:NativeApi,name:String,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope()
 var status by remember{mutableStateOf<JSONObject?>(null)};var error by remember{mutableStateOf("")};var busy by remember{mutableStateOf(true)};var confirmStop by remember{mutableStateOf(false)}
 suspend fun load(action:String?=null){busy=true;error="";try{status=api.sharing(action)}catch(e:Exception){error=e.message?:"Could not load sharing settings"}finally{busy=false}}
 LaunchedEffect(Unit){load()}
 val url=status?.optString("url")?:"";val qr=remember(url){if(url.isNotBlank())runCatching{catalogQr(url)}.getOrNull()else null}
 Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(androidx.compose.foundation.rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Share library",style=MaterialTheme.typography.headlineSmall)
  Text("Friends can view the full book list. Borrowers, contact details and loans stay private.")
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
  if(url.isNotBlank()&&qr!=null){
   Image(qr.asImageBitmap(),"QR code for the read-only book list",Modifier.fillMaxWidth().aspectRatio(1f))
   Text(url,style=MaterialTheme.typography.bodySmall)
   Text("Your library server must be running for friends to open the list.",style=MaterialTheme.typography.bodySmall)
   Button(onClick={context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).apply{type="text/plain";putExtra(Intent.EXTRA_TEXT,"$name — book list\n$url")},"Share book-list link"))}){Text("Share link")}
   OutlinedButton(onClick={try{
    val folder=File(context.cacheDir,"shared").apply{mkdirs()};val file=File(folder,"library-qr.png")
    file.outputStream().use{qr.compress(Bitmap.CompressFormat.PNG,100,it)}
    val uri=FileProvider.getUriForFile(context,context.packageName+".files",file)
    context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).apply{type="image/png";putExtra(Intent.EXTRA_STREAM,uri);putExtra(Intent.EXTRA_TEXT,"$name — book list\n$url");addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)},"Share book-list QR"))
   }catch(e:Exception){error=e.message?:"Could not share QR image"}}){Text("Share QR image")}
   TextButton(enabled=!busy,onClick={confirmStop=true}){Text("Stop sharing…")}
  }else if(status?.optBoolean("configured")==true){
   Text("Create a read-only link and QR for your library.")
   Button(enabled=!busy,onClick={scope.launch{load("enable")}}){Text("Create sharing QR")}
  }else if(status!=null){
   Text("Set up public HTTPS sharing on your library server first. On Windows, use Connection setup → Set up public book-list sharing; on Linux or Pi, follow the Linux setup guide. Forward TCP ports 80 and 443 to the server, then return here.")
  }
  if(error.isNotBlank())Text(error,color=MaterialTheme.colorScheme.error)
  TextButton(onClick=onClose){Text("Close")}
 }}}
 if(confirmStop)AlertDialog(onDismissRequest={confirmStop=false},title={Text("Stop sharing?")},text={Text("The current friend link and QR will stop working for everyone.")},confirmButton={TextButton(onClick={confirmStop=false;scope.launch{load("disable")}}){Text("Stop sharing")}},dismissButton={TextButton(onClick={confirmStop=false}){Text("Cancel")}})
}
