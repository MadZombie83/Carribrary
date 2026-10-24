package com.household.homelibrary
import android.content.Intent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import kotlinx.coroutines.launch
import org.json.JSONObject

@Composable fun BorrowerAccountsDialog(api:NativeApi,snapshot:JSONObject,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var accounts by remember{mutableStateOf(emptyList<JSONObject>())};var busy by remember{mutableStateOf(false)};var error by remember{mutableStateOf("")};var deleting by remember{mutableStateOf<JSONObject?>(null)};var resetUrl by remember{mutableStateOf("")};var confirm by remember{mutableStateOf<JSONObject?>(null)}
 fun load(){scope.launch{busy=true;try{accounts=api.accountAdmin().optJSONArray("accounts")?.objects()?:emptyList();error=""}catch(e:Exception){error=e.message?:"Update the server or reconnect."}finally{busy=false}}}
 fun change(action:String,input:JSONObject){scope.launch{busy=true;try{val result=api.accountAdmin(action,input);result.optJSONObject("snapshot")?.let(onChanged);if(action=="reset")resetUrl=result.optString("url");accounts=api.accountAdmin().optJSONArray("accounts")?.objects()?:emptyList();error=""}catch(e:Exception){error=e.message?:"Account change failed"}finally{busy=false}}}
 LaunchedEffect(Unit){load()};val state=snapshot.optJSONObject("state")?:JSONObject();val profiles=state.optJSONArray("readers")?.objects()?:emptyList();val borrowers=state.optJSONArray("borrowers")?.objects()?.filter{!it.optBoolean("archived")}?:emptyList()
 Dialog(onDismissRequest=onClose,properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){Row{Text("Borrower accounts",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(onClick=onClose){Text("Close")}};Text("Visitors sign up from the shared library QR. Approve and link only the borrower/profile that belongs to that person.");if(busy)LinearProgressIndicator(Modifier.fillMaxWidth());if(error.isNotBlank())Text(error,color=MaterialTheme.colorScheme.error);Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
 if(resetUrl.isNotBlank())Card{Column(Modifier.padding(12.dp)){Text("Private one-use password reset link · expires in 30 minutes");Button(onClick={context.startActivity(Intent.createChooser(Intent(Intent.ACTION_SEND).apply{type="text/plain";putExtra(Intent.EXTRA_TEXT,"Your Carribrary password reset link (valid 30 minutes):\n$resetUrl")},"Send private reset link"))}){Text("Share reset link")};TextButton(onClick={resetUrl=""}){Text("Dismiss")}}}
 if(accounts.isEmpty())Text("No accounts yet. Share the library QR and ask visitors to choose Create account / Sign in.")
 accounts.forEach{account->key(account.optString("id")){var choice by remember{mutableStateOf("")};var open by remember{mutableStateOf(false)};Card(Modifier.fillMaxWidth()){Column(Modifier.padding(14.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){Text(account.optString("name"),style=MaterialTheme.typography.titleLarge);Text("@${account.optString("username")} · ${account.optString("status")}");if(account.optString("contact").isNotBlank())Text(account.optString("contact"))
 if(account.optString("status")!="active"){
 val options=listOf("" to "Create new reader and borrower")+profiles.flatMap{profile->val removed=profile.optString("borrowerId").isNotBlank()&&borrowers.none{it.optString("id")==profile.optString("borrowerId")};if(removed)listOf("restore:"+profile.optString("id") to ("Restore borrower: "+profile.optString("name")),"replace:"+profile.optString("id") to ("New borrower for reader: "+profile.optString("name"))) else listOf("r:"+profile.optString("id") to ("Reader: "+profile.optString("name")))}+borrowers.map{"b:"+it.optString("id") to ("Borrower: "+it.optString("name"))}
 Box{OutlinedButton(onClick={open=true},enabled=!busy){Text(options.find{it.first==choice}?.second?:"Choose profile")};DropdownMenu(expanded=open,onDismissRequest={open=false}){options.forEach{(id,label)->DropdownMenuItem(text={Text(label)},onClick={choice=id;open=false})}}}
 Button(enabled=!busy,onClick={confirm=JSONObject().put("action","approve").put("accountId",account.optString("id")).put("readerId",if(choice.substringBefore(":") in listOf("r","restore","replace"))choice.substringAfter(":") else "").put("borrowerId",if(choice.startsWith("b:"))choice.drop(2) else "").put("restoreBorrower",choice.startsWith("restore:")).put("createBorrower",choice.startsWith("replace:")).put("label",account.optString("name"))}){Text(if(account.optString("status")=="blocked")"Unblock & approve" else "Approve / activate")}
 }else Text("Linked reading profile: "+(profiles.find{it.optString("id")==account.optString("readerId")}?.optString("name")?:"Needs relinking"))
 TextButton(enabled=!busy,onClick={deleting=account;resetUrl=""}){Text("Delete account / personal information")}
 if(account.optString("status")=="blocked")TextButton(enabled=!busy,onClick={confirm=JSONObject().put("action","unblock").put("accountId",account.optString("id")).put("label",account.optString("name"))}){Text("Unblock (review link next)")}
 if(account.optString("status")!="blocked")Row{TextButton(enabled=!busy,onClick={confirm=JSONObject().put("action","block").put("accountId",account.optString("id")).put("label",account.optString("name"))}){Text("Block account")};TextButton(enabled=!busy,onClick={confirm=JSONObject().put("action","reset").put("accountId",account.optString("id")).put("label",account.optString("name"))}){Text("Reset password")}}
 }}}}
 TextButton(enabled=!busy,onClick={load()}){Text("Refresh accounts")}
 }}}}
 deleting?.let{account->AccountDeletionDialog(api,account,snapshot,onChanged){deleting=null;load()}}
 confirm?.let{pending->AlertDialog(onDismissRequest={confirm=null},title={Text(when(pending.optString("action")){"approve"->"Approve ${pending.optString("label")}?";"block"->"Block this account?";"unblock"->"Unblock this account?";else->"Create a password reset link?"})},text={Text(when(pending.optString("action")){"approve"->"Confirm this person owns the selected lending records and any household reading profile being transferred. Their encrypted private lists stay inaccessible to admins.";"unblock"->"The account returns to pending approval. Choose a borrower/profile and approve it next. Old sign-ins and reading QR links stay revoked.";"block"->"All current sign-ins and this profile’s personal reading QR will stop. Library history stays.";else->"Give the one-use sign-in reset link privately to this borrower. It expires in 30 minutes and cannot unlock private lists. Older reading must be encrypted first."})},confirmButton={TextButton(onClick={val action=pending.optString("action");confirm=null;change(action,pending)}){Text("Confirm")}},dismissButton={TextButton(onClick={confirm=null}){Text("Cancel")}})}
}
