using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Web.Script.Serialization;
public static class TrackLink {
 static string Normalize(string s){return Regex.Replace((s??"").Trim(),@"\s+"," ");}
 public static string Validate(string json,string title,string artist){
  try{
   var d=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
   if(d==null || !d.ContainsKey("trackId") || !d.ContainsKey("albumId") || !d.ContainsKey("title") || !d.ContainsKey("artists"))return null;
   string track=Convert.ToString(d["trackId"]),album=Convert.ToString(d["albumId"]);
   if(!Regex.IsMatch(track,@"^\d+$") || !Regex.IsMatch(album,@"^\d+$"))return null;
   if(!string.Equals(Normalize(Convert.ToString(d["title"])),Normalize(title),StringComparison.OrdinalIgnoreCase))return null;
   var artists=d["artists"] as IEnumerable;
   if(artists==null || string.IsNullOrWhiteSpace(artist))return null;
   var names=artists.Cast<object>().Select(a=>Normalize(Convert.ToString(a))).Where(a=>a.Length>0).ToArray();
   if(names.Length==0 || names.Any(a=>Normalize(artist).IndexOf(a,StringComparison.OrdinalIgnoreCase)<0))return null;
   return "https://music.yandex.ru/album/"+album+"/track/"+track;
  }catch{return null;}
 }
 const string Expression=@"(() => {
 const read = s => {
  const m=s?.queueState?.currentEntity?.value?.entity?.data?.meta;
  if(!m)return null;
  return {trackId:String(m.id??'').split(':')[0],albumId:String(m.albums?.[0]?.id??m.albumId??''),title:String(m.title??''),artists:(m.artists??[]).map(a=>String(a.name??''))};
 };
 let direct=read(window.sonataState);if(direct)return direct;
 const pending=[];
 for(const el of [document.getElementById('__next'),document.getElementById('root'),document.body,...Array.from(document.body?.children??[])]){
  if(!el)continue;
  for(const k of Object.keys(el)){
   if(k.startsWith('__reactFiber$'))pending.push(el[k]);
   if(k.startsWith('__reactContainer$'))pending.push(el[k]?.stateNode?.current??el[k]);
  }
 }
 const seen=new Set();let count=0;
 while(pending.length && count++<25000){
  const n=pending.pop();if(!n||seen.has(n))continue;seen.add(n);
  const found=read(n.memoizedProps?.value?.state);if(found)return found;
  pending.push(n.child,n.sibling,n.return);
 }
 return null;
})()";
 public static async Task<string> Read(string title,string artist){
  try{
   var serializer=new JavaScriptSerializer();
   using(var http=new HttpClient(new HttpClientHandler{UseProxy=false}){Timeout=TimeSpan.FromSeconds(2)}){
    var text=await http.GetStringAsync("http://127.0.0.1:19229/json/list");
    var pages=serializer.Deserialize<Dictionary<string,object>[]>(text);
    var page=pages.FirstOrDefault(p=>p.ContainsKey("type") && Convert.ToString(p["type"])=="page" && p.ContainsKey("url") && Regex.IsMatch(Convert.ToString(p["url"]),@"^music-application://desktop(?:/|$)"));
    if(page==null || !page.ContainsKey("webSocketDebuggerUrl"))return null;
    Uri uri;
    if(!Uri.TryCreate(Convert.ToString(page["webSocketDebuggerUrl"]),UriKind.Absolute,out uri) || uri.Scheme!="ws" || uri.Host!="127.0.0.1" || uri.Port!=19229)return null;
    using(var ws=new ClientWebSocket())using(var timeout=new CancellationTokenSource(2500)){
     await ws.ConnectAsync(uri,timeout.Token);
     var bytes=Encoding.UTF8.GetBytes(serializer.Serialize(new {id=1,method="Runtime.evaluate",@params=new {expression=Expression,returnByValue=true}}));
     await ws.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,timeout.Token);
     byte[] buffer=new byte[8192];
     using(var message=new MemoryStream()){
      WebSocketReceiveResult part;
      do{
       part=await ws.ReceiveAsync(new ArraySegment<byte>(buffer),timeout.Token);
       if(part.MessageType!=WebSocketMessageType.Text)return null;
       message.Write(buffer,0,part.Count);if(message.Length>65536)return null;
      }while(!part.EndOfMessage);
      var response=serializer.Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(message.ToArray()));
      var result=response["result"] as Dictionary<string,object>;
      if(result==null || result.ContainsKey("exceptionDetails"))return null;
      var remote=result["result"] as Dictionary<string,object>;
      if(remote==null || !remote.ContainsKey("value") || remote["value"]==null)return null;
      return Validate(serializer.Serialize(remote["value"]),title,artist);
     }
    }
   }
  }catch{return null;}
 }
}
