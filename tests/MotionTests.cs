using System;
class MotionTests{
 static int Main(){try{
  var motion=new DiscMotion();motion.Advance(2,false);if(motion.Angle!=0)throw new Exception("Paused disc moved");
  motion.Advance(2,true);if(Math.Abs(motion.Angle-36)>0.001)throw new Exception("Playing disc did not rotate");
  motion.Advance(1,false);if(Math.Abs(motion.Angle-36)>0.001)throw new Exception("Pause reset angle");
  motion.Advance(20,true);if(Math.Abs(motion.Angle-36)>0.001)throw new Exception("Angle did not wrap");
  Console.WriteLine("PASS disc playing, paused, pause angle preserved, full revolution");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
