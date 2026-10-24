using System.Net;using System.Net.Sockets;using System.Text;using System.Text.Json;
namespace HomeLibrary;
public sealed class Discovery(Identity identity,Settings settings):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stop){if(!settings.EnableDiscovery)return;using var socket=new UdpClient(new IPEndPoint(IPAddress.Any,settings.DiscoveryPort));var rates=new Dictionary<string,DateTimeOffset>();
 while(!stop.IsCancellationRequested){try{var packet=await socket.ReceiveAsync(stop);if(Encoding.UTF8.GetString(packet.Buffer)!="HOME_LIBRARY_DISCOVER_V1")continue;var ip=packet.RemoteEndPoint.Address.ToString();var now=DateTimeOffset.UtcNow;if(rates.TryGetValue(ip,out var last)&&now-last<TimeSpan.FromSeconds(1))continue;rates[ip]=now;if(rates.Count>1000)rates.Clear();var answer=JsonSerializer.SerializeToUtf8Bytes(new{service="home-library",serverId=identity.ServerId,port=settings.PhonePort,name=Environment.MachineName},Library.Json);await socket.SendAsync(answer,packet.RemoteEndPoint,stop);}catch(OperationCanceledException){break;}}
 }
}
