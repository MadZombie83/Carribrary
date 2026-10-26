package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import org.json.JSONObject
@OptIn(ExperimentalLayoutApi::class)
@Composable fun RequestInbox(state:JSONObject,search:String,enabled:Boolean,onDecision:(String,String)->Unit,onHandOver:(JSONObject)->Unit){
 val books=state.optJSONArray("books")?.objects()?:emptyList();val loans=state.optJSONArray("loans")?.objects()?:emptyList()
 var filter by remember{mutableStateOf("open")}
 val all=(state.optJSONArray("requests")?.objects()?:emptyList()).filter{r->listOf(r.optString("name"),r.optString("contact"),r.optString("copyId"),books.find{it.optString("id")==r.optString("copyId")}?.optString("title")?:"").any{it.contains(search,true)}}.sortedByDescending{it.optString("submittedAt")}
 val requests=all.filter{filter=="all"||filter=="open"&&it.optString("status") in listOf("pending","approved")||filter=="closed"&&it.optString("status") !in listOf("pending","approved")}
 FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)){for((id,label)in listOf("open" to "Open","all" to "All","closed" to "Closed"))FilterChip(selected=filter==id,onClick={filter=id},label={Text(label)})}
 Text("${requests.size} matching requests",style=MaterialTheme.typography.labelLarge)
 Text("Requests hold their physical copy while awaiting approval. Approval keeps it held until handover; no loan starts before then. Manage holds under More → Lending & holds.",style=MaterialTheme.typography.bodySmall)
 if(requests.isEmpty())PhoneEmptyState("No matching requests","Try another status or search. New borrowing requests appear here.")
 requests.forEach{request->val book=books.find{it.optString("id")==request.optString("copyId")};val active=loans.any{it.optString("copyId")==request.optString("copyId")&&it.isNull("returnedDate")};val status=request.optString("status");val hold=(state.optJSONArray("holds")?.objects()?:emptyList()).find{it.optString("requestId")==request.optString("id")}
  Card(Modifier.fillMaxWidth()){Column(Modifier.padding(16.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){
   Text(book?.optString("title")?:request.optString("copyId"),style=MaterialTheme.typography.titleLarge)
   if(book!=null&&listOf("room","bookcase","shelf").any{book.optString(it).isNotBlank()})Text("Location: "+listOf("room","bookcase","shelf").map{book.optString(it)}.filter{it.isNotBlank()}.joinToString(" · "))
   Text("${request.optString("name")} · ${request.optString("contact")}")
   PhoneStatusPill(status.replaceFirstChar{it.uppercase()});Text("${request.optString("copyId")} · ${request.optString("submittedAt").take(10)}",style=MaterialTheme.typography.bodySmall)
   if(request.optString("message").isNotBlank())Text(request.optString("message"))
   if(active)Text("Currently on loan",style=MaterialTheme.typography.bodySmall);if(hold!=null)Text("Held: ${hold.optString("stage")}");if(hold==null&&status in listOf("pending","approved"))Text("Legacy request without a reservation. Resolve competing holds, then Approve to reserve this copy.")
   FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp)){
    if(status=="pending"||status=="approved"&&hold==null)Button(enabled=enabled&&book?.optBoolean("archived")!=true,onClick={onDecision("approveRequest",request.optString("id"))}){Text("Approve")}
    if(status=="approved")Button(enabled=enabled&&!active&&hold!=null&&book?.optBoolean("archived")!=true,onClick={onHandOver(request)}){Text("Check out / hand over")}
    if(status in listOf("pending","approved"))OutlinedButton(enabled=enabled,onClick={onDecision("declineRequest",request.optString("id"))}){Text("Decline")}
   }
  }}
 }
}
