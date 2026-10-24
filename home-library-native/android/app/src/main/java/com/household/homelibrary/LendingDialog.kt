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
import kotlinx.coroutines.launch
import org.json.JSONObject
import org.json.JSONArray
import java.time.LocalDate

@OptIn(ExperimentalLayoutApi::class)
@Composable fun LendingDialog(api:NativeApi,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val scope=rememberCoroutineScope();var current by remember{mutableStateOf(snapshot)};var tab by remember{mutableStateOf("holds")};var query by remember{mutableStateOf("")};var selected by remember{mutableStateOf(listOf<String>())};var borrower by remember{mutableStateOf("")};var due by remember{mutableStateOf("")};var checkoutDate by remember{mutableStateOf(LocalDate.now().toString())};var confirmed by remember{mutableStateOf(false)};var scanning by remember{mutableStateOf(false)};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var handover by remember{mutableStateOf<JSONObject?>(null)};var release by remember{mutableStateOf<JSONObject?>(null)};var batchLimit by remember{mutableStateOf("")};var activeLimit by remember{mutableStateOf("")}
 val state=current.optJSONObject("state")?:JSONObject();val books=state.optJSONArray("books")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList();val people=state.optJSONArray("borrowers")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList();val holds=state.optJSONArray("holds")?.objects()?:emptyList();val loans=state.optJSONArray("loans")?.objects()?.filter{it.isNull("returnedDate")}?:emptyList();val requests=state.optJSONArray("requests")?.objects()?:emptyList()
 fun task(work:suspend()->Unit){scope.launch{busy=true;note="";try{work()}catch(e:Exception){note=e.message?:"Reconnect and try again.";if(e is ApiError&&e.status==409)runCatching{current=api.refresh();onChanged(current)}}finally{busy=false}}}
 fun save(action:String,input:JSONObject,done:()->Unit={}){task{current=api.mutate(action,input,current.optLong("revision"));onChanged(current);note="Saved";done()}}
 LaunchedEffect(Unit){task{current=api.refresh();onChanged(current);batchLimit=(current.optJSONObject("state")?.optInt("maxBatchCheckout",5)?:5).toString();activeLimit=(current.optJSONObject("state")?.optInt("maxActiveLoans",5)?:5).toString()}}
 fun matches(value:String)=circulationMatches(books.map{b->CirculationCopy(b.optString("id"),b.optString("isbn"),b.optString("title"),b.optString("author"),loans.any{it.optString("copyId")==b.optString("id")},copyUid=b.optString("copyUid"),onHold=holds.any{it.optString("copyId")==b.optString("id")})},value,tab=="return").filter{it.id !in selected}
 fun add(id:String){if(id in selected){note="That copy is already selected.";return};if(tab=="checkout"&&selected.size>=state.optInt("maxBatchCheckout",5)){note="This checkout batch has reached its limit.";return};selected=selected+id;query="";note=""}
 Dialog(onDismissRequest={if(!busy)onClose()},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(10.dp)){
 Row{Text("Lending & holds",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(enabled=!busy,onClick=onClose){Text("Close")}}
 if(busy)LinearProgressIndicator(Modifier.fillMaxWidth());if(note.isNotBlank())Text(note,color=MaterialTheme.colorScheme.primary)
 FlowRow{for((id,label)in listOf("holds" to "Holds","checkout" to "Batch checkout","return" to "Batch return","limits" to "Limits"))TextButton(enabled=!busy,onClick={tab=id;query="";selected=emptyList();confirmed=false}){Text(if(tab==id)"✓ $label" else label)}}
 Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
 if(tab=="holds"){
 Text("Holds reserve physical copies. No loan or due date starts until handover. Review pending requests under More → Requests.")
 holds.forEach{h->val r=requests.find{it.optString("id")==h.optString("requestId")};val p=people.find{it.optString("id")==h.optString("borrowerId")};Card(Modifier.fillMaxWidth()){Column(Modifier.padding(12.dp)){Text(books.find{it.optString("id")==h.optString("copyId")}?.optString("title")?:h.optString("copyId"),style=MaterialTheme.typography.titleMedium);Text("${h.optString("copyId")} · ${r?.optString("name")?:p?.optString("name")?:"Borrower"} · ${h.optString("stage")}");if(h.optString("stage")!="pending"){TextButton(enabled=!busy,onClick={save("readyHold",JSONObject().put("holdId",h.optString("id")))}){Text("Ready for pickup / delivery")};TextButton(enabled=!busy,onClick={handover=h;due="";checkoutDate=LocalDate.now().toString();confirmed=false}){Text("Confirm handover")}};TextButton(enabled=!busy,onClick={release=h}){Text("Release hold")}}}}
 if(holds.isEmpty())Text("No active holds.")
 Text("Hold a book for someone",style=MaterialTheme.typography.titleMedium)
 OutlinedTextField(query,{query=it},label={Text("ISBN, copy ID, title or author")},modifier=Modifier.fillMaxWidth())
 OutlinedButton(enabled=!busy,onClick={scanning=true}){Text("Scan barcode")}
 LendingChoice("Borrower",borrower,people.map{it.optString("id") to it.optString("name")}){borrower=it}
 // Hold creation always uses available copies, even if the last batch tab was Return.
 val available=circulationMatches(books.map{b->CirculationCopy(b.optString("id"),b.optString("isbn"),b.optString("title"),b.optString("author"),loans.any{it.optString("copyId")==b.optString("id")},copyUid=b.optString("copyUid"),onHold=holds.any{it.optString("copyId")==b.optString("id")})},query,false)
 available.forEach{b->OutlinedButton(enabled=!busy&&borrower.isNotBlank(),onClick={save("createHold",JSONObject().put("copyId",b.id).put("borrowerId",borrower)){query=""}}){Text("Hold ${b.id} · ${b.title}")}}
 }else if(tab=="limits"){
 Text("Separate limits for one checkout batch and a borrower’s total active loans. Choose 1–100. Returns are not restricted by borrowing limits. Existing loans are kept.")
 OutlinedTextField(batchLimit,{batchLimit=it},label={Text("Maximum books per checkout batch")});OutlinedTextField(activeLimit,{activeLimit=it},label={Text("Maximum active loans per borrower")})
 Button(enabled=!busy,onClick={val b=batchLimit.toIntOrNull();val a=activeLimit.toIntOrNull();if(b==null||a==null||b !in 1..100||a !in 1..100)note="Choose limits from 1 to 100." else save("setLendingLimits",JSONObject().put("maxBatchCheckout",b).put("maxActiveLoans",a))}){Text("Save lending limits")}
 }else{
 Text(if(tab=="checkout")"Select up to ${state.optInt("maxBatchCheckout",5)} available copies for one borrower. Hand held books over from Holds / Requests." else "Select checked-out copies to return. No borrowing limit applies to returns.")
 OutlinedTextField(query,{query=it},label={Text("ISBN, copy ID, title or author")},modifier=Modifier.fillMaxWidth());OutlinedButton(enabled=!busy,onClick={scanning=true}){Text("Scan barcode")}
 matches(query).forEach{b->OutlinedButton(enabled=!busy,onClick={add(b.id)}){Text("Add ${b.id} · ${b.title}")}}
 Text("Review ${selected.size} selected books",style=MaterialTheme.typography.titleMedium)
 selected.forEach{id->Row{Text("$id · ${books.find{it.optString("id")==id}?.optString("title")?:""}",Modifier.weight(1f));TextButton(enabled=!busy,onClick={selected=selected-id}){Text("Remove")}}}
 if(tab=="checkout"){LendingChoice("Borrower",borrower,people.map{it.optString("id") to it.optString("name")}){borrower=it};OutlinedTextField(checkoutDate,{checkoutDate=it},label={Text("Handover date YYYY-MM-DD")});OutlinedTextField(due,{due=it},label={Text("Due YYYY-MM-DD (optional)")});Row{Checkbox(checked=confirmed,onCheckedChange={confirmed=it});Text("The borrower has received these books.")}}
 Text("The batch saves together. If any book conflicts, no books in the batch change.")
 Button(enabled=!busy&&selected.isNotEmpty()&&(tab=="return"||borrower.isNotBlank()&&confirmed),onClick={save(if(tab=="return")"batchReturn" else "batchCheckout",JSONObject().put("copyIds",JSONArray(selected)).put("borrowerId",borrower).put("checkoutDate",checkoutDate).put("dueDate",due).put("handoverConfirmed",confirmed)){selected=emptyList();confirmed=false}}){Text(if(tab=="return")"Confirm batch return" else "Confirm batch checkout")}
 }
 }
 }}}
 if(scanning)Dialog(onDismissRequest={scanning=false},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Scanner(api,"copy",onRead={value->scanning=false;query=value;if(tab in listOf("checkout","return")){val found=matches(value);if(found.size==1)add(found[0].id)}},onClose={scanning=false})}}
 release?.let{h->AlertDialog(onDismissRequest={release=null},title={Text("Release this hold?")},text={Text("The copy will become available. A linked request will be declined.")},confirmButton={TextButton(enabled=!busy,onClick={save("releaseHold",JSONObject().put("holdId",h.optString("id"))){release=null}}){Text("Release hold")}},dismissButton={TextButton(enabled=!busy,onClick={release=null}){Text("Cancel")}})}
 handover?.let{h->AlertDialog(onDismissRequest={if(!busy)handover=null},title={Text("Confirm physical handover")},text={Column{Text(books.find{it.optString("id")==h.optString("copyId")}?.optString("title")?:h.optString("copyId"));OutlinedTextField(checkoutDate,{checkoutDate=it},label={Text("Handover date YYYY-MM-DD")});OutlinedTextField(due,{due=it},label={Text("Due date (optional)")});Row{Checkbox(checked=confirmed,onCheckedChange={confirmed=it});Text("The borrower has received this book.")};if(note.isNotBlank())Text(note)}},confirmButton={TextButton(enabled=!busy&&confirmed,onClick={save("handoverHold",JSONObject().put("holdId",h.optString("id")).put("checkoutDate",checkoutDate).put("dueDate",due).put("handoverConfirmed",true)){handover=null;confirmed=false}}){Text("Check out")}},dismissButton={TextButton(enabled=!busy,onClick={handover=null}){Text("Cancel")}})}
}
@Composable private fun LendingChoice(label:String,value:String,options:List<Pair<String,String>>,onSelect:(String)->Unit){var open by remember{mutableStateOf(false)};Box{OutlinedButton(onClick={open=true}){Text("$label: "+(options.find{it.first==value}?.second?:"Choose"))};DropdownMenu(expanded=open,onDismissRequest={open=false}){options.forEach{(id,text)->DropdownMenuItem(text={Text(text)},onClick={open=false;onSelect(id)})}}}}
