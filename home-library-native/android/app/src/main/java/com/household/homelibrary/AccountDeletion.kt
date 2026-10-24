package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import kotlinx.coroutines.launch
import org.json.JSONObject
import org.json.JSONArray

@Composable fun AccountDeletionDialog(api:NativeApi,account:JSONObject,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val scope=rememberCoroutineScope();var current by remember{mutableStateOf(snapshot)};var full by remember{mutableStateOf(false)};var reader by remember{mutableStateOf("")};var borrower by remember{mutableStateOf("")};var backups by remember{mutableStateOf(false)};var guests by remember{mutableStateOf(setOf<String>())};var guestDirty by remember{mutableStateOf(false)};var preview by remember{mutableStateOf<JSONObject?>(null)};var confirmation by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var deleted by remember{mutableStateOf(false)}
 fun input()=JSONObject().put("accountId",account.optString("id")).put("deleteInformation",full).put("readerId",if(full)reader else "").put("borrowerId",if(full)borrower else "").put("removeBackups",full&&backups).put("guestRequestIds",JSONArray(if(full)guests.toList() else emptyList<String>()))
 fun task(work:suspend()->Unit){scope.launch{busy=true;note="";try{work()}catch(e:Exception){note=e.message?:"Deletion failed. Refresh accounts and review before trying again.";if(e is ApiError&&e.status==409)preview=null}finally{busy=false}}}
 fun review(){task{preview=api.accountAdmin("deletion-preview",input());guestDirty=false;confirmation=""}}
 LaunchedEffect(Unit){task{current=api.refresh()}}
 val state=current.optJSONObject("state")?:JSONObject()
 Dialog(onDismissRequest={if(!busy)onClose()},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
 Row{Text("Delete borrower account",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(enabled=!busy,onClick=onClose){Text("Close")}}
 Text("@${account.optString("username")} · ${account.optString("name")}");if(busy)LinearProgressIndicator(Modifier.fillMaxWidth());if(note.isNotBlank())Text(note,color=MaterialTheme.colorScheme.error)
 Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
 if(deleted){Text("Account deleted. Refresh paired devices to remove old cached information.");Button(onClick=onClose){Text("Done")}}else{
 Row(verticalAlignment=Alignment.CenterVertically){RadioButton(selected=!full,enabled=!busy,onClick={full=false;guests=emptySet();preview=null});Text("Delete sign-in account and private list; keep lending records")}
 Row(verticalAlignment=Alignment.CenterVertically){RadioButton(selected=full,enabled=!busy,onClick={full=true;preview=null});Text("Delete account and linked personal information permanently")}
 if(full){Text("Linked records are included automatically. Add another profile/borrower only if it belongs to this person, especially for unlinked pending accounts.")
 DeletionChoice("Additional reader",reader,listOf("" to "No additional profile")+(state.optJSONArray("readers")?.objects()?:emptyList()).map{it.optString("id") to it.optString("name")},!busy){reader=it;guests=emptySet();preview=null}
 DeletionChoice("Additional borrower",borrower,listOf("" to "No additional borrower")+(state.optJSONArray("borrowers")?.objects()?:emptyList()).map{it.optString("id") to "${it.optString("name")} · ${it.optString("id")}${if(it.optBoolean("archived"))" (removed)" else ""}"},!busy){borrower=it;guests=emptySet();preview=null}
 Row(verticalAlignment=Alignment.CenterVertically){Checkbox(checked=backups,enabled=!busy,onCheckedChange={backups=it;preview=null});Text("Also delete ALL stored automatic library backups and legacy Windows safety ZIPs (whole installation); protected recovery points require Update and recovery")}
 }
 Text("Both scopes delete the encrypted private list and sign-in access. Export an encrypted personal backup first if wanted. Account-only keeps operational lending records. Books stay. Protected Windows recovery points are removed separately in Update and recovery. Full information deletion removes reading notes/ratings, requests and returned-loan history without Recycle Bin recovery. Return active loans first.")
 Text("Exported backups, emails and offline devices can retain older copies. Refresh paired devices. Stored backup cleanup cannot recall copies elsewhere or securely erase external storage.")
 OutlinedButton(enabled=!busy,onClick={review()}){Text(if(preview==null)"Preview deletion" else "Refresh preview")}
 preview?.let{p->Card(Modifier.fillMaxWidth()){Column(Modifier.padding(14.dp),verticalArrangement=Arrangement.spacedBy(10.dp)){
 Text("Review deletion",style=MaterialTheme.typography.titleLarge)
 if(full){Text("Borrowers: "+(p.optJSONArray("borrowers")?.objects()?.joinToString{"${it.optString("name")} · ${it.optString("id")}"}?.ifBlank{"None linked/selected"}?:"None"));Text("Readers: "+(p.optJSONArray("readers")?.objects()?.joinToString{it.optString("name")}?.ifBlank{"None linked/selected"}?:"None"));Text("${p.optInt("readingEntries")} reading entries · ${p.optInt("requests")} requests · ${p.optInt("returnedLoans")} returned loans · ${p.optInt("recoveryEntries")} recovery entries · ${p.optInt("holds")} holds released")
 if(p.optInt("activeLoans")>0)Text("Return ${p.optInt("activeLoans")} active loan(s) before deleting personal information.",color=MaterialTheme.colorScheme.error)
 val candidates=p.optJSONArray("guestRequests")?.objects()?:emptyList();if(candidates.isNotEmpty()){Text("Other guest requests with matching contact details: select only this person's. Shared contact does not prove ownership; unselected requests stay.");candidates.forEach{r->val id=r.optString("id");Row(verticalAlignment=Alignment.CenterVertically){Checkbox(checked=id in guests,enabled=!busy,onCheckedChange={check->guests=if(check)guests+id else guests-id;guestDirty=true});Text("${r.optString("name")} · ${r.optString("contact")} · ${r.optString("title")}")}};OutlinedButton(enabled=!busy&&guestDirty,onClick={review()}){Text("Update preview for selected guest requests")}}
 Text(if(backups)"Delete all ${p.optInt("storedBackups")} stored automatic/safety backups for the whole installation." else "Keep ${p.optInt("storedBackups")} stored backups; they may contain older information.")
 }else Text("Operational borrower, loans and requests stay. Sign-in credentials and the encrypted private list are deleted.")
 OutlinedTextField(confirmation,{confirmation=it},enabled=!busy,label={Text("Type username ${p.optString("username")} to confirm")},modifier=Modifier.fillMaxWidth())
 Button(enabled=!busy&&!guestDirty&&confirmation==p.optString("username")&&p.optInt("activeLoans")==0,onClick={val request=input().put("previewToken",p.optString("previewToken")).put("confirmation",confirmation);task{val result=api.accountAdmin("delete",request);result.optJSONObject("snapshot")?.let(onChanged);deleted=true;val warnings=result.optJSONArray("warnings");note=if(warnings==null)"" else (0 until warnings.length()).joinToString("\n"){warnings.optString(it)}}},colors=ButtonDefaults.buttonColors(containerColor=MaterialTheme.colorScheme.error)){Text("Delete permanently")}
 }}}
 }
 }
 }}}
}
@Composable private fun DeletionChoice(label:String,value:String,options:List<Pair<String,String>>,enabled:Boolean,onChange:(String)->Unit){var open by remember{mutableStateOf(false)};Text(label);Box{OutlinedButton(enabled=enabled,onClick={open=true}){Text(options.find{it.first==value}?.second?:"Choose")};DropdownMenu(expanded=open,onDismissRequest={open=false}){options.forEach{(id,text)->DropdownMenuItem(text={Text(text)},onClick={onChange(id);open=false})}}}}
