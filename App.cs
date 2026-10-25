using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.IO;

class AudioContext : ApplicationContext {
 readonly PlayerForm form=new PlayerForm();
 readonly NotifyIcon tray=new NotifyIcon();
 readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
 readonly Rpc rpc=new Rpc();
 readonly PresenceState presence=new PresenceState();
 readonly Icon icon;
 readonly Label title,artist,progress,status;
 GlobalSystemMediaTransportControlsSessionManager manager;
 GlobalSystemMediaTransportControlsSession session;
 bool updating,exiting;
 string lastJson="",lastIdentity="";
 DateTime lastSent=DateTime.MinValue; DateTime lastLinkCheck=DateTime.MinValue; string linkKey="",trackUrl=null;
 public AudioContext() {
  icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  form.Icon=icon;
  title=form.Track;artist=form.Artist;progress=form.Time;status=form.Status;
  title.Text="hishi audio";artist.Text="Включите музыку";
  form.Previous.Click+=async(s,e)=>await Control("previous");
  form.Toggle.Click+=async(s,e)=>await Control("toggle");
  form.Next.Click+=async(s,e)=>await Control("next");
  form.Timeline.Seek+=async fraction=>await Seek(fraction);
  form.HideRequested+=HideToTray;
  form.FormClosing+=(s,e)=>{if(!exiting && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;HideToTray();}};
  form.Resize+=(s,e)=>{if(form.WindowState==FormWindowState.Minimized)HideToTray();};
  var tips=new ToolTip();status.TextChanged+=(s,e)=>{var full=status.Text;tips.SetToolTip(status,full);if(full.Contains("\n"))status.Text=full.Replace("\r","").Replace("\n"," ");};
  form.Disposed+=(s,e)=>tips.Dispose();
  var menu=new ContextMenuStrip();
  menu.Items.Add("Открыть",null,(s,e)=>Open());
  menu.Items.Add("Пауза / Пуск",null,async(s,e)=>await Control("toggle"));
  menu.Items.Add("Следующий трек",null,async(s,e)=>await Control("next"));
  menu.Items.Add("Выход",null,(s,e)=>ExitThread());
  tray.Icon=icon;tray.Text="hishi audio";tray.ContextMenuStrip=menu;tray.Visible=true;
  tray.DoubleClick+=(s,e)=>Open();
  tray.BalloonTipClicked+=(s,e)=>Open();
  NotifyHidden();
  timer.Interval=2000;timer.Tick+=async(s,e)=>await Refresh();timer.Start();
 }
 void NotifyHidden(){tray.ShowBalloonTip(3500,"hishi audio","Приложение свёрнуто в трей. Двойной щелчок по значку откроет плеер.",ToolTipIcon.Info);}
 void HideToTray(){form.Hide();NotifyHidden();}
 void Open(){form.WindowState=FormWindowState.Normal;form.Show();form.Activate();}
 async Task Seek(double fraction){
  try{if(session==null)return;var timeline=session.GetTimelineProperties();var range=timeline.EndTime-timeline.StartTime;
   if(range.Ticks>0)await Wait(session.TryChangePlaybackPositionAsync(timeline.StartTime.Ticks+(long)(range.Ticks*fraction)));
  }catch(Exception e){status.Text=e.GetBaseException().Message;}
 }
 async Task Artwork(GlobalSystemMediaTransportControlsSessionMediaProperties media){
  if(media.Thumbnail==null)return;
  try{
   using(var stream=await Wait(media.Thumbnail.OpenReadAsync())){
    if(stream.Size==0 || stream.Size>4194304)return;
    using(var reader=new DataReader(stream.GetInputStreamAt(0))){
     uint count=(uint)stream.Size;await Wait<uint>(reader.LoadAsync(count));var bytes=new byte[count];reader.ReadBytes(bytes);
     using(var memory=new MemoryStream(bytes))using(var image=Image.FromStream(memory)){if(!exiting)form.SetArtwork(new Bitmap(image));}
    }
   }
  }catch{} // Artwork failure must not interrupt playback or RPC.
 }
 static async Task<T> Wait<T>(Windows.Foundation.IAsyncOperation<T> operation){
  var info=(Windows.Foundation.IAsyncInfo)operation;
  try{
   var deadline=DateTime.UtcNow.AddSeconds(3);
   while(info.Status==Windows.Foundation.AsyncStatus.Started){
    if(DateTime.UtcNow>=deadline){info.Cancel();throw new TimeoutException("Плеер не ответил за 3 секунды");}
    await Task.Delay(25);
   }
   return operation.GetResults();
  }finally{info.Close();}
 }
 async Task Control(string command){
  try{
   var active=session;if(active==null)throw new Exception("Сначала запустите музыку");
   bool success;
   using(var timeout=new CancellationTokenSource(3000)){
    if(command=="next")success=await Wait(active.TrySkipNextAsync());
    else if(command=="previous")success=await Wait(active.TrySkipPreviousAsync());
    else success=await Wait(active.TryTogglePlayPauseAsync());
   }
   if(!success)throw new Exception("Плеер не поддерживает эту команду");
  }catch(Exception e){MessageBox.Show(form,e.GetBaseException().Message,"hishi audio");}
 }
 async Task Publish(System.Collections.Generic.Dictionary<string,object> candidate){
  var activity=presence.Resolve(candidate,DateTimeOffset.UtcNow.ToUnixTimeSeconds());
  var serializer=new JavaScriptSerializer();var signature=serializer.Serialize(activity);
  var identity=activity==null?"null":serializer.Serialize(new object[]{activity["details"],activity.ContainsKey("state")?activity["state"]:null,activity["buttons"]});
  double elapsed=(DateTime.UtcNow-lastSent).TotalSeconds;
  if(identity!=lastIdentity || (signature!=lastJson && elapsed>=5) || elapsed>=15){
   bool sent=await Task.Run(()=>rpc.Update(activity,Process.GetCurrentProcess().Id));
   if(sent){lastIdentity=identity;lastJson=signature;lastSent=DateTime.UtcNow;}
  }
 }
 async Task Refresh(){
  if(updating||exiting)return;updating=true; string failure=null;
  try{
   using(var timeout=new CancellationTokenSource(3000)){
    if(manager==null)manager=await Wait(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
    var sessions=manager.GetSessions();
    session=sessions.FirstOrDefault(s=>Regex.IsMatch(s.SourceAppUserModelId,"yandex.*music|music.*yandex|яндекс.*музык",RegexOptions.IgnoreCase));
    System.Collections.Generic.Dictionary<string,object> activity=null;
    if(session!=null){
     var media=await Wait(session.TryGetMediaPropertiesAsync());
     bool playing=session.GetPlaybackInfo().PlaybackStatus==GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
     var timeline=session.GetTimelineProperties();double duration=(timeline.EndTime-timeline.StartTime).TotalSeconds;
     double position=(timeline.Position-timeline.StartTime).TotalSeconds;
     if(playing)position+=Math.Max(0,(DateTimeOffset.Now-timeline.LastUpdatedTime).TotalSeconds);
     position=Math.Max(0,Math.Min(position,Math.Max(0,duration)));
     title.Text=media.Title;artist.Text=media.Artist;
     form.SetPlaying(playing);form.Toggle.Glyph=playing?"pause":"play";form.Toggle.Invalidate();form.Timeline.Fraction=duration>0?position/duration:0;form.Timeline.Invalidate();
     progress.Text=TimeSpan.FromSeconds(position).ToString(@"mm\:ss")+" / "+TimeSpan.FromSeconds(Math.Max(0,duration)).ToString(@"mm\:ss")+(playing?" ▶":" Пауза");
          activity=Activity.Build(media.Title,media.Artist,playing,position,duration,DateTimeOffset.UtcNow.ToUnixTimeSeconds());
     var key=media.Title+"\n"+media.Artist;
     if(key!=linkKey){linkKey=key;trackUrl=null;lastLinkCheck=DateTime.MinValue;form.ResetArtwork();await Artwork(media);}
     if(playing && activity!=null && (DateTime.UtcNow-lastLinkCheck).TotalSeconds>=5){
      lastLinkCheck=DateTime.UtcNow;trackUrl=await TrackLink.Read(media.Title,media.Artist);
     }
     if(activity!=null && trackUrl!=null){
      ((System.Collections.Generic.Dictionary<string,object>[])activity["buttons"])[0]["url"]=trackUrl;
     }
    }else{form.SetPlaying(false);title.Text="Яндекс Музыка не найдена";artist.Text="Откройте приложение и запустите трек";progress.Text="";}
    if(exiting)return;
    await Publish(activity);if(exiting)return;status.Text=rpc.Status;
    if(session==null && sessions.Count>0)status.Text+="\r\nДоступно: "+string.Join(", ",sessions.Select(s=>s.SourceAppUserModelId));
   }
  }catch(Exception e){
   form.SetPlaying(false);session=null;manager=null;
   failure=e.GetBaseException().Message;
  }finally{updating=false;}
  if(failure!=null && !exiting){await Publish(null);if(!exiting)status.Text="Ошибка: "+failure;}
 }
 protected override void ExitThreadCore(){
  if(exiting)return;exiting=true;timer.Stop();timer.Dispose();
  rpc.Update(null,Process.GetCurrentProcess().Id);rpc.Dispose();tray.Visible=false;tray.Dispose();
  form.Dispose();icon.Dispose();base.ExitThreadCore();
 }
}
static class Program {
 [STAThread]static void Main(){
  using(var mutex=new Mutex(false,@"Local\HishiAudio-1563817011404996628")){
   bool owned;try{owned=mutex.WaitOne(0);}catch(AbandonedMutexException){owned=true;}
   if(!owned){MessageBox.Show("hishi audio уже запущен. Найдите значок в трее.","hishi audio");return;}
   try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);PlayerStartup.Ensure();Application.Run(new AudioContext());}
   catch(Exception e){MessageBox.Show(e.GetBaseException().Message,"hishi audio: ошибка");}
   finally{mutex.ReleaseMutex();}
  }
 }
}
