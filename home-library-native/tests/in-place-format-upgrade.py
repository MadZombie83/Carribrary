#!/usr/bin/env python3
"""Upgrade old->new DLLs with temporary data; never opens an installed library.
Usage: python3 in-place-upgrade.py DOTNET OLD_DLL NEW_DLL
"""
from pathlib import Path
import copy,base64,http.cookiejar,json,os,socket,ssl,subprocess,sys,tempfile,time,urllib.request,urllib.error
from cryptography import x509
from cryptography.hazmat.primitives.asymmetric import rsa,padding
from cryptography.hazmat.primitives import hashes,serialization
runtime,old,new=map(lambda v:str(Path(v).resolve()),sys.argv[1:4])
def free_port():
 with socket.socket() as s:s.bind(('127.0.0.1',0));return s.getsockname()[1]
ports=set()
while len(ports)<3:ports.add(free_port())
a,p,c=ports;admin=f'http://localhost:{a}';mobile=f'https://localhost:{p}';public=f'http://localhost:{c}'
with tempfile.TemporaryDirectory(prefix='carribrary-in-place-upgrade-') as tmp:
 env=os.environ.copy();env.update(HOME_LIBRARY_DATA=tmp,Library__AdminPort=str(a),Library__PhonePort=str(p),Library__CatalogPort=str(c),Library__EnableDiscovery='false',Library__PublicCatalogOrigin='https://library.test')
 jar=http.cookiejar.CookieJar();client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar));process=None;log=None
 def request(url,body=None,headers=None,context=None,session=True):
  h=dict(headers or {});payload=None
  if body is not None:payload=json.dumps(body).encode();h.update({'Content-Type':'application/json','Origin':admin if url.startswith(admin) else 'https://library.test'})
  req=urllib.request.Request(url,data=payload,headers=h)
  response=urllib.request.urlopen(req,context=context,timeout=20) if context else (client.open(req,timeout=20) if session else urllib.request.urlopen(req,timeout=20))
  with response:
   raw=response.read();return json.loads(raw) if 'json' in response.headers.get('Content-Type','') else raw.decode(),response.headers
 def start(dll):
  global process,log
  log=open(Path(tmp)/'server.log','a');process=subprocess.Popen([runtime,dll],env=env,stdout=log,stderr=subprocess.STDOUT)
  for _ in range(160):
   if process.poll() is not None:raise RuntimeError('Upgrade fixture server exited; inspect its private temporary log locally.')
   try:request(admin+'/local-auth/health',session=False);return
   except OSError:time.sleep(.1)
  raise RuntimeError('Upgrade fixture readiness timeout')
 def stop():
  global process,log
  if process is not None:
   process.terminate();process.wait(timeout=20);process=None;log.close()
 def local(path,body=None):return request(admin+path,body)[0]
 def change(action,input):return local('/api/action',{'action':action,'input':input,'revision':local('/api/state')['revision']})['result']
 try:
  start(old);password='Disposable upgrade fixture passphrase 241!';local('/local-auth/enroll',{'username':'Upgrade Admin','password':password,'code':(Path(tmp)/'local-admin-setup.txt').read_text().strip()})
  change('addBook',{'isbn':'9780140328721','title':'Upgrade fixture book','author':'Test Author'});borrower=change('addBorrower',{'name':'Upgrade Reader','contact':'upgrade@example.test'});reader=change('addReader',{'name':'Household Reader'});change('saveReading',{'readerId':reader['id'],'copyId':'BK-0001','status':'want','rating':0,'notes':'Private household fixture note'});change('checkout',{'copyId':'BK-0001','borrowerId':borrower['id'],'checkoutDate':'2026-10-01','dueDate':'2026-11-01'})
  import struct,zlib
  def png_chunk(name,value):return struct.pack('>I',len(value))+name+value+struct.pack('>I',zlib.crc32(name+value)&0xffffffff)
  png=b'\x89PNG\r\n\x1a\n'+png_chunk(b'IHDR',struct.pack('>IIBBBBB',512,1536,8,6,0,0,0))+png_chunk(b'IDAT',zlib.compress((b'\0'+b'\x79\x41\xa5\xff'*512)*1536))+png_chunk(b'IEND',b'')
  change('setLibrarySidebarArtwork',{'artwork':base64.b64encode(png).decode()});change('saveCustomTheme',{'name':'Original household theme'})
  sharing=local('/api/sharing',{'action':'enable'});token=sharing['url'].split('/s/')[1].strip('/');before=local('/api/state')
  code=local('/api/browser-admins/pair',{})['code'];_,headers=request(public+'/companion/enroll',{'code':code,'name':'Upgrade Browser'},session=False);cookie=headers['Set-Cookie'].split(';')[0]
  assert request(public+'/companion/api/state',headers={'Cookie':cookie},session=False)[0]['state']==before['state']
  pem=ssl.get_server_certificate(('localhost',p));ctx=ssl.create_default_context(cadata=pem);ctx.check_hostname=False
  pair=local('/api/devices/pair',{'serverUrl':mobile});key=rsa.generate_private_key(public_exponent=65537,key_size=2048);public_key=base64.b64encode(key.public_key().public_bytes(serialization.Encoding.DER,serialization.PublicFormat.SubjectPublicKeyInfo)).decode();signature=base64.b64encode(key.sign(('home-library-pair:v1:'+pair['code']+':'+public_key).encode(),padding.PKCS1v15(),hashes.SHA256())).decode()
  enrolled=request(mobile+'/mobile/pair',{'code':pair['code'],'name':'Upgrade Phone','publicKey':public_key,'signature':signature},context=ctx,session=False)[0]
  cert=x509.load_der_x509_certificate(base64.b64decode(enrolled['certificate']));ca=x509.load_der_x509_certificate(base64.b64decode(enrolled['caCertificate']));certfile=Path(tmp)/'client.pem';keyfile=Path(tmp)/'client.key';certfile.write_bytes(cert.public_bytes(serialization.Encoding.PEM)+ca.public_bytes(serialization.Encoding.PEM));keyfile.write_bytes(key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()));keyfile.chmod(0o600);ctx.load_cert_chain(certfile,keyfile)
  assert request(mobile+'/mobile/state',context=ctx)[0]['state']==before['state'];stop();start(new)
  try:local('/api/state');raise AssertionError('Local session must expire across restart')
  except urllib.error.HTTPError as error:assert error.code==401
  local('/local-auth/login',{'username':'Upgrade Admin','password':password});after=copy.deepcopy(before);after['state']['version']=7;after['state']['renewalPolicy']={'enabled':True,'automatic':False,'days':14,'maximum':2}
  for book in after['state']['books']:book.setdefault('importNotes','');book.setdefault('genre','')
  for reader in after['state']['readers']:reader.setdefault('annualGoals',{})
  for entry in after['state']['readingEntries']:
   entry.setdefault('currentPage',0);entry.setdefault('totalPages',0);entry.setdefault('sessions',[]);entry.setdefault('annotations',[])
  for loan in after['state']['loans']:
   loan['renewals']=[]
   for key,value in {'source':'librarian','kioskId':'','returnSource':'','returnRequestedAt':'','returnRequestedSource':'','returnKioskId':''}.items():loan.setdefault(key,value)
  after['state']['selfService']={'checkout':False,'return':False,'kiosk':False,'loanDays':14,'idleSeconds':90,'returnInstructions':'Place the book in the library’s designated return area. A librarian will confirm receipt.'}
  observed=local('/api/state')
  import uuid
  for expected,actual in zip(after['state']['borrowers'],observed['state']['borrowers']):
   uid=actual['borrowerUid'];assert uuid.UUID(hex=uid).hex==uid;expected['borrowerUid']=uid
  for theme in after['state']['savedThemes']:theme.setdefault('sidebarArtwork',None)
  assert local('/api/state')==after
  if before['state']['version']<4:assert list((Path(tmp)/'backups').glob('before-catalog-v4-*.hlbackup'))
  if before['state']['version']<5:assert list((Path(tmp)/'backups').glob('before-theme-art-v5-*.hlbackup'))
  if before['state']['version']<6:assert list((Path(tmp)/'backups').glob('before-reading-v6-*.hlbackup'))
  assert list((Path(tmp)/'backups').glob('before-self-service-v7-*.hlbackup'))
  assert local('/api/sharing')['url']==sharing['url'];assert request(public+'/s/'+token+'/books.json',session=False)[0]['books'][0]['title']=='Upgrade fixture book'
  assert request(public+'/companion/api/state',headers={'Cookie':cookie},session=False)[0]['state']==after['state'];assert request(mobile+'/mobile/state',context=ctx)[0]['state']==after['state'];assert ssl.get_server_certificate(('localhost',p))==pem
  welcome=request(public+'/',session=False)[0];assert 'welcome-cards' in welcome and token not in welcome and 'upgrade@example.test' not in welcome and 'Private household fixture note' not in welcome
  stop()
  try:start(old);raise AssertionError('An older server must reject format 7')
  except RuntimeError:pass
  finally:stop()
  import sqlite3
  with sqlite3.connect(Path(tmp)/'library.sqlite') as db:assert json.loads(db.execute('SELECT data FROM library WHERE id=1').fetchone()[0])['version']==7
  start(new);local('/local-auth/login',{'username':'Upgrade Admin','password':password});assert local('/api/state')==after
  print('PASS real in-place format-3/4/5/6→7 server upgrade preserves catalog, contacts, active loan, household reading, local credentials, shared link, native mTLS identity and paired browser; private pre-format backups exist, saved palette/PNG survive, older server rejects format 7 and local session expires safely; branded front door reveals no token/private records.')
 finally:stop()
