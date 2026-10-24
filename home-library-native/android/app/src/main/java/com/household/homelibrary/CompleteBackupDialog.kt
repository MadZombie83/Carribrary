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
import java.time.LocalDate
@Composable fun CompleteBackupDialog(api:NativeApi,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var password by remember{mutableStateOf("")};var confirmation by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")}
 val save=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/octet-stream")){uri->if(uri==null){password="";confirmation=""}else scope.launch{
  busy=true;note="Preparing a consistent backup. Changes pause briefly, then resume automatically. Keep this screen open."
  try{withContext(Dispatchers.IO){context.contentResolver.openOutputStream(uri,"wt")!!.use{api.completeBackup(password,it)}};note="Complete backup saved. Keep the file and its password safely, preferably on another drive."}
  catch(error:Exception){note=(error.message?:"Backup failed.")+" Delete the incomplete file and try again."}
  finally{busy=false;password="";confirmation=""}
 }}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Complete server data backup",style=MaterialTheme.typography.titleLarge)
  Text("Includes current books, loans, accounts, encrypted borrower vaults, pairing keys, keyring and saved server settings. The server prepares the snapshot consistently; you do not need to stop it.")
  Text("Password-protected. Keep the password safely: it cannot be reset. Borrower private lists still need their separate passphrases. Windows-protected settings/keys require the same Windows computer.")
  OutlinedTextField(password,{password=it},label={Text("Backup password (12–128 characters)")},visualTransformation=PasswordVisualTransformation(),enabled=!busy,modifier=Modifier.fillMaxWidth())
  OutlinedTextField(confirmation,{confirmation=it},label={Text("Confirm password")},visualTransformation=PasswordVisualTransformation(),enabled=!busy,modifier=Modifier.fillMaxWidth())
  Text("Current data only, up to 2 GB / 10,000 files. Earlier backups, program files, external HTTPS/proxy certificates, system service/tasks and environment-only secrets are separate. Recovery uses the offline tool with the server stopped; Restore library export is for JSON only.",style=MaterialTheme.typography.bodySmall)
  Button(enabled=!busy&&password.length in 12..128&&password==confirmation,onClick={save.launch("Carribrary-complete-${LocalDate.now()}.carribrary-backup")}){Text("Choose where to save")}
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
  if(note.isNotBlank())Text(note)
  TextButton(enabled=!busy,onClick=onClose){Text("Close")}
 }}}
}
