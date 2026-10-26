package com.household.homelibrary

data class SummaryCopy(val id:String,val archived:Boolean=false)
data class SummaryLoan(val copyId:String,val returned:Boolean=false)
data class PhoneSummaryCounts(val physicalCopies:Int,val available:Int,val onLoan:Int,val activeRequests:Int)
fun phoneSummaryCounts(copies:List<SummaryCopy>,loans:List<SummaryLoan>,heldCopies:List<String>,requestStatuses:List<String>):PhoneSummaryCounts {
 val physical=copies.filter{!it.archived};val ids=physical.map{it.id}.toSet()
 val activeLoans=loans.filter{!it.returned&&it.copyId in ids};val borrowed=activeLoans.map{it.copyId}.toSet();val held=heldCopies.toSet()
 return PhoneSummaryCounts(physical.size,physical.count{it.id !in borrowed&&it.id !in held},activeLoans.size,requestStatuses.count{it in listOf("pending","approved")})
}
