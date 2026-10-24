package com.household.homelibrary
import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.Image
import androidx.compose.ui.res.painterResource
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
import org.json.JSONObject

@Composable fun BookAboutDialog(book:JSONObject,api:NativeApi,online:Boolean,onClose:()->Unit){
 val context=LocalContext.current;var text by remember(book.optString("id")){mutableStateOf(book.optString("description"))};var source by remember{mutableStateOf("")};var url by remember{mutableStateOf("")}
 LaunchedEffect(book.optString("id")){if(text.isBlank()){if(!online)text="Connect to your library to look up a description." else{ text="Looking up a short description…";try{val result=api.about(book.optString("id"));text=result.optString("description");source=result.optString("source");url=result.optString("sourceUrl")}catch(_:Exception){text="Description unavailable. Try again later."}}}}
 Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){Text(book.optString("title"),style=MaterialTheme.typography.titleLarge);Text(book.optString("author"));Text(text);if(source=="Google Books")Image(painterResource(R.drawable.powered_by_google),"Powered by Google",Modifier.width(62.dp).height(30.dp));if(source.isNotBlank())Text(source,style=MaterialTheme.typography.bodySmall);if(url.startsWith("https://"))TextButton(onClick={context.startActivity(Intent(Intent.ACTION_VIEW,Uri.parse(url)))}){Text("Description source")};TextButton(onClick=onClose){Text("Close")}}}
 }
}
