#!/usr/bin/env python3
"""Run browser circulation checks against a disposable published Linux server.
Dependencies: Node + Playwright + Chrome. NODE, PLAYWRIGHT_MODULE and CHROME_PATH
may select existing local tools. This never uses the normal library ports/data.
"""
from pathlib import Path
import os, subprocess, tempfile, time, urllib.request, socket, sys
root=Path(__file__).resolve().parents[1]
def port():
 with socket.socket() as s:
  s.bind(('127.0.0.1',0));return s.getsockname()[1]
ports=set()
while len(ports)<3:ports.add(port())
admin,phone,catalog=ports
with tempfile.TemporaryDirectory(prefix='hl-circulation-ui-') as temp:
 env=os.environ.copy();env.update(HOME_LIBRARY_DATA=temp,Library__AdminPort=str(admin),Library__PhonePort=str(phone),Library__CatalogPort=str(catalog),Library__EnableDiscovery='false',Library__PublicCatalogOrigin=('https://catalog.example' if len(sys.argv)>1 and sys.argv[1].endswith('.py') else 'https://library.test'),PUBLIC_TEST_URL=f'http://localhost:{catalog}',LIBRARY_TEST_URL=f'http://localhost:{admin}',PHONE_TEST_URL=f'https://localhost:{phone}')
 with open(Path(temp)/'server.log','w') as log:
  server=subprocess.Popen([str(root/'release/linux-x64/HomeLibrary.Server')],env=env,stdout=log,stderr=subprocess.STDOUT)
  try:
   for _ in range(60):
    if server.poll() is not None:
     log.flush();raise RuntimeError('Temporary server exited: '+(Path(temp)/'server.log').read_text()[-4000:])
    try:urllib.request.urlopen(env['LIBRARY_TEST_URL']+'/api/state',timeout=1);break
    except Exception:time.sleep(.2)
   else:raise RuntimeError('Temporary server failed to start')
   suite=sys.argv[1] if len(sys.argv)>1 else 'circulation-ui.mjs'
   subprocess.run([sys.executable if suite.endswith('.py') else env.get('NODE','node'),str(root/'tests'/suite)],env=env,check=True)
  finally:
   if server.poll() is None:server.terminate();server.wait(timeout=10)
