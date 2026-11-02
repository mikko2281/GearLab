using System;using System.Linq;using System.Collections.Generic;using System.Windows.Media.Media3D;
namespace GearLab {
 public sealed class BodyLoadBounds {public double MomentNmm,ForceN,MassKg;public string Method="path-offset-centres-1";}
 public static class BodyLoadEnvelope {
  static double Length(double x,double y,double z){return Math.Sqrt(x*x+y*y+z*z);}
  static double Travel(RobotJoint j){return j.Linear?Math.Max(Math.Abs(j.Minimum),Math.Abs(j.Maximum)):0;}
  internal static double ComponentRadius(RobotCadEntry e){var p=RobotBuild.Centre(e.Part);if(e.NativeMotion==null){p=e.Local.Transform(p);return Length(p.X,p.Y,p.Z);}p=e.NativeBasePlacement.Transform(p);var motion=e.NativeMotion;double radius;
   if(new[]{"fixed","input","ring","carrier","sun","stageCarrier","waveFlex"}.Contains(motion.Kind))radius=Length(p.X,p.Y,p.Z);
   else if(new[]{"mechanism","pairOutput","beltOutput","discA","discB","planet","planetAxle"}.Contains(motion.Kind)){var delta=p-motion.Centre;double orbit=motion.Kind=="discA"||motion.Kind=="discB"?motion.Result.P.Eccentric:motion.Kind=="planet"||motion.Kind=="planetAxle"?motion.Radius:Length(motion.Centre.X,motion.Centre.Y,0);radius=delta.Length+orbit;}
   else throw new Exception("Для вложенного движения компонентов нет ограничивающего объёма. Проверьте его отдельно перед автоматическим подбором корпуса.");
   return radius+Length(e.Local.OffsetX,e.Local.OffsetY,e.Local.OffsetZ);
  }
  public static BodyLoadBounds Calculate(RobotDocument doc,RobotJoint root,RobotBuild model=null){
   // Own root offset moves the joint and its load together, so it is not a lever.
   // Each child offset is a lever and must be counted along its ancestor path.
   if(root.Linear)throw new Exception("Для линейного привода нет процедурного монтажного шаблона: усилие в Н нельзя использовать как момент.");
   var errors=doc.Validate();if(errors.Count>0)throw new Exception(errors[0]);
   var b=model??RobotCad.Build(doc);if(!b.Model.Valid)throw new Exception("Нельзя оценить нагрузку повреждённой CAD-сборки: "+string.Join("; ",b.Model.Errors.Take(3)));
   var path=new Dictionary<string,double>{{root.Id,0}};
   foreach(var j in doc.Ordered().Where(j=>j.Id!=root.Id&&doc.Descendant(j.Id,root.Id))){var p=doc.Find(j.ParentId);path[j.Id]=path[p.Id]+p.Length+Travel(p)+Length(j.OffsetX,j.OffsetY,j.OffsetZ);}
   var result=new BodyLoadBounds();
   Action<double,double> add=(force,lever)=>{result.ForceN+=force;result.MomentNmm+=force*lever;};
   foreach(var e in b.Entries.Values.Where(e=>!e.Guide&&e.OwnerId!=null&&doc.Descendant(e.OwnerId,root.Id))){
    var j=doc.Find(e.JointId);double mass=b.Mass(e);if(mass<=0)continue;
    double radius=path[j.Id]+ComponentRadius(e)+(e.Category=="Инструмент"?20:0)+(e.InputSide?0:Travel(j));
    result.MassKg+=mass;add(mass*doc.Gravity,radius);
   }
   foreach(var j in doc.Ordered().Where(j=>doc.Descendant(j.Id,root.Id))){
    double tip=path[j.Id]+Travel(j)+j.Length+RobotToolGeometry.LoadExtension(doc,j);
    result.MassKg+=j.Payload;add(j.Payload*doc.Gravity+Length(j.ForceX,j.ForceY,j.ForceZ),tip);
    if(j.BodyGenerator!=null&&j.BodyGenerator.Enabled&&j.GeneratedBody==null){
     double estimate=(Math.PI*(Math.Pow(j.GearboxDiameter/2+6,2)-Math.Pow(j.GearboxDiameter/2,2))*(j.MotorLength+j.GearboxLength)+Math.PI*(400-256)*j.Length)*j.Density*1e-9+.18;
     double reserve=Math.Max(.25,estimate),extent=Math.Max(j.Length,j.MotorLength+j.GearboxLength+j.GearboxDiameter+Length(j.MotorOffsetX,j.MotorOffsetY,0));
     result.MassKg+=reserve;add(reserve*doc.Gravity,path[j.Id]+Travel(j)+extent);
    }
   }
   result.ForceN*=doc.SafetyFactor;result.MomentNmm*=doc.SafetyFactor;
   // This bounds modelled gravity/point forces over joint positions by triangle
   // inequality. Placeholder masses are estimates until geometry is regenerated.
   return result;
  }
 }
}
