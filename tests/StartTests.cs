using System;
class StartTests {
 static int Main(){try{
  var start=LaunchSettings.Player(@"C:\Music Folder\Яндекс Музыка.exe");
  if(start.UseShellExecute || !start.CreateNoWindow)throw new Exception("Console-free direct launch required");
  if(start.FileName!=@"C:\Music Folder\Яндекс Музыка.exe")throw new Exception("Path with spaces must remain executable path");
  if(start.Arguments!="--remote-debugging-address=127.0.0.1 --remote-debugging-port=19229")throw new Exception("Debug port must match HishiAudio loopback endpoint");
  bool rejected=false;try{LaunchSettings.Player("");}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Missing executable accepted");
    if(PlayerStartup.Decide(false,false)!=StartupAction.Launch)throw new Exception("Closed player must launch");
  if(PlayerStartup.Decide(true,true)!=StartupAction.Keep)throw new Exception("Debug player must not restart");
  if(PlayerStartup.Decide(true,false)!=StartupAction.Restart)throw new Exception("Ordinary player must restart");
  Console.WriteLine("PASS direct launch, no console, spaced path, loopback arguments, invalid path");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
