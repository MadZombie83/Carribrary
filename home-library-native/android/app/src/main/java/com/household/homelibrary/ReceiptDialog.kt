package com.household.homelibrary
import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import org.json.JSONObject

@Composable fun ReceiptDialog(draft:JSONObject,onClose:()->Unit){
 val context=LocalContext.current;var message by remember{mutableStateOf("")}
 val medium=draft.optString("medium");val recipient=draft.optString("recipient");val body=draft.optString("body")
 Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Borrowing receipt",style=MaterialTheme.typography.headlineSmall)
  Text("For ${draft.optString("borrowerName")}"+(if(recipient.isNotBlank())" · $recipient" else ""))
  SelectionContainer{Text(body)}
  Text("Review the receipt, then tap Send in your texting or email app.",style=MaterialTheme.typography.bodySmall)
  if(medium in listOf("text","email"))Button(onClick={try{
   val intent=if(medium=="text")Intent(Intent.ACTION_SENDTO,Uri.fromParts("smsto",recipient,null)).putExtra("sms_body",body)
    else Intent(Intent.ACTION_SENDTO,Uri.parse("mailto:"+Uri.encode(recipient)+"?subject="+Uri.encode(draft.optString("subject"))+"&body="+Uri.encode(body))).putExtra(Intent.EXTRA_SUBJECT,draft.optString("subject")).putExtra(Intent.EXTRA_TEXT,body)
   context.startActivity(intent);message="Message app opened. Send the receipt there."
  }catch(_:android.content.ActivityNotFoundException){message="No compatible message app found. Copy the receipt instead."}}){Text(if(medium=="text")"Open text" else "Open email")}
  else Text("No single phone number or email address is recorded. You can copy this receipt.")
  OutlinedButton(onClick={(context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager).setPrimaryClip(ClipData.newPlainText("Borrowing receipt",body));message="Receipt copied"}){Text("Copy receipt")}
  if(message.isNotBlank())Text(message)
  TextButton(onClick=onClose){Text("Done")}
 }}}
}
