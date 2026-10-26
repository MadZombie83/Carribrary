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
@Composable fun VerifyBackupDialog(api:NativeApi,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var selected by remember{mutableStateOf<android.net.Uri?>(null)};var password by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")}
 val choose=rememberLauncherForActivityResult(ActivityResultContracts.OpenDocument()){uri->selected=uri;note=""}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Verify complete backup",style=MaterialTheme.typography.titleLarge)
  Text("Choose a saved .carribrary-backup file. It is uploaded privately to your paired server, checked and deleted from temporary server storage. Your saved file and the live library are not changed.")
  OutlinedButton(enabled=!busy,onClick={choose.launch(arrayOf("application/octet-stream","*/*"))}){Text(if(selected==null)"Choose saved backup" else "Choose a different backup")}
  if(selected!=null)Text("Backup selected",style=MaterialTheme.typography.bodySmall)
  OutlinedTextField(password,{password=it;note=""},label={Text("Backup password (12–128 characters)")},visualTransformation=PasswordVisualTransformation(),enabled=!busy,modifier=Modifier.fillMaxWidth())
  Text("Checks authenticated encryption, every file hash and SQLite integrity. Up to 2 GB of data / 10,000 files. Temporary server free space is needed for the upload, decrypted archive and recovered files. Keep this screen open; large files can be checked with the offline server tool instead.",style=MaterialTheme.typography.bodySmall)
  Button(enabled=!busy&&selected!=null&&password.length in 12..128,onClick={val uri=selected?:return@Button;scope.launch{busy=true;note="Uploading and checking the backup. No library is being restored.";try{val report=withContext(Dispatchers.IO){context.contentResolver.openInputStream(uri)!!.use{api.verifyCompleteBackup(password,it)}};note="Backup integrity verified: ${report.optInt("files")} files, ${(report.optLong("bytes")/1048576.0).toInt()} MB. Created ${report.optString("createdUtc")}. Server ${report.optString("version")}, library format ${report.optInt("libraryFormat")}. Temporary recovered data was deleted. No library was restored.";if(!report.optBoolean("supportedLibraryFormat"))note+=" This server does not support the recorded library format; do not restore with this version.";if(report.optBoolean("sameWindowsComputerRequired"))note+=" Windows keys/settings may require the original Windows computer."}catch(error:Exception){note=(error.message?:"Verification did not finish.")+" No library was restored."}finally{busy=false;password=""}}}){Text("Verify saved backup")}
  if(busy)LinearProgressIndicator(Modifier.fillMaxWidth())
  if(note.isNotBlank())Text(note)
  Text("Verification is not a complete recovery rehearsal. Restore requires a stopped server, a compatible version and separate external HTTPS/service settings. Borrowers still need their private-list credentials. Restoring an old backup rewinds passwords, deletions and revocations. See Help → Verify complete backup for offline commands.",style=MaterialTheme.typography.bodySmall)
  TextButton(enabled=!busy,onClick=onClose){Text("Close")}
 }}}
}
