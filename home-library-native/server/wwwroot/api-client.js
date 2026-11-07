// Shared JSON transport for the browser apps.
// Error copy deliberately stays at each call site: the borrower, kiosk and reading
// surfaces phrase failures differently, so only the request/response handling lives here.
//
// Options:
//   body            object to send; its presence selects POST and JSON headers
//   credentials     fetch credentials mode (defaults to fetch's same-origin)
//   token           bearer token for paired/reading clients
//   timeoutMs       abort the request after this long
//   errorText       (status) => message when the server returns no explicit error
//   parseErrorText  message used when a successful-looking reply cannot be read
//   withStatus      attach the HTTP status to the thrown error
//   onUnauthorized  (response) => void, called before the error is thrown
export async function requestJson(url,{body,credentials,token,timeoutMs,errorText,parseErrorText,withStatus=false,onUnauthorized}={}){
 const headers={};
 if(token)headers.Authorization='Bearer '+token;
 if(body)headers['Content-Type']='application/json';
 const response=await fetch(url,{
  method:body?'POST':'GET',
  ...(credentials?{credentials}:{}),
  headers,
  body:body?JSON.stringify(body):undefined,
  cache:'no-store',
  ...(timeoutMs?{signal:AbortSignal.timeout(timeoutMs)}:{})
 });
 let data={};
 if(parseErrorText)data=await response.json().catch(()=>{throw Error(parseErrorText);});
 else try{data=await response.json();}catch{}
 if(!response.ok){
  onUnauthorized?.(response);
  const error=Error(data.error||errorText(response.status));
  if(withStatus)error.status=response.status;
  throw error;
 }
 return data;
}
