export function googleCoverLink(raw){
 try{const u=new URL(raw);if(!['https:','http:'].includes(u.protocol)||u.hostname!=='books.google.com'||u.username||u.password||u.port)return '';const id=u.searchParams.get('id')||'';return /^[A-Za-z0-9_-]{1,100}$/.test(id)?'https://books.google.com/books?id='+id:'';}catch{return '';}
}
export function googleCredit(raw){const link=googleCoverLink(raw);return link?`<a class="google-credit" href="${link}" target="_blank" rel="noopener noreferrer"><img src="powered-by-google.png" alt="Powered by Google" width="62" height="30"><span>Google Books</span></a>`:'';}
