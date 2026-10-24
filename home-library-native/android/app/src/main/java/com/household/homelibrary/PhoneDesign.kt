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
@Composable fun PhoneNavIcon(target:String){Icon(painterResource(when(target){"Catalog"->R.drawable.nav_catalog;"On loan"->R.drawable.nav_loans;"Reading"->R.drawable.nav_reading;else->R.drawable.nav_more}),contentDescription=null,modifier=Modifier.size(24.dp))}
@Composable fun PhoneLibraryBanner(state:JSONObject,onHelp:()->Unit){
 val colors=MaterialTheme.colorScheme
 Column(Modifier.fillMaxWidth().background(Brush.linearGradient(listOf(colors.primary,colors.primary.copy(alpha=.88f)))).padding(horizontal=20.dp,vertical=22.dp),verticalArrangement=Arrangement.spacedBy(8.dp)){
  Row(verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(12.dp)){
   LibraryBrandIcon(state)
   if(state.optString("libraryIcon192").isBlank())Icon(painterResource(R.drawable.nav_catalog),null,tint=colors.onPrimary,modifier=Modifier.size(34.dp))
   Column(Modifier.weight(1f)){Text(state.optString("libraryName","Carribrary"),style=MaterialTheme.typography.headlineMedium,color=colors.onPrimary);val subtitle=state.optString("bannerSubtitle","A shelf worth sharing");if(subtitle.isNotBlank())Text(subtitle,style=MaterialTheme.typography.bodySmall,color=colors.tertiary)}
   TextButton(onClick=onHelp,colors=ButtonDefaults.textButtonColors(contentColor=colors.onPrimary)){Text("Help")}
  }
 }
}
@Composable fun PhoneLibrarySummary(copies:Int,onLoan:Int,available:Int){
 OutlinedCard(Modifier.fillMaxWidth(),colors=CardDefaults.outlinedCardColors(containerColor=MaterialTheme.colorScheme.surface),border=BorderStroke(1.dp,MaterialTheme.colorScheme.outlineVariant)){
  Row(Modifier.fillMaxWidth().padding(vertical=14.dp),horizontalArrangement=Arrangement.SpaceEvenly){for((number,label)in listOf(copies to "Books",onLoan to "On loan",available to "Available"))Column(horizontalAlignment=Alignment.CenterHorizontally){Text(number.toString(),style=MaterialTheme.typography.titleLarge,color=MaterialTheme.colorScheme.primary);Text(label,style=MaterialTheme.typography.labelMedium,color=MaterialTheme.colorScheme.onSurfaceVariant)}}
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
