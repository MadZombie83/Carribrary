package com.household.homelibrary

data class CirculationCopy(val id:String,val isbn:String,val title:String,val author:String,val onLoan:Boolean,val archived:Boolean=false,val series:String="",val collectionName:String="",val onHold:Boolean=false,val copyUid:String="")
fun circulationCode(raw:String)=raw.trim().uppercase().replace(Regex("[\\s-]"),"")
fun validCirculationISBN(raw:String):Boolean {
 val code=circulationCode(raw)
 return when {
  code.matches(Regex("[0-9]{9}[0-9X]"))->code.mapIndexed{i,c->(if(c=='X')10 else c-'0')*(10-i)}.sum()%11==0
  code.matches(Regex("[0-9]{13}"))->code.mapIndexed{i,c->(c-'0')*(if(i%2==0)1 else 3)}.sum()%10==0
  else->false
 }
}
fun circulationEdition(raw:String):String {
 val code=circulationCode(raw);if(code.length!=10)return code
 val prefix="978"+code.take(9);return prefix+((10-prefix.mapIndexed{i,c->(c-'0')*(if(i%2==0)1 else 3)}.sum()%10)%10)
}
fun validCopyReference(raw:String)=raw.trim().matches(Regex("carribrary:copy:v1:[a-f0-9]{32}",RegexOption.IGNORE_CASE))
fun circulationMatches(copies:List<CirculationCopy>,raw:String,returning:Boolean):List<CirculationCopy> {
 val code=circulationCode(raw);if(code.isBlank())return emptyList()
 val id=if(code.matches(Regex("BK[0-9]+")))"BK-"+code.drop(2).padStart(4,'0') else ""
 return copies.filter{!it.archived&&it.onLoan==returning&&(returning||!it.onHold)&&when {
  raw.trim().startsWith("carribrary:",true)->validCopyReference(raw)&&it.copyUid.equals(raw.trim().takeLast(32),true)
  id.isNotEmpty()->it.id==id
  code.matches(Regex("[0-9X]+"))->validCirculationISBN(code)&&circulationEdition(it.isbn)==circulationEdition(code)
  else->"${it.title} ${it.author} ${it.series}".contains(raw.trim(),true)
 }}
}
