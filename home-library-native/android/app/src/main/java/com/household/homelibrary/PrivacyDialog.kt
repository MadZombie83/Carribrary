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

@Composable fun PrivacyDialog(onClose:()->Unit){
 val context=LocalContext.current
 val policy=remember{context.assets.open("privacy.txt").bufferedReader().use{it.readText()}}
 val notices=remember{context.assets.open("third-party-notices.txt").bufferedReader().use{it.readText()}}
 var showNotices by remember{mutableStateOf(false)}
 Dialog(onDismissRequest=onClose){Surface(shape=MaterialTheme.shapes.large){Column(Modifier.padding(20.dp).verticalScroll(rememberScrollState()),verticalArrangement=Arrangement.spacedBy(12.dp)){
  Text("Carribrary",style=MaterialTheme.typography.titleLarge)
  Text("Version ${BuildConfig.VERSION_NAME}")
  Text(if(showNotices)notices else policy)
  TextButton(onClick={showNotices=!showNotices}){Text(if(showNotices)"Privacy policy" else "Third-party notices")}
  TextButton(onClick=onClose){Text("Close")}
 }}}
}
