using System;using System.Linq;using System.Drawing;using System.Windows.Media.Media3D;
namespace GearLab {
 public static class RobotToolGeometry {
  public static double LoadExtension(RobotDocument doc,RobotJoint joint){if(!doc.ReferenceGripper||joint.BodyGenerator==null||!joint.BodyGenerator.Enabled||!joint.BodyGenerator.ReferenceDesign||doc.Ordered().LastOrDefault()!=joint)return 0;return Math.Max(10,2*Math.Max(2.4,joint.BodyGenerator.MinWall)+joint.BodyGenerator.InsertDiameter)+64;}
 }
 public static partial class RobotCad {
  static void AddReferenceTool(RobotBuild b){if(!b.Document.ReferenceGripper||b.Document.Joints.Count==0)return;var j=b.Document.Ordered().Last();if(j.GeneratedBody==null||j.BodyGenerator==null||!j.BodyGenerator.ReferenceDesign||j.GeneratedBody.Fingerprint!=ProceduralBodySpec.Fingerprint(b.Document,j,j.BodyGenerator))return;var port=j.GeneratedBody.Result.Mates.FirstOrDefault(p=>p.Id=="tool_mount");if(port==null)return;
   var x=port.X;var y=port.Y;var n=port.Normal;var o=port.Origin;var local=new Matrix3D(x[0],x[1],x[2],0,y[0],y[1],y[2],0,n[0],n[1],n[2],0,o[0],o[1],o[2],1)*RobotMath.Frame(j.Axis);
   double diameter=port.BoltCircle+10;var adapter=Round("Адаптер захвата · совпадающий крепёж",diameter,4,12,0);adapter.Holes.AddRange(Pattern(port.BoltCircle/2,port.BoltDiameter,port.BoltPoints.Length,Math.PI/4));var e=Add(b,j,"tool_adapter",adapter,local,false,"Инструмент",true,1270);e.Part.Color=Color.FromArgb(37,39,43);
   var palm=Box("Захват · компоновочный корпус",60,28,4,30);e=Add(b,j,"tool_palm",palm,local,false,"Инструмент",true,1270);e.Part.Color=Color.FromArgb(37,39,43);
   double opening=b.Document.GripperOpening;
   for(int side=-1;side<=1;side+=2){var carriage=Box("Захват · ползун",6,22,34,8);carriage.X=side*(opening/2+3);e=Add(b,j,"tool_carriage_"+side,carriage,local,false,"Инструмент",false,7850);e.Part.Color=Color.FromArgb(70,74,79);
    var finger=Box("Захват · палец",4,12,42,14);finger.X=side*(opening/2+2);e=Add(b,j,"tool_jaw_"+side,finger,local,false,"Инструмент",true,1270);e.Part.Color=Color.FromArgb(37,39,43);
    var tip=Box("Захват · наружная часть пальца",3,12,56,8);tip.X=side*(opening/2+2.5);e=Add(b,j,"tool_tip_"+side,tip,local,false,"Инструмент",true,1270);e.Part.Color=Color.FromArgb(37,39,43);
    var pad=Box("Захват · контактная накладка",1,12,56,8);pad.X=side*(opening/2+.5);e=Add(b,j,"tool_pad_"+side,pad,local,false,"Инструмент",true,1100);e.Part.Color=Color.FromArgb(237,118,39);
   }
   b.Notes.Add("Двухпальцевый захват — редактируемая компоновочная модель с совпадающими отверстиями адаптера. Привод пальцев, усилие зажима и его крепёж ещё не рассчитаны.");
  }
 }
 public sealed partial class RobotDesignerForm {
  internal void ApplyReferenceDesign(){Mutate(()=>{UpgradeReferenceSpacing(Document);foreach(var j in Document.Joints){j.BodyStyle=RobotBodyStyle.Закрытый_трубчатый;if(j.BodyGenerator==null)j.BodyGenerator=new ProceduralBodySettings();j.BodyGenerator.Connected=j.BodyGenerator.Enabled=j.BodyGenerator.AutoRegenerate=j.BodyGenerator.ReferenceDesign=true;j.BodyGenerator.BlenderDesign=false;}});}
  internal void ApplyBlenderDesign(){Mutate(()=>{foreach(var j in Document.Joints){j.BodyStyle=RobotBodyStyle.Закрытый_трубчатый;if(j.BodyGenerator==null)j.BodyGenerator=new ProceduralBodySettings();j.BodyGenerator.Connected=j.BodyGenerator.Enabled=j.BodyGenerator.AutoRegenerate=j.BodyGenerator.ReferenceDesign=j.BodyGenerator.BlenderDesign=true;j.BodyGenerator.FrameWall=Math.Max(6.4,j.BodyGenerator.FrameWall);}});}
  void ToggleReferenceGripper(){Mutate(()=>{Document.ReferenceGripper=!Document.ReferenceGripper;if(Document.ReferenceGripper&&Document.Joints.Count>0){var last=Document.Ordered().Last();if(last.BodyGenerator==null)last.BodyGenerator=new ProceduralBodySettings();last.BodyGenerator.Connected=last.BodyGenerator.Enabled=last.BodyGenerator.AutoRegenerate=last.BodyGenerator.ReferenceDesign=true;}});}
  void ToggleGripperOpening(){if(Document.ReferenceGripper)Mutate(()=>Document.GripperOpening=Document.GripperOpening<10?26:2);}
 }
}
