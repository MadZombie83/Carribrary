package com.household.homelibrary
import android.os.Bundle
import android.content.Intent
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.platform.LocalContext
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import kotlinx.coroutines.launch
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import org.json.JSONArray
import java.net.URL
import java.time.LocalDate

class MainActivity:ComponentActivity(){override fun onCreate(savedInstanceState:Bundle?){super.onCreate(savedInstanceState);setContent{
 val api=remember{NativeApi(applicationContext)};var theme by remember{mutableStateOf(api.preference("theme","purple-blue"))}
 var custom by remember{mutableStateOf(api.preference("custom-palette",""))}
 MaterialTheme(colorScheme=libraryColors(theme,custom),shapes=phoneShapes,typography=phoneTypography){LibraryApp(api,theme){theme=it;custom=api.preference("custom-palette","");api.savePreference("theme",it)}}
}}}

fun JSONArray.objects():List<JSONObject> = (0 until length()).mapNotNull{optJSONObject(it)}
fun edition(value:String):String{val code=value.trim().uppercase().replace(Regex("[\\s-]"),"");if(code.length!=10)return code;val prefix="978"+code.take(9);val sum=prefix.mapIndexed{i,c->(c-'0')*(if(i%2==0)1 else 3)}.sum();return prefix+((10-sum%10)%10)}
fun duplicates(books:List<JSONObject>,draft:JSONObject)=books.filter{edition(it.optString("isbn"))==edition(draft.optString("isbn"))||(draft.optString("title").isNotBlank()&&draft.optString("author").isNotBlank()&&it.optString("title").trim().equals(draft.optString("title").trim(),true)&&it.optString("author").trim().equals(draft.optString("author").trim(),true))}
@OptIn(ExperimentalLayoutApi::class)
@Composable fun LibraryApp(api:NativeApi,theme:String,onTheme:(String)->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var paired by remember{mutableStateOf(api.paired)};var snapshot by remember{mutableStateOf(api.cached()?:JSONObject().put("revision",0).put("state",JSONObject().put("books",JSONArray()).put("borrowers",JSONArray()).put("loans",JSONArray())))}
 var collection by remember{mutableStateOf(api.preference("collection",""))};var collectionOpen by remember{mutableStateOf(false)};var collectionDraft by remember{mutableStateOf(JSONObject())};var collectionName by remember{mutableStateOf("")}
 var filtersVisible by remember{mutableStateOf(false)}
 var adminNotices by remember{mutableStateOf(JSONObject())};var noticesError by remember{mutableStateOf("")};
 var manageAction by remember{mutableStateOf("")}
 var receiptDraft by remember{mutableStateOf<JSONObject?>(null)};var busy by remember{mutableStateOf(false)};var online by remember{mutableStateOf(false)};var notice by remember{mutableStateOf("")};var view by remember{mutableStateOf(if((context as? android.app.Activity)?.intent?.getBooleanExtra("openRequests",false)==true)"Home" else "Catalog")};var search by remember{mutableStateOf("")}
 var dialog by remember{mutableStateOf(if((context as? android.app.Activity)?.intent?.getBooleanExtra("openRequests",false)==true)"notifications" else "")};var scanner by remember{mutableStateOf("")};var draft by remember{mutableStateOf(JSONObject())};var borrower by remember{mutableStateOf("")};var due by remember{mutableStateOf("")};var checkoutDate by remember{mutableStateOf(LocalDate.now().toString())};var pairingText by remember{mutableStateOf("")};var phoneName by remember{mutableStateOf(api.deviceName)}
 var descending by remember{mutableStateOf(api.preference("direction","asc")=="desc")};var sort by remember{mutableStateOf(api.preference("sort","title"))};var sortOpen by remember{mutableStateOf(false)};var size by remember{mutableStateOf(api.preference("size","10").toIntOrNull()?.takeIf{it in listOf(10,25,50)}?:10)};var sizeOpen by remember{mutableStateOf(false)};var page by remember{mutableStateOf(1)}
 val state=snapshot.optJSONObject("state")?:JSONObject();val books=state.optJSONArray("books")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList();val people=state.optJSONArray("borrowers")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList();val loans=state.optJSONArray("loans")?.objects()?.filter{it.isNull("returnedDate")}?:emptyList()
 LaunchedEffect(snapshot.optLong("revision"),theme){if(theme.startsWith("custom-")){state.optJSONArray("savedThemes")?.objects()?.find{"custom-"+it.optString("id")==theme}?.let{val raw=it.toString();if(api.preference("custom-palette","")!=raw){api.savePreference("custom-palette",raw);onTheme(theme)}}}}
 val collections=state.optJSONArray("collections")?.objects()?:listOf(JSONObject().put("id","general").put("name","General"));val selectedCollection=collection.takeIf{id->collections.any{it.optString("id")==id}}?:""
 fun launchTask(task:suspend()->Unit){scope.launch{busy=true;notice="";try{task()}catch(e:Exception){notice=e.message?:"Connection failed";online=api.online;if(e is ApiError&&e.status==409){runCatching{snapshot=api.refresh();online=true}}}finally{busy=false}}}
 fun refresh(){launchTask{snapshot=api.refresh();online=true;notice="Library refreshed"}}
 fun scan(kind:String){scanner=kind}
 LaunchedEffect(paired){if(paired){runCatching{snapshot=api.refresh();online=true}.onFailure{online=false;notice=it.message?:"Offline — showing cached library. Connect to make changes."}}}
 LaunchedEffect(paired,snapshot.optLong("revision"),dialog){if(paired)while(true){if((context as? ComponentActivity)?.lifecycle?.currentState?.isAtLeast(androidx.lifecycle.Lifecycle.State.STARTED)!=false){runCatching{api.adminNotifications()}.onSuccess{adminNotices=it;noticesError=""}.onFailure{noticesError="Notifications could not refresh. Check the connection; listed items may be out of date."}};kotlinx.coroutines.delay(30000)}}
 Surface(Modifier.fillMaxSize()){
 Scaffold(containerColor=MaterialTheme.colorScheme.background,bottomBar={if(paired)NavigationBar{for((target,label,glyph)in listOf(Triple("Home","Home","⌂"),Triple("Catalog","Catalog","▤"),Triple("On loan","Loans","↗"),Triple("Reading","Reading","✓"),Triple("More","Settings","☰")))NavigationBarItem(selected=view==target||(target=="More"&&view in listOf("Borrowers","Requests","History")),onClick={view=target;search="";page=1},icon={PhoneNavIcon(target)},label={Text(label)})}}){screenPadding->
 Column(Modifier.fillMaxSize().padding(screenPadding).consumeWindowInsets(screenPadding).safeDrawingPadding().verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  PhoneLibraryBanner(state,onHelp={dialog="help"},requests=adminNotices.optInt("count"),onRequests=if(paired)({dialog="notifications"}) else null)
  Column(Modifier.padding(horizontal=16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text(if(paired){if(online)"Connected to library server" else "Offline · cached books"}else "Pair with your library server",style=MaterialTheme.typography.bodySmall)
  val message=state.optString("encouragingMessage","A place for every book.");if(message.isNotBlank())Text(message,style=MaterialTheme.typography.titleLarge.copy(fontFamily=FontFamily.Serif),color=MaterialTheme.colorScheme.primary)
  if(paired&&view in listOf("Home","Catalog"))PhoneLibrarySummary(phoneSummaryCounts(
   (state.optJSONArray("books")?.objects()?:emptyList()).map{SummaryCopy(it.optString("id"),it.optBoolean("archived"))},
   (state.optJSONArray("loans")?.objects()?:emptyList()).map{SummaryLoan(it.optString("copyId"),!it.isNull("returnedDate"))},
   (state.optJSONArray("holds")?.objects()?:emptyList()).map{it.optString("copyId")},
   (state.optJSONArray("requests")?.objects()?:emptyList()).map{it.optString("status")}))
  if(!paired){Text("Install Carribrary Server on your own Windows, Linux or Raspberry Pi computer, then scan its private pairing QR. The phone companion requires that server; no hosted library is included.");OutlinedTextField(phoneName,{phoneName=it},label={Text("Phone name")},modifier=Modifier.fillMaxWidth());Button(onClick={scan("pair")}){Text("Scan pairing QR")};OutlinedTextField(pairingText,{pairingText=it},label={Text("Pairing code (manual fallback)")},modifier=Modifier.fillMaxWidth());Button(enabled=!busy&&pairingText.isNotBlank(),onClick={launchTask{api.pair(pairingText,phoneName);paired=true;pairingText="";snapshot=api.refresh();online=true}}){Text("Pair phone")}}
  else{
   if(view=="Borrowers")PhoneSectionHeading("Borrowers","Lending contacts, active loans and reminder permissions.")
   if(view=="Requests")PhoneSectionHeading("Requests","Review holds and approve a handover when the book reaches its borrower.")
   if(view=="History")PhoneSectionHeading("Loan history","Past and current borrowing, separate from personal reading.")
   if(view!="More")OutlinedTextField(search,{search=it;page=1;if(view=="Home")view="Catalog"},leadingIcon={Icon(painterResource(R.drawable.nav_search),null)},shape=MaterialTheme.shapes.medium,label={Text(if(view in listOf("Catalog","On loan"))"Search title, author, series, ISBN or copy ID" else if(view=="Reading")"Search reading titles, authors or notes" else "Search books or borrowers")},modifier=Modifier.fillMaxWidth())
   if(api.hasPending())Button(onClick={launchTask{snapshot=api.retryPending();online=true;notice="Previous change confirmed"}},enabled=!busy){Text("Retry pending change")}
   if(view=="More"){PhoneMoreMenu(online&&!busy,(state.optJSONArray("requests")?.objects()?:emptyList()).count{it.optString("status") in listOf("pending","approved")},canRefresh=!busy){action->when(action){"Borrowers","Requests","History"->{view=action;search="";page=1};"refresh"->refresh();"csv","devices","backup","restore"->{manageAction=action;dialog="manage"};"settings"->{manageAction="";dialog="manage"};"collections"->{collectionDraft=JSONObject();collectionName="";dialog="collections"};else->dialog=action}}}else if(view=="Home"){
    PhoneSectionHeading("Dashboard","Your library at a glance. Search above or choose an everyday action.")
    PhonePanel{Text("Quick actions",style=MaterialTheme.typography.titleLarge);FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)){Button(enabled=online&&!busy,onClick={draft=JSONObject();dialog="book"}){Text("Add book")};OutlinedButton(enabled=online&&!busy,onClick={draft=JSONObject();borrower="";due="";checkoutDate=LocalDate.now().toString();dialog="checkout"}){Text("Check out")};OutlinedButton(enabled=online&&!busy,onClick={draft=JSONObject();dialog="return"}){Text("Return")};OutlinedButton(enabled=online&&!busy,onClick={dialog="sharing"}){Text("Share library")};OutlinedButton(enabled=online&&!busy,onClick={dialog="acquisitions"}){Text("Books to buy")}}}
    PhonePanel{Text("People & lending",style=MaterialTheme.typography.titleLarge);TextButton(onClick={view="Borrowers";search="";page=1}){Text("Borrowers (${people.size})")};TextButton(onClick={view="Requests";search="";page=1}){Text("Review requests")};TextButton(onClick={view="On loan";search="";page=1}){Text("View active loans")}}
   }else if(view=="Reading"){ReadingTracker(state,api,search,online&&!busy,notice,onChange={action,input,done->launchTask{snapshot=api.mutate(action,input,snapshot.optLong("revision"));notice="Reading tracker updated";done()}},onShare={id->draft=JSONObject().put("readerId",id);dialog="readingShare"})}else if(view=="History"){LoanHistory(state,search)}else if(view=="Requests"){
    RequestInbox(state,search,online&&!busy,onDecision={action,id->launchTask{snapshot=api.mutate(action,JSONObject().put("requestId",id),snapshot.optLong("revision"));notice="Request updated"}},onHandOver={request->draft=request;borrower="";due="";checkoutDate=LocalDate.now().toString();dialog="handover"})
   }else if(view=="Borrowers"){
    Button(enabled=online&&!busy,onClick={draft=JSONObject();dialog="borrower"}){Text("Add borrower")}
    if(people.isEmpty())PhoneEmptyState("No borrowers yet","Add a lending contact to check out a book.")
    people.filter{(it.optString("name")+" "+it.optString("contact")).contains(search,true)}.sortedBy{it.optString("name").lowercase()}.forEach{person->Card(Modifier.fillMaxWidth()){Column(Modifier.padding(16.dp)){Text(person.optString("name"),style=MaterialTheme.typography.titleLarge);Text(person.optString("contact").ifBlank{"No contact recorded"},color=MaterialTheme.colorScheme.onSurfaceVariant);PhoneStatusPill("${loans.count{it.optString("borrowerId")==person.optString("id")}} books on loan");TextButton(enabled=online&&!busy,onClick={draft=JSONObject(person.toString()).put("borrowerId",person.optString("id"));dialog="editBorrower"}){Text("Edit / email reminders")};TextButton(enabled=online&&!busy,onClick={dialog="patronCards"}){Text("Library cards / PINs")};TextButton(enabled=online&&!busy,onClick={draft=person;dialog="deleteBorrower"}){Text("Delete borrower",color=MaterialTheme.colorScheme.error)}}}}
   }else{
    FlowRow(horizontalArrangement=Arrangement.spacedBy(6.dp)){Button(enabled=online&&!busy,onClick={draft=JSONObject().put("collectionId",selectedCollection.ifBlank{"general"});dialog="book"}){Text("Add book")};OutlinedButton(enabled=online&&!busy,onClick={draft=JSONObject();borrower="";due="";checkoutDate=LocalDate.now().toString();dialog="checkout"}){Text("Check out")};OutlinedButton(enabled=online&&!busy,onClick={draft=JSONObject();dialog="return"}){Text("Return")}}
    OutlinedButton(onClick={filtersVisible=!filtersVisible}){Text(if(filtersVisible)"Hide filters & sorting" else "Filter & sort · ${collections.find{it.optString("id")==selectedCollection}?.optString("name")?:"All collections"} · ${sortLabels[sort]?:"Title"} · $size per page")}
    if(filtersVisible)FlowRow{Box{OutlinedButton(onClick={collectionOpen=true}){Text("Collection: "+(collections.find{it.optString("id")==selectedCollection}?.optString("name")?:"All"))};DropdownMenu(expanded=collectionOpen,onDismissRequest={collectionOpen=false}){DropdownMenuItem(text={Text("All collections")},onClick={collection="";api.savePreference("collection","");collectionOpen=false;page=1});collections.forEach{c->DropdownMenuItem(text={Text(c.optString("name"))},onClick={collection=c.optString("id");api.savePreference("collection",collection);collectionOpen=false;page=1})}}};TextButton(enabled=online&&!busy,onClick={dialog="collections";collectionDraft=JSONObject();collectionName=""}){Text("Manage")};TextButton(enabled=online&&!busy,onClick={dialog="bulk"}){Text("Bulk edit")}}
    if(filtersVisible)FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)){
     Box{OutlinedButton(onClick={sortOpen=true}){Text("Sort by: ${sortLabels[sort]?:"Title"}")};DropdownMenu(expanded=sortOpen,onDismissRequest={sortOpen=false}){for((key,label)in sortLabels)DropdownMenuItem(text={Text(label)},onClick={sort=key;api.savePreference("sort",key);sortOpen=false;page=1})}}
     OutlinedButton(onClick={descending=!descending;api.savePreference("direction",if(descending)"desc" else "asc");page=1}){Text(if(sort=="id"){if(descending)"High → low" else "Low → high"}else if(descending)"Z → A" else "A → Z")}
     Box{OutlinedButton(onClick={sizeOpen=true}){Text("$size per page")};DropdownMenu(expanded=sizeOpen,onDismissRequest={sizeOpen=false}){for(n in listOf(10,25,50))DropdownMenuItem(text={Text("$n")},onClick={size=n;api.savePreference("size",n.toString());sizeOpen=false;page=1})}}
    }
    val matching=sortedCatalog(books.filter{selectedCollection.isBlank()||it.optString("collectionId","general")==selectedCollection}.filter{b->listOf("title","author","series","isbn","id","publisher","room","bookcase","shelf").any{b.optString(it).contains(search,true)}&&(view!="On loan"||loans.any{it.optString("copyId")==b.optString("id")})},sort,descending)
    val pages=maxOf(1,(matching.size+size-1)/size);val current=page.coerceIn(1,pages);Text("${matching.size} matching copies · Page $current of $pages")
    if(matching.isEmpty())OutlinedCard(Modifier.fillMaxWidth()){Column(Modifier.padding(20.dp)){Text(if(search.isBlank())"No books here yet" else "No matching books",style=MaterialTheme.typography.titleLarge);Text(if(search.isBlank())"Add a book by scanning its ISBN or entering it manually." else "Try a title, author, ISBN or catalog number.")}}
    matching.drop((current-1)*size).take(size).forEach{book->key(book.optString("id")){
     val loan=loans.find{it.optString("copyId")==book.optString("id")};val person=people.find{it.optString("id")==loan?.optString("borrowerId")}
     val held=(state.optJSONArray("holds")?.objects()?:emptyList()).any{it.optString("copyId")==book.optString("id")}
     PhoneBookCard(book,status=if(loan!=null&&!loan.optString("returnRequestedAt").isBlank())"Return pending physical receipt · ${person?.optString("name")?:"Unknown"}" else if(loan!=null)"Borrowed by ${person?.optString("name")?:"Unknown"} · ${loan.optString("dueDate").ifBlank{"No due date"}}" else if(held)"On hold" else "Available",onLoan=loan!=null,canCirculate=online&&!busy&&!held,canEdit=online&&!busy,
      onCirculate={draft=JSONObject().put("copyId",book.optString("id"));borrower="";due="";checkoutDate=LocalDate.now().toString();dialog=if(loan==null)"checkout" else "return"},
      onAbout={draft=book;dialog="about"},onEdit={draft=JSONObject(book.toString()).put("copyId",book.optString("id"));dialog="editBook"},onDelete={draft=book;dialog="deleteBook"},onReceipt={launchTask{receiptDraft=api.receipt(book.optString("id"))}})
    }}
    Row(horizontalArrangement=Arrangement.spacedBy(12.dp)){OutlinedButton(enabled=current>1,onClick={page=current-1}){Text("Previous")};OutlinedButton(enabled=current<pages,onClick={page=current+1}){Text("Next")}}
   }
   PhoneProductFooter()
  }
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
  if(notice.isNotBlank())Text(notice,color=MaterialTheme.colorScheme.primary)
 }
 }
 }
 }
 if(dialog=="catalogBatch")BatchCatalogDialog(api,snapshot,onChanged={snapshot=it},onClose={dialog=""})
 if(dialog=="browserAdmins")BrowserAdminPairingDialog(api,onClose={dialog=""})
 if(dialog=="requestAlerts")RequestAlertsDialog(api,onClose={dialog=""})
 if(dialog=="libraryInsights")LibraryInsightsDialog(api){dialog=""}
 if(dialog=="portableBackups")PortableBackupsDialog(api,onClose={dialog=""})
 if(dialog=="backupVerify")VerifyBackupDialog(api,onClose={dialog=""})
 if(dialog=="notifications")AdminNotificationDialog(adminNotices,noticesError,onRefresh={launchTask{adminNotices=api.adminNotifications();noticesError=""}},onSelect={tool->launchTask{snapshot=api.refresh();online=true};dialog="";when(tool){"requests"->{view="Requests";search="";page=1};"active-loans"->{view="On loan";search="";page=1};"accounts-button"->dialog="accounts";"books-to-buy"->dialog="acquisitions";"holds"->dialog="lending";"portable-backups"->dialog="portableBackups";"reminders"->dialog="reminders";"push-overview"->dialog="requestAlerts";"complete-recovery-review"->dialog="setup";else->dialog=tool}},onClose={dialog=""})
 if(dialog=="renewals")RenewalsDialog(api,snapshot,onChanged={snapshot=it},onClose={dialog=""})
 if(scanner.isNotEmpty())Dialog(onDismissRequest={scanner=""},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){
  Scanner(api,scanner,onRead={value->val kind=scanner;scanner="";if(kind=="pair")launchTask{api.pair(value,phoneName);paired=true;pairingText="";snapshot=api.refresh();online=true}else if(kind=="patron")launchTask{borrower=api.tools("patron-cards",JSONObject().put("action","resolve").put("code",value)).getString("borrowerId")}else {val next=JSONObject(draft.toString()).put(if(kind=="isbn")"isbn" else "copyId",value);if(kind=="isbn")next.remove("coverUrl") else next.remove("selectedCopy");draft=next}},onClose={scanner=""})
 }}
 receiptDraft?.let{ReceiptDialog(it){receiptDraft=null}}
 if(dialog=="patronCards")PatronCardsDialog(snapshot,api){dialog=""}
 if(dialog=="selfService")SelfServiceDialog(snapshot,api,onChanged={snapshot=it;online=true}){dialog=""}
 if(dialog=="copyTools")CopyTools(snapshot,api,onChanged={snapshot=it;online=api.online}){dialog=""}
 if(dialog=="dataControls")DataControlsDialog(api,snapshot,onChanged={snapshot=it;online=api.online},onCacheCleared={snapshot=JSONObject().put("state",JSONObject());online=false;notice="Local downloads cleared; Refresh downloads the library again."}){dialog=""}
 if(dialog=="acquisitions")AcquisitionDialog(api,snapshot,onChanged={snapshot=it;online=true}){dialog=""}
 if(dialog=="lending")LendingDialog(api,snapshot,onChanged={snapshot=it;online=true}){dialog=""}
 if(dialog=="help")PhoneHelpDialog{dialog=""}
 if(dialog in listOf("recycle","reminders","bulk","setup"))LibraryToolsDialog(dialog,api,snapshot,onChanged={snapshot=it;online=true}){dialog=""}
 if(dialog=="accounts")BorrowerAccountsDialog(api,snapshot,onChanged={snapshot=it;online=true}){dialog=""}
 if(dialog=="privacy")PrivacyDialog{dialog=""}
 if(dialog=="readingShare")ReadingShareDialog(api,draft.optString("readerId")){dialog=""}
 if(dialog=="about")BookAboutDialog(draft,api,online){dialog=""}
 if(dialog=="sharing")ShareLibraryDialog(api,state.optString("libraryName","Carribrary")){dialog=""}
 if(dialog=="manage")ManageLibraryDialog(api,snapshot,initialAction=manageAction,onChanged={snapshot=it;online=true},onForget={api.forget();paired=false;online=false;dialog="";snapshot=JSONObject().put("state",JSONObject())},onClose={dialog=""})
 if(dialog in listOf("deleteBook","deleteBorrower")){
  val isBook=dialog=="deleteBook";val id=draft.optString("id");val active=loans.any{it.optString(if(isBook)"copyId" else "borrowerId")==id}
  val history=(state.optJSONArray("loans")?.objects()?:emptyList()).any{it.optString(if(isBook)"copyId" else "borrowerId")==id}||(isBook&&(state.optJSONArray("requests")?.objects()?:emptyList()).any{it.optString("copyId")==id})||(isBook&&(state.optJSONArray("readingEntries")?.objects()?:emptyList()).any{it.optString("copyId")==id})||(!isBook&&(state.optJSONArray("readers")?.objects()?:emptyList()).any{it.optString("borrowerId")==id})
  AlertDialog(onDismissRequest={if(!busy)dialog=""},title={Text(if(isBook)"Delete book?" else "Delete borrower?")},text={Column{Text(draft.optString(if(isBook)"title" else "name"));Text(if(active)"Return the borrowed books before deleting this record." else "Moved to the Recycle Bin, where it can be restored. History stays. Reused copy numbers get a free number on restore.");if(notice.isNotBlank())Text(notice)}},confirmButton={if(!active)TextButton(enabled=online&&!busy,onClick={launchTask{snapshot=api.mutate(if(isBook)"removeBook" else "removeBorrower",JSONObject().put(if(isBook)"copyId" else "borrowerId",id),snapshot.optLong("revision"));dialog="";notice="Moved to Recycle Bin"}}){Text("Move to Recycle Bin")}},dismissButton={TextButton(enabled=!busy,onClick={dialog=""}){Text("Cancel")}})
 }
 if(dialog=="collections")Dialog(onDismissRequest={if(!busy)dialog=""}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(8.dp)){
  Text("Collections",style=MaterialTheme.typography.titleLarge);Text("Separate books; shared borrowers and loans. No fixed collection limit.")
  collections.forEach{c->Text(c.optString("name"));Row{TextButton(enabled=!busy,onClick={collectionDraft=c;collectionName=c.optString("name")}){Text("Rename")};if(c.optString("id")!="general")TextButton(enabled=!busy&&online,onClick={launchTask{snapshot=api.mutate("removeCollection",JSONObject().put("collectionId",c.optString("id")),snapshot.optLong("revision"));notice="Empty collection deleted"}}){Text("Delete empty")}}}
  OutlinedTextField(collectionName,{collectionName=it},label={Text(if(collectionDraft.has("id"))"Rename collection" else "New collection name")},enabled=!busy)
  Button(enabled=online&&!busy&&collectionName.isNotBlank(),onClick={launchTask{snapshot=api.mutate(if(collectionDraft.has("id"))"renameCollection" else "addCollection",JSONObject().put("collectionId",collectionDraft.optString("id")).put("name",collectionName),snapshot.optLong("revision"));collectionName="";collectionDraft=JSONObject();notice="Collection saved"}}){Text("Save collection")}
  if(collectionDraft.has("id"))TextButton(onClick={collectionDraft=JSONObject();collectionName=""}){Text("Add a new collection")}
  if(notice.isNotBlank())Text(notice);TextButton(enabled=!busy,onClick={dialog=""}){Text("Close")}
 }}}
 if(dialog=="theme")CustomThemeDialog(state,theme,online,busy,notice,onPreset={key->
  state.optJSONArray("savedThemes")?.objects()?.find{"custom-"+it.optString("id")==key}?.let{api.savePreference("custom-palette",JSONObject(it.toString()).apply{remove("sidebarArtwork")}.toString())};onTheme(key)
 },onPublish={key->launchTask{snapshot=api.mutate("setLibraryTheme",JSONObject().put("libraryTheme",key),snapshot.optLong("revision"));notice="Selected theme applied to shared library"}},onSave={palette->launchTask{snapshot=api.mutate("saveCustomTheme",palette,snapshot.optLong("revision"));val fresh=snapshot.getJSONObject("state");val key=fresh.getString("libraryTheme");fresh.optJSONArray("savedThemes")?.objects()?.find{"custom-"+it.optString("id")==key}?.let{api.savePreference("custom-palette",JSONObject(it.toString()).apply{remove("sidebarArtwork")}.toString())};onTheme(key);notice="Custom library theme saved";dialog=""}},onClose={dialog=""})
 if(dialog !in listOf("patronCards","selfService","copyTools","notifications","browserAdmins","requestAlerts","libraryInsights","portableBackups","catalogBatch","renewals","backupVerify")&&dialog.isNotEmpty()&&dialog !in listOf("acquisitions","dataControls","lending","about","recycle","reminders","bulk","setup","help","accounts","readingShare","sharing","theme","manage","deleteBook","deleteBorrower","privacy","collections"))Dialog(onDismissRequest={if(!busy)dialog=""}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  Text(when(dialog){"book"->"Add physical copy";"editBook"->"Edit book";"borrower"->"Add borrower";"editBorrower"->"Edit borrower";"checkout"->"Check out";"return"->"Return copy";"handover"->"Hand over requested book";else->"Connection"},style=MaterialTheme.typography.titleLarge)
  var coverResults by remember(dialog,draft.optString("isbn")){mutableStateOf<JSONObject?>(null)}
  fun set(field:String,value:String){val next=JSONObject(draft.toString()).put(field,value);if(field=="copyId")next.remove("selectedCopy");if(field=="isbn"&&value!=draft.optString("isbn")){next.remove("coverUrl");coverResults=null};draft=next}
  if(dialog=="connection"){
   Text("Phone: $phoneName");Text("If the server identity changes, pair again from the server’s admin interface.");Button(enabled=!busy,onClick={api.forget();paired=false;online=false;dialog="";snapshot=JSONObject().put("state",JSONObject());notice="Pair this phone again from the server’s admin interface."}){Text("Forget connection")}
  }else{
   if(dialog=="handover")Text("Copy ${draft.optString("copyId")} · Requested by ${draft.optString("name")} · ${draft.optString("contact")}")
   val fields=when(dialog){"book","editBook"->listOf("isbn","title","author","series","publisher","description","coverUrl","room","bookcase","shelf","importNotes");"borrower","editBorrower"->listOf("name","contact");"handover"->emptyList();else->listOf("copyId")}
   for(field in fields)OutlinedTextField(enabled=!busy,value=draft.optString(field),onValueChange={if(field!="importNotes"||it.length<=8000)set(field,it)},label={Text(when(field){"isbn"->"ISBN / EAN";"copyId"->"ISBN / EAN, copy ID, title or author";"description"->"About this book (optional, up to 4,000 characters)";"coverUrl"->"Cover URL (optional)";"room"->"Room (admin only)";"bookcase"->"Bookcase (admin only)";"shelf"->"Shelf (admin only)";"importNotes"->"Private import notes (up to 8,000 characters)";else->field.replaceFirstChar{it.uppercase()}})},modifier=Modifier.fillMaxWidth())
   if(dialog in listOf("borrower","editBorrower"))Row(verticalAlignment=Alignment.CenterVertically){Checkbox(checked=draft.optBoolean("reminderConsent"),onCheckedChange={draft=JSONObject(draft.toString()).put("reminderConsent",it)});Text("Borrower agreed to automatic email due-date reminders (optional)")}
   if(dialog in listOf("book","editBook","checkout","return"))OutlinedButton(enabled=!busy,onClick={scan(if(dialog in listOf("book","editBook"))"isbn" else "copy")}){Text("Scan barcode")}
   if(dialog in listOf("book","editBook")){var bookCollectionOpen by remember{mutableStateOf(false)};Box{OutlinedButton(onClick={bookCollectionOpen=true}){Text("Collection: "+(collections.find{it.optString("id")==draft.optString("collectionId","general")}?.optString("name")?:"General"))};DropdownMenu(expanded=bookCollectionOpen,onDismissRequest={bookCollectionOpen=false}){collections.forEach{c->DropdownMenuItem(text={Text(c.optString("name"))},onClick={set("collectionId",c.optString("id"));bookCollectionOpen=false})}}}}
   if(dialog in listOf("book","editBook"))OutlinedButton(enabled=!busy,onClick={launchTask{val isbn=draft.optString("isbn").replace("-","").trim();require(isbn.matches(Regex("[0-9]{9}[0-9X]|[0-9]{13}"))){"Enter ISBN first"};val info=withContext(Dispatchers.IO){val connection=URL("https://openlibrary.org/api/books?bibkeys=ISBN:$isbn&format=json&jscmd=data").openConnection();connection.connectTimeout=5000;connection.readTimeout=10000;JSONObject(connection.getInputStream().bufferedReader().use{it.readText()}).optJSONObject("ISBN:$isbn")};if(info==null)notice="No match; enter details manually" else{set("title",info.optString("title"));set("author",info.optJSONArray("authors")?.objects()?.joinToString(", "){it.optString("name")}?:"");set("publisher",info.optJSONArray("publishers")?.objects()?.joinToString(", "){it.optString("name")}?:"");if(draft.optString("coverUrl").isBlank())set("coverUrl",info.optJSONObject("cover")?.optString("medium")?:"")};if(draft.optString("coverUrl").isBlank()){coverResults=api.covers(isbn,draft.optString("title"),draft.optString("author"));notice=coverResults?.optString("message")?:""}}}){Text("Look up book details")}
   if(dialog in listOf("book","editBook")){
    OutlinedButton(enabled=online&&!busy,onClick={launchTask{val isbn=draft.optString("isbn").replace("-","").trim();coverResults=api.covers(isbn,draft.optString("title"),draft.optString("author"));notice=coverResults?.optString("message")?:""}}){Text("Search Books for a cover")}
    for(candidate in coverResults?.optJSONArray("covers")?.objects()?:emptyList()){
     Row(horizontalArrangement=Arrangement.spacedBy(10.dp)){
      BookCover(JSONObject().put("title",candidate.optString("title")).put("coverUrl",candidate.optString("url")))
      Column{Text(candidate.optString("source"));Text(candidate.optString("title"));Text(if(candidate.optBoolean("exactEdition"))"ISBN-linked image — review cover" else "May be another edition — confirm cover");TextButton(onClick={set("coverUrl",candidate.optString("url"));coverResults=null;notice="Cover selected. Save the book to keep it."}){Text("Use this cover")};TextButton(onClick={context.startActivity(Intent(Intent.ACTION_VIEW,android.net.Uri.parse(candidate.optString("sourceUrl"))))}){Text("Source")}}
     }
    }
   }
   val circulation=dialog in listOf("checkout","return")
   val eligible=if(circulation)circulationMatches(books.map{b->CirculationCopy(b.optString("id"),b.optString("isbn"),b.optString("title"),b.optString("author"),loans.any{it.optString("copyId")==b.optString("id")},series=b.optString("series"),collectionName=collections.find{it.optString("id")==b.optString("collectionId","general")}?.optString("name")?:"General",copyUid=b.optString("copyUid"),onHold=(state.optJSONArray("holds")?.objects()?:emptyList()).any{it.optString("copyId")==b.optString("id")})},draft.optString("copyId"),dialog=="return") else emptyList()
   val chosenCopy=if(eligible.size==1)eligible[0].id else draft.optString("selectedCopy").takeIf{id->eligible.any{it.id==id}}?:""
   if(circulation){
    Text("Scan the book’s existing ISBN barcode or optional copy label. Manual search works too.")
    if(eligible.isEmpty())Text(if(draft.optString("copyId").isBlank())"Enter or scan a book to find a copy." else if(dialog=="return")"No checked-out copy of this book was found." else "No available copy of this book was found.")
    else{Text(if(eligible.size==1)"Selected copy:" else "Choose the physical copy:");eligible.forEach{b->val loan=loans.find{it.optString("copyId")==b.id};val person=people.find{it.optString("id")==loan?.optString("borrowerId")};Row{RadioButton(selected=chosenCopy==b.id,onClick={set("selectedCopy",b.id)},enabled=!busy);Column{Text("${b.id} · ${b.title} · ${b.author} · ${b.collectionName}");if(loan!=null)Text("Borrowed by ${person?.optString("name")?:"Unknown"} · Out ${loan.optString("checkoutDate")} · ${loan.optString("dueDate").ifBlank{"No due date"}}")}}}}
   }
   var personOpen by remember{mutableStateOf(false)}
   if(dialog in listOf("checkout","handover")){OutlinedButton(enabled=!busy,onClick={scan("patron")}){Text("Scan library card")};if(dialog=="handover")Text("Leave borrower unselected to find or create one from the request.");OutlinedTextField(checkoutDate,{checkoutDate=it},label={Text("Checkout date YYYY-MM-DD")});Box{OutlinedButton(onClick={personOpen=true}){Text(people.find{it.optString("id")==borrower}?.optString("name")?:if(dialog=="handover")"Find or create borrower" else "Select borrower")};DropdownMenu(expanded=personOpen,onDismissRequest={personOpen=false}){people.forEach{p->DropdownMenuItem(text={Text(p.optString("name"))},onClick={borrower=p.optString("id");personOpen=false})}}};OutlinedTextField(due,{due=it},label={Text("Due date YYYY-MM-DD (optional)")})}
   var allowDuplicate by remember(dialog,draft.optString("isbn"),draft.optString("title"),draft.optString("author"),snapshot.optLong("revision")){mutableStateOf(false)}
   if(dialog in listOf("book","editBook")){
    val matches=duplicates(books.filter{it.optString("id")!=draft.optString("copyId")},draft)
    if(matches.isNotEmpty()){Text("Possible existing copies: "+matches.joinToString{it.optString("id")});Row{Checkbox(allowDuplicate,{allowDuplicate=it});Text("I want to add another physical copy")}}
   }
   Button(enabled=online&&!busy&&(!circulation||chosenCopy.isNotBlank()),onClick={launchTask{
    val input=JSONObject(draft.toString());val action=when(dialog){"book"->"addBook";"editBook"->"updateBook";"borrower"->"addBorrower";"editBorrower"->"updateBorrower";"checkout"->"checkout";"handover"->"fulfillRequest";else->"returnBook"}
    if(circulation)input.put("copyId",chosenCopy)
    if(action in listOf("checkout","fulfillRequest")){input.put("borrowerId",borrower).put("checkoutDate",checkoutDate).put("dueDate",due)}
    if(action=="fulfillRequest")input.put("requestId",draft.optString("id"))
    if(action in listOf("addBook","updateBook")&&allowDuplicate)input.put("confirmedDuplicates",JSONArray(duplicates(books.filter{it.optString("id")!=draft.optString("copyId")},draft).map{it.optString("id")}))
    snapshot=api.mutate(action,input,snapshot.optLong("revision"));online=true;dialog="";notice="Saved"
    if(action in listOf("checkout","fulfillRequest")){notice="Book checked out";try{receiptDraft=api.receipt(input.optString("copyId"))}catch(e:Exception){notice="Book checked out. Receipt unavailable: ${e.message}. Use Borrowing receipt on this loan to try again."}}
   }}){Text(if(dialog=="checkout")"Confirm checkout" else if(dialog=="return")"Confirm return" else "Save")}
  }
  if(notice.isNotBlank())Text(notice)
  TextButton(enabled=!busy,onClick={dialog=""}){Text("Close")}
 }}}
}
