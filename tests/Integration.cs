using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
class Integration {
 static byte[] Read(Stream s,int n) { var b=new byte[n]; int p=0; while(p<n) { var task=s.ReadAsync(b,p,n-p); if(!task.Wait(3000)) throw new Exception("Read timeout at "+p); int r=task.Result; if(r==0) throw new Exception("EOF"); p+=r; } return b; }
 static string Receive(Stream s) { var h=Read(s,8); return Encoding.UTF8.GetString(Read(s,BitConverter.ToInt32(h,4))); }
 static void Send(Stream s,int op,string json) { var b=Rpc.Frame(op,json); s.Write(b,0,b.Length); }
 static int Main() {
  try {
   using(var server=new NamedPipeServerStream("discord-ipc-0",PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous))
   using(var client=new Rpc()) {
    Console.WriteLine("server ready"); var waiting=server.BeginWaitForConnection(null,null); new Thread(()=>client.Connect()){IsBackground=true}.Start(); Console.WriteLine("connect requested");
    if(!waiting.AsyncWaitHandle.WaitOne(3000)) throw new Exception("connect timeout"); server.EndWaitForConnection(waiting);
    Console.WriteLine("connected"); if(!Receive(server).Contains("1563817011404996628")) throw new Exception("handshake ID");
    Console.WriteLine("handshake received"); Send(server,1,"{\"evt\":\"READY\"}");
    var limit=DateTime.UtcNow.AddSeconds(3); while(client.Status!="Discord подключён" && DateTime.UtcNow<limit) Thread.Sleep(10);
    var send=System.Threading.Tasks.Task.Run(()=>client.Update(Activity.Build("Трек","Автор",true,20,180,1000),123));
    Console.WriteLine("activity sent"); var data=Receive(server); if(!send.Result) throw new Exception("update failed"); if(!data.Contains("SET_ACTIVITY") || !data.Contains("Трек") || !data.Contains("123")) throw new Exception("activity data");
    Send(server,3,"{\"ping\":1}"); if(Receive(server)!="{\"ping\":1}") throw new Exception("ping/pong");
    var clear=System.Threading.Tasks.Task.Run(()=>client.Update(null,123)); if(!Receive(server).Contains("\"activity\":null") || !clear.Result) throw new Exception("clear presence");
    Console.WriteLine("PASS IPC handshake, READY, SET_ACTIVITY, ping/pong, clear");
   }
   return 0;
  } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
 }
}




