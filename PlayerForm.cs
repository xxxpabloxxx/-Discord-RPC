using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
class GlyphButton:Control {
 public string Glyph; bool hover;
 public GlyphButton(string glyph){Glyph=glyph;Size=new Size(38,38);Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.White;TabStop=true;AccessibleName=glyph;}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
  if(hover)using(var b=new SolidBrush(Color.FromArgb(238,240,246)))g.FillEllipse(b,1,1,Width-2,Height-2);
  using(var brush=new SolidBrush(Glyph=="close"?Color.FromArgb(135,139,148):Color.FromArgb(25,26,33)))using(var pen=new Pen(Color.FromArgb(135,139,148),1.6f)){
   if(Glyph=="close"){float cx=Width/2f,cy=Height/2f;g.DrawLine(pen,cx-4,cy-4,cx+4,cy+4);g.DrawLine(pen,cx+4,cy-4,cx-4,cy+4);}
   else if(Glyph=="pause"){g.FillRectangle(brush,13,11,4,17);g.FillRectangle(brush,22,11,4,17);}
   else if(Glyph=="play")g.FillPolygon(brush,new[]{new Point(14,10),new Point(28,19),new Point(14,28)});
   else {
    int direction=Glyph=="next"?1:-1;
    for(int i=0;i<2;i++){int x=direction==1?10+i*9:28-i*9;g.FillPolygon(brush,new[]{new Point(x,11),new Point(x+direction*11,19),new Point(x,27)});}
   }
  }
 }
}
class SeekBar:Control {
 public double Fraction=0;public event Action<double> Seek;
 public SeekBar(){Height=18;Cursor=Cursors.Hand;BackColor=Color.White;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);AccessibleName="Позиция воспроизведения";}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=new Pen(Color.FromArgb(221,222,227),4)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,3,9,Width-3,9);p.Color=Color.FromArgb(23,74,195);g.DrawLine(p,3,9,3+(float)(Math.Max(0,Math.Min(1,Fraction))*(Width-6)),9);}}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left&&Seek!=null)Seek(Math.Max(0,Math.Min(1,(double)e.X/Width)));}
}
class PlayerForm:Form {
 public readonly Label Track=new Label(),Artist=new Label(),Time=new Label(),Status=new Label();
 public readonly GlyphButton Previous=new GlyphButton("previous"),Toggle=new GlyphButton("play"),Next=new GlyphButton("next"),CloseButton=new GlyphButton("close");
 public readonly SeekBar Timeline=new SeekBar();
 public event Action HideRequested;
 Image artwork; Bitmap backdrop; bool backdropDirty=true;
 readonly Timer animation=new Timer {Interval=15};
 readonly System.Diagnostics.Stopwatch clock=new System.Diagnostics.Stopwatch();
 readonly DiscMotion motion=new DiscMotion();
 bool playing;
 public void SetPlaying(bool value){playing=value;UpdateAnimation();}
 void UpdateAnimation(){bool enabled=playing&&Visible;if(enabled&&!animation.Enabled){clock.Restart();animation.Start();}else if(!enabled){animation.Stop();clock.Reset();}}
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
 void Drag(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}}
 public PlayerForm(){
  FormBorderStyle=FormBorderStyle.None;ClientSize=new Size(590,244);BackColor=Color.White;StartPosition=FormStartPosition.CenterScreen;
  animation.Tick+=(s,e)=>{double elapsed=clock.Elapsed.TotalSeconds;clock.Restart();motion.Advance(elapsed,playing);QueueFrame();};
  VisibleChanged+=(s,e)=>{backdropDirty=true;UpdateAnimation();QueueFrame();};
  Font=new Font("Segoe UI",10);Text="hishi audio";DoubleBuffered=true;
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("hishi.cover")){
   if(stream!=null)using(var image=Image.FromStream(stream))artwork=new Bitmap(image);
  }
  Place(Track,236,48,260,26);Track.Font=new Font("Segoe UI Semibold",12);Track.AutoEllipsis=true;Track.Text="hishi audio";
  Place(Artist,237,78,260,22);Artist.ForeColor=Color.FromArgb(126,127,137);Artist.Font=new Font("Segoe UI",9);Artist.AutoEllipsis=true;
  Place(Timeline,237,109,280,18);
  Place(Time,237,131,170,18);Time.Font=new Font("Segoe UI",8);Time.ForeColor=Color.FromArgb(150,151,159);
  Place(Previous,228,151,38,38);Place(Toggle,326,151,38,38);Place(Next,424,151,38,38);
  Place(CloseButton,524,37,32,32);CloseButton.AccessibleName="Свернуть в трей";
  Place(Status,237,195,285,18);Status.Font=new Font("Segoe UI",8);Status.ForeColor=Color.FromArgb(150,151,159);Status.AutoEllipsis=true;
  CloseButton.Click+=(s,e)=>{if(HideRequested!=null)HideRequested();};
  MouseDown+=Drag;Track.MouseDown+=Drag;Artist.MouseDown+=Drag;Status.MouseDown+=Drag;
 }
 void Place(Control c,int x,int y,int w,int h){c.Bounds=new Rectangle(x,y,w,h);c.BackColor=Color.White;Controls.Add(c);c.Invalidated+=(s,e)=>DirtyBackdrop();c.TextChanged+=(s,e)=>DirtyBackdrop();}
 static GraphicsPath Round(RectangleF r,float radius){var path=new GraphicsPath();float d=radius*2;path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;}
 public void ResetArtwork(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("hishi.cover")){if(stream!=null)using(var image=Image.FromStream(stream))SetArtwork(new Bitmap(image));}}
 public void SetArtwork(Image image){var old=artwork;artwork=image;if(old!=null)old.Dispose();Invalidate();}
 bool queued,rendering;
 [StructLayout(LayoutKind.Sequential)]struct NativePoint {public int X,Y;public NativePoint(int x,int y){X=x;Y=y;}}
 [StructLayout(LayoutKind.Sequential)]struct NativeSize {public int Width,Height;public NativeSize(int w,int h){Width=w;Height=h;}}
 [StructLayout(LayoutKind.Sequential,Pack=1)]struct Blend {public byte Operation,Flags,Alpha,Format;}
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr window,IntPtr dest,ref NativePoint location,ref NativeSize size,IntPtr source,ref NativePoint origin,int color,ref Blend blend,int flags);
 [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr window);
 [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr window,IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr bitmap);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr bitmap);
 protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000;return p;}}
 void DirtyBackdrop(){if(rendering)return;backdropDirty=true;QueueFrame();}
 void QueueFrame(){if(queued||rendering||!Visible||!IsHandleCreated||IsDisposed)return;queued=true;BeginInvoke((Action)(()=>{queued=false;if(!IsDisposed&&Visible)Present();}));}
 protected override void OnInvalidated(InvalidateEventArgs e){base.OnInvalidated(e);DirtyBackdrop();}
 protected override void OnShown(EventArgs e){base.OnShown(e);Present();}
 protected override void OnPaint(PaintEventArgs e){QueueFrame();}
 void Present(){
  if(rendering)return;rendering=true;
  try{
   using(var bitmap=RenderFrame()){
    IntPtr screen=GetDC(IntPtr.Zero),dc=CreateCompatibleDC(screen),handle=IntPtr.Zero,old=IntPtr.Zero;
    try{
     handle=bitmap.GetHbitmap(Color.FromArgb(0));old=SelectObject(dc,handle);
     var location=new NativePoint(Left,Top);var size=new NativeSize(Width,Height);var origin=new NativePoint(0,0);var blend=new Blend {Alpha=255,Format=1};
     if(!UpdateLayeredWindow(Handle,screen,ref location,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }finally{if(old!=IntPtr.Zero)SelectObject(dc,old);if(handle!=IntPtr.Zero)DeleteObject(handle);if(dc!=IntPtr.Zero)DeleteDC(dc);if(screen!=IntPtr.Zero)ReleaseDC(IntPtr.Zero,screen);}
   }
  }finally{rendering=false;}
 }
 Bitmap BuildBackdrop(){
  var bitmap=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
  using(var g=Graphics.FromImage(bitmap)){
   g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   for(int i=10;i>=1;i--)using(var shadow=new SolidBrush(Color.FromArgb(3,0,0,0)))using(var path=Round(new RectangleF(95-i,29-i/2f,465+i*2,188+i*1.5f),23+i/2f))g.FillPath(shadow,path);
   using(var path=Round(new RectangleF(95,28,465,188),23))g.FillPath(Brushes.White,path);
   var circle=new Rectangle(14,16,206,206);
   for(int i=9;i>=1;i--)using(var shadow=new SolidBrush(Color.FromArgb(3,0,0,0)))g.FillEllipse(shadow,14-i/2f,17-i/2f,206+i,206+i);
   foreach(Control control in Controls){if(!control.Visible)continue;using(var child=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(child,new Rectangle(0,0,child.Width,child.Height));g.DrawImageUnscaled(child,control.Left,control.Top);}}
  }
  return bitmap;
 }
 public Bitmap RenderFrame(){
  if(backdrop==null || backdropDirty){var old=backdrop;backdrop=BuildBackdrop();backdropDirty=false;if(old!=null)old.Dispose();}
  var bitmap=new Bitmap(Width,Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
  using(var g=Graphics.FromImage(bitmap)){
   g.Clear(Color.Transparent);g.DrawImageUnscaled(backdrop,0,0);
   g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   var circle=new Rectangle(14,16,206,206);
   if(artwork!=null){
    using(var disc=new Bitmap(210,210,System.Drawing.Imaging.PixelFormat.Format32bppPArgb)){
     using(var dg=Graphics.FromImage(disc)){dg.Clear(Color.Transparent);dg.TranslateTransform(105,105);dg.RotateTransform((float)motion.Angle);dg.InterpolationMode=InterpolationMode.HighQualityBicubic;dg.DrawImage(artwork,new Rectangle(-105,-105,210,210));}
     using(var brush=new TextureBrush(disc)){brush.TranslateTransform(12,14);g.FillEllipse(brush,circle);}
    }
   }else g.FillEllipse(Brushes.LightGray,circle);
   using(var ring=new Pen(Color.FromArgb(70,255,255,255),1))g.DrawEllipse(ring,circle);
   g.FillEllipse(Brushes.White,111,113,12,12);

  }
  return bitmap;
 }
 protected override void Dispose(bool disposing){if(disposing){animation.Stop();animation.Dispose();if(backdrop!=null){backdrop.Dispose();backdrop=null;}}if(disposing&&artwork!=null){artwork.Dispose();artwork=null;}base.Dispose(disposing);}
}
