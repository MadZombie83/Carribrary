package com.household.homelibrary
fun main(){
val copies=listOf(
CirculationCopy("BK-0001","9780140328721","Matilda","Roald Dahl",false,false),
CirculationCopy("BK-0002","0140328726","Matilda","Roald Dahl",true,false),
CirculationCopy("BK-0003","9780140328721","Matilda","Roald Dahl",true,false),
CirculationCopy("BK-0004","9780140328721","Matilda","Roald Dahl",false,true),
CirculationCopy("BK-0005","9780547928227","Other book","Roald Dahl",false,false)
)
check(circulationMatches(copies,"9780140328721",false).map{it.id}==listOf<String>("BK-0001")) { "9780140328721" }
check(circulationMatches(copies,"9780140328721",true).map{it.id}==listOf<String>("BK-0002","BK-0003")) { "9780140328721" }
check(circulationMatches(copies,"0-14-032872-6",true).map{it.id}==listOf<String>("BK-0002","BK-0003")) { "0-14-032872-6" }
check(circulationMatches(copies,"BK-0002",false).map{it.id}==listOf<String>()) { "BK-0002" }
check(circulationMatches(copies,"bk2",true).map{it.id}==listOf<String>("BK-0002")) { "bk2" }
check(circulationMatches(copies,"BK-0001",true).map{it.id}==listOf<String>()) { "BK-0001" }
check(circulationMatches(copies,"BK-0004",false).map{it.id}==listOf<String>()) { "BK-0004" }
check(circulationMatches(copies,"9780547928227",true).map{it.id}==listOf<String>()) { "9780547928227" }
check(circulationMatches(copies,"9780547928227",false).map{it.id}==listOf<String>("BK-0005")) { "9780547928227" }
check(circulationMatches(copies,"9780140328722",true).map{it.id}==listOf<String>()) { "9780140328722" }
check(circulationMatches(copies,"",false).map{it.id}==listOf<String>()) { "" }
check(circulationMatches(copies,"Matilda",false).map{it.id}==listOf<String>("BK-0001")) { "Matilda" }
check(circulationMatches(copies,"dahl",true).map{it.id}==listOf<String>("BK-0002","BK-0003")) { "dahl" }
check(circulationMatches(copies,"BK-9999",true).map{it.id}==listOf<String>()) { "BK-9999" }
check(circulationMatches(copies,"9780000000002",true).map{it.id}==listOf<String>()) { "9780000000002" }
println("Android circulation fixtures: PASS")
val held=copies.map{if(it.id=="BK-0001")it.copy(onHold=true) else it}
check(circulationMatches(held,"9780140328721",false).isEmpty()) { "Held copy cannot be checked out" }
check(circulationMatches(held,"9780140328721",true).size==2) { "Holds do not affect returns" }
val uid="1234567890abcdef1234567890abcdef"
val label="carribrary:copy:v1:$uid"
val original=CirculationCopy("BK-0001","9780140328721","Title","Author",false,copyUid=uid)
check(circulationMatches(listOf(original),label,false).single().id=="BK-0001")
check(circulationMatches(listOf(original.copy(id="BK-0100")),label.uppercase(),false).single().id=="BK-0100")
check(circulationMatches(listOf(original.copy(copyUid="abcdef1234567890abcdef1234567890")),label,false).isEmpty())
check(circulationMatches(listOf(original.copy(onLoan=true)),label,true).single().id=="BK-0001")
check(circulationMatches(listOf(original.copy(onHold=true)),label,false).isEmpty())
check(circulationMatches(listOf(original),label.replace(":v1:",":v2:"),false).isEmpty())
println("Kotlin permanent QR identity, reuse, return and eligibility: PASS")
}
