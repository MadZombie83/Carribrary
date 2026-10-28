package com.household.homelibrary

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.foundation.background
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import org.json.JSONObject

val phoneShapes=Shapes(small=RoundedCornerShape(8.dp),medium=RoundedCornerShape(14.dp),large=RoundedCornerShape(22.dp))
val phoneTypography=Typography().let{it.copy(headlineMedium=it.headlineMedium.copy(fontFamily=FontFamily.Serif),titleLarge=it.titleLarge.copy(fontFamily=FontFamily.Serif))}
@Composable fun PhoneNavIcon(target:String){Icon(painterResource(when(target){"Home"->R.drawable.nav_home;"Catalog"->R.drawable.nav_catalog;"On loan"->R.drawable.nav_loans;"Reading"->R.drawable.nav_reading;else->R.drawable.nav_more}),contentDescription=null,modifier=Modifier.size(24.dp))}
@Composable fun PhoneLibraryBanner(state:JSONObject,onHelp:()->Unit,requests:Int=0,onRequests:(()->Unit)?=null){
 val colors=MaterialTheme.colorScheme
 Column(Modifier.fillMaxWidth().background(Brush.linearGradient(listOf(colors.primary,colors.primary.copy(alpha=.88f)))).padding(horizontal=20.dp,vertical=22.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){
  Row(verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(12.dp)){
   LibraryBrandIcon(state)
   if(state.optString("libraryIcon192").isBlank())Icon(painterResource(R.drawable.nav_catalog),null,tint=colors.onPrimary,modifier=Modifier.size(34.dp))
   Column(Modifier.weight(1f)){Text(state.optString("libraryName","Carribrary"),style=MaterialTheme.typography.headlineMedium,color=colors.onPrimary);val subtitle=state.optString("bannerSubtitle","A shelf worth sharing");if(subtitle.isNotBlank())Text(subtitle,style=MaterialTheme.typography.bodySmall,color=colors.tertiary)}
   if(onRequests!=null)BadgedBox(badge={if(requests>0)Badge{Text(if(requests>99)"99+" else requests.toString())}}){IconButton(onClick=onRequests){Icon(painterResource(R.drawable.nav_bell),"Admin notifications: $requests",tint=colors.onPrimary)}}
   TextButton(onClick=onHelp,colors=ButtonDefaults.textButtonColors(contentColor=colors.onPrimary)){Text("Help")}
  }
 }
}
@Composable fun PhoneLibrarySummary(summary:PhoneSummaryCounts){
 val colors=MaterialTheme.colorScheme
 val items=listOf(Triple(summary.physicalCopies,"Physical copies",R.drawable.nav_catalog),Triple(summary.available,"Available",R.drawable.summary_check),Triple(summary.onLoan,"On loan",R.drawable.summary_clock),Triple(summary.activeRequests,"Active requests",R.drawable.summary_people))
 val backgrounds=listOf(colors.secondaryContainer,Color(0xffedf9f6),Color(0xfffff5ee),Color(0xffedf5ff))
 val accents=listOf(colors.primary,Color(0xff087955),Color(0xffb85306),Color(0xff2853be))
 BoxWithConstraints(Modifier.fillMaxWidth()){
  val columns=if(maxWidth>=600.dp)4 else 2
  Column(verticalArrangement=Arrangement.spacedBy(12.dp)){
   for(row in items.indices.chunked(columns))Row(horizontalArrangement=Arrangement.spacedBy(12.dp)){
    for(index in row)OutlinedCard(Modifier.weight(1f),shape=MaterialTheme.shapes.medium,colors=CardDefaults.outlinedCardColors(containerColor=backgrounds[index]),border=BorderStroke(1.dp,colors.outlineVariant)){
     Column(Modifier.fillMaxWidth().padding(14.dp),verticalArrangement=Arrangement.spacedBy(7.dp)){
      Surface(shape=RoundedCornerShape(50),color=accents[index].copy(alpha=.1f)){Icon(painterResource(items[index].third),null,tint=accents[index],modifier=Modifier.padding(10.dp).size(26.dp))}
      Text(items[index].first.toString(),style=MaterialTheme.typography.headlineMedium,color=if(index==0)colors.primary else Color(0xff18363c))
      Text(items[index].second,style=MaterialTheme.typography.labelLarge,color=if(index==0)colors.primary else Color(0xff18363c))
     }
    }
   }
  }
 }
}
@OptIn(ExperimentalLayoutApi::class)
@Composable fun PhoneBookCard(book:JSONObject,status:String,onLoan:Boolean,canCirculate:Boolean,canEdit:Boolean,onCirculate:()->Unit,onAbout:()->Unit,onEdit:()->Unit,onDelete:()->Unit,onReceipt:()->Unit){
 var menu by remember(book.optString("id")){mutableStateOf(false)}
 OutlinedCard(Modifier.fillMaxWidth(),shape=MaterialTheme.shapes.medium,colors=CardDefaults.outlinedCardColors(containerColor=MaterialTheme.colorScheme.surface),border=BorderStroke(1.dp,MaterialTheme.colorScheme.outlineVariant)){
  Column(Modifier.padding(18.dp),verticalArrangement=Arrangement.spacedBy(12.dp)){
   Row(horizontalArrangement=Arrangement.spacedBy(14.dp)){
    BookCover(book)
    Column(Modifier.weight(1f),verticalArrangement=Arrangement.spacedBy(5.dp)){
     Text(book.optString("id"),style=MaterialTheme.typography.labelMedium,color=MaterialTheme.colorScheme.onSurfaceVariant)
     Text(book.optString("title"),style=MaterialTheme.typography.titleLarge,color=MaterialTheme.colorScheme.onSurface)
     Text(book.optString("author"),style=MaterialTheme.typography.bodyMedium,color=MaterialTheme.colorScheme.onSurfaceVariant)
     if(book.optString("series").isNotBlank())Text(book.optString("series"),style=MaterialTheme.typography.bodySmall)
    }
   }
   Text(status,style=MaterialTheme.typography.labelLarge,color=MaterialTheme.colorScheme.primary)
   HorizontalDivider(color=MaterialTheme.colorScheme.outlineVariant)
   FlowRow(horizontalArrangement=Arrangement.spacedBy(8.dp),verticalArrangement=Arrangement.spacedBy(4.dp)){
    Button(enabled=canCirculate,onClick=onCirculate){Text(if(onLoan)"Return" else "Check out")}
    OutlinedButton(onClick=onAbout){Text("About")}
    Box{TextButton(onClick={menu=true}){Text("Book options")};DropdownMenu(expanded=menu,onDismissRequest={menu=false}){
     DropdownMenuItem(text={Text("Edit book")},enabled=canEdit,onClick={menu=false;onEdit()})
     if(onLoan)DropdownMenuItem(text={Text("Borrowing receipt")},enabled=canEdit,onClick={menu=false;onReceipt()})
     DropdownMenuItem(text={Text("Move to Recycle Bin",color=MaterialTheme.colorScheme.error)},enabled=canEdit,onClick={menu=false;onDelete()})
    }}
   }
   val location=listOf("room","bookcase","shelf").map{book.optString(it)}.filter{it.isNotBlank()}.joinToString(" · ")
   if(location.isNotBlank())Text("Location: $location",style=MaterialTheme.typography.bodySmall,color=MaterialTheme.colorScheme.onSurfaceVariant)
  }
 }
}

@Composable fun PhoneSectionHeading(title:String,description:String){
 Column(verticalArrangement=Arrangement.spacedBy(6.dp)){
  Text(title,style=MaterialTheme.typography.headlineMedium,color=MaterialTheme.colorScheme.primary)
  Text(description,style=MaterialTheme.typography.bodyMedium,color=MaterialTheme.colorScheme.onSurfaceVariant)
 }
}
@Composable fun PhonePanel(content:@Composable ColumnScope.()->Unit){
 OutlinedCard(Modifier.fillMaxWidth(),shape=MaterialTheme.shapes.large,colors=CardDefaults.outlinedCardColors(containerColor=MaterialTheme.colorScheme.surface),border=BorderStroke(1.dp,MaterialTheme.colorScheme.outlineVariant)){
  Column(Modifier.padding(18.dp),verticalArrangement=Arrangement.spacedBy(10.dp),content=content)
 }
}
@Composable fun PhoneEmptyState(title:String,description:String){PhonePanel{Text(title,style=MaterialTheme.typography.titleLarge);Text(description,color=MaterialTheme.colorScheme.onSurfaceVariant)}}
@Composable fun PhoneStatusPill(text:String){Surface(color=MaterialTheme.colorScheme.secondaryContainer,contentColor=MaterialTheme.colorScheme.onSecondaryContainer,shape=RoundedCornerShape(50)){Text(text,Modifier.padding(horizontal=12.dp,vertical=6.dp),style=MaterialTheme.typography.labelMedium)}}
@Composable fun PhoneDisclosure(title:String,initialOpen:Boolean=false,content:@Composable ColumnScope.()->Unit){
 var expanded by remember(title){mutableStateOf(initialOpen)}
 PhonePanel{TextButton(onClick={expanded=!expanded},modifier=Modifier.fillMaxWidth()){Text((if(expanded)"▾ " else "▸ ")+title,style=MaterialTheme.typography.titleLarge)};if(expanded)content()}
}
