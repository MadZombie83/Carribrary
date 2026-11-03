#!/usr/bin/env python3
"""Run supported browser flows with real local administrator enrollment in disposable data.
Requires CARRIBRARY_DOTNET, CARRIBRARY_NODE, PLAYWRIGHT_MODULE and test TLS certificate/key.
No production installation is opened or reset.
"""
from pathlib import Path
import os,subprocess,time,signal,urllib.request,json,tempfile,sys
root=Path(__file__).resolve().parents[2]
dotnet=os.environ['CARRIBRARY_DOTNET'];node=os.environ['CARRIBRARY_NODE']
logdir=root/'.build-tools/release-233-regressions';logdir.mkdir(parents=True,exist_ok=True)
proxy=logdir/'proxy.py';proxy.write_text('''import http.server,http.client,ssl,os
class Handler(http.server.BaseHTTPRequestHandler):
 def proxy(self):
  remote=http.client.HTTPConnection('127.0.0.1',19875,timeout=180)
  try:
   body=self.rfile.read(int(self.headers.get('Content-Length','0')))
   headers={k:v for k,v in self.headers.items() if k.lower() not in ('host','connection','transfer-encoding')}
   remote.request(self.command,self.path,body,headers);r=remote.getresponse();self.send_response(r.status)
   for k,v in r.getheaders():
    if k.lower() not in ('connection','transfer-encoding','server','date'):self.send_header(k,v)
   self.end_headers()
   while True:
    b=r.read(65536)
    if not b:break
    self.wfile.write(b)
  except (BrokenPipeError,ConnectionResetError):pass
  finally:remote.close()
 def do_GET(self):self.proxy()
 def do_POST(self):self.proxy()
 def do_HEAD(self):self.proxy()
 def log_message(self,*args):pass
server=http.server.ThreadingHTTPServer(('127.0.0.1',19877),Handler);ctx=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER);ctx.load_cert_chain(os.environ['CARRIBRARY_TEST_CERT'],os.environ['CARRIBRARY_TEST_KEY']);server.socket=ctx.wrap_socket(server.socket,server_side=True);server.serve_forever()
''')
checks=['server-updates','borrower-layout','product-info','self-service','public-connection','artwork-platforms','purchase-suggestions','admin-notifications','pwa-diagnostic','analytics-navigation','reading-polish','appearance-portable','theme-artwork','complete-recovery','backup-verification','acquisition-renewals','borrower-controls','summary-panels','release-polish','admin-landing','settings-pages','admin-pages','custom-themes','repair-requests','accounts','private-vault','account-deletion','borrower-app','data-controls','lending','circulation','collections-google','registration-security','turnstile','complete-backup','quick-start','ecosystem']
if len(sys.argv)>1:checks=sys.argv[1:]
base=os.environ.copy();base.update(Library__AdminPort='19873',Library__PhonePort='19874',Library__CatalogPort='19875',Library__DiscoveryPort='19876',Library__EnableDiscovery='false',LIBRARY_TEST_URL='http://localhost:19873',DOTNET_COMMAND=dotnet)
plog=open(logdir/'proxy.log','w');gateway=subprocess.Popen([sys.executable,str(proxy)],cwd=root,env=base,stdout=plog,stderr=subprocess.STDOUT)
results=json.loads((logdir/'results.json').read_text()) if (logdir/'results.json').exists() else {}
try:
 for name in checks:
  script=name+'-ui.mjs';origin='https://localhost:19877' if name in ['responsive-artwork','product-info','self-service','public-connection','artwork-platforms','purchase-suggestions','pwa-diagnostic','admin-notifications','analytics-navigation','appearance-portable','theme-artwork','backup-verification','acquisition-renewals','summary-panels','release-polish','settings-pages','admin-pages','custom-themes','repair-requests','borrower-app','complete-backup','quick-start','ecosystem','circulation','lending','collections-google'] else 'https://library.test'
  fixture=tempfile.mkdtemp(prefix='hl-circulation-ui-233-');env=base.copy();env.update(HOME_LIBRARY_DATA=fixture,Library__PublicCatalogOrigin=origin,PUBLIC_TEST_URL='https://localhost:19877' if origin.startswith('https://localhost') else 'http://localhost:19875',GUIDE_VERIFY_ONLY='' if os.environ.get('CARRIBRARY_CAPTURE_GUIDE')=='1' and name=='quick-start' else '1')
  if name=='complete-recovery':env.update(FLATPAK_ID='com.household.HomeLibrary')
  if name=='turnstile':env.update(BorrowerSecurity__TurnstileSiteKey='public-test-sitekey',BorrowerSecurity__TurnstileSecretKey='private-fixture-secret')
  slog=open(logdir/(name+'.server.log'),'w');server=subprocess.Popen([dotnet,str(root/'home-library-native/server/bin/Release/net10.0/HomeLibrary.Server.dll')],cwd=root,env=env,stdout=slog,stderr=subprocess.STDOUT)
  try:
   for _ in range(180):
    if server.poll() is not None:raise RuntimeError('Server failed: '+name)
    try:
     with urllib.request.urlopen(base['LIBRARY_TEST_URL']+'/local-auth/health',timeout=1) as reply:assert json.load(reply)['ready']
     break
    except OSError:time.sleep(.1)
   else:raise RuntimeError('Readiness timeout: '+name)
   with open(logdir/(name+'.log'),'w') as log:result=subprocess.run([node,str(root/'home-library-native/tests'/script)],cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=180)
   results[name]=result.returncode==0;print(name+': '+('PASS' if results[name] else 'FAIL (see log)'),flush=True)
  finally:
   server.terminate()
   try:server.wait(timeout=15)
   except subprocess.TimeoutExpired:server.kill();server.wait()
   slog.close()
finally:
 gateway.terminate();gateway.wait(timeout=10);plog.close()
 (logdir/'results.json').write_text(json.dumps(results,indent=2)+'\n')
if not all(results[name] for name in checks):sys.exit(1)
