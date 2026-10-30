package com.household.homelibrary
import android.graphics.*
import android.graphics.pdf.PdfDocument
import android.util.Base64
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import kotlinx.coroutines.launch
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.ByteArrayOutputStream

// CR80 is exactly 243 × 153 PDF points. Paper is independent of printer scaling.
fun patronCardPdf(cards:List<JSONObject>,state:JSONObject,paper:String,duplex:Boolean,crop:Boolean):ByteArray {
 require(cards.size in 1..500&&paper in listOf("single","letter","a4")&&cards.all{it.optString("id").matches(Regex("[A-Za-z0-9_-]{43}"))}){"Select 1–500 active cards."}
 val single=paper=="single";val pw=if(single)243 else if(paper=="a4")595 else 612;val ph=if(single)153 else if(paper=="a4")842 else 792;val cols=if(single)1 else 2;val per=if(single)1 else 8;val gap=17f;val left=if(single)0f else (pw-486f-gap)/2;val top=if(single)0f else 28f
 val palette=state.optJSONArray("savedThemes")?.objects()?.find{"custom-"+it.optString("id")==state.optString("libraryTheme")};val colors=libraryColors(state.optString("libraryTheme"),palette?.toString()?:"");val ink=colors.primary.toArgb()
 fun bitmap(key:String)=runCatching{val bytes=Base64.decode(state.optString(key),Base64.DEFAULT);BitmapFactory.decodeByteArray(bytes,0,bytes.size)}.getOrNull()
 val icon=bitmap("libraryIcon192");val art=bitmap("librarySidebarArtwork");val pdf=PdfDocument();val p=Paint(Paint.ANTI_ALIAS_FLAG)
 fun text(c:Canvas,value:String,x:Float,y:Float,width:Float,size:Float,bold:Boolean=false){p.color=Color.WHITE;p.isFakeBoldText=bold;p.textSize=size;while(p.measureText(value)>width&&p.textSize>6f)p.textSize-=.25f;val fitted=if(p.measureText(value)>width)value.take(p.breakText(value,true,width-p.measureText("…"),null))+"…" else value;c.drawText(fitted,x,y,p)}
 try{var pageNo=1;for(batch in cards.chunked(per))for(back in if(duplex)listOf(false,true)else listOf(false)){
  val page=pdf.startPage(PdfDocument.PageInfo.Builder(pw,ph,pageNo++).create());val c=page.canvas;c.drawColor(Color.WHITE)
  for((i,card)in batch.withIndex()){
   val col=if(back&&!single)1-i%2 else i%cols;val x=left+col*(243+gap);val y=top+(i/cols)*(153+gap)
   c.save();c.translate(x,y);c.clipRect(0f,0f,243f,153f);p.color=ink;p.alpha=255;c.drawRect(0f,0f,243f,153f,p)
   art?.let{val scale=maxOf(243f/it.width,153f/it.height);val w=it.width*scale;val h=it.height*scale;p.alpha=65;c.drawBitmap(it,null,RectF((243-w)/2,(153-h)/2,(243+w)/2,(153+h)/2),p);p.alpha=255}
   if(back){text(c,state.optString("libraryName"),12f,28f,219f,15f,true);text(c,state.optString("bannerSubtitle"),12f,50f,219f,10f);text(c,"Present this card to borrow books.",12f,85f,219f,10f);text(c,"Report a lost card to the librarian.",12f,103f,219f,10f);text(c,"Keep your PIN separate and private.",12f,121f,219f,10f)}else{
    icon?.let{c.drawBitmap(it,null,RectF(12f,12f,38f,38f),null)};val tx=if(icon==null)12f else 43f;text(c,state.optString("libraryName"),tx,28f,147-tx,14f,true);text(c,state.optString("bannerSubtitle"),12f,54f,126f,9f);text(c,"LIBRARY MEMBER",12f,78f,126f,8f);text(c,card.optString("name"),12f,100f,126f,13f,true);text(c,card.optString("borrowerId"),12f,122f,126f,10f)
    val qr=catalogQr("carribrary:card:v1:"+card.optString("id"));p.color=Color.WHITE;c.drawRect(151f,36f,233f,118f,p);c.drawBitmap(qr,null,RectF(154f,39f,230f,115f),null);qr.recycle()
   };c.restore();if(crop&&!single){p.color=Color.GRAY;p.strokeWidth=.4f;for(xx in listOf(x,x+243))for(yy in listOf(y,y+153)){c.drawLine(xx-3,yy,xx+3,yy,p);c.drawLine(xx,yy-3,xx,yy+3,p)}}
  };pdf.finishPage(page)
 };return ByteArrayOutputStream().use{pdf.writeTo(it);it.toByteArray()}}finally{pdf.close();icon?.recycle();art?.recycle()}
}

@Composable fun PatronCardsDialog(snapshot:JSONObject,api:NativeApi,onClose:()->Unit){
 val state=snapshot.getJSONObject("state");val people=state.getJSONArray("borrowers").objects().filter{!it.optBoolean("archived")};val scope=rememberCoroutineScope();val context=LocalContext.current
 var registry by remember{mutableStateOf(JSONObject())};var borrower by remember{mutableStateOf(people.firstOrNull()?.optString("id")?:"")};var pin by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var selected by remember{mutableStateOf(setOf<String>())};var paper by remember{mutableStateOf("letter")};var duplex by remember{mutableStateOf(false)};var crop by remember{mutableStateOf(true)};var confirm by remember{mutableStateOf<Pair<String,String>?>(null)};var bytes by remember{mutableStateOf<ByteArray?>(null)}
 fun run(action:suspend ()->Unit){scope.launch{busy=true;try{action()}catch(e:Exception){note=e.message?:"Unable to update cards"}finally{busy=false}}}
 LaunchedEffect(Unit){run{registry=api.tools("patron-cards")}}
 val save=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/pdf")){uri->try{val data=bytes;if(uri!=null&&data!=null){context.contentResolver.openOutputStream(uri)?.use{it.write(data)}?:error("Unable to save PDF");note="Saved. Print at Actual size / 100%. Test duplex alignment first."}}catch(e:Exception){note=e.message?:"Unable to save"}finally{bytes=null}}
 if(confirm!=null)AlertDialog(onDismissRequest={confirm=null},title={Text("Confirm card change")},text={Text(if(confirm!!.first=="issue")"A replacement immediately invalidates the old card." else if(confirm!!.first=="review")"Confirm this card was not lost, replaced or revoked after the backup." else "This card will stop working immediately. Loan history is preserved.")},confirmButton={TextButton(onClick={val change=confirm!!;confirm=null;run{api.tools("patron-cards",JSONObject().put("action",change.first).put("id",change.second).put("borrowerId",borrower).put("pin",pin));pin="";registry=api.tools("patron-cards");selected=emptySet();note="Card settings updated"}}){Text("Confirm")}},dismissButton={TextButton(onClick={confirm=null}){Text("Cancel")}})
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  Text("Library cards",style=MaterialTheme.typography.headlineSmall);Text("No online account or phone is required. A card identifies the borrower; kiosks also require a separate private PIN.")
  for(person in people)Row{RadioButton(selected=borrower==person.optString("id"),onClick={borrower=person.optString("id");pin=""});Text(person.optString("name"))}
  OutlinedTextField(pin,{pin=it.filter(Char::isDigit).take(12)},label={Text("Optional PIN · 6–12 digits")},visualTransformation=androidx.compose.ui.text.input.PasswordVisualTransformation(),singleLine=true)
  Button(enabled=!busy&&borrower.isNotEmpty(),onClick={confirm="issue" to ""}){Text("Issue / replace card")};OutlinedButton(enabled=!busy&&pin.length>=6,onClick={run{api.tools("patron-cards",JSONObject().put("action","pin").put("borrowerId",borrower).put("pin",pin));pin="";note="PIN changed; give it privately to the borrower."}}){Text("Set / reset PIN")}
  HorizontalDivider();Text("Print / save PDF",style=MaterialTheme.typography.titleMedium)
  for((key,title) in listOf("letter" to "Letter · 8 cards","a4" to "A4 · 8 cards","single" to "Single CR80 · 3.375 × 2.125 in"))Row{RadioButton(selected=paper==key,onClick={paper=key});Text(title)}
  Row{Checkbox(duplex,{duplex=it});Text("Front/back · flip on long edge")};Row{Checkbox(crop,{crop=it});Text("Cut guides")}
  val cards=registry.optJSONArray("cards")?.objects()?:emptyList()
  for(card in cards){val id=card.optString("id");val person=people.find{it.optString("id")==card.optString("borrowerId")};val revoked=card.optBoolean("revoked");val review=card.optBoolean("reviewRequired");Card{Column(Modifier.padding(12.dp)){Text(person?.optString("name")?:card.optString("borrowerId"));Text(if(revoked)"Revoked" else if(review)"Restore review needed" else "Active");Row{Checkbox(id in selected,{checked->selected=if(checked)(selected+id).take(500).toSet() else selected-id},enabled=!revoked&&!review&&person!=null);Text("Include in PDF")};if(!revoked)TextButton(enabled=!busy,onClick={confirm="revoke" to id}){Text("Revoke")};if(!revoked&&review)TextButton(enabled=!busy,onClick={confirm="review" to id}){Text("Confirm legitimate card")}}}}
  Button(enabled=!busy&&selected.isNotEmpty(),onClick={run{val current=api.tools("patron-cards").getJSONArray("cards").objects();val printable=current.filter{it.optString("id") in selected&&!it.optBoolean("revoked")&&!it.optBoolean("reviewRequired")}.map{card->JSONObject(card.toString()).put("name",people.find{it.optString("id")==card.optString("borrowerId")}?.optString("name")?:error("Borrower unavailable"))};bytes=withContext(Dispatchers.Default){patronCardPdf(printable,state,paper,duplex,crop)};save.launch("Carribrary-library-cards.pdf")}}){Text("Save selected cards as PDF")}
  if(note.isNotEmpty())Text(note);TextButton(enabled=!busy,onClick=onClose){Text("Done")}
 }}}
}

@Composable fun SelfServiceDialog(snapshot:JSONObject,api:NativeApi,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val scope=rememberCoroutineScope();var current by remember{mutableStateOf(snapshot)};val initial=snapshot.getJSONObject("state").optJSONObject("selfService")?:JSONObject();var checkout by remember{mutableStateOf(initial.optBoolean("checkout"))};var returns by remember{mutableStateOf(initial.optBoolean("return"))};var kiosk by remember{mutableStateOf(initial.optBoolean("kiosk"))};var days by remember{mutableStateOf(initial.optInt("loanDays",14).toString())};var idle by remember{mutableStateOf(initial.optInt("idleSeconds",90).toString())};var instructions by remember{mutableStateOf(initial.optString("returnInstructions"))};var registry by remember{mutableStateOf(JSONObject())};var name by remember{mutableStateOf("")};var note by remember{mutableStateOf("")};var busy by remember{mutableStateOf(false)};var revoke by remember{mutableStateOf("")}
 fun run(action:suspend ()->Unit){scope.launch{busy=true;try{action()}catch(e:Exception){note=e.message?:"Unable to update self-service"}finally{busy=false}}};LaunchedEffect(Unit){run{registry=api.tools("patron-cards")}}
 if(revoke.isNotBlank())AlertDialog(onDismissRequest={revoke=""},title={Text("Revoke kiosk now?")},text={Text("This station will need a new pairing code.")},confirmButton={TextButton(onClick={val id=revoke;revoke="";run{api.tools("kiosks/revoke",JSONObject().put("id",id));registry=api.tools("patron-cards")}}){Text("Revoke")}},dismissButton={TextButton(onClick={revoke=""}){Text("Cancel")}})
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  Text("Borrower self-service & kiosks",style=MaterialTheme.typography.headlineSmall);Text("Optional trust-based lending. Self-returns remain on loan until a librarian physically confirms receipt.")
  Row{Checkbox(checkout,{checkout=it});Text("Allow self-checkout")};Row{Checkbox(returns,{returns=it});Text("Allow self-return reports")};Row{Checkbox(kiosk,{kiosk=it});Text("Enable authorized kiosks")}
  OutlinedTextField(days,{days=it},label={Text("Loan days · 1–365")});OutlinedTextField(idle,{idle=it},label={Text("Idle seconds · 30–300")});OutlinedTextField(instructions,{instructions=it},label={Text("Physical return instructions")})
  Button(enabled=!busy,onClick={run{current=api.mutate("setSelfService",JSONObject().put("checkout",checkout).put("return",returns).put("kiosk",kiosk).put("loanDays",days.toIntOrNull()?:0).put("idleSeconds",idle.toIntOrNull()?:0).put("returnInstructions",instructions),current.optLong("revision"));onChanged(current);note="Permissions saved"}}){Text("Save permissions")}
  HorizontalDivider();Text("A kiosk uses this same server. Open /kiosk/ on your public HTTPS hostname in its browser. A dedicated OS account and Windows Assigned Access / Linux restrictions are separate setup steps.")
  OutlinedTextField(name,{name=it},label={Text("Station name")});OutlinedButton(enabled=!busy,onClick={run{val paired=api.tools("kiosks/pair",JSONObject().put("name",name));note="Enter ${paired.getString("code")} on the kiosk within 10 minutes. This code grants no administrator access."}}){Text("Create one-use kiosk code")}
  for(station in registry.optJSONArray("stations")?.objects()?:emptyList()){Text(station.optString("name")+if(station.optBoolean("revoked"))" · revoked" else " · expires "+station.optString("expires"));if(!station.optBoolean("revoked"))TextButton(enabled=!busy,onClick={revoke=station.optString("id")}){Text("Revoke station")}}
  if(note.isNotEmpty())Text(note);TextButton(enabled=!busy,onClick=onClose){Text("Done")}
 }}}
}
