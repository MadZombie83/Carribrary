package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.graphics.asImageBitmap
import kotlinx.coroutines.launch
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import org.json.JSONObject
import java.time.LocalDate

private val readingStatuses=linkedMapOf("want" to "Want to read","reading" to "Currently reading","finished" to "Finished")
@OptIn(ExperimentalLayoutApi::class)
@Composable fun ReadingTracker(state:JSONObject,api:NativeApi,search:String,enabled:Boolean,error:String,onChange:(String,JSONObject,()->Unit)->Unit,onShare:(String)->Unit){
 val readers=state.optJSONArray("readers")?.objects()?:emptyList();val entries=state.optJSONArray("readingEntries")?.objects()?:emptyList();val books=state.optJSONArray("books")?.objects()?:emptyList();val borrowers=state.optJSONArray("borrowers")?.objects()?:emptyList()
 var selected by remember{mutableStateOf(api.preference("reader",""))};val reader=readers.find{it.optString("id")==selected}?:readers.firstOrNull()?:JSONObject();val readerId=reader.optString("id")
 var filter by remember{mutableStateOf("tbr")};var mode by remember{mutableStateOf("")};var entry by remember{mutableStateOf(JSONObject())};var profileName by remember{mutableStateOf("")};var linkedBorrower by remember{mutableStateOf("")}
 var detail by remember{mutableStateOf<JSONObject?>(null)};var currentPage by remember{mutableStateOf("0")};var totalPages by remember{mutableStateOf("0")}
 var copyId by remember{mutableStateOf("")};var status by remember{mutableStateOf("want")};var started by remember{mutableStateOf("")};var finished by remember{mutableStateOf("")};var rating by remember{mutableStateOf(0)};var notes by remember{mutableStateOf("")}
 fun edit(bookId:String){val old=entries.find{it.optString("readerId")==readerId&&it.optString("copyId")==bookId};copyId=bookId;entry=old?:JSONObject();status=old?.optString("status")?:"want";started=old?.optString("startedDate")?:"";finished=old?.optString("finishedDate")?:"";rating=old?.optInt("rating")?:0;notes=old?.optString("notes")?:"";currentPage=(old?.optInt("currentPage")?:0).toString();totalPages=(old?.optInt("totalPages")?:0).toString();mode="entry"}
 PhoneSectionHeading("Reading tracker","Household-managed TBR and reading history. Borrower account reading stays encrypted and private.")
 ReadingMenu("Reader",readerId,readers.map{it.optString("id") to (it.optString("name")+if(it.optString("borrowerId").isNotBlank())" (borrower)" else "")}){selected=it;api.savePreference("reader",it)}
 FlowRow{TextButton(enabled=enabled,onClick={profileName="";linkedBorrower="";mode="addReader"}){Text("Add reader / borrower")};if(readerId.isNotBlank()){TextButton(enabled=enabled,onClick={profileName=reader.optString("name");linkedBorrower=reader.optString("borrowerId");mode="renameReader"}){Text("Edit reader")};TextButton(enabled=enabled,onClick={mode="deleteReader"}){Text("Delete reader",color=MaterialTheme.colorScheme.error)};TextButton(enabled=enabled,onClick={onShare(readerId)}){Text("Personal link / QR")}}}
 if(readerId.isBlank())PhoneEmptyState("Who is reading?","Add a household reader to start a reading list. This is separate from borrower sign-in accounts.")
 val personal=entries.filter{it.optString("readerId")==readerId}
 if(readerId.isNotBlank())PhonePanel{Text(reader.optString("name"),style=MaterialTheme.typography.titleLarge);FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){PhoneStatusPill("${personal.count{it.optString("status") in listOf("want","reading")}} TBR");PhoneStatusPill("${personal.count{it.optString("status")=="finished"}} finished")}}
 if(readerId.isNotBlank())ReadingInsights(reader,personal,books,enabled,onChange)
 ReadingMenu("Status",filter,listOf("tbr" to "To Be Read (TBR)","" to "All reading history")+readingStatuses.map{(k,v)->k to "$v (${personal.count{it.optString("status")==k}})"}){filter=it}
 Button(enabled=enabled&&readerId.isNotBlank()&&books.any{!it.optBoolean("archived")},onClick={edit(books.first{!it.optBoolean("archived")}.optString("id"))}){Text("Add book to TBR / reading list")}
 val visible=personal.filter{filter.isBlank()||(if(filter=="tbr")it.optString("status") in listOf("want","reading") else it.optString("status")==filter)}.mapNotNull{e->books.find{it.optString("id")==e.optString("copyId")}?.let{e to it}}.filter{(e,b)->(b.optString("title")+" "+b.optString("author")+" "+e.optString("notes")).contains(search,true)}.sortedBy{it.second.optString("title").lowercase()}
 if(readerId.isNotBlank()&&visible.isEmpty())PhoneEmptyState("No reading entries found","Add a catalog book or choose another reading status.")
 visible.forEach{(e,b)->key(e.optString("id")){Card(Modifier.fillMaxWidth()){Column(Modifier.padding(16.dp),verticalArrangement=Arrangement.spacedBy(6.dp)){Row(horizontalArrangement=Arrangement.spacedBy(12.dp)){BookCover(b);Column(Modifier.weight(1f)){Text(b.optString("title"),style=MaterialTheme.typography.titleLarge);Text(b.optString("author"));Text(b.optString("id")+if(b.optBoolean("archived"))" · Removed from catalog" else "")}};Text(readingStatuses[e.optString("status")]?:"");ReadingPageProgress(e);TextButton(onClick={detail=e}){Text("Sessions, quotes & notes")};if(e.optInt("rating")>0)Text("★".repeat(e.optInt("rating"))+" (${e.optInt("rating")}/5)");if(e.optString("startedDate").isNotBlank())Text("Started ${e.optString("startedDate")}");if(e.optString("finishedDate").isNotBlank())Text("Finished ${e.optString("finishedDate")}");if(e.optString("notes").isNotBlank())Text(e.optString("notes"));FlowRow{if(e.optString("status")!="finished")TextButton(enabled=enabled,onClick={onChange("saveReading",JSONObject(e.toString()).put("readerId",readerId).put("status","finished").put("finishedDate",LocalDate.now().toString())){}}){Text("Mark as read")};TextButton(enabled=enabled,onClick={edit(b.optString("id"))}){Text("Edit reading entry")};TextButton(enabled=enabled,onClick={entry=e;mode="remove"}){Text("Remove from reading list",color=MaterialTheme.colorScheme.error)}}}}}}
 detail?.let{chosen->ReadingDetails(entries.find{it.optString("id")==chosen.optString("id")}?:chosen,readerId,enabled,error,onChange){detail=null}}
 if(mode.isNotEmpty())Dialog(onDismissRequest={mode=""}){Surface{Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text(if(mode=="entry")"Reading · ${reader.optString("name")}" else if(mode=="deleteReader")"Delete reader?" else if(mode=="remove")"Remove reading entry?" else "Reader profile",style=MaterialTheme.typography.titleLarge)
  if(mode in listOf("addReader","renameReader")){OutlinedTextField(profileName,{profileName=it.take(80)},label={Text("Reader name")});Text("Household readers are admin-managed. Borrowers set up private reading through their own approved account.");Text("All household admins can view these lists. This is not a separate sign-in.");Button(enabled=enabled&&profileName.isNotBlank(),onClick={onChange(mode,JSONObject().put("readerId",readerId).put("name",profileName).put("borrowerId","")){mode=""}}){Text("Save reader")}}
  if(mode=="entry"){
   ReadingMenu("Book",copyId,books.filter{!it.optBoolean("archived")||it.optString("id")==entry.optString("copyId")}.sortedBy{it.optString("title")}.map{it.optString("id") to "${it.optString("title")} · ${it.optString("id")}"}){edit(it)}
   ReadingMenu("Status",status,readingStatuses.toList()){status=it;if(it=="want"){started="";finished=""};if(it=="reading"){finished="";if(started.isBlank())started=LocalDate.now().toString()};if(it=="finished"&&finished.isBlank())finished=LocalDate.now().toString()}
   if(status!="want")OutlinedTextField(started,{started=it},label={Text("Started YYYY-MM-DD (optional)")})
   if(status=="finished")OutlinedTextField(finished,{finished=it},label={Text("Finished YYYY-MM-DD (optional)")})
   OutlinedTextField(currentPage,{currentPage=it.filter(Char::isDigit).take(6)},label={Text("Current page")});OutlinedTextField(totalPages,{totalPages=it.filter(Char::isDigit).take(6)},label={Text("Total pages (0 = unknown)")})
   ReadingMenu("Rating",rating.toString(),(0..5).map{it.toString() to if(it==0)"Unrated" else "$it stars"}){rating=it.toInt()}
   OutlinedTextField(notes,{notes=it.take(2000)},label={Text("Notes (optional)")},minLines=3)
   Button(enabled=enabled,onClick={onChange("saveReading",JSONObject().put("readerId",readerId).put("copyId",copyId).put("status",status).put("startedDate",started).put("finishedDate",finished).put("currentPage",currentPage.toIntOrNull()?:0).put("totalPages",totalPages.toIntOrNull()?:0).put("rating",rating).put("notes",notes)){mode=""}}){Text("Save reading entry")}
  }
  if(mode in listOf("remove","deleteReader")){Text(if(mode=="deleteReader")"Remove this reader’s reading entries first. Catalog books stay." else "This removes only this reader’s entry. The catalog book stays.");Button(enabled=enabled,onClick={onChange(if(mode=="remove")"removeReading" else "removeReader",JSONObject().put("readerId",readerId).put("copyId",entry.optString("copyId"))){mode=""}}){Text("Confirm removal")}}
  if(error.isNotBlank())Text(error,color=MaterialTheme.colorScheme.error)
  TextButton(onClick={mode=""}){Text("Close")}
 }}}
}
@Composable private fun ReadingMenu(label:String,value:String,items:List<Pair<String,String>>,onSelect:(String)->Unit){var open by remember{mutableStateOf(false)};Box{OutlinedButton(onClick={open=true}){Text("$label: "+(items.find{it.first==value}?.second?:"Choose"))};DropdownMenu(expanded=open,onDismissRequest={open=false}){items.forEach{(id,name)->DropdownMenuItem(text={Text(name)},onClick={open=false;onSelect(id)})}}}}
@Composable fun ReadingShareDialog(api:NativeApi,readerId:String,onClose:()->Unit){
 val context=androidx.compose.ui.platform.LocalContext.current;val scope=rememberCoroutineScope();var result by remember{mutableStateOf<JSONObject?>(null)};var error by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var confirm by remember{mutableStateOf("")}
 fun load(action:String?=null){scope.launch{busy=true;try{result=api.readingSharing(readerId,action);error=""}catch(e:Exception){error=e.message?:"Connection failed"}finally{busy=false}}}
 LaunchedEffect(readerId){load()};val url=result?.optString("url")?:"";val bitmap=remember(url){if(url.isNotBlank())runCatching{catalogQr(url)}.getOrNull()else null}
 Dialog(onDismissRequest=onClose){Surface{Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){Text("Personal reading link / QR",style=MaterialTheme.typography.titleLarge);Text("Only this reader’s reading list and book requests. Anyone holding this link can use it. Keep it private.");if(busy)LinearProgressIndicator();if(bitmap!=null)androidx.compose.foundation.Image(bitmap.asImageBitmap(),"Private personal reading QR",Modifier.fillMaxWidth().aspectRatio(1f));if(url.isNotBlank()){Button(onClick={context.startActivity(android.content.Intent.createChooser(android.content.Intent(android.content.Intent.ACTION_SEND).apply{type="text/plain";putExtra(android.content.Intent.EXTRA_TEXT,"Your private reading list and book requests\n$url")},"Send personal reading link"))}){Text("Share private link")};TextButton(enabled=!busy,onClick={confirm="replace"}){Text("Replace link / revoke old QR")};TextButton(enabled=!busy,onClick={confirm="disable"}){Text("Revoke personal access")}}else if(result?.optBoolean("configured")==true)Button(enabled=!busy,onClick={load("enable")}){Text("Create personal link / QR")}else Text("Configure public HTTPS book-list sharing on your server first.");if(error.isNotBlank())Text(error);TextButton(onClick=onClose){Text("Close")}}}}
 if(confirm.isNotBlank())AlertDialog(onDismissRequest={confirm=""},title={Text("Revoke the current personal link?")},text={Text("The old link and QR will stop working. Reading history stays.")},confirmButton={TextButton(onClick={val action=confirm;confirm="";load(action)}){Text("Confirm")}},dismissButton={TextButton(onClick={confirm=""}){Text("Cancel")}})
}
