using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
class LinkTests {
 static void Check(bool result,string name){if(!result)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static int Main(){try{
  Check(TrackLink.Validate("{\"trackId\":\"123\",\"albumId\":\"456\",\"title\":\"Song\",\"artists\":[\"Artist\"]}","Song","Artist")=="https://music.yandex.ru/album/456/track/123","exact track link");
  Check(TrackLink.Validate("{}","Song","Artist")==null,"missing IDs rejected");
  Check(TrackLink.Validate("{\"trackId\":\"123\",\"albumId\":\"456\",\"title\":\"Old song\",\"artists\":[\"Artist\"]}","Song","Artist")==null,"stale track rejected");
  Check(TrackLink.Validate("{\"trackId\":\"../evil\",\"albumId\":\"456\",\"title\":\"Song\",\"artists\":[\"Artist\"]}","Song","Artist")==null,"invalid ID rejected");
  Check(TrackLink.Validate("{\"trackId\":\"123\",\"albumId\":\"456\",\"title\":\"Song\",\"artists\":[\"Different Artist\"]}","Song","Artist")==null,"wrong artist rejected");
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
