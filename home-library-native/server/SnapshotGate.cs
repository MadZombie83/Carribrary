namespace HomeLibrary;
// Async leases can cross awaits/threads. Ordinary operations remain concurrent;
// a complete snapshot waits for them and temporarily prevents new operations.
public sealed class SnapshotGate {
 readonly SemaphoreSlim entrance=new(1,1),exclusive=new(1,1);readonly object sync=new();int readers;
 public async ValueTask<IDisposable> Shared(CancellationToken stop=default){
  await entrance.WaitAsync(stop);
  try{lock(sync){if(readers==0&&!exclusive.Wait(0))throw new InvalidOperationException("Snapshot gate invariant failed.");readers++;}return new Lease(()=>{lock(sync){if(--readers==0)exclusive.Release();}});}finally{entrance.Release();}
 }
 public async ValueTask<IDisposable> Exclusive(CancellationToken stop=default){
  await entrance.WaitAsync(stop);try{await exclusive.WaitAsync(stop);}catch{entrance.Release();throw;}
  return new Lease(()=>{exclusive.Release();entrance.Release();});
 }
 sealed class Lease(Action release):IDisposable {Action? action=release;public void Dispose()=>Interlocked.Exchange(ref action,null)?.Invoke();}
}
