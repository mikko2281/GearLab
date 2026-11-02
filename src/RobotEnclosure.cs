using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
namespace GearLab {
 public static partial class RobotCad {
  // Annular sectors form two removable, constant-thickness skins. They remain
  // editable extruded CAD profiles, rather than display-only meshes.
  static MechanismPart SkinSector(string name,double radius,double wall,double start,double sweep,double length){
   var outline=new List<MechanismPoint>();int count=Math.Max(12,(int)Math.Ceiling(sweep*32/Math.PI));
   for(int i=0;i<=count;i++){double a=start+sweep*i/count;outline.Add(new MechanismPoint(radius*Math.Cos(a),radius*Math.Sin(a)));}
   for(int i=count;i>=0;i--){double a=start+sweep*i/count;outline.Add(new MechanismPoint((radius-wall)*Math.Cos(a),(radius-wall)*Math.Sin(a)));}
   return new MechanismPart{Kind="sketch",Name=name,Length=length,Outline=outline};
  }
  static void Enclosure(RobotBuild b,RobotJoint j,Matrix3D axis,bool moving){
   double w=j.Wall,fit=j.Fit,r=j.SkinDiameter/2;
   // Include the off-centre motor and its diagonal, including the supplied connector envelope.
   double motorR=Math.Sqrt(Math.Pow(Math.Abs(j.MotorOffsetX)+j.MotorFrame/2,2)+Math.Pow(Math.Abs(j.MotorOffsetY)+j.MotorFrame/2,2))+3;
   double inner=Math.Max(j.GearboxDiameter/2+fit,motorR+fit),outer=inner+w;
   double back=-j.GearboxLength-w-j.MotorLength-w-fit;
   if(moving){
    var jacket=Round("Закрытый цилиндрический кожух мотора и передачи",outer*2,-back,inner*2,back);
    Add(b,j,"skin_actuator",jacket,axis,true,"Корпус",true,j.Density);
    var rear=Round("Задняя крышка кожуха · кабельный ввод",outer*2,w,0,back-w-fit);
    if(j.Cables)rear.Holes.Add(new MechanismHole{Name="Ввод кабеля",Diameter=j.CableDiameter+2*j.CableClearance});
    double boltR=inner+w/2,boltD=Math.Min(j.BoltDiameter,Math.Max(2,w-2*fit-1));
    rear.Holes.AddRange(Pattern(boltR,boltD+2*fit,4,Math.PI/4));
    Add(b,j,"skin_rear",rear,axis,true,"Корпус",true,j.Density);
    var badge=Round("Оранжевая накладка торца шарнира",Math.Min(j.MotorFrame,outer*1.2),1.2,Math.Min(j.MotorFrame,outer*1.2)*.65,back-w-2*fit-1.2);
    Add(b,j,"skin_badge",badge,axis,true,"Корпус",true,j.Density);
    var front=Round("Передняя крышка кожуха · проход выходного вала",outer*2,w,j.BearingInner+2*fit,fit);
    front.Holes.AddRange(Pattern(boltR,boltD+2*fit,4,Math.PI/4));
    Add(b,j,"skin_front",front,axis,true,"Корпус",true,j.Density);
    for(int i=0;i<4;i++){double a=Math.PI/4+Engine.Tau*i/4;Bolt(b,j,"skin_front_bolt_"+i,boltR*Math.Cos(a),boltR*Math.Sin(a),fit-8,w+8,boltD,axis,true);}
    var trim=Round("Оранжевое кольцо шарнира",outer*2+1,w*.65,outer*2-1,-w*.65-fit);
    Add(b,j,"skin_trim",trim,axis,true,"Корпус",true,j.Density);
    double hubZ=w+2*fit,hubH=Math.Max(8,w*2);
    var hub=Round("Опорный фланец трубчатого звена",Math.Min(j.SkinDiameter+4,j.GearboxDiameter),hubH,j.BearingInner+2*fit,hubZ);
    hub.Holes.AddRange(Pattern(Math.Min(j.SkinDiameter,j.GearboxDiameter)/2-7,j.BoltDiameter+2*fit,4,0));
    Add(b,j,"hub",hub,axis,false,"Корпус",true,j.Density);
   }
   var direction=new Vector3D(j.EndX,j.EndY,j.EndZ);direction.Normalize();
   var normal=j.Axis-direction*Vector3D.DotProduct(j.Axis,direction);
   if(normal.LengthSquared<1e-8)normal=Vector3D.CrossProduct(direction,Math.Abs(direction.Z)<.8?new Vector3D(0,0,1):new Vector3D(0,1,0));
   normal.Normalize();double offset=w+2*fit+Math.Max(8,w*2)+r-w;
   var tube=RobotMath.Frame(direction)*RobotMath.Translation(normal.X*offset,normal.Y*offset,normal.Z*offset);
   double opening=j.SkinPanelAngle*Math.PI/180,gap=fit/r,centre=j.SkinPanelRotation*Math.PI/180;
   var main=SkinSector("Трубчатое звено · основная оболочка",r,w,centre+opening/2+gap,Engine.Tau-opening-2*gap,j.Length);
   var cover=SkinSector("Съёмная оранжевая сервисная крышка",r,w,centre-opening/2+gap,opening-2*gap,j.Length);
   double boltCircle=r-w/2,diameter=Math.Min(j.BoltDiameter,Math.Max(2,w-2*fit-1)),hole=diameter+2*fit;
   // Axial blind sockets in shell ends align with the removable end cap.
   foreach(double a in Enumerable.Range(0,4).Select(i=>centre+Engine.Tau*i/4)){
    var socket=new MechanismHole{Name="Глухое гнездо крепления торцевой крышки",X=boltCircle*Math.Cos(a),Y=boltCircle*Math.Sin(a),Diameter=hole,Depth=Math.Min(8,j.Length/3)};
    (Math.Acos(Math.Cos(a-centre))<opening/2?cover:main).Holes.Add(socket);
   }
   Add(b,j,"skin_tube",main,tube,false,"Корпус",true,j.Density);
   Add(b,j,"skin_panel",cover,tube,false,"Корпус",true,j.Density);
   var end=Round("Торцевая крышка звена · кабельный канал",r*2,w,j.Cables?j.CableDiameter+2*j.CableClearance:0,j.Length+fit);
   end.Holes.AddRange(Pattern(boltCircle,hole,4,centre));
   Add(b,j,"skin_end",end,tube,false,"Корпус",true,j.Density);
   var rearEnd=Round("Входной торец звена · проход проводки",r*2,w,2*(r-w),-w-fit);
   Add(b,j,"skin_begin",rearEnd,tube,false,"Корпус",true,j.Density);
   for(int i=0;i<4;i++){double a=centre+Engine.Tau*i/4;Bolt(b,j,"skin_end_bolt_"+i,boltCircle*Math.Cos(a),boltCircle*Math.Sin(a),j.Length-Math.Min(8,j.Length/3),w+Math.Min(8,j.Length/3)+fit,diameter,tube,false);}
   if(j.Cables){var start=tube.Transform(new Point3D(0,0,0));var finish=tube.Transform(new Point3D(0,0,j.Length+w));Cable(b,j,"cable_internal",start,finish,false);}
   if(string.IsNullOrEmpty(j.ParentId)&&j.OffsetZ>30){
    double diameterBase=Math.Max(outer*2,j.SkinDiameter+20);
    var foot=Round("Полое цилиндрическое основание",diameterBase,j.OffsetZ-w,diameterBase-2*w,-j.OffsetZ+w);
    foot.X=j.Axis.X*(back/2);foot.Y=j.Axis.Y*(back/2);
    Add(b,j,"skin_base",foot,Matrix3D.Identity,true,"Корпус",true,j.Density);
    var floor=Round("Монтажная подошва основания",diameterBase,w,j.Cables?j.CableDiameter+2*j.CableClearance:0,-j.OffsetZ);
    floor.X=foot.X;floor.Y=foot.Y;
    floor.Holes.AddRange(Pattern(diameterBase/2-w-5,j.BoltDiameter+2*fit,4,Math.PI/4));
    Add(b,j,"skin_base_floor",floor,Matrix3D.Identity,true,"Корпус",true,j.Density);
   }
   b.Notes.Add(j.Name+": закрытый трубчатый корпус по визуальному референсу. Оранжевая сервисная крышка и торцы — отдельные CAD-детали. Полость кожуха включает диагональ и смещение мотора. Стыковочные кронштейны, фиксацию крышек, вентиляцию и зазоры полного движения проверьте перед изготовлением.");
  }
 }
}
