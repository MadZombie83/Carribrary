"""Print the current HTML Quick Start as a PDF; no second, stale text copy.
Requires reportlab and Pillow. Never starts or changes an installed library.
"""
from pathlib import Path
from html.parser import HTMLParser
from xml.sax.saxutils import escape
import re
from PIL import Image as PILImage
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, KeepTogether, PageBreak
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.colors import HexColor
root=Path(__file__).resolve().parents[2];project=root/'home-library-native'
version=re.search(r'Version="([^"]+)"',(project/'server/SetupStatus.cs').read_text())[1].removesuffix('-preview')
out=root/'output/pdf'/f'Carribrary-Quick-Start-{version}.pdf';out.parent.mkdir(parents=True,exist_ok=True)
class Guide(HTMLParser):
 def __init__(self):super().__init__();self.main=False;self.tag=None;self.text=[];self.blocks=[];self.list_count=0;self.in_ol=False
 def handle_starttag(self,tag,attrs):
  attrs=dict(attrs)
  if tag=='main':self.main=True
  if not self.main:return
  if tag=='nav':self.nav=True
  if tag=='ol':self.in_ol=True;self.list_count=0
  if tag in ('h1','h2','p','li','figcaption'):self.tag=tag;self.text=[]
  if tag=='img':self.blocks.append(('img',attrs['src']))
 def handle_data(self,text):
  if self.main and self.tag:self.text.append(text)
 def handle_endtag(self,tag):
  if tag==self.tag:
   value=' '.join(''.join(self.text).split())
   if tag=='li':self.list_count+=1;value=f'{self.list_count}. '+value
   if value and 'Print / save as PDF' not in value:self.blocks.append((tag,value))
   self.tag=None
  if tag=='main':self.main=False
  if tag=='ol':self.in_ol=False
parser=Guide();parser.feed((project/'server/wwwroot/quick-start.html').read_text())
styles=getSampleStyleSheet();styles.add(ParagraphStyle(name='CBody',fontName='Helvetica',fontSize=10.5,leading=15,spaceAfter=9,textColor=HexColor('#25334a')));styles.add(ParagraphStyle(name='CHead',fontName='Helvetica-Bold',fontSize=15,leading=19,spaceBefore=14,spaceAfter=10,keepWithNext=True,textColor=HexColor('#493276')));styles.add(ParagraphStyle(name='CTitle',fontName='Helvetica-Bold',fontSize=24,leading=29,spaceAfter=12,textColor=HexColor('#493276')));styles.add(ParagraphStyle(name='CCaption',parent=styles['CBody'],fontSize=8.5,leading=11,textColor=HexColor('#556276'),spaceAfter=12))
def clean(s):return s.replace('→',' > ').replace('·',' | ').replace('’',"'").replace('‘',"'").replace('—','-').replace('–','-').replace('…','...')
story=[Paragraph('Carribrary '+version+' | Quick start',styles['CCaption'])];section_count=0
for kind,value in parser.blocks:
 if kind=='h2':
  section_count+=1
  if section_count>1:story.append(PageBreak())
 if kind=='img':
  path=project/'server/wwwroot'/value
  with PILImage.open(path) as img:w,h=img.size
  scale=min(460/w,(260 if section_count==1 else 360)/h);story.append(Image(str(path),width=w*scale,height=h*scale));story.append(Spacer(1,8))
 else:story.append(Paragraph(escape(clean(value)),styles['CTitle' if kind=='h1' else 'CHead' if kind=='h2' else 'CCaption' if kind=='figcaption' else 'CBody']))
def footer(canvas,doc):
 canvas.saveState();canvas.setStrokeColor(HexColor('#d9d4ec'));canvas.line(44,38,568,38);canvas.setFont('Helvetica',8);canvas.setFillColor(HexColor('#556276'));canvas.drawString(44,25,'Carribrary '+version+' | Documentation reviewed 2026-10-30');canvas.drawRightString(568,25,str(doc.page));canvas.restoreState()
SimpleDocTemplate(str(out),pagesize=(612,792),leftMargin=44,rightMargin=44,topMargin=38,bottomMargin=54,title='Carribrary '+version+' Quick Start',author='Carribrary').build(story,onFirstPage=footer,onLaterPages=footer)
print(out)
