package com.household.homelibrary
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
import org.json.JSONObject

@Composable fun PhoneMoreMenu(enabled:Boolean,requests:Int,canRefresh:Boolean=true,onSelect:(String)->Unit){
 Text("More",style=MaterialTheme.typography.headlineMedium)
 Text("Manage lending, sharing and library settings.")
 for((title,items)in listOf(
  "People & lending" to listOf("lending" to "Lending & holds / batch lending","Borrowers" to "Borrowers","Requests" to "Requests ($requests)","accounts" to "Borrower accounts & approvals","History" to "Loan history"),
  "Sharing & collections" to listOf("acquisitions" to "Books to buy","sharing" to "Share library / QR","collections" to "Manage collections"),
  "Settings & backups" to listOf("dataControls" to "Data & privacy / clear history","theme" to "Colors","settings" to "Library name & sayings","copyTools" to "Copy labels & numbering","recycle" to "Recycle Bin","reminders" to "Email reminders","setup" to "Setup & updates","csv" to "Export catalog CSV","backup" to "Complete server data backup","restore" to "Restore library backup","devices" to "Paired admin phones","connection" to "Connection"))) {
  var expanded by remember(title){mutableStateOf(false)}
  Card(Modifier.fillMaxWidth()){Column(Modifier.padding(12.dp)){TextButton(onClick={expanded=!expanded},modifier=Modifier.fillMaxWidth()){Text((if(expanded)"▾ " else "▸ ")+title)};if(expanded)items.forEach{(action,label)->TextButton(enabled=enabled||action in listOf("Borrowers","Requests","History","theme","connection","dataControls"),onClick={onSelect(action)},modifier=Modifier.fillMaxWidth()){Text(label)}}}}
 }
 Row(horizontalArrangement=Arrangement.spacedBy(8.dp)){OutlinedButton(onClick={onSelect("refresh")},enabled=canRefresh){Text("Refresh")};OutlinedButton(onClick={onSelect("help")}){Text("Help")};TextButton(onClick={onSelect("privacy")}){Text("App info")}}
}
@Composable fun PhoneHelpDialog(onClose:()->Unit){val context=LocalContext.current;val topics=remember{runCatching{JSONObject(context.assets.open("help.json").bufferedReader().use{it.readText()}).getJSONArray("topics").objects()}.getOrDefault(emptyList())};var search by remember{mutableStateOf("")}
 Dialog(onDismissRequest=onClose,properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){Row{Text("Carribrary help",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(onClick=onClose){Text("Close")}};OutlinedTextField(search,{search=it},label={Text("Search help")},modifier=Modifier.fillMaxWidth());Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){topics.filter{(it.optString("title")+" "+it.optString("body")).contains(search,true)}.forEach{topic->var open by remember(topic.optString("title")){mutableStateOf(false)};Card(Modifier.fillMaxWidth()){Column(Modifier.padding(14.dp)){TextButton(onClick={open=!open}){Text((if(open)"▾ " else "▸ ")+topic.optString("title"))};if(open||search.isNotBlank())Text(topic.optString("body"))}}};Text("Android ${BuildConfig.VERSION_NAME}",style=MaterialTheme.typography.bodySmall)}}}}
}
