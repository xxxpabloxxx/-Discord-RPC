using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
public enum StartupAction { Launch, Keep, Restart }
public static class LaunchSettings {
 public static ProcessStartInfo Player(string path){
  if(string.IsNullOrWhiteSpace(path))throw new ArgumentException("Player path is required");
  return new ProcessStartInfo {FileName=path,Arguments="--remote-debugging-address=127.0.0.1 --remote-debugging-port=19229",UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Normal,WorkingDirectory=Path.GetDirectoryName(path)};
 }
}
public static class PlayerStartup {
 public static StartupAction Decide(bool running,bool debug){return debug?StartupAction.Keep:running?StartupAction.Restart:StartupAction.Launch;}
 static bool DebugReady(){try{using(var socket=new TcpClient()){return socket.ConnectAsync("127.0.0.1",19229).Wait(500)&&socket.Connected;}}catch{return false;}}
 static string RunningPath(){
  string found=null;
  foreach(var p in Process.GetProcesses()){try{if(p.ProcessName=="Яндекс Музыка"||p.ProcessName=="YandexMusic"){var path=p.MainModule.FileName;if(File.Exists(path))found=path;}}catch{}finally{p.Dispose();}}
  return found;
 }
 static void Stop(string executable){
  // Only touch processes whose full executable path matches the detected player.
  var processes=new System.Collections.Generic.List<Process>();
  try{
   foreach(var p in Process.GetProcesses()){
    bool match=false;try{match=string.Equals(p.MainModule.FileName,executable,StringComparison.OrdinalIgnoreCase);}catch{}
    if(match)processes.Add(p);else p.Dispose();
   }
   foreach(var p in processes){try{if(!p.HasExited)p.CloseMainWindow();}catch(InvalidOperationException){}}
   var deadline=DateTime.UtcNow.AddSeconds(4);
   while(DateTime.UtcNow<deadline && processes.Exists(p=>!p.HasExited))System.Threading.Thread.Sleep(100);
   foreach(var p in processes){if(!p.HasExited)p.Kill();}
   foreach(var p in processes){if(!p.WaitForExit(3000))throw new Exception("Не удалось завершить процесс плеера. Перезапуск отменён.");}
  }finally{foreach(var p in processes)p.Dispose();}
 }
 public static void Ensure(){
  try{
   string running=RunningPath();var action=Decide(running!=null,DebugReady());
   if(action==StartupAction.Keep)return;
   string player=running??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),@"Programs\YandexMusic\Яндекс Музыка.exe");
   if(!File.Exists(player)){
    using(var dialog=new OpenFileDialog {Title="Выберите установленную Яндекс Музыку",Filter="Executable (*.exe)|*.exe",FileName="Яндекс Музыка.exe"}){
     if(dialog.ShowDialog()!=DialogResult.OK)return;player=dialog.FileName;
    }
   }
   if(action==StartupAction.Restart)Stop(player);
   using(var process=Process.Start(LaunchSettings.Player(player))){}
  }catch(Exception e){MessageBox.Show("Не удалось перезапустить Яндекс Музыку: "+e.GetBaseException().Message+"\nHishiAudio продолжит работать.","hishi audio");}
 }
}
