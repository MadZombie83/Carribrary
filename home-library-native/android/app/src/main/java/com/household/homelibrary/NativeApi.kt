package com.household.homelibrary
import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.net.URL
import java.net.InetAddress
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.security.*
import java.math.BigInteger
import java.security.interfaces.RSAPublicKey
import java.security.cert.CertificateFactory
import java.security.cert.X509Certificate
import java.net.Socket
import javax.net.ssl.*
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec
import org.json.JSONObject
import java.io.File
import java.util.UUID
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class NativeApi(private val context:Context) {
 private val prefs=context.getSharedPreferences("connection",Context.MODE_PRIVATE)
 private val keys=KeyStore.getInstance("AndroidKeyStore").apply{load(null)}
 private val keyAlias get()=prefs.getString("keyAlias","home-library-phone-key")!!
 private val pairingKeyAlias="home-library-phone-key-v2"
 private var validatedTlsKey:String?=null
 private val cacheAlias="home-library-cache-key"
 fun preference(name:String,default:String)=prefs.getString("ui-"+name,default)!!
 fun savePreference(name:String,value:String){prefs.edit().putString("ui-"+name,value).apply()}
 suspend fun batchLookup(isbn:String)=withContext(Dispatchers.IO){currentRequest("/mobile/catalog-batch/lookup?isbn="+android.net.Uri.encode(isbn))}
 suspend fun covers(isbn:String,title:String,author:String)=withContext(Dispatchers.IO){
  try{currentRequest("/mobile/covers?isbn="+android.net.Uri.encode(isbn)+"&title="+android.net.Uri.encode(title)+"&author="+android.net.Uri.encode(author))}
  catch(error:ApiError){if(error.status==404)throw IllegalStateException("Update the server to 2.0.12 or later to search cover sources.");throw error}
 }
 suspend fun requestAlertIds()=withContext(Dispatchers.IO){currentRequest("/mobile/state").getJSONObject("state").optJSONArray("requests")?.objects()?.filter{it.optString("status")=="pending"}?.map{it.optString("id")}?:emptyList()}
 suspend fun historyPreview(input:JSONObject)=withContext(Dispatchers.IO){currentRequest("/mobile/history/preview",input)}
 fun clearCachedLibrary(){check(!hasPending()){"Resolve Retry pending change before clearing downloaded data."};for(name in listOf("library.cache","sharing.cache")){val file=File(context.filesDir,name);check(!file.exists()||file.delete()){"Could not clear downloaded data."}};online=false}
 suspend fun about(copyId:String)=withContext(Dispatchers.IO){currentRequest("/mobile/books/"+android.net.Uri.encode(copyId)+"/about")}
 suspend fun browserAdmins(action:String?=null,input:JSONObject?=null)=withContext(Dispatchers.IO){currentRequest("/mobile/browser-admins"+(action?.let{"/"+it}?:""),input)}
 suspend fun receipt(copyId:String)=withContext(Dispatchers.IO){currentRequest("/mobile/receipts/"+android.net.Uri.encode(copyId))}
 suspend fun insights(range:String,input:JSONObject?=null)=withContext(Dispatchers.IO){require(range in listOf("7","30","90","all"));currentRequest("/mobile/insights"+(if(input==null)"?range=$range"else ""),input)}
 suspend fun tools(path:String,input:JSONObject?=null)=withContext(Dispatchers.IO){require(path in listOf("backup/portable","backup/portable/run","reminders","reminders/test","setup","setup/dns","catalog-batch/csv","catalog-batch/preview"));currentRequest("/mobile/"+path,input)}
 suspend fun accountAdmin(action:String?=null,input:JSONObject?=null)=withContext(Dispatchers.IO){
  if(action=="delete")check(!hasPending()){ "Resolve Retry pending change before deleting an account." }
  val result=currentRequest("/mobile/accounts"+(action?.let{"/"+it}?:""),input)
  if(action=="delete")result.optJSONObject("snapshot")?.let{File(context.filesDir,"library.cache").delete();saveFile("library.cache",it.toString());online=true}
  result
 }
 suspend fun readingSharing(readerId:String,action:String?=null)=withContext(Dispatchers.IO){currentRequest("/mobile/readers/"+android.net.Uri.encode(readerId)+"/sharing",action?.let{JSONObject().put("action",it)})}
 suspend fun sharing(action:String?=null)=withContext(Dispatchers.IO){
  try {val result=currentRequest("/mobile/sharing",action?.let{JSONObject().put("action",it)});saveFile("sharing.cache",result.toString());result}
  catch(error:ApiError){if(error.status==404)throw IllegalStateException("Update the library server to enable sharing from your phone.");throw error}
 }
 var online=false;private set
 val paired get()=prefs.contains("certificate")
 val serverAddress get()=prefs.getString("url","")!!
 suspend fun devices()=withContext(Dispatchers.IO){currentRequest("/mobile/devices")}
 suspend fun createPairing()=withContext(Dispatchers.IO){currentRequest("/mobile/devices/pair",JSONObject().put("serverUrl",serverAddress))}
 suspend fun revokeDevice(id:String)=withContext(Dispatchers.IO){currentRequest("/mobile/devices/revoke",JSONObject().put("deviceId",id))}
 val deviceName get()=prefs.getString("name","My phone")!!
 var cameraId:String
  get()=prefs.getString("camera","")!!
  set(value){prefs.edit().putString("camera",value).apply()}
 private fun decode(s:String)=Base64.decode(s,Base64.DEFAULT)
 private fun encode(b:ByteArray)=Base64.encodeToString(b,Base64.NO_WRAP)
 private fun pin(cert:X509Certificate)=encode(MessageDigest.getInstance("SHA-256").digest(cert.publicKey.encoded))
 private fun cacheKey():SecretKey {
  if(!keys.containsAlias(cacheAlias)){val generator=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");generator.init(KeyGenParameterSpec.Builder(cacheAlias,KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());generator.generateKey()}
  return keys.getKey(cacheAlias,null) as SecretKey
 }
 private fun saveFile(name:String,value:String){val cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.ENCRYPT_MODE,cacheKey());val target=File(context.filesDir,name);val tmp=File(context.filesDir,name+".tmp");tmp.writeBytes(cipher.iv+cipher.doFinal(value.toByteArray(Charsets.UTF_8)));if(!tmp.renameTo(target))throw IllegalStateException("Could not save encrypted cache.")}
 private fun readFile(name:String):String?{val file=File(context.filesDir,name);if(!file.exists())return null;return try{val data=file.readBytes();val cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.DECRYPT_MODE,cacheKey(),GCMParameterSpec(128,data.copyOfRange(0,12)));String(cipher.doFinal(data.copyOfRange(12,data.size)),Charsets.UTF_8)}catch(_:Exception){null}}
 fun cached():JSONObject?=readFile("library.cache")?.let{val snapshot=JSONObject(it);val state=snapshot.optJSONObject("state");if(state!=null){val hidden=state.optJSONArray("readers")?.objects()?.filter{it.optString("borrowerId").isNotBlank()}?.map{it.optString("id")}?.toSet()?:emptySet();state.put("readers",org.json.JSONArray(state.optJSONArray("readers")?.objects()?.filter{it.optString("id") !in hidden}?:emptyList<JSONObject>()));state.put("readingEntries",org.json.JSONArray(state.optJSONArray("readingEntries")?.objects()?.filter{it.optString("readerId") !in hidden}?:emptyList<JSONObject>()));saveFile("library.cache",snapshot.toString())};snapshot}
 fun hasPending()=File(context.filesDir,"pending.cache").exists()
 // Conscrypt computes RSA-PSS padding itself, then calls RSA/ECB/NoPadding
 // with the hardware private key. DIGEST_NONE alone does not authorize PAD_NONE.
 private fun validateTlsKey(alias:String) {
  if(validatedTlsKey==alias)return
  try {
   val privateKey=keys.getKey(alias,null) as PrivateKey
   val publicKey=keys.getCertificate(alias).publicKey as RSAPublicKey
   val input=ByteArray((publicKey.modulus.bitLength()+7)/8).apply{this[lastIndex]=42}
   val cipher=Cipher.getInstance("RSA/ECB/NoPadding")
   cipher.init(Cipher.ENCRYPT_MODE,privateKey)
   val signature=cipher.doFinal(input)
   check(BigInteger(1,signature).modPow(publicKey.publicExponent,publicKey.modulus)==BigInteger(1,input)){"Hardware signing test failed"}
   // TLS 1.2 can also request PKCS#1 signatures through the same cipher upcall.
   val legacy=Cipher.getInstance("RSA/ECB/PKCS1Padding")
   legacy.init(Cipher.ENCRYPT_MODE,privateKey)
   val digestInfo=decode("MDEwDQYJYIZIAWUDBAIBBQAEIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")
   val legacySignature=legacy.doFinal(digestInfo)
   val verifier=Signature.getInstance("NONEwithRSA")
   verifier.initVerify(publicKey);verifier.update(digestInfo)
   check(verifier.verify(legacySignature)){"Hardware PKCS1 signing test failed"}
   validatedTlsKey=alias
  } catch(error:Exception) {
   throw IllegalStateException(if(alias=="home-library-phone-key")
    "This phone needs updated pairing. Tap Connection, then Forget connection, and scan a new private pairing QR from the server’s admin interface."
    else "This phone could not use its secure signing key. Pairing was not changed. Please report this error: ${error.javaClass.simpleName}: ${error.message}",error)
  }
 }
 private fun socketFactory(expectedPin:String,client:Boolean):SSLSocketFactory {
  val trust=object:X509TrustManager {
   override fun getAcceptedIssuers()=emptyArray<X509Certificate>()
   override fun checkClientTrusted(chain:Array<X509Certificate>,authType:String){throw java.security.cert.CertificateException("Not a client trust manager")}
   override fun checkServerTrusted(chain:Array<X509Certificate>,authType:String){
    val cert=chain.firstOrNull()?:throw java.security.cert.CertificateException("Missing server certificate");cert.checkValidity()
    if(!MessageDigest.isEqual(pin(cert).toByteArray(),expectedPin.toByteArray()))throw java.security.cert.CertificateException("Server identity changed. Pair again from the server’s admin interface.")
    if(cert.extendedKeyUsage?.contains("1.3.6.1.5.5.7.3.1")!=true)throw java.security.cert.CertificateException("Not a server certificate")
    cert.verify(cert.publicKey) // Locally generated, self-signed server identity bound to the pairing QR.
   }
  }
  val managers=if(client){
   val activeAlias=keyAlias
   validateTlsKey(activeAlias)
   val factory=CertificateFactory.getInstance("X.509")
   val cert=factory.generateCertificate(decode(prefs.getString("certificate","")!!).inputStream()) as X509Certificate
   val ca=factory.generateCertificate(decode(prefs.getString("ca","")!!).inputStream()) as X509Certificate
   arrayOf<KeyManager>(object:X509ExtendedKeyManager(){
    override fun getClientAliases(keyType:String?,issuers:Array<out java.security.Principal>?)=if(keyType=="RSA")arrayOf(activeAlias)else null
    override fun chooseClientAlias(keyType:Array<out String>?,issuers:Array<out java.security.Principal>?,socket:Socket?)=if(keyType?.any{it=="RSA"}==true)activeAlias else null
    override fun chooseEngineClientAlias(keyType:Array<out String>?,issuers:Array<out java.security.Principal>?,engine:SSLEngine?)=if(keyType?.any{it=="RSA"}==true)activeAlias else null
    override fun getServerAliases(keyType:String?,issuers:Array<out java.security.Principal>?)=null
    override fun chooseServerAlias(keyType:String?,issuers:Array<out java.security.Principal>?,socket:Socket?)=null
    override fun getCertificateChain(alias:String?)=arrayOf(cert,ca)
    override fun getPrivateKey(alias:String?)=keys.getKey(activeAlias,null) as PrivateKey
   })
  }else null
  return SSLContext.getInstance("TLS").apply{init(managers,arrayOf(trust),SecureRandom())}.socketFactory
 }
 private fun request(base:String,path:String,expectedPin:String,client:Boolean,body:JSONObject?=null):JSONObject {
  val url=URL(base+path);require(url.protocol=="https"&&url.userInfo==null){"HTTPS address required"}
  val connection=url.openConnection() as HttpsURLConnection
  connection.instanceFollowRedirects=false;connection.connectTimeout=5000;connection.readTimeout=if(path=="/mobile/backup/portable/run")180000 else if(path=="/mobile/accounts/delete")45000 else if(path.startsWith("/mobile/covers?")||path.startsWith("/mobile/reminders"))35000 else 15000;connection.sslSocketFactory=socketFactory(expectedPin,client)
  // The QR binds the public key to this endpoint; no trust-all or redirect fallback.
  connection.hostnameVerifier=HostnameVerifier{hostname,session->hostname==url.host&&runCatching{pin(session.peerCertificates[0] as X509Certificate)==expectedPin}.getOrDefault(false)}
  try{if(body!=null){connection.requestMethod="POST";connection.doOutput=true;connection.setRequestProperty("Content-Type","application/json");connection.outputStream.use{it.write(body.toString().toByteArray())}}
   val status=connection.responseCode;val raw=(if(status in 200..299)connection.inputStream else connection.errorStream)?.bufferedReader()?.use{it.readText()}?:"{}";val json=runCatching{JSONObject(raw)}.getOrDefault(JSONObject())
   if(status !in 200..299)throw ApiError(status,json.optString("error","Server rejected this request ($status)."));return json
  }finally{connection.disconnect()}
 }
 private fun discover():String? {
  val cm=context.getSystemService(Context.CONNECTIVITY_SERVICE) as android.net.ConnectivityManager
  if(cm.getNetworkCapabilities(cm.activeNetwork)?.hasTransport(android.net.NetworkCapabilities.TRANSPORT_WIFI)!=true)return null
  return try{DatagramSocket().use{socket->socket.broadcast=true;socket.soTimeout=1000;val bytes="HOME_LIBRARY_DISCOVER_V1".toByteArray();socket.send(DatagramPacket(bytes,bytes.size,InetAddress.getByName("255.255.255.255"),7445));val packet=DatagramPacket(ByteArray(1024),1024);socket.receive(packet);val found=JSONObject(String(packet.data,0,packet.length));if(found.optString("serverId")==prefs.getString("serverId","")&&found.optInt("port") in 1..65535)"https://${packet.address.hostAddress}:${found.optInt("port")}" else null}}catch(_:Exception){null}
 }
 suspend fun completeBackup(password:String,output:java.io.OutputStream)=withContext(Dispatchers.IO){
  check(paired){"Pair this phone first."};require(password.length in 12..128){"Choose a backup password of 12–128 characters."}
  val remote=prefs.getString("url","")!!;val expected=prefs.getString("pin","")!!;val candidates=listOfNotNull(discover(),remote).distinct();var written=0L
  for((index,base) in candidates.withIndex()){
   val url=URL(base+"/mobile/backup/complete");val connection=url.openConnection() as HttpsURLConnection
   connection.instanceFollowRedirects=false;connection.connectTimeout=5000;connection.readTimeout=180000;connection.sslSocketFactory=socketFactory(expected,true)
   connection.hostnameVerifier=HostnameVerifier{host,session->host==url.host&&runCatching{pin(session.peerCertificates[0] as X509Certificate)==expected}.getOrDefault(false)}
   try{
    connection.requestMethod="POST";connection.doOutput=true;connection.setRequestProperty("Content-Type","application/json");connection.outputStream.use{it.write(JSONObject().put("password",password).toString().toByteArray())}
    val status=connection.responseCode;if(status !in 200..299){val error=connection.errorStream?.bufferedReader()?.use{it.readText()}?:"{}";throw ApiError(status,runCatching{JSONObject(error).optString("error")}.getOrDefault("").ifBlank{if(status==404)"Update Carribrary Server to 2.0.30 or later to use complete backups." else "Complete backup failed ($status)."})}
    val expectedLength=connection.contentLengthLong
    connection.inputStream.use{source->val buffer=ByteArray(65536);while(true){val n=source.read(buffer);if(n<0)break;output.write(buffer,0,n);written+=n}}
    if(expectedLength>=0&&written!=expectedLength)throw java.io.IOException("The backup download was incomplete.")
    return@withContext
   }catch(error:Exception){if(error is ApiError||written>0||index==candidates.lastIndex)throw error}finally{connection.disconnect()}
  }
 }
 suspend fun latestPortableBackup(output:java.io.OutputStream)=withContext(Dispatchers.IO){
  check(paired){"Pair this phone first."}
  val remote=prefs.getString("url","")!!;val expected=prefs.getString("pin","")!!;val candidates=listOfNotNull(discover(),remote).distinct();var written=0L
  for((index,base) in candidates.withIndex()){
   val url=URL(base+"/mobile/backup/portable/latest");val connection=url.openConnection() as HttpsURLConnection
   connection.instanceFollowRedirects=false;connection.connectTimeout=5000;connection.readTimeout=180000;connection.sslSocketFactory=socketFactory(expected,true)
   connection.hostnameVerifier=HostnameVerifier{host,session->host==url.host&&runCatching{pin(session.peerCertificates[0] as X509Certificate)==expected}.getOrDefault(false)}
   try{
    connection.requestMethod="GET"
    val status=connection.responseCode;if(status !in 200..299){val error=connection.errorStream?.bufferedReader()?.use{it.readText()}?:"{}";throw ApiError(status,runCatching{JSONObject(error).optString("error")}.getOrDefault("").ifBlank{if(status==404)"Update Carribrary Server to 2.0.30 or later to use complete backups." else "Complete backup failed ($status)."})}
    val expectedLength=connection.contentLengthLong
    connection.inputStream.use{source->val buffer=ByteArray(65536);while(true){val n=source.read(buffer);if(n<0)break;output.write(buffer,0,n);written+=n}}
    if(expectedLength>=0&&written!=expectedLength)throw java.io.IOException("The backup download was incomplete.")
    return@withContext
   }catch(error:Exception){if(error is ApiError||written>0||index==candidates.lastIndex)throw error}finally{connection.disconnect()}
  }
 }
 suspend fun verifyCompleteBackup(password:String,input:java.io.InputStream)=withContext(Dispatchers.IO){
  check(paired){"Pair this phone first."};require(password.length in 12..128){"Enter the backup password (12–128 characters)."}
  val expected=prefs.getString("pin","")!!;val remote=prefs.getString("url","")!!;val base=discover()?:remote
  val url=URL(base+"/mobile/backup/verify");val connection=url.openConnection() as HttpsURLConnection
  connection.instanceFollowRedirects=false;connection.connectTimeout=5000;connection.readTimeout=600000;connection.sslSocketFactory=socketFactory(expected,true)
  connection.hostnameVerifier=HostnameVerifier{host,session->host==url.host&&runCatching{pin(session.peerCertificates[0] as X509Certificate)==expected}.getOrDefault(false)}
  try{
   connection.requestMethod="POST";connection.doOutput=true;connection.setRequestProperty("Content-Type","application/octet-stream");connection.setChunkedStreamingMode(65536)
   val metadata=JSONObject().put("password",password).toString().toByteArray(Charsets.UTF_8)
   connection.outputStream.use{output->output.write(java.nio.ByteBuffer.allocate(4).order(java.nio.ByteOrder.LITTLE_ENDIAN).putInt(metadata.size).array());output.write(metadata);val buffer=ByteArray(65536);var total=0L;while(true){val n=input.read(buffer);if(n<0)break;total+=n;require(total<=2L*1024*1024*1024+64*1024*1024){"Backup exceeds the supported verification size."};output.write(buffer,0,n)}}
   val status=connection.responseCode;val raw=(if(status in 200..299)connection.inputStream else connection.errorStream)?.bufferedReader()?.use{it.readText()}?:"{}";val report=runCatching{JSONObject(raw)}.getOrDefault(JSONObject())
   if(status !in 200..299||!report.optBoolean("verified"))throw ApiError(status,report.optString("error").ifBlank{if(status==404)"Update Carribrary Server to 2.0.44 or later for backup verification." else "Verification could not finish. Check the connection or use the offline tool."})
   report
  }finally{connection.disconnect()}
 }
 private fun currentRequest(path:String,body:JSONObject?=null):JSONObject {
  check(paired){"Pair this phone first."};val remote=prefs.getString("url","")!!;val expected=prefs.getString("pin","")!!
  val local=discover();if(local!=null&&local!=remote){try{return request(local,path,expected,true,body)}catch(error:ApiError){throw error}catch(_:Exception){}}
  return request(remote,path,expected,true,body)
 }
 suspend fun pair(raw:String,name:String)=withContext(Dispatchers.IO){
  require(raw.startsWith("hl-pair:")){"Scan a private phone-pairing QR from the server’s admin interface, not the public book-list QR."}
  val pair=JSONObject(String(Base64.decode(raw.removePrefix("hl-pair:"),Base64.URL_SAFE or Base64.NO_PADDING or Base64.NO_WRAP),Charsets.UTF_8))
  require(pair.getInt("version")==1){"Unsupported pairing code"};require(java.time.Instant.parse(pair.getString("expires")).isAfter(java.time.Instant.now())){"Pairing code expired"}
  val url=pair.getString("serverUrl");require(URL(url).protocol=="https"&&URL(url).userInfo==null&&URL(url).path.isEmpty()){"Invalid HTTPS endpoint"}
  if(!keys.containsAlias(pairingKeyAlias)){val generator=KeyPairGenerator.getInstance("RSA","AndroidKeyStore");val spec=KeyGenParameterSpec.Builder(pairingKeyAlias,KeyProperties.PURPOSE_SIGN).setKeySize(2048).setDigests(KeyProperties.DIGEST_NONE,KeyProperties.DIGEST_SHA256,KeyProperties.DIGEST_SHA384,KeyProperties.DIGEST_SHA512).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).setSignaturePaddings(KeyProperties.SIGNATURE_PADDING_RSA_PKCS1,KeyProperties.SIGNATURE_PADDING_RSA_PSS);if(android.os.Build.VERSION.SDK_INT>=35)spec.setMgf1Digests(KeyProperties.DIGEST_SHA1,KeyProperties.DIGEST_SHA256,KeyProperties.DIGEST_SHA384,KeyProperties.DIGEST_SHA512);generator.initialize(spec.build());generator.generateKeyPair()}
  validateTlsKey(pairingKeyAlias)
  val public=encode(keys.getCertificate(pairingKeyAlias).publicKey.encoded);val challenge="home-library-pair:v1:${pair.getString("code")}:$public"
  val signer=Signature.getInstance("SHA256withRSA");signer.initSign(keys.getKey(pairingKeyAlias,null) as PrivateKey);signer.update(challenge.toByteArray())
  val result=request(url,"/mobile/pair",pair.getString("serverKey"),false,JSONObject().put("code",pair.getString("code")).put("name",name.trim()).put("publicKey",public).put("signature",encode(signer.sign())))
  prefs.edit().putString("keyAlias",pairingKeyAlias).putString("url",url).putString("pin",pair.getString("serverKey")).putString("serverId",pair.getString("serverId")).putString("certificate",result.getString("certificate")).putString("ca",result.getString("caCertificate")).putString("name",name.trim()).apply()
  File(context.filesDir,"library.cache").delete();File(context.filesDir,"pending.cache").delete();online=true
 }
 suspend fun refresh()=withContext(Dispatchers.IO){try{val state=currentRequest("/mobile/state");saveFile("library.cache",state.toString());online=true;state}catch(e:Exception){online=false;throw e}}
 suspend fun mutate(action:String,input:JSONObject,revision:Long)=withContext(Dispatchers.IO){
  check(online){"Offline: reconnect before making changes."}
  val pending=readFile("pending.cache")?.let{JSONObject(it)}
  if(pending!=null)throw IllegalStateException("A previous change is awaiting confirmation. Tap Retry pending change first.")
  val body=JSONObject().put("action",action).put("input",input).put("revision",revision).put("requestId",UUID.randomUUID().toString());saveFile("pending.cache",body.toString());sendPending(body)
 }
 private fun sendPending(body:JSONObject):JSONObject {try{val state=currentRequest("/mobile/action",body);saveFile("library.cache",state.toString());File(context.filesDir,"pending.cache").delete();online=true;return state}catch(e:ApiError){File(context.filesDir,"pending.cache").delete();throw e}catch(e:Exception){online=false;throw IllegalStateException("Connection interrupted. The change may be saved. Reconnect and tap Retry pending change.",e)}}
 suspend fun retryPending()=withContext(Dispatchers.IO){val pending=readFile("pending.cache")?:throw IllegalStateException("No pending change");sendPending(JSONObject(pending))}
 fun forget(){RequestAlerts.disable(context);prefs.edit().remove("url").remove("pin").remove("serverId").remove("certificate").remove("ca").apply();File(context.filesDir,"library.cache").delete();File(context.filesDir,"pending.cache").delete();File(context.filesDir,"sharing.cache").delete();online=false}
}
class ApiError(val status:Int,message:String):Exception(message)
