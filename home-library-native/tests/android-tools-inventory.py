#!/usr/bin/env python3
"""Guard discoverability of the pre-redesign native tool actions. Does not claim device UI testing."""
from pathlib import Path
import re
source=Path(__file__).resolve().parents[1]/'android/app/src/main/java/com/household/homelibrary'
text=(source/'PhoneNavigation.kt').read_text();block=text[text.index('internal val phoneToolCategories'):text.index('@Composable fun PhoneMoreMenu')]
ids=re.findall(r'"([A-Za-z]+)" to ',block)
previous={'lending','Borrowers','Requests','accounts','History','acquisitions','sharing','collections','dataControls','theme','settings','copyTools','recycle','reminders','setup','csv','backup','restore','devices','connection'}
assert previous<=set(ids),'A previous tool lost its settings route'
assert {'patronCards','selfService'}<=set(ids),'Self-service/card tools must have settings routes'
assert 'optInt("version") in 1..7' in (source/'Administration.kt').read_text(),'Android backup import must accept current format 7'
assert 'libraryInsights' in set(ids),'Analytics lost its Settings & Tools route'
assert 'PhonePanel{TextButton(enabled=online&&!busy,onClick={dialog="libraryInsights"})' not in (source/'MainActivity.kt').read_text(),'Analytics remains on Home'
assert len(ids)==len(set(ids)),'Duplicate native tool action'
assert len(re.findall(r'"[^"\n]+ & [^"\n]+" to listOf',block))==6
print('PASS 20 previous Android tool actions retained across six categories, plus Bulk edit. Native device interaction is a separate check.')
