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

internal val phoneToolCategories=listOf(
 "Library & Appearance" to listOf("theme" to "Theme & artwork","settings" to "Library identity"),
 "Books & Organization" to listOf("acquisitions" to "Books to buy","sharing" to "Share library / QR","collections" to "Manage collections","copyTools" to "Copy labels & numbering","bulk" to "Bulk edit books","catalogBatch" to "Batch add / CSV import"),
 "Borrowing & Reminders" to listOf("selfService" to "Borrower self-service & kiosks","lending" to "Lending & holds / batch lending","Requests" to "Book requests","renewals" to "Borrower renewals","reminders" to "Email reminders","requestAlerts" to "Request notifications"),
 "People & Access" to listOf("patronCards" to "Library cards / PINs","Borrowers" to "Borrowers","accounts" to "Borrower accounts & approvals","devices" to "Paired admin phones","browserAdmins" to "iPhone / browser pairing"),
 "Data & History" to listOf("libraryInsights" to "Analytics & insights","History" to "Loan history","dataControls" to "Data & privacy / clear history","recycle" to "Recycle Bin","csv" to "Export catalog CSV","backup" to "Complete server data backup","portableBackups" to "Weekly portable backups","backupVerify" to "Verify complete backup","restore" to "Restore library backup"),
 "System & Advanced" to listOf("setup" to "Setup & updates","connection" to "Connection")
)
@Composable fun PhoneMoreMenu(enabled:Boolean,requests:Int,canRefresh:Boolean=true,onSelect:(String)->Unit){
 var category by remember{mutableStateOf("")};var query by remember{mutableStateOf("")}
 PhoneSectionHeading(if(category.isBlank())"Settings & Tools" else category,"Library tools, grouped by purpose. Your saved settings and access stay the same.")
 if(category.isNotBlank())OutlinedButton(onClick={category="";query=""}){Text("← Settings & Tools")}
 OutlinedTextField(query,{query=it},label={Text("Search all settings & tools")},modifier=Modifier.fillMaxWidth())
 val matching=phoneToolCategories.filter{category.isBlank()||query.isNotBlank()||it.first==category}
 for((title,items)in matching){
  val found=items.filter{query.isBlank()||(title+" "+it.second).contains(query,true)}
  if(query.isBlank()&&category.isBlank())PhonePanel{Text(title,style=MaterialTheme.typography.titleLarge);Text("${items.size} tools",color=MaterialTheme.colorScheme.onSurfaceVariant);OutlinedButton(onClick={category=title}){Text("Open category →")}}
  else if(found.isNotEmpty())PhonePanel{Text(title,style=MaterialTheme.typography.titleLarge);for((action,label)in found){TextButton(enabled=enabled||action in listOf("Borrowers","Requests","History","theme","connection","dataControls"),onClick={onSelect(action)},modifier=Modifier.fillMaxWidth()){Text(label+if(action=="Requests")" ($requests)" else "",color=if(action=="restore")MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.primary)}}}
 }
 if(query.isNotBlank()&&phoneToolCategories.flatMap{(title,items)->items.filter{(title+" "+it.second).contains(query,true)}}.isEmpty())PhoneEmptyState("No matching tools","Try a name such as backup, colors, requests or pairing.")
 Row(horizontalArrangement=Arrangement.spacedBy(8.dp)){OutlinedButton(onClick={onSelect("refresh")},enabled=canRefresh){Text("Refresh")};OutlinedButton(onClick={onSelect("help")}){Text("Help")};TextButton(onClick={onSelect("privacy")}){Text("App info")}}
}
@Composable fun PhoneHelpDialog(onClose:()->Unit){val context=LocalContext.current;val topics=remember{runCatching{JSONObject(context.assets.open("help.json").bufferedReader().use{it.readText()}).getJSONArray("topics").objects()}.getOrDefault(emptyList())};var search by remember{mutableStateOf("")}
 Dialog(onDismissRequest=onClose,properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.fillMaxSize().safeDrawingPadding().padding(16.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){Row{Text("Carribrary help",style=MaterialTheme.typography.headlineSmall,modifier=Modifier.weight(1f));TextButton(onClick=onClose){Text("Close")}};OutlinedTextField(search,{search=it},label={Text("Search help")},modifier=Modifier.fillMaxWidth());Column(Modifier.weight(1f).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){topics.filter{(it.optString("title")+" "+it.optString("body")).contains(search,true)}.forEach{topic->var open by remember(topic.optString("title")){mutableStateOf(false)};Card(Modifier.fillMaxWidth()){Column(Modifier.padding(14.dp)){TextButton(onClick={open=!open}){Text((if(open)"▾ " else "▸ ")+topic.optString("title"))};if(open||search.isNotBlank())Text(topic.optString("body"))}}};PhoneProductFooter()}}}}
}

@Composable fun RequestAlertsDialog(api:NativeApi,onClose:()->Unit){Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){Text("Request notifications",style=MaterialTheme.typography.headlineSmall);RequestAlertControls(api);TextButton(onClick=onClose){Text("Close")}}}}}

@Composable fun PhoneProductFooter(){Text("© 2026 Chezburber Studios · Carribrary ${BuildConfig.VERSION_NAME}",style=MaterialTheme.typography.bodySmall,color=MaterialTheme.colorScheme.onSurfaceVariant,modifier=Modifier.fillMaxWidth().padding(vertical=12.dp),textAlign=androidx.compose.ui.text.style.TextAlign.Center)}
