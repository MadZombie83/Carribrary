package com.household.homelibrary
import android.Manifest
import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.job.JobInfo
import android.app.job.JobParameters
import android.app.job.JobScheduler
import android.app.job.JobService
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import kotlinx.coroutines.*
import java.security.MessageDigest

object RequestAlerts {
 private const val JOB=2301;private const val CHANNEL="library-requests";private const val NOTICE=2301
 private fun prefs(c:Context)=c.getSharedPreferences("request-alerts",Context.MODE_PRIVATE)
 fun enabled(c:Context)=prefs(c).getBoolean("enabled",false)
 fun permitted(c:Context)=Build.VERSION.SDK_INT<33||c.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS)==PackageManager.PERMISSION_GRANTED
 fun hashes(ids:List<String>)=ids.map{MessageDigest.getInstance("SHA-256").digest(it.toByteArray()).joinToString(""){b->"%02x".format(b)}}.toSet()
 fun enable(c:Context,baseline:List<String>){check(permitted(c)){"Allow notification permission first."};val job=JobInfo.Builder(JOB,ComponentName(c,RequestAlertJob::class.java)).setPeriodic(15*60*1000L).setPersisted(true).setRequiredNetworkType(JobInfo.NETWORK_TYPE_ANY).setRequiresBatteryNotLow(true).build();prefs(c).edit().putBoolean("enabled",true).putStringSet("seen",hashes(baseline)).apply();if(c.getSystemService(JobScheduler::class.java).schedule(job)!=JobScheduler.RESULT_SUCCESS){disable(c);error("Android could not schedule request alerts. Try again later.")}}
 fun disable(c:Context){prefs(c).edit().clear().apply();c.getSystemService(JobScheduler::class.java).cancel(JOB);c.getSystemService(NotificationManager::class.java).cancel(NOTICE)}
 fun accept(c:Context,ids:List<String>){if(!enabled(c)||!permitted(c))return;val now=hashes(ids);val seen=prefs(c).getStringSet("seen",emptySet())?:emptySet();if((now-seen).isEmpty())return;show(c);prefs(c).edit().putStringSet("seen",(seen+now).toList().takeLast(2000).toSet()).apply()}
 fun show(c:Context){check(permitted(c)){"Notification permission is off."};val manager=c.getSystemService(NotificationManager::class.java);manager.createNotificationChannel(NotificationChannel(CHANNEL,"Library requests",NotificationManager.IMPORTANCE_DEFAULT));val intent=Intent(c,MainActivity::class.java).putExtra("openRequests",true).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK);val open=PendingIntent.getActivity(c,NOTICE,intent,PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE);manager.notify(NOTICE,Notification.Builder(c,CHANNEL).setSmallIcon(R.drawable.ic_request_notification).setContentTitle("Library administration").setContentText("There are library items to review. Open the notification bell.").setContentIntent(open).setAutoCancel(true).setVisibility(Notification.VISIBILITY_PRIVATE).build())}
}
class RequestAlertJob:JobService(){private var job:Job?=null
 override fun onStartJob(params:JobParameters):Boolean{if(!RequestAlerts.enabled(this)||!RequestAlerts.permitted(this))return false;job=CoroutineScope(SupervisorJob()+Dispatchers.IO).launch{try{val api=NativeApi(applicationContext);if(!api.paired){RequestAlerts.disable(applicationContext);return@launch};val ids=api.requestAlertIds();ensureActive();RequestAlerts.accept(applicationContext,ids)}catch(e:CancellationException){throw e}catch(_:Exception){/* Offline/revoked: never reveal credentials or private records in a notification. */}finally{val completed=isActive;withContext(NonCancellable+Dispatchers.Main){if(completed)jobFinished(params,false)}}};return true}
 override fun onStopJob(params:JobParameters):Boolean{job?.cancel();return RequestAlerts.enabled(this)}
 override fun onDestroy(){job?.cancel();super.onDestroy()}
}
@Composable fun RequestAlertControls(api:NativeApi){val context=LocalContext.current;val scope=rememberCoroutineScope();var enabled by remember{mutableStateOf(RequestAlerts.enabled(context))};var busy by remember{mutableStateOf(false)};var note by remember{mutableStateOf("")}
 fun enable(){scope.launch{busy=true;try{val baseline=api.requestAlertIds();RequestAlerts.enable(context,baseline);enabled=true;note="Background admin alerts enabled. Android checks about every 15 minutes; battery/OS settings may delay checks."}catch(e:Exception){note=e.message?:"Could not enable request alerts"}finally{busy=false}}}
 val permission=rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()){allowed->if(allowed)enable()else note="Notifications stay off. You can allow them later in Android Settings → Apps → Carribrary → Notifications."}
 OutlinedButton(enabled=!busy,onClick={if(enabled){RequestAlerts.disable(context);enabled=false;note="Background admin alerts off."}else if(!RequestAlerts.permitted(context)&&Build.VERSION.SDK_INT>=33)permission.launch(Manifest.permission.POST_NOTIFICATIONS)else enable()}){Text(if(enabled)"Turn off background admin alerts" else "Enable background admin alerts")}
 if(enabled)TextButton(enabled=!busy,onClick={try{RequestAlerts.show(context);note="Generic test notification shown; check Android notification settings if hidden."}catch(e:Exception){note=e.message?:"Permission is off"}}){Text("Test request notification")}
 Text("Optional native Android alerts use periodic checks of your paired server, without Firebase or a PWA. They are not instant push.",style=MaterialTheme.typography.bodySmall)
 if(note.isNotBlank())Text(note)
}
