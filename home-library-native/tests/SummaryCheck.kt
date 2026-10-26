package com.household.homelibrary
fun main(){
 check(phoneSummaryCounts(emptyList(),emptyList(),emptyList(),emptyList())==PhoneSummaryCounts(0,0,0,0))
 val copies=listOf(SummaryCopy("a"),SummaryCopy("b"),SummaryCopy("c"),SummaryCopy("d"),SummaryCopy("archived",true))
 val loans=listOf(SummaryLoan("a"),SummaryLoan("b",true),SummaryLoan("archived"),SummaryLoan("missing"))
 val holds=listOf("a","c","missing","archived")
 check(phoneSummaryCounts(copies,loans,holds,listOf("pending","approved","fulfilled","cancelled","declined"))==PhoneSummaryCounts(4,2,1,2))
 check(phoneSummaryCounts(copies,loans.map{it.copy(returned=true)},emptyList(),emptyList())==PhoneSummaryCounts(4,4,0,0))
 println("PASS Android summary: archived/orphan/returned exclusion, overlapping holds, active request statuses, return refresh and empty state")
}
