using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
public static class Activity {
 static string Limit(string s) { s=(s??"").Trim(); if(s.Length>128) { s=s.Substring(0,128); if(char.IsHighSurrogate(s[s.Length-1])) s=s.Substring(0,s.Length-1); } return s.Length==1?s+" ":s; }
 public static Dictionary<string,object> Build(string title,string artist,bool playing,double position,double duration,long now) {
  if(!playing || string.IsNullOrWhiteSpace(title)) return null;
  var a=new Dictionary<string,object>{{"type",2},{"name","𝖆𝖚𝖉𝖎𝖔 𝖗𝖊𝖕𝖊𝖆𝖙𝖊𝖗 𝖋𝖗𝖔𝖒 𝖍𝖎𝖘𝖍𝖎"},{"details",Limit(title)}};
  a["buttons"]=new Dictionary<string,object>[] {new Dictionary<string,object>{{"label","Слушать на ЯМ"},{"url","https://music.yandex.ru/"}},new Dictionary<string,object>{{"label","developer"},{"url","https://hishi.icu"}}};
  if(!string.IsNullOrWhiteSpace(artist)) a["state"]=Limit(artist);
  if(duration>0 && position>=0 && position<duration) a["timestamps"]=new Dictionary<string,object>{{"start",now-(long)position},{"end",now+(long)(duration-position)}};
  return a;
 }
}
public sealed class Rpc : IDisposable {
 readonly object gate=new object(); NamedPipeClientStream pipe; volatile bool ready; DateTime connectingSince;
 public string Status="Discord: ожидание";
 readonly JavaScriptSerializer json=new JavaScriptSerializer();
 public static byte[] Frame(int opcode,string value) {
  byte[] b=Encoding.UTF8.GetBytes(value), result=new byte[b.Length+8];
  Buffer.BlockCopy(BitConverter.GetBytes(opcode),0,result,0,4); Buffer.BlockCopy(BitConverter.GetBytes(b.Length),0,result,4,4); Buffer.BlockCopy(b,0,result,8,b.Length); return result;
 }
 void Send(int op,string value) { lock(gate) { if(pipe==null) throw new IOException("Discord disconnected"); byte[] b=Frame(op,value); if(!pipe.WriteAsync(b,0,b.Length).Wait(1500)) throw new IOException("Discord write timeout"); } }
 static byte[] Read(Stream s,int count) { byte[] b=new byte[count]; int n=0; while(n<count) { int r=s.Read(b,n,count-n); if(r==0) throw new EndOfStreamException(); n+=r; } return b; }
 public bool Connect() {
  if(ready) return true;
  if(pipe!=null) { if((DateTime.UtcNow-connectingSince).TotalSeconds<10) return false; Dispose(); }
  for(int i=0;i<10;i++) {
   var candidate=new NamedPipeClientStream(".","discord-ipc-"+i,PipeDirection.InOut,PipeOptions.Asynchronous);
   try { candidate.Connect(70); pipe=candidate; connectingSince=DateTime.UtcNow; Send(0,json.Serialize(new {v=1,client_id="1563817011404996628"})); var active=pipe;
    new Thread(()=>Receive(active)){IsBackground=true}.Start(); return false;
   } catch { candidate.Dispose(); pipe=null; }
  }
  Status="Discord не найден. Откройте настольный Discord."; return false;
 }
 void Receive(NamedPipeClientStream active) {
  try { while(true) {
   byte[] h=Read(active,8); int op=BitConverter.ToInt32(h,0),len=BitConverter.ToInt32(h,4);
   if(len<0||len>1048576) throw new IOException("Invalid IPC frame");
   string data=Encoding.UTF8.GetString(Read(active,len));
   if(op==3) { Send(4,data); continue; } if(op==2) throw new IOException("Discord closed connection");
   if(op!=1) continue;
   var msg=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(data);
   object evt; if(msg.TryGetValue("evt",out evt) && (string)evt=="READY") { ready=true; Status="Discord подключён"; }
   else if(evt!=null && (string)evt=="ERROR") { Status="Discord RPC: "+data; }
  }} catch { lock(gate) { if(pipe==active) { ready=false; pipe=null; Status="Discord отключён; переподключение…"; } } active.Dispose(); }
 }
 public bool Update(Dictionary<string,object> activity,int pid) {
  if(!Connect()) return false;
  try { Send(1,json.Serialize(new {cmd="SET_ACTIVITY",args=new {pid=pid,activity=activity},nonce=Guid.NewGuid().ToString()})); return true; }
  catch { Dispose(); return false; }
 }
 public void Dispose() { lock(gate) { ready=false; var old=pipe; pipe=null; if(old!=null) old.Dispose(); } }
}

public sealed class PresenceState {
 Dictionary<string,object> previous;
 long? missingSince;
 public Dictionary<string,object> Resolve(Dictionary<string,object> candidate,long now) {
  if(candidate!=null) { previous=candidate; missingSince=null; return candidate; }
  if(previous==null) return null;
  if(!missingSince.HasValue) missingSince=now;
  if(now-missingSince.Value>=8) previous=null;
  return previous;
 }
}