namespace HomeLibrary;
public sealed class BackupWorker(Store store,SnapshotGate snapshots,ILogger<BackupWorker> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stop){long previous=-1;using var timer=new PeriodicTimer(TimeSpan.FromMinutes(15));do{try{using var lease=await snapshots.Shared(stop);previous=store.ScheduledBackup(previous);}catch(Exception error){logger.LogError(error,"Scheduled library backup failed");}}while(await timer.WaitForNextTickAsync(stop));}
}
