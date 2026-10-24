#!/usr/bin/env python3
"""Check a prior server payload upgrades in place without losing data or phone trust.
Pass a pre-update published server directory. Uses temporary data and random ports.
"""
from pathlib import Path
import base64,hashlib,json,os,socket,ssl,subprocess,sys,tempfile,time,urllib.request,urllib.error
from cryptography import x509
from cryptography.hazmat.primitives.asymmetric import rsa,padding
from cryptography.hazmat.primitives import hashes,serialization
root=Path(__file__).resolve().parents[1];old=Path(sys.argv[1]).resolve()
def port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
ports=set()
while len(ports)<3:ports.add(port())
admin,phone,catalog=ports;origin=f'http://localhost:{admin}';mobile=f'https://localhost:{phone}'
def req(path,body=None,ctx=None):
 headers={'Content-Type':'application/json','Origin':origin} if body is not None else {}
 with urllib.request.urlopen(urllib.request.Request((mobile if ctx else origin)+path,data=None if body is None else json.dumps(body).encode(),headers=headers),context=ctx,timeout=15) as response:return json.load(response)
with tempfile.TemporaryDirectory(prefix='carribrary-brand-upgrade-') as tmp:
 env=os.environ.copy();env.update(HOME_LIBRARY_DATA=tmp,Library__AdminPort=str(admin),Library__PhonePort=str(phone),Library__CatalogPort=str(catalog),Library__EnableDiscovery='false',Library__PublicCatalogOrigin='https://library.test')
 def start(payload):
  log=open(Path(tmp)/'upgrade-test.log','a');p=subprocess.Popen([str(payload/'HomeLibrary.Server')],env=env,stdout=log,stderr=subprocess.STDOUT)
  for _ in range(100):
   if p.poll() is not None:raise RuntimeError((Path(tmp)/'upgrade-test.log').read_text()[-2000:])
   try:req('/api/state');return p,log
   except Exception:time.sleep(.1)
  p.terminate();p.wait();log.close();raise RuntimeError('Server did not become ready')
 def stop(p,log):p.terminate();p.wait(timeout=15);log.close()
 def change(action,input):
  s=req('/api/state');return req('/api/action',{'action':action,'input':input,'revision':s['revision']})
 p,log=start(old)
 try:
  change('setLibraryName',{'libraryName':"Queen B's Library"});change('addBook',{'isbn':'9780140328721','title':'Matilda','author':'Roald Dahl'});borrower=change('addBorrower',{'name':'Upgrade reader','contact':'upgrade@example.test'})['result'];change('addReader',{'name':'Household reader'});change('createHold',{'copyId':'BK-0001','borrowerId':borrower['id']})
  sharing=req('/api/sharing',{'action':'enable'});before=req('/api/state');pair=req('/api/devices/pair',{'serverUrl':mobile})
  pem=ssl.get_server_certificate(('localhost',phone));ctx=ssl.create_default_context(cadata=pem);ctx.check_hostname=False
  key=rsa.generate_private_key(public_exponent=65537,key_size=2048);public=base64.b64encode(key.public_key().public_bytes(serialization.Encoding.DER,serialization.PublicFormat.SubjectPublicKeyInfo)).decode();proof=base64.b64encode(key.sign(('home-library-pair:v1:'+pair['code']+':'+public).encode(),padding.PKCS1v15(),hashes.SHA256())).decode()
  enrolled=req('/mobile/pair',{'code':pair['code'],'name':'Existing phone','publicKey':public,'signature':proof},ctx)
  cert=x509.load_der_x509_certificate(base64.b64decode(enrolled['certificate']));ca=x509.load_der_x509_certificate(base64.b64decode(enrolled['caCertificate']));certfile=Path(tmp)/'test-client.pem';keyfile=Path(tmp)/'test-client.key';certfile.write_bytes(cert.public_bytes(serialization.Encoding.PEM)+ca.public_bytes(serialization.Encoding.PEM));keyfile.write_bytes(key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()));keyfile.chmod(0o600);ctx.load_cert_chain(certfile,keyfile)
  assert req('/mobile/state',ctx=ctx)['state']==before['state']
 finally:stop(p,log)
 p,log=start(root/'release/linux-x64')
 try:
  assert req('/api/state')==before,'Existing library changed during rename'
  assert req('/api/sharing')['url']==sharing['url'],'Sharing URL changed'
  assert ssl.get_server_certificate(('localhost',phone))==pem,'Pinned server certificate changed'
  assert req('/mobile/state',ctx=ctx)==before,'Existing paired phone no longer works'
  assert req('/api/devices')[0]['id']==enrolled['deviceId']
  with urllib.request.urlopen(origin) as response:assert '<title>Carribrary' in response.read().decode()
  assert change('setLibraryTheme',{'libraryTheme':'ocean'})['state']['libraryName']=="Queen B's Library"
  print('PASS in-place 2.0.25 → Carribrary upgrade: unchanged catalog, custom name, borrower, reader, hold, revision, share link, device identity, pinned certificate and authenticated phone access; new Carribrary page title')
 finally:stop(p,log)
