package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.launch
import org.json.JSONObject
@Composable fun PurchaseSuggestionReview(api:NativeApi){
 val scope=rememberCoroutineScope();var items by remember{mutableStateOf(emptyList<JSONObject>())};var note by remember{mutableStateOf("Loading purchase suggestions…")};var busy by remember{mutableStateOf(false)};var decision by remember{mutableStateOf<JSONObject?>(null)}
 fun load(){scope.launch{busy=true;try{items=api.tools("purchase-suggestions").optJSONArray("items")?.objects()?:emptyList();note=if(items.isEmpty())"No purchase suggestions yet." else ""}catch(e:Exception){note="Purchase suggestions could not load. Update the server to 2.0.50 or later and check the connection."}finally{busy=false}}}
 LaunchedEffect(Unit){load()}
 Text("Reader purchase suggestions",style=MaterialTheme.typography.titleLarge);Text("Books and series suggested by readers. Considering a suggestion does not purchase or reserve a book.")
 if(note.isNotBlank())Text(note);TextButton(enabled=!busy,onClick={load()}){Text("Refresh suggestions")}
 items.forEach{x->OutlinedCard(Modifier.fillMaxWidth()){Column(Modifier.padding(12.dp),verticalArrangement=Arrangement.spacedBy(6.dp)){Text(x.optString("title"),style=MaterialTheme.typography.titleMedium);Text("${x.optString("kind")} · ${x.optString("status")} · ${x.optString("submittedAt").take(10)}");if(x.optString("author").isNotBlank())Text(x.optString("author"));if(x.optString("isbn").isNotBlank())Text("ISBN ${x.optString("isbn")}");if(x.optString("notes").isNotBlank())Text(x.optString("notes"));for((status,label)in listOf("considering" to "Keep for consideration","declined" to "Decline suggestion","delete" to "Delete suggestion")){if(status!=x.optString("status"))TextButton(enabled=!busy,onClick={decision=JSONObject().put("id",x.optString("id")).put("expectedStatus",x.optString("status")).put("status",status)}){Text(label)}}}}}
 decision?.let{input->AlertDialog(onDismissRequest={decision=null},title={Text(if(input.optString("status")=="delete")"Delete suggestion permanently?" else "Review purchase suggestion?")},text={Text(if(input.optString("status")=="delete")"Remove it from the current server. Older complete backups may retain it." else "This changes its review status only. It does not buy, reserve or add a book to the catalog.")},confirmButton={TextButton(onClick={decision=null;scope.launch{busy=true;try{api.tools("purchase-suggestions",input);load()}catch(e:Exception){note=e.message?:"Could not update suggestion"}finally{busy=false}}}){Text("Confirm")}},dismissButton={TextButton(onClick={decision=null}){Text("Cancel")}})}
}
