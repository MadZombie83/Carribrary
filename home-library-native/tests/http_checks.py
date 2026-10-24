import os,urllib.request,urllib.error,json,ssl,base64,hashlib,tempfile,pathlib,http.client
from cryptography import x509
from cryptography.hazmat.primitives.asymmetric import rsa,padding
from cryptography.hazmat.primitives import hashes,serialization
ADMIN=os.environ.get('LIBRARY_TEST_URL','http://localhost:4180');PHONE=os.environ.get('PHONE_TEST_URL','https://localhost:7443');PUBLIC=os.environ.get('PUBLIC_TEST_URL','http://localhost:4181')
def req(base,path,body=None,context=None,origin=ADMIN):
 data=None if body is None else json.dumps(body).encode();headers={}
 if data is not None:headers={'Content-Type':'application/json','Origin':origin}
 try:
  with urllib.request.urlopen(urllib.request.Request(base+path,data=data,headers=headers),context=context,timeout=10) as r:
   raw=r.read();return r.status,json.loads(raw) if raw else None
 except urllib.error.HTTPError as e:return e.code,None
pair=req(ADMIN,'/api/devices/pair',{'serverUrl':PHONE})[1]
pem=ssl.get_server_certificate(('localhost',__import__('urllib.parse',fromlist=['urlparse']).urlparse(PHONE).port));leaf=x509.load_pem_x509_certificate(pem.encode())
spki=leaf.public_key().public_bytes(serialization.Encoding.DER,serialization.PublicFormat.SubjectPublicKeyInfo)
assert base64.b64encode(hashlib.sha256(spki).digest()).decode()==pair['serverKey']
ctx=ssl.create_default_context(cadata=pem);ctx.check_hostname=False
assert req(PHONE,'/mobile/state',context=ctx)[0]==401
assert req(PHONE,'/mobile/devices',context=ctx)[0]==401
assert req(ADMIN,'/api/devices/pair',{'serverUrl':PHONE},origin='https://evil.example')[0]==403
key=rsa.generate_private_key(public_exponent=65537,key_size=2048);public=base64.b64encode(key.public_key().public_bytes(serialization.Encoding.DER,serialization.PublicFormat.SubjectPublicKeyInfo)).decode()
proof=base64.b64encode(key.sign(('home-library-pair:v1:'+pair['code']+':'+public).encode(),padding.PKCS1v15(),hashes.SHA256())).decode()
body={'code':pair['code'],'name':'HTTP test phone','publicKey':public,'signature':proof}
status,enrolled=req(PHONE,'/mobile/pair',body,ctx);assert status==200
assert req(PHONE,'/mobile/pair',body,ctx)[0]==401
with tempfile.TemporaryDirectory(prefix='hl-client-cert-') as folder:
 p=pathlib.Path(folder)
 cert=x509.load_der_x509_certificate(base64.b64decode(enrolled['certificate']));ca=x509.load_der_x509_certificate(base64.b64decode(enrolled['caCertificate']))
 (p/'cert.pem').write_bytes(cert.public_bytes(serialization.Encoding.PEM)+ca.public_bytes(serialization.Encoding.PEM));(p/'key.pem').write_bytes(key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()));(p/'key.pem').chmod(0o600)
 ctx.load_cert_chain(p/'cert.pem',p/'key.pem')
 status,state=req(PHONE,'/mobile/state',context=ctx);assert status==200
 assert req(PHONE,'/api/state',context=ctx)[0]==404
 assert req(PHONE,'/mobile/action',{'action':'setLibraryName','input':{'libraryName':'Phone-administered library'},'revision':state['revision']},ctx)[0]==200
 state=req(PHONE,'/mobile/state',context=ctx)[1]
 status,presentation=req(PHONE,'/mobile/action',{'action':'setLibraryPresentation','input':{'bannerSubtitle':'Sharing joy','encouragingMessage':'Keep going ♥'},'revision':state['revision']},ctx)
 assert status==200 and presentation['state']['bannerSubtitle']=='Sharing joy'
 assert req(ADMIN,'/api/state')[1]['state']['encouragingMessage']=='Keep going ♥'
 state=presentation

 assert req(PHONE,'/mobile/sharing',context=ctx)[0]==200
 assert req(PHONE,'/mobile/receipts/BK-0001',context=ctx)[0]==400
 assert req(PHONE,'/mobile/action',{'action':'restore','input':state['state'],'revision':state['revision']},ctx)[0]==200
 state=req(PHONE,'/mobile/state',context=ctx)[1]
 add={'action':'addBook','input':{'isbn':'9780141439518','title':'TLS test book','author':'Test Author','coverUrl':'https://covers.openlibrary.org/b/isbn/9780141439518-M.jpg'},'revision':state['revision'],'requestId':'cedec4d8-53d3-40b4-9181-80c4bd1daabc'}
 assert req(PHONE,'/mobile/action',add,ctx)[0]==200
 assert req(PHONE,'/mobile/action',add,ctx)[0]==200
 current=req(ADMIN,'/api/state')[1];assert len(current['state']['books'])==1
 assert req(ADMIN,'/api/action',{'action':'setLibraryName','input':{'libraryName':'Our Family Library'},'revision':current['revision']})[0]==200
 current=req(ADMIN,'/api/state')[1]
 borrower=req(ADMIN,'/api/action',{'action':'addBorrower','input':{'name':'Receipt Borrower','contact':'receipt+test@example.com'},'revision':current['revision']})[1]
 loan=req(PHONE,'/mobile/action',{'action':'checkout','input':{'copyId':'BK-0001','borrowerId':'BR-0001','checkoutDate':__import__('datetime').date.today().isoformat(),'dueDate':'2099-01-01'},'revision':borrower['revision']},ctx)[1]
 code,receipt=req(PHONE,'/mobile/receipts/BK-0001',context=ctx);assert code==200;assert receipt['medium']=='email';assert receipt['recipient']=='receipt+test@example.com';assert '2099-01-01' in receipt['body'];assert req(PUBLIC,'/api/receipts/BK-0001')[0]==404
 share=req(PHONE,'/mobile/sharing',{'action':'enable'},ctx)[1];path=urllib.request.urlparse(share['url']).path if hasattr(urllib.request,'urlparse') else __import__('urllib.parse',fromlist=['urlparse']).urlparse(share['url']).path
 public=req(PUBLIC,path+'books.json')[1];assert public['books'][0]['title']=='TLS test book';assert public['books'][0]['coverUrl']=='https://covers.openlibrary.org/b/isbn/9780141439518-M.jpg';assert set(public['books'][0])=={'id','isbn','title','description','author','series','publisher','coverUrl','collectionId','availability'};assert public['libraryName']=='Our Family Library';assert set(public)=={'books','libraryName','libraryTheme','bannerSubtitle','encouragingMessage','collections'};assert public['bannerSubtitle']=='Sharing joy';assert public['encouragingMessage']=='Keep going ♥';assert 'receipt+test@example.com' not in json.dumps(public)
 assert req(PUBLIC,'/api/state')[0]==404
 assert req(PUBLIC,path+'books.json',{})[0]==405

 visitor={'expectedIsbn':'9780141439518','expectedTitle':'TLS test book','copyId':'BK-0001','name':'Visitor','contact':'visitor@example.com','message':'Please','requestId':str(__import__('uuid').uuid4())}
 assert req(PUBLIC,path+'requests',visitor,origin='https://catalog.example')[0]==409
 returned=req(PHONE,'/mobile/action',{'action':'returnBook','input':{'copyId':'BK-0001'},'revision':loan['revision']},ctx)[1]
 status,ack=req(PUBLIC,path+'requests',visitor,origin='https://catalog.example');assert status==200 and set(ack)=={'id','status'}
 assert req(PUBLIC,path+'requests',visitor,origin='https://catalog.example')[1]==ack
 assert req(PUBLIC,path+'requests')[0]==404
 assert req(PUBLIC,path+'requests',visitor,origin='https://evil.example')[0]==403
 assert req(PUBLIC,'/s/invalid/requests',visitor,origin='https://catalog.example')[0]==404
 assert 'visitor@example.com' not in json.dumps(req(PUBLIC,path+'books.json')[1])
 current=req(ADMIN,'/api/state')[1];assert current['state']['requests'][0]['id']==ack['id']
 assert req(PHONE,'/mobile/action',{'action':'requestBook','input':visitor,'revision':current['revision']},ctx)[0]==403
 approved=req(PHONE,'/mobile/action',{'action':'approveRequest','input':{'requestId':ack['id']},'revision':current['revision']},ctx)[1]
 assert len(approved['state']['loans'])==1 and len(approved['state']['borrowers'])==1
 assert len(approved['state']['holds'])==1 and all(l['returnedDate'] for l in approved['state']['loans'])
 assert req(PHONE,'/mobile/action',{'action':'removeBook','input':{'copyId':'BK-0001'},'revision':approved['revision']},ctx)[0]==400
 assert req(PHONE,'/mobile/action',{'action':'checkout','input':{'copyId':'BK-0001','borrowerId':'BR-0001','checkoutDate':__import__('datetime').date.today().isoformat()},'revision':approved['revision']},ctx)[0]==409
 fulfillment={'action':'fulfillRequest','input':{'requestId':ack['id'],'checkoutDate':__import__('datetime').date.today().isoformat(),'dueDate':'2099-02-01'},'revision':approved['revision']}
 status,fulfilled=req(PHONE,'/mobile/action',fulfillment,ctx);assert status==200 and fulfilled['state']['requests'][0]['status']=='fulfilled'
 assert req(PHONE,'/mobile/receipts/BK-0001',context=ctx)[1]['recipient']=='visitor@example.com'
 assert 'visitor@example.com' not in json.dumps(req(PUBLIC,path+'books.json')[1])
 statuses=[req(PUBLIC,path+'requests',visitor,origin='https://catalog.example')[0] for _ in range(11)];assert 429 in statuses
 req(PHONE,'/mobile/sharing',{'action':'disable'},ctx);assert req(PUBLIC,path+'books.json')[0]==404
 current=req(PHONE,'/mobile/state',context=ctx)[1]
 status,edited=req(PHONE,'/mobile/action',{'action':'updateBook','input':{'copyId':'BK-0001','isbn':'9780141439518','title':'Edited on Android','author':'Test Author'},'revision':current['revision']},ctx);assert status==200
 restored=req(PHONE,'/mobile/action',{'action':'restore','input':current['state'],'revision':edited['revision']},ctx)[1];assert restored['state']['books'][0]['title']=='TLS test book'
 returned=req(PHONE,'/mobile/action',{'action':'returnBook','input':{'copyId':'BK-0001'},'revision':restored['revision']},ctx)[1]
 archived=req(PHONE,'/mobile/action',{'action':'removeBorrower','input':{'borrowerId':'BR-0002'},'revision':returned['revision']},ctx)[1];assert archived['state']['borrowers'][1]['archived'] and len(archived['state']['loans'])==2
 unused=req(PHONE,'/mobile/action',{'action':'addBorrower','input':{'name':'Unused'},'revision':archived['revision']},ctx)[1]
 removed=req(PHONE,'/mobile/action',{'action':'removeBorrower','input':{'borrowerId':'BR-0003'},'revision':unused['revision']},ctx)[1];assert len(removed['state']['borrowers'])==2
 tracked=req(PHONE,'/mobile/action',{'action':'addReader','input':{'name':'Phone reader'},'revision':removed['revision']},ctx)[1]
 readerid=tracked['result']['id'];tracked=req(PHONE,'/mobile/action',{'action':'saveReading','input':{'readerId':readerid,'copyId':'BK-0001','status':'reading','notes':'PRIVATE READING HTTP TEST'},'revision':tracked['revision']},ctx)[1]
 assert req(PHONE,'/mobile/readers/'+readerid+'/sharing',context=ctx)[0]==200
 grant=req(PHONE,'/mobile/readers/'+readerid+'/sharing',{'action':'enable'},ctx)[1];assert grant['enabled']
 assert req(PUBLIC,'/reading/state')[0]==401
 assert req(PUBLIC,'/api/readers/'+readerid+'/sharing')[0]==404
 token=grant['url'].split('#')[1]
 with urllib.request.urlopen(urllib.request.Request(PUBLIC+'/reading/state',headers={'Authorization':'Bearer '+token})) as own:assert 'PRIVATE READING HTTP TEST' in own.read().decode()
 assert req(PHONE,'/mobile/readers/'+readerid+'/sharing',{'action':'disable'},ctx)[1]['enabled']==False
 removed=tracked
 removedbook=req(PHONE,'/mobile/action',{'action':'removeBook','input':{'copyId':'BK-0001'},'revision':removed['revision']},ctx)[1];assert removedbook['state']['books'][0]['archived'] and len(removedbook['state']['requests'])==1
 shared=req(PHONE,'/mobile/sharing',{'action':'enable'},ctx)[1]
 signup={'username':'mobile-reader','password':'A mobile test passphrase!','name':'Phone reader','contact':'','shareToken':shared['url'].split('/s/')[1].strip('/')}
 status,registered=req(PUBLIC,'/account/register',signup,origin='https://catalog.example');assert status==200
 accountid=registered['account']['id'];assert req(PHONE,'/mobile/accounts',context=ctx)[0]==200
 status,approvedaccount=req(PHONE,'/mobile/accounts/approve',{'accountId':accountid,'readerId':readerid},ctx);assert status==200 and approvedaccount['account']['status']=='active'
 assert req(PHONE,'/mobile/accounts/reset',{'accountId':accountid},ctx)[0]==403 # legacy reading must be encrypted before admin-issued reset
 assert req(PHONE,'/mobile/readers/'+readerid+'/sharing',context=ctx)[0]>=400
 assert req(PHONE,'/mobile/accounts/block',{'accountId':accountid},ctx)[0]==200
 selection={'accountId':accountid}
 status,preview=req(PHONE,'/mobile/accounts/deletion-preview',selection,ctx);assert status==200 and bool(preview['username'])
 assert req(PUBLIC,'/api/accounts/deletion-preview',selection,origin='https://catalog.example')[0]==405
 selection.update(confirmation=preview['username'],previewToken=preview['previewToken'])
 assert req(PHONE,'/mobile/accounts/delete',selection,ctx)[0]==200
 assert all(account['id']!=accountid for account in req(PHONE,'/mobile/accounts',context=ctx)[1]['accounts'])
 assert req(PUBLIC,'/api/accounts')[0]==404
 devices=req(PHONE,'/mobile/devices',context=ctx)[1];assert devices['currentDeviceId']==enrolled['deviceId'] and len(devices['devices'])==1
 assert req(PUBLIC,'/mobile/devices')[0]==404
 assert req(PHONE,'/mobile/devices/pair',{'serverUrl':PHONE},ctx)[0]==200
 assert req(PHONE,'/mobile/devices/revoke',{'deviceId':enrolled['deviceId']},ctx)[0]==200
 try:status,_=req(PHONE,'/mobile/state',context=ctx);assert status==401
 except (ssl.SSLError,urllib.error.URLError,http.client.RemoteDisconnected):pass
print('PASS mobile administration, safe deletion, history, restore, device management and live HTTPS pairing, certificate authentication, phone revocation, CSRF checks, idempotent retry, public projection and QR revocation')
