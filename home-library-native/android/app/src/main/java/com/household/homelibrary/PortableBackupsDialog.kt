package com.household.homelibrary
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONObject
@Composable fun PortableBackupsDialog(api:NativeApi,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var status by remember{mutableStateOf(JSONObject())};var enabled by remember{mutableStateOf(false)};var accepted by remember{mutableStateOf(false)};var password by remember{mutableStateOf("")};var confirmation by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")}
 fun task(block:suspend()->Unit){scope.launch{busy=true;note="";try{block()}catch(e:Exception){note=e.message?:"Operation failed"}finally{busy=false}}}
 LaunchedEffect(Unit){task{status=api.tools("backup/portable");enabled=status.optBoolean("enabled");accepted=enabled}}
 val save=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/octet-stream")){uri->if(uri!=null)task{withContext(Dispatchers.IO){context.contentResolver.openOutputStream(uri,"wt")!!.use{api.latestPortableBackup(it)}};note="Portable backup saved. Keep its password separately and a copy away from the server."}}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  Text("Portable replacement backup",style=MaterialTheme.typography.headlineSmall)
  Text("New complete archives contain their server keys inside password encryption, for recovery onto another computer. Older Windows archives require the original computer. Borrower private-list secrets remain separate.")
  Row{Checkbox(enabled,{enabled=it},enabled=!busy);Text("Automatic backup every seven days")}
  Text("Runs while the server is on, checked every 15 minutes. Retains only four successful automatic archives. Downloaded/manual copies are not removed. Copy the latest off the server to protect against drive loss.")
  OutlinedTextField(password,{password=it},label={Text(if(status.optBoolean("passwordSaved"))"New password (blank keeps saved)" else "Backup password (12–128 characters)")},visualTransformation=PasswordVisualTransformation(),enabled=!busy,modifier=Modifier.fillMaxWidth())
  OutlinedTextField(confirmation,{confirmation=it},label={Text("Confirm new password")},visualTransformation=PasswordVisualTransformation(),enabled=!busy,modifier=Modifier.fillMaxWidth())
  Text("The schedule password is protected on your server and never displayed. Keep it separately: existing archives cannot be recovered without their password. A changed password applies only to future backups.")
  Row{Checkbox(accepted,{accepted=it},enabled=!busy);Text("Keep only the four newest automatic backups")}
  Button(enabled=!busy&&status.has("enabled"),onClick={task{status=api.tools("backup/portable",JSONObject().put("enabled",enabled).put("password",password).put("confirmation",confirmation).put("retentionAccepted",accepted));password="";confirmation="";note="Schedule saved"}}){Text("Save weekly backup settings")}
  Text("${status.optInt("count")} retained · Last: ${status.optString("lastSuccessUtc").takeUnless{it=="null"||it.isBlank()}?:"none yet"}")
  if(status.optString("lastError").isNotBlank())Text(status.optString("lastError"))
  OutlinedButton(enabled=!busy&&status.optBoolean("enabled"),onClick={task{status=api.tools("backup/portable/run",JSONObject().put("requestId",java.util.UUID.randomUUID().toString())).getJSONObject("status");note="Portable backup ready"}}){Text("Create scheduled backup now")}
  OutlinedButton(enabled=!busy&&status.optInt("count")>0,onClick={save.launch("Carribrary-portable-latest.carribrary-backup")}){Text("Save latest to phone or cloud drive")}
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth());if(note.isNotBlank())Text(note)
  TextButton(enabled=!busy,onClick=onClose){Text("Close")}
 }}}
}
