package com.household.homelibrary

import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.pdf.PdfDocument
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import kotlinx.coroutines.launch
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import java.io.ByteArrayOutputStream

fun copyLabelPdf(books:List<JSONObject>,library:String,a4:Boolean,start:Int):ByteArray {
 val rows=if(a4)8 else 10;require(start in 1..rows*3&&books.size in 1..500&&books.all{!it.optBoolean("archived")&&validCopyReference("carribrary:copy:v1:"+it.optString("copyUid"))}){"Select 1–500 current copies and a valid starting position."}
 val mm=72f/25.4f;val pageW=if(a4)595 else 612;val pageH=if(a4)842 else 792
 val left=(if(a4)7.25f else 4.7625f)*mm;val top=(if(a4)12.9f else 12.7f)*mm
 val width=(if(a4)63.5f else 66.675f)*mm;val height=(if(a4)33.9f else 25.4f)*mm;val gap=(if(a4)2.5f else 3.175f)*mm
 val pdf=PdfDocument();val paint=Paint(Paint.ANTI_ALIAS_FLAG).apply{color=Color.BLACK};val total=start-1+books.size
 try{for(pageNumber in 0 until (total+rows*3-1)/(rows*3)){
  val page=pdf.startPage(PdfDocument.PageInfo.Builder(pageW,pageH,pageNumber+1).create());val canvas=page.canvas;canvas.drawColor(Color.WHITE)
  for(slot in 0 until rows*3){val index=pageNumber*rows*3+slot-(start-1);if(index !in books.indices)continue;val book=books[index];val x=left+(slot%3)*(width+gap);val y=top+(slot/3)*height
   val qr=catalogQr("carribrary:copy:v1:"+book.optString("copyUid"));canvas.drawBitmap(qr,null,RectF(x+mm,y+(height-23*mm)/2,x+24*mm,y+(height+23*mm)/2),null);qr.recycle()
   canvas.save();canvas.clipRect(x+25*mm,y+mm,x+width-mm,y+height-mm);paint.isFakeBoldText=true;paint.textSize=11f;canvas.drawText(book.optString("id"),x+25*mm,y+5*mm,paint);paint.isFakeBoldText=false;paint.textSize=8f
   val title=book.optString("title");val count=paint.breakText(title,true,width-26*mm,null);canvas.drawText(title.take(count),x+25*mm,y+10*mm,paint);canvas.drawText(title.drop(count).take(paint.breakText(title.drop(count),true,width-26*mm,null)),x+25*mm,y+14*mm,paint)
   paint.textSize=7f;canvas.drawText(library.take(paint.breakText(library,true,width-26*mm,null)),x+25*mm,y+20*mm,paint);canvas.restore()
  };pdf.finishPage(page)
 };return ByteArrayOutputStream().use{pdf.writeTo(it);it.toByteArray()}}finally{pdf.close()}
}

@Composable fun CopyTools(snapshot:JSONObject,api:NativeApi,onChanged:(JSONObject)->Unit,onClose:()->Unit){
 val context=LocalContext.current;val scope=rememberCoroutineScope();var current by remember{mutableStateOf(snapshot)};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")};var search by remember{mutableStateOf("")};var selected by remember{mutableStateOf(setOf<String>())};var a4 by remember{mutableStateOf(false)};var start by remember{mutableStateOf("1")};var bytes by remember{mutableStateOf<ByteArray?>(null)}
 val state=current.getJSONObject("state");val books=state.getJSONArray("books").objects().filter{!it.optBoolean("archived")};val matches=books.filter{"${it.optString("id")} ${it.optString("title")} ${it.optString("author")}".contains(search,true)}
 val save=rememberLauncherForActivityResult(ActivityResultContracts.CreateDocument("application/pdf")){uri->try{val data=bytes;if(uri!=null&&data!=null){context.contentResolver.openOutputStream(uri)?.use{it.write(data)}?:error("Could not save PDF");note="Labels saved. Print at Actual size / 100%; test alignment on plain paper first."}}catch(e:Exception){note=e.message?:"Could not save labels"}finally{bytes=null;busy=false}}
 Dialog(onDismissRequest={if(!busy)onClose()}){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(10.dp)){
  Text("Copy labels & numbering",style=MaterialTheme.typography.headlineSmall)
  Text("ISBN scanning works without labels. QR labels permanently identify a physical copy and contain no account or pairing secrets.")
  Text("New numbers: "+if(state.optString("copyNumberPolicy")=="next-number")"Always use next number" else "Fill available gaps")
  Text("This changes future additions only. Existing numbers and history remain protected. Old BK-only labels can point to replacement books when a number is reused; these QR labels cannot.")
  for((policy,title)in listOf("fill-gaps" to "Fill available gaps","next-number" to "Always use next number"))OutlinedButton(enabled=!busy,onClick={scope.launch{busy=true;try{current=api.mutate("setCopyNumberPolicy",JSONObject().put("policy",policy),current.optLong("revision"));onChanged(current);note="Numbering choice saved"}catch(e:Exception){note=e.message?:"Could not save"}finally{busy=false}}}){Text(title)}
  HorizontalDivider();Text("Print optional labels",style=MaterialTheme.typography.titleMedium)
  Text("${if(a4)"A4 · 24 labels (63.5 × 33.9 mm)" else "Letter · 30 labels (Avery 5160 geometry)"}")
  OutlinedButton(enabled=!busy,onClick={a4=!a4;start="1"}){Text("Use "+if(a4)"Letter" else "A4")}
  OutlinedTextField(start,{start=it},enabled=!busy,label={Text("First unused position (across, then down)")})
  OutlinedTextField(search,{search=it},enabled=!busy,label={Text("Find title, author or BK number")})
  Text("${selected.size} selected · maximum 500 per batch")
  Row{TextButton(enabled=!busy,onClick={selected=(selected+matches.map{it.optString("id")}).take(500).toSet()}){Text("Select matches")};TextButton(enabled=!busy,onClick={selected=emptySet()}){Text("Clear")}}
  for(book in matches){Row{Checkbox(checked=book.optString("id") in selected,enabled=!busy,onCheckedChange={checked->val id=book.optString("id");selected=if(checked&&selected.size<500)selected+id else selected-id});Text("${book.optString("id")} · ${book.optString("title")}")}}
  Button(enabled=!busy&&selected.isNotEmpty(),onClick={scope.launch{busy=true;try{bytes=withContext(Dispatchers.Default){copyLabelPdf(books.filter{it.optString("id") in selected},state.optString("libraryName"),a4,start.toIntOrNull()?:0)};save.launch("Carribrary-copy-labels.pdf")}catch(e:Exception){note=e.message?:"Could not create labels";busy=false}}}){Text("Save label PDF")}
  Text("Print at Actual size / 100%. Check your sheet dimensions and test on plain paper first. No label designer or print-history tracking yet.",style=MaterialTheme.typography.bodySmall)
  if(note.isNotBlank())Text(note);TextButton(enabled=!busy,onClick=onClose){Text("Done")}
 }}}
}
