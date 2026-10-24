package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import kotlinx.coroutines.*
import org.json.JSONObject
import org.json.JSONArray
import java.net.URL

@Composable fun AcquisitionDialog(api:NativeApi,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val state=snapshot.optJSONObject("state")?:JSONObject();val items=state.optJSONArray("booksToBuy")?.objects()?:emptyList();val scope=rememberCoroutineScope()
 var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var edit by remember{mutableStateOf<JSONObject?>(null)};var acquire by remember{mutableStateOf<JSONObject?>(null)};var remove by remember{mutableStateOf<JSONObject?>(null)};var scan by remember{mutableStateOf(false)};var results by remember{mutableStateOf<JSONObject?>(null)}
 fun update(k:String,v:String){edit=JSONObject(edit?.toString()?:"{}").put(k,v);if(k=="isbn"){edit=JSONObject(edit.toString()).put("coverUrl","");results=null}}
 fun task(block:suspend()->Unit){if(busy)return;busy=true;note="";scope.launch{try{block()}catch(e:Exception){note=e.message?:"Could not save"}finally{busy=false}}}
 suspend fun save(action:String,input:JSONObject){val fresh=api.refresh();onChanged(api.mutate(action,input,fresh.optLong("revision")))}
 Dialog(onDismissRequest={if(!busy)onClose()},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(10.dp)){
 Row{Text("Books to buy",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(enabled=!busy,onClick=onClose){Text("Close")}}
 Text("Private household acquisition wishlist. No copy number is assigned until you own the book.")
 Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
 if(edit==null){Button(enabled=!busy,onClick={edit=JSONObject();results=null;note=""}){Text("Scan / add a book")};if(items.isEmpty())Text("Your wishlist is empty.")
 items.sortedBy{it.optString("title").lowercase()}.forEach{x->key(x.optString("id")){Card(Modifier.fillMaxWidth()){Column(Modifier.padding(12.dp)){Row(horizontalArrangement=Arrangement.spacedBy(10.dp)){BookCover(x);Column{Text(x.optString("title"),style=MaterialTheme.typography.titleMedium);Text(x.optString("author"));Text("ISBN "+x.optString("isbn"))}};if(x.optString("notes").isNotBlank())Text(x.optString("notes"));TextButton(enabled=!busy,onClick={edit=JSONObject(x.toString());results=null;note=""}){Text("Edit")};Button(enabled=!busy,onClick={acquire=x;note=""}){Text("Acquired — add to library")};TextButton(enabled=!busy,onClick={remove=x}){Text("Remove from wishlist")}}}}}
 }else{
 OutlinedButton(enabled=!busy,onClick={scan=true}){Text("Scan ISBN barcode")}
 for((field,label)in listOf("isbn" to "ISBN / EAN","title" to "Title","author" to "Author","publisher" to "Publisher","coverUrl" to "Cover URL (optional)","notes" to "Private acquisition notes"))OutlinedTextField(value=edit!!.optString(field),onValueChange={update(field,it)},label={Text(label)},enabled=!busy,modifier=Modifier.fillMaxWidth())
 OutlinedButton(enabled=!busy,onClick={task{val isbn=edit!!.optString("isbn").replace("-","").trim();require(isbn.matches(Regex("[0-9]{9}[0-9X]|[0-9]{13}"))){"Enter a valid ISBN first"};val info=withContext(Dispatchers.IO){val c=URL("https://openlibrary.org/api/books?bibkeys=ISBN:$isbn&format=json&jscmd=data").openConnection().apply{connectTimeout=5000;readTimeout=10000};c.getInputStream().use{JSONObject(it.bufferedReader().readText()).optJSONObject("ISBN:$isbn")}};if(info==null)note="No match found. Enter details manually." else{val next=JSONObject(edit.toString()).put("title",info.optString("title")).put("author",info.optJSONArray("authors")?.objects()?.joinToString(", "){it.optString("name")}?:"").put("publisher",info.optJSONArray("publishers")?.objects()?.joinToString(", "){it.optString("name")}?:"");if(next.optString("coverUrl").isBlank())next.put("coverUrl",info.optJSONObject("cover")?.optString("medium")?:"");edit=next;note="Review the details before saving."}}}){Text("Look up book details")}
 OutlinedButton(enabled=!busy,onClick={task{results=api.covers(edit!!.optString("isbn"),edit!!.optString("title"),edit!!.optString("author"));note=results?.optString("message")?:""}}){Text("Search Books for a cover")}
 results?.optJSONArray("covers")?.objects()?.forEach{c->Row{BookCover(JSONObject().put("title",c.optString("title")).put("coverUrl",c.optString("url")));TextButton(enabled=!busy,onClick={update("coverUrl",c.optString("url"))}){Text("Use this cover")}}}
 BookCover(edit!!)
 Button(enabled=!busy&&edit!!.optString("isbn").isNotBlank()&&edit!!.optString("title").isNotBlank(),onClick={task{save("saveAcquisition",edit!!);edit=null;results=null;note="Saved to wishlist; no physical copy added."}}){Text("Save to wishlist")};TextButton(enabled=!busy,onClick={edit=null;results=null}){Text("Back to wishlist")}
 }
 if(note.isNotBlank())Text(note)
 }
 }}}
 if(scan)Dialog(onDismissRequest={scan=false},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Scanner(api,"isbn",onRead={value->scan=false;edit=JSONObject(edit?.toString()?:"{}").put("isbn",value).put("title","").put("author","").put("publisher","").put("coverUrl","");results=null},onClose={scan=false})}}
 remove?.let{x->AlertDialog(onDismissRequest={remove=null},title={Text("Remove wishlist entry?")},text={Text(x.optString("title"))},confirmButton={TextButton(enabled=!busy,onClick={task{save("removeAcquisition",JSONObject().put("id",x.optString("id")));remove=null}}){Text("Remove")}},dismissButton={TextButton(enabled=!busy,onClick={remove=null}){Text("Cancel")}})}
 acquire?.let{x->var confirmed by remember(x.optString("id")){mutableStateOf(false)};var duplicatesConfirmed by remember(x.optString("id")){mutableStateOf(false)};var room by remember{mutableStateOf("")};var bookcase by remember{mutableStateOf("")};var shelf by remember{mutableStateOf("")};val duplicates=duplicates(state.optJSONArray("books")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList(),x)
 AlertDialog(onDismissRequest={if(!busy)acquire=null},title={Text("Add acquired book")},text={Column(Modifier.verticalScroll(rememberScrollState())){Text(x.optString("title"));Text("Added to General; you can change its collection in Edit book.");for((value,label,setter)in listOf(Triple(room,"Room (private)",{v:String->room=v}),Triple(bookcase,"Bookcase (private)",{v:String->bookcase=v}),Triple(shelf,"Shelf (private)",{v:String->shelf=v})))OutlinedTextField(value=value,onValueChange=setter,label={Text(label)});Row{Checkbox(confirmed,{confirmed=it});Text("I now own this physical book")};if(duplicates.isNotEmpty()){Text("Possible owned copies: "+duplicates.joinToString{it.optString("id")});Row{Checkbox(duplicatesConfirmed,{duplicatesConfirmed=it});Text("Add another physical copy")}};if(note.isNotBlank())Text(note)}},confirmButton={TextButton(enabled=!busy&&confirmed&&(duplicates.isEmpty()||duplicatesConfirmed),onClick={task{save("acquireBook",JSONObject().put("id",x.optString("id")).put("purchaseConfirmed",true).put("confirmedDuplicates",JSONArray(duplicates.map{it.optString("id")})).put("room",room).put("bookcase",bookcase).put("shelf",shelf));acquire=null;note="Physical copy added; wishlist entry removed."}}){Text("Add physical copy")}},dismissButton={TextButton(enabled=!busy,onClick={acquire=null}){Text("Cancel")}})
 }
}
