package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import org.json.JSONObject
@Composable fun AdminNotificationDialog(data:JSONObject,error:String,onRefresh:()->Unit,onSelect:(String)->Unit,onClose:()->Unit){
 Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Admin notifications",style=MaterialTheme.typography.headlineSmall)
  Text("Opening an item does not dismiss it. Resolve it in the relevant tool, then refresh.")
  if(error.isNotBlank())Text(error,color=MaterialTheme.colorScheme.error)
  val items=data.optJSONArray("items")?.objects()?:emptyList()
  if(items.isEmpty()&&error.isBlank())Text("No items need attention.")
  items.forEach{n->OutlinedCard(Modifier.fillMaxWidth()){Column(Modifier.padding(12.dp)){Text("${n.optString("title")} (${n.optInt("count")})",style=MaterialTheme.typography.titleMedium);Text(n.optString("description"));TextButton(onClick={onSelect(n.optString("tool"))}){Text("Review")}}}}
  OutlinedButton(onClick=onRefresh){Text("Refresh notifications")};TextButton(onClick=onClose){Text("Close")}
 }}}
}
