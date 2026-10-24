package com.household.homelibrary
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import kotlinx.coroutines.launch
import org.json.JSONObject
@Composable fun DataControlsDialog(api:NativeApi,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onCacheCleared:()->Unit,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var current by remember{mutableStateOf(snapshot)};var reader by remember{mutableStateOf("all")};var reading by remember{mutableStateOf(true)};var loans by remember{mutableStateOf(false)};var requests by remember{mutableStateOf(false)};var preview by remember{mutableStateOf<JSONObject?>(null)};var text by remember{mutableStateOf("")};var note by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var cacheConfirm by remember{mutableStateOf(false)};var menu by remember{mutableStateOf(false)}
 val profiles=current.optJSONObject("state")?.optJSONArray("readers")?.objects()?:emptyList()
 fun selection()=JSONObject().put("readerId",if(reader=="all")"" else reader).put("allReaders",reader=="all").put("reading",reading).put("loans",loans).put("requests",requests)
 fun task(work:suspend()->Unit){scope.launch{busy=true;note="";try{work()}catch(e:Exception){note=e.message?:"Could not complete this action. Preview again after refreshing."}finally{busy=false}}}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface{Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
 Text("Data & privacy",style=MaterialTheme.typography.titleLarge)
 Text("Clear completed history for a reader or the entire library. Active loans, holds, open requests and unfinished reading stay. Old private backups, exports and other devices may retain earlier data. No new pre-clear backup is made.")
 Box{OutlinedButton(enabled=!busy,onClick={menu=true}){Text(if(reader=="all")"Entire library history" else profiles.find{it.optString("id")==reader}?.optString("name")?:"Choose reader")};DropdownMenu(expanded=menu,onDismissRequest={menu=false}){DropdownMenuItem(text={Text("Entire library history")},onClick={reader="all";menu=false;preview=null});profiles.forEach{r->DropdownMenuItem(text={Text(r.optString("name"))},onClick={reader=r.optString("id");menu=false;preview=null})}}}
 for((label,value,change)in listOf(Triple("Finished reading, notes and ratings",reading,{v:Boolean->reading=v}),Triple("Returned loans and their linked closed requests",loans,{v:Boolean->loans=v}),Triple("Other closed requests",requests,{v:Boolean->requests=v})))Row{Checkbox(checked=value,onCheckedChange={change(it);preview=null});Text(label)}
 Button(enabled=!busy&&(reading||loans||requests),onClick={task{current=api.refresh();onChanged(current);preview=api.historyPreview(selection());text=""}}){Text("Preview removal")}
 preview?.let{p->Text("Remove ${p.optInt("reading")} finished reading entries, ${p.optInt("loans")} returned loans and ${p.optInt("requests")} closed requests. Keep ${p.optInt("protectedLoans")} active loans, ${p.optInt("protectedHolds")} holds and ${p.optInt("unfinishedReading")} unfinished reading entries.");OutlinedTextField(text,{text=it},label={Text("Type CLEAR HISTORY")});Button(enabled=!busy&&text=="CLEAR HISTORY",onClick={task{current=api.mutate("clearHistory",selection().put("confirmation",text).put("previewToken",p.optString("previewToken")),p.optLong("revision"));onChanged(current);preview=null;note="Selected completed history cleared"}}){Text("Clear this history permanently")}}
 HorizontalDivider()
 OutlinedButton(enabled=!busy,onClick={cacheConfirm=true}){Text("Clear downloaded library on this phone")}
 Text("Only this device’s encrypted catalog/sharing cache. Pairing and server data stay. Unsynced pending changes must be resolved first. Refresh downloads the library again.")
 OutlinedButton(enabled=!busy,onClick={task{val origin=api.tools("setup").optString("publicOrigin");val uri=Uri.parse(origin);require(uri.scheme=="https"&&!uri.host.isNullOrBlank()){"Configure public HTTPS first, or open the borrower account page on your library’s shared link."};context.startActivity(Intent(Intent.ACTION_VIEW,uri.buildUpon().encodedPath("/account/delete-help").clearQuery().fragment(null).build()))}}){Text("Borrower account / data deletion")}
 Text("Borrowers can delete their own accounts from My data & privacy in their browser. The admin companion uses device pairing, not a separate borrower sign-in.")
 if(busy)LinearProgressIndicator();if(note.isNotBlank())Text(note);TextButton(enabled=!busy,onClick=onClose){Text("Close")}
 }}}
 if(cacheConfirm)AlertDialog(onDismissRequest={cacheConfirm=false},title={Text("Clear downloaded data on this phone?")},text={Text("Server records and pairing stay. This cannot wipe another person’s device. Refresh downloads the catalog again.")},confirmButton={TextButton(onClick={try{api.clearCachedLibrary();cacheConfirm=false;onCacheCleared();onClose()}catch(e:Exception){cacheConfirm=false;note=e.message?:"Could not clear cache"}}){Text("Clear local downloads")}},dismissButton={TextButton(onClick={cacheConfirm=false}){Text("Cancel")}})
}
