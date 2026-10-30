package com.household.homelibrary
import android.content.Intent
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
import java.time.LocalDate

@Composable fun LoanHistory(state:JSONObject,search:String){
 val books=state.optJSONArray("books")?.objects()?:emptyList();val people=state.optJSONArray("borrowers")?.objects()?:emptyList()
 val loans=(state.optJSONArray("loans")?.objects()?:emptyList()).sortedByDescending{it.optString("checkoutDate")}.filter{loan->listOf(loan.optString("copyId"),books.find{it.optString("id")==loan.optString("copyId")}?.optString("title")?:"",people.find{it.optString("id")==loan.optString("borrowerId")}?.optString("name")?:"").any{it.contains(search,true)}}
 PhoneStatusPill("${loans.count{it.isNull("returnedDate")}} on loan · ${loans.count{!it.isNull("returnedDate")}} returned")
 if(loans.isEmpty())PhoneEmptyState("No matching loans","Try another book, borrower or catalog number.")
 loans.forEach{loan->Card(Modifier.fillMaxWidth()){Column(Modifier.padding(16.dp),verticalArrangement=Arrangement.spacedBy(6.dp)){
  Text(books.find{it.optString("id")==loan.optString("copyId")}?.optString("title")?:loan.optString("copyId"),style=MaterialTheme.typography.titleMedium)
  Text(loan.optString("copyId")+" · "+(people.find{it.optString("id")==loan.optString("borrowerId")}?.optString("name")?:"Borrower"))
  Text("Out: ${loan.optString("checkoutDate")}\nDue: ${loan.optString("dueDate").ifBlank{"Not set"}}\nReturned: ${if(loan.isNull("returnedDate"))"Still on loan" else loan.optString("returnedDate")}")
 }}}
}
private fun csvCell(value:String):String{val safe=if(value.firstOrNull() in listOf('=','+','@','-','\t','\r'))"'"+value else value;return "\""+safe.replace("\"","\"\"")+"\""}
@Composable fun ManageLibraryDialog(api:NativeApi,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onForget:()->Unit,initialAction:String="",onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var busy by remember{mutableStateOf(false)};var notice by remember{mutableStateOf("")}
 val state=snapshot.optJSONObject("state")?:JSONObject();var name by remember{mutableStateOf(state.optString("libraryName","Carribrary"))}
 var subtitle by remember{mutableStateOf(state.optString("bannerSubtitle","A shelf worth sharing"))};var saying by remember{mutableStateOf(state.optString("encouragingMessage","A place for every book."))}
 var completeBackup by remember{mutableStateOf(initialAction=="backup")};
 var fileContent by remember{mutableStateOf("")};var restore by remember{mutableStateOf<JSONObject?>(null)}
 var devices by remember{mutableStateOf<JSONObject?>(null)};var pairing by remember{mutableStateOf("")};var expiry by remember{mutableStateOf("")};var revoke by remember{mutableStateOf<JSONObject?>(null)}
 fun task(block:suspend()->Unit){scope.launch{busy=true;notice="";try{block()}catch(e:Exception){notice=e.message?:"Operation failed";if(e is ApiError&&e.status==409)runCatching{onChanged(api.refresh())}}finally{busy=false}}}
 val saveBackup=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/json")){uri->if(uri!=null)task{val text=fileContent;withContext(Dispatchers.IO){context.contentResolver.openOutputStream(uri,"wt")!!.use{it.write(text.toByteArray(Charsets.UTF_8))}};notice="Backup saved. It contains private borrower and loan details."}}
 val saveCsv=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("text/csv")){uri->if(uri!=null)task{val text=fileContent;withContext(Dispatchers.IO){context.contentResolver.openOutputStream(uri,"wt")!!.use{it.write(text.toByteArray(Charsets.UTF_8))}};notice="CSV saved"}}
 val openBackup=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()){uri->if(uri!=null)task{
  restore=withContext(Dispatchers.IO){context.contentResolver.openInputStream(uri)!!.use{input->val output=ByteArrayOutputStream();val buffer=ByteArray(8192);var total=0;while(true){val count=input.read(buffer);if(count<0)break;total+=count;require(total<=10*1024*1024){"Backup is too large (maximum 10 MB)."};output.write(buffer,0,count)};JSONObject(output.toString("UTF-8")).also{require(it.optInt("version") in 1..7&&it.optJSONArray("books")!=null&&it.optJSONArray("borrowers")!=null&&it.optJSONArray("loans")!=null){"Select a Carribrary JSON backup."}}}}
 }}
 LaunchedEffect(initialAction){when(initialAction){
  "backup"->{completeBackup=true}
  "restore"->openBackup.launch(arrayOf("application/json","text/plain","application/octet-stream"))
  "devices"->task{devices=api.devices()}
  "csv"->task{val fresh=api.refresh();onChanged(fresh);val fields=listOf("id","isbn","title","author","series","publisher","collection");fileContent="\uFEFF"+fields.joinToString(",")+"\r\n"+fresh.getJSONObject("state").getJSONArray("books").objects().filter{!it.optBoolean("archived")}.joinToString("\r\n"){book->fields.joinToString(","){csvCell(if(it=="collection")fresh.getJSONObject("state").optJSONArray("collections")?.objects()?.find{c->c.optString("id")==book.optString("collectionId","general")}?.optString("name")?:"General" else book.optString(it))}};saveCsv.launch("home-library-books.csv")}
 }}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  PhoneSectionHeading("Manage library","Library appearance, safe backups and approved devices.")
  PhoneDisclosure("Library & appearance",initialAction.isBlank()){
  LibraryBrandingControls(api,onChanged)
  OutlinedTextField(name,{name=it},label={Text("Library name")},modifier=Modifier.fillMaxWidth(),enabled=!busy)
  Button(enabled=!busy,onClick={task{onChanged(api.mutate("setLibraryName",JSONObject().put("libraryName",name),snapshot.optLong("revision")));notice="Library name saved"}}){Text("Save name")}
  OutlinedTextField(subtitle,{subtitle=it},label={Text("Banner subtitle")},modifier=Modifier.fillMaxWidth(),enabled=!busy)
  OutlinedTextField(saying,{saying=it},label={Text("Encouraging saying")},modifier=Modifier.fillMaxWidth(),enabled=!busy)
  Text("Leave a saying blank to hide it. Subtitle: 160 characters; saying: 240.",style=MaterialTheme.typography.bodySmall)
  Button(enabled=!busy&&subtitle.length<=160&&saying.length<=240,onClick={task{val fresh=api.refresh();onChanged(api.mutate("setLibraryPresentation",JSONObject().put("bannerSubtitle",subtitle).put("encouragingMessage",saying),fresh.optLong("revision")));notice="Library sayings saved"}}){Text("Save sayings")}
  }
  PhoneDisclosure("Data, exports & recovery",initialAction in listOf("backup","restore","csv")){
  OutlinedButton(enabled=!busy,onClick={completeBackup=true}){Text("Complete server data backup")}
  OutlinedButton(enabled=!busy,onClick={task{val fresh=api.refresh();onChanged(fresh);fileContent=fresh.getJSONObject("state").toString(2);saveBackup.launch("home-library-${LocalDate.now()}.json")}}){Text("Export library JSON")}
  OutlinedButton(enabled=!busy,onClick={openBackup.launch(arrayOf("application/json","text/plain","application/octet-stream"))}){Text("Restore library export")}
  Text("JSON backups contain private contacts and loans. Restore replaces the household library; an encrypted rollback backup is saved on the library server.",style=MaterialTheme.typography.bodySmall)
  OutlinedButton(enabled=!busy,onClick={task{val fresh=api.refresh();onChanged(fresh);val fields=listOf("id","isbn","title","author","series","publisher","collection");fileContent="\uFEFF"+fields.joinToString(",")+"\r\n"+(fresh.getJSONObject("state").getJSONArray("books").objects().filter{!it.optBoolean("archived")}.joinToString("\r\n"){book->fields.joinToString(","){csvCell(if(it=="collection")fresh.getJSONObject("state").optJSONArray("collections")?.objects()?.find{c->c.optString("id")==book.optString("collectionId","general")}?.optString("name")?:"General" else book.optString(it))}});saveCsv.launch("home-library-books.csv")}}){Text("Export books CSV")}
  OutlinedButton(enabled=!busy,onClick={task{val fresh=api.refresh();onChanged(fresh);val library=fresh.getJSONObject("state");val text=library.optString("libraryName","Carribrary")+"\n\n"+sortedCatalog(library.getJSONArray("books").objects().filter{!it.optBoolean("archived")},"title",false).joinToString("\n\n"){book->"${book.optString("id")} · ${book.optString("title")}\n${book.optString("author")}"+(if(book.optString("series").isBlank())"" else "\nSeries: "+book.optString("series"))};context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).setType("text/plain").putExtra(Intent.EXTRA_TEXT,text),"Share book list"))}}){Text("Share book list as text")}
  }
  PhoneDisclosure("Approved phones",initialAction=="devices"){
  OutlinedButton(enabled=!busy,onClick={task{devices=api.devices()}}){Text("Paired phones")}
  if(devices!=null){
   Text("Approved phones can manage the entire library. Pair only trusted household phones.",style=MaterialTheme.typography.bodySmall)
   Button(enabled=!busy,onClick={task{val pair=api.createPairing();pairing="hl-pair:"+Base64.encodeToString(pair.toString().toByteArray(Charsets.UTF_8),Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP);expiry=pair.optString("expires")}}){Text("Create private pairing QR")}
   if(pairing.isNotBlank()){val qr=remember(pairing){catalogQr(pairing)};Image(qr.asImageBitmap(),"Private phone pairing QR",Modifier.fillMaxWidth().aspectRatio(1f));Text("Private · single use · expires $expiry. Scan using Carribrary on the new phone.");SelectionContainer{Text(pairing,style=MaterialTheme.typography.bodySmall)}}
   devices!!.optJSONArray("devices")?.objects()?.forEach{device->Text(device.optString("name")+" · "+(if(device.optBoolean("revoked"))"Revoked" else "Approved")+(if(device.optString("id")==devices!!.optString("currentDeviceId"))" · This phone" else ""));if(!device.optBoolean("revoked"))TextButton(enabled=!busy,onClick={revoke=device}){Text("Revoke phone")}}
  }
  }
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth());if(notice.isNotBlank())Text(notice)
  TextButton(enabled=!busy,onClick=onClose){Text("Done")}
 }}}
 restore?.let{backup->AlertDialog(onDismissRequest={restore=null},title={Text("Replace the household library?")},text={Text("Restore ${backup.getJSONArray("books").length()} book records and ${backup.getJSONArray("borrowers").length()} borrower records? Everyone will see this change. The current library is backed up on the library server before replacement.")},confirmButton={TextButton(enabled=!busy,onClick={task{val fresh=api.refresh();onChanged(api.mutate("restore",backup,fresh.optLong("revision")));restore=null;notice="Library restored"}}){Text("Restore")}},dismissButton={TextButton(enabled=!busy,onClick={restore=null}){Text("Cancel")}})}
 revoke?.let{device->val self=device.optString("id")==devices?.optString("currentDeviceId");AlertDialog(onDismissRequest={revoke=null},title={Text("Revoke ${device.optString("name")}? ")},text={Text(if(self)"This phone will disconnect and need a new pairing QR from another approved phone or the server’s admin." else "This phone will lose access and need a new private pairing QR to reconnect.")},confirmButton={TextButton(enabled=!busy,onClick={task{api.revokeDevice(device.optString("id"));revoke=null;if(self)onForget() else{devices=api.devices();notice="Phone revoked"}}}){Text("Revoke")}},dismissButton={TextButton(enabled=!busy,onClick={revoke=null}){Text("Cancel")}})}
 if(completeBackup)CompleteBackupDialog(api,onClose={completeBackup=false})
}
