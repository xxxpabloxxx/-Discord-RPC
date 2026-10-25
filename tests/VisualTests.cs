using System;using System.Drawing;using System.Windows.Forms;
class VisualTests {
 [STAThread]static int Main(){try{
  using(var form=new PlayerForm())using(var bitmap=form.RenderFrame()){
   if(bitmap.GetPixel(0,0).A!=0)throw new Exception("Outer area must be fully transparent");
   int partial=0;for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){int alpha=bitmap.GetPixel(x,y).A;if(alpha>0&&alpha<255)partial++;}
   if(partial<150)throw new Exception("Missing antialiased alpha edges");
  }
  using(var button=new GlyphButton("close")){button.Size=new Size(32,32);using(var bitmap=new Bitmap(32,32)){button.DrawToBitmap(bitmap,new Rectangle(0,0,32,32));double sx=0,sy=0;int count=0;for(int y=0;y<32;y++)for(int x=0;x<32;x++){var p=bitmap.GetPixel(x,y);if(p.R<200&&p.G<200&&p.B<200){sx+=x;sy+=y;count++;}}if(count==0||Math.Abs(sx/count-16)>0.6||Math.Abs(sy/count-16)>0.6)throw new Exception("Close glyph is off center");}}
  Console.WriteLine("PASS transparent exterior, fractional-alpha edges, centered close glyph");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
