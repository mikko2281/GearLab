using System;using System.IO;using System.Linq;using System.Text;
namespace GearLab {
 public static class BlenderReferenceTests {
  public static int Run(string folder){Directory.CreateDirectory(folder);try{
   string file=Path.Combine(folder,"SixAxis_Connected.robot.json");if(!File.Exists(file))RobotDocument.BlenderExample().Save(file);
   var d=RobotDocument.Parse(File.ReadAllText(file));if(d.Validate().Count!=0||d.Joints.Count!=6||d.Joints.Count(j=>j.AxisReversed)!=2)throw new Exception("Author model: six real seams, two reversed Y axes");
   var clone=d.Clone();var parent=clone.Joints[0];string keyBefore=ProceduralBodySpec.Fingerprint(clone,parent,parent.BodyGenerator);clone.Joints[1].AxisReversed=!clone.Joints[1].AxisReversed;if(keyBefore==ProceduralBodySpec.Fingerprint(clone,parent,parent.BodyGenerator))throw new Exception("Child axis reversal must invalidate the real parent mounting geometry");
   if(RobotDocument.Parse(d.Serialize()).Joints[1].Axis.Y!=-1)throw new Exception("Negative shoulder axis must persist");
   if(new RobotJoint().AxisReversed)throw new Exception("Old projects must retain positive axes");
   foreach(var j in d.Joints)File.WriteAllText(Path.Combine(folder,"spec-"+d.Joints.IndexOf(j)+".json"),CADDocument.Serializer().Serialize(ProceduralBodySpec.Build(d,j,j.BodyGenerator)),Encoding.UTF8);
   int result=ConnectedRobotTests.Run(folder);if(result!=0)return result;
   d=RobotDocument.Parse(File.ReadAllText(file));if(d.Joints.Take(5).Any(j=>j.GeneratedBody.Result.Modules["design"]!="blender-elbow"))throw new Exception("Native L-link topology missing");
   File.Copy(file,Path.Combine(folder,"Robot1_800mm.robot.json"),true);File.WriteAllText(Path.Combine(folder,"blender-results.txt"),"PASS author model: 800 mm scale, six actual interfaces, signed axes, native L elbows, mounting/collision/movement/STEP/STL checks in connected-results.txt.",Encoding.UTF8);return 0;
  }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"blender-error.txt"),ex.ToString(),Encoding.UTF8);return 1;}}
 }
}
