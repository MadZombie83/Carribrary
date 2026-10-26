package com.household.homelibrary
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import kotlinx.coroutines.launch
import org.json.JSONObject

@Composable fun RenewalsDialog(api:NativeApi,initial:JSONObject,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val scope=rememberCoroutineScope();var snapshot by remember{mutableStateOf(initial)};val state=snapshot.optJSONObject("state")?:JSONObject();var policy by remember{mutableStateOf(JSONObject((state.optJSONObject("renewalPolicy")?:JSONObject().put("enabled",true).put("days",14).put("maximum",2)).toString()))};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")}
 fun action(name:String,input:JSONObject){scope.launch{busy=true;try{snapshot=api.mutate(name,input,snapshot.optLong("revision"));onChanged(snapshot);note="Renewal settings updated"}catch(e:Exception){note=e.message?:"Could not update renewal";runCatching{snapshot=api.refresh();onChanged(snapshot)}}finally{busy=false}}}
 LaunchedEffect(Unit){runCatching{snapshot=api.refresh();onChanged(snapshot);policy=JSONObject(snapshot.getJSONObject("state").optJSONObject("renewalPolicy")?.toString()?:"{}")} .onFailure{note=it.message?:"Offline"}}
 Dialog(onDismissRequest={if(!busy)onClose()},properties=DialogProperties(usePlatformDefaultWidth=false)){Surface(Modifier.fillMaxSize()){Column(Modifier.safeDrawingPadding().padding(16.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
 Row{Text("Borrower renewals",style=MaterialTheme.typography.titleLarge,modifier=Modifier.weight(1f));TextButton(enabled=!busy,onClick=onClose){Text("Close")}}
 Text("Borrowers request extensions from My loans. Holds and open book requests prevent renewal. Approval extends the due date without returning the book.")
 Row{Switch(policy.optBoolean("enabled"),{policy=JSONObject(policy.toString()).put("enabled",it)});Text("Allow renewal requests")};Row{Switch(policy.optBoolean("automatic"),{policy=JSONObject(policy.toString()).put("automatic",it)});Text("Automatically approve eligible requests")}
 for((key,label)in listOf("days" to "Days per extension (1–90)","maximum" to "Maximum renewals (1–20)"))OutlinedTextField(policy.optString(key),{policy=JSONObject(policy.toString()).put(key,it)},label={Text(label)})
 Button(enabled=!busy,onClick={action("setRenewalPolicy",JSONObject().put("enabled",policy.optBoolean("enabled")).put("automatic",policy.optBoolean("automatic")).put("days",policy.optString("days").toIntOrNull()?:0).put("maximum",policy.optString("maximum").toIntOrNull()?:0))}){Text("Save renewal policy")}
 Text("Requests and history",style=MaterialTheme.typography.titleLarge)
 val loans=state.optJSONArray("loans")?.objects()?:emptyList();if(loans.none{it.optJSONArray("renewals")?.length()?.let{it>0}==true})Text("No renewal requests yet")
 for(loan in loans)for(r in loan.optJSONArray("renewals")?.objects()?:emptyList())PhonePanel{Text((state.optJSONArray("books")?.objects()?.find{it.optString("id")==loan.optString("copyId")}?.optString("title")?:loan.optString("copyId")),style=MaterialTheme.typography.titleLarge);Text(state.optJSONArray("borrowers")?.objects()?.find{it.optString("id")==loan.optString("borrowerId")}?.optString("name")?:"Borrower");Text(r.optString("oldDueDate")+" → "+r.optString("newDueDate")+" · "+r.optString("status"));if(r.optString("status")=="pending"){Button(enabled=!busy,onClick={action("approveRenewal",JSONObject().put("loanId",loan.optString("id")).put("renewalId",r.optString("id")))}){Text("Approve extension")};OutlinedButton(enabled=!busy,onClick={action("declineRenewal",JSONObject().put("loanId",loan.optString("id")).put("renewalId",r.optString("id")))}){Text("Decline")}}}
 if(note.isNotBlank())Text(note)
 }}}
}
