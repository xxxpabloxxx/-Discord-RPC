using System;
public sealed class DiscMotion {
 public double Angle {get;private set;}
 public void Advance(double seconds,bool playing){if(playing && seconds>0 && !double.IsNaN(seconds) && !double.IsInfinity(seconds))Angle=(Angle+seconds*18)%360;}
}
