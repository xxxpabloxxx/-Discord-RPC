using System;
using System.Collections.Generic;
class Tests {
 static void Check(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); }
 static int Main() {
  try {
   Check(Activity.Build("Song","Artist",false,20,180,1000)==null,"pause clears presence");
   Check(Activity.Build("  ","Artist",true,20,180,1000)==null,"empty track clears presence");
   var a=Activity.Build("Song","Artist",true,20,180,1000);
   Check((int)a["type"]==2,"listening activity");
      Check(a.ContainsKey("name") && ((string)a["name"]).StartsWith("𝖆𝖚𝖉𝖎𝖔"),"custom activity name sent");
   Check(a.ContainsKey("buttons"),"listen button included");
   var buttons=(Dictionary<string,object>[])a["buttons"];
   Check((string)buttons[0]["label"]=="Слушать на ЯМ" && (string)buttons[0]["url"]=="https://music.yandex.ru/","listen button opens Yandex Music");
   Check(buttons.Length==2 && (string)buttons[1]["label"]=="developer" && (string)buttons[1]["url"]=="https://hishi.icu","developer link button");
   var ts=(Dictionary<string,object>)a["timestamps"];
   Check((long)ts["start"]==980 && (long)ts["end"]==1160,"track progress timestamps");
   a=Activity.Build("Song","Artist",true,200,180,1000);
   Check(!a.ContainsKey("timestamps"),"invalid timeline omitted");
   a=Activity.Build(new string('a',200),"",true,0,0,1000);
   Check(((string)a["details"]).Length==128,"long titles bounded");
   byte[] frame=Rpc.Frame(1,"Привет");
   Check(BitConverter.ToInt32(frame,4)==12 && frame.Length==20,"IPC UTF8 byte length");
      var presence=new PresenceState();
   var first=Activity.Build("First","Artist",true,10,180,1000);
   Check(presence.Resolve(first,1000)==first,"playing track published immediately");
   Check(presence.Resolve(null,1002)==first,"switch gap retains last track");
   Check(presence.Resolve(null,1008)==first,"short buffering gap retained");
   var second=Activity.Build("Second","Artist",true,0,200,1009);
   Check(presence.Resolve(second,1009)==second,"next track replaces previous immediately");
   Check(presence.Resolve(null,1010)==second,"new pause starts fresh grace period");
   Check(presence.Resolve(null,1018)==null,"sustained pause clears after eight seconds");
   Check(presence.Resolve(null,1020)==null,"missing player stays cleared");
   Check(new PresenceState().Resolve(null,1000)==null,"startup without player stays empty");
   Check(presence.Resolve(first,1021)==first,"playback resumes after clear");
   return 0;
  } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
 }
}
