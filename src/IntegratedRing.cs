using System;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace GearLab {
 public static class IntegratedRing {
  public static double Bridge(Parameters p){return p.RingBridge>0?p.RingBridge:p.Roller*.45;}
  public static CadLoop Opening(Result r){var p=r.P;double radius=p.Roller/2,half=Bridge(p)/2,wall=p.Radius+radius+p.Clearance,step=Engine.Tau/r.Pins;
   if(half<=0||half>=radius)throw new Exception("Ширина связки зацепа должна быть больше 0 и меньше диаметра зацепа. 0 в поле означает авто (45% диаметра).");
   double angle=Math.Asin(half/radius),rootAngle=Math.Asin(half/wall),rootX=Math.Sqrt(wall*wall-half*half),shoulder=p.Radius+Math.Sqrt(radius*radius-half*half);if(rootAngle>=step/2)throw new Exception("Связки соседних зацепов пересекаются: уменьшите их ширину.");
   var points=new List<PointF>();Action<double,double,double> add=(x,y,a)=>points.Add(new PointF((float)(x*Math.Cos(a)-y*Math.Sin(a)),(float)(x*Math.Sin(a)+y*Math.Cos(a))));int count=Math.Max(32,(int)Math.Ceiling((Engine.Tau-2*angle)*Math.Sqrt(radius/.025)));
   for(int j=0;j<r.Pins;j++){double a=j*step;add(rootX,-half,a);add(shoulder,-half,a);for(int k=1;k<=count;k++){double t=Engine.Tau-angle-(Engine.Tau-2*angle)*k/count;add(p.Radius+radius*Math.Cos(t),radius*Math.Sin(t),a);}add(rootX,half,a);int arcs=Math.Max(2,(int)Math.Ceiling((step-2*rootAngle)*Math.Sqrt(wall/.025)));for(int k=1;k<arcs;k++){double t=a+rootAngle+(step-2*rootAngle)*k/arcs;points.Add(Engine.Polar(wall,t));}}
   return CadLoop.Polygon(points.ToArray());
  }
  public static bool ClearsBridges(Result r,int phases){var p=r.P;double half=Bridge(p)/2,rad=p.Roller/2,shoulder=p.Radius+Math.Sqrt(rad*rad-half*half),step=Engine.Tau/r.Pins;
   for(int j=0;j<phases;j++){double input=Engine.Tau*j/phases,ring=Engine.RingAngle(p,input);for(int disk=0;disk<p.Discs;disk++){double spin=Engine.DiscAngle(p,input)-(disk==0?0:Math.PI/r.Lobes),cx=p.Eccentric*Math.Cos(input+disk*Math.PI),cy=p.Eccentric*Math.Sin(input+disk*Math.PI),c=Math.Cos(spin-ring),s=Math.Sin(spin-ring),ox=cx*Math.Cos(ring)+cy*Math.Sin(ring),oy=-cx*Math.Sin(ring)+cy*Math.Cos(ring);foreach(var v in r.Profile){double x=v.X*c-v.Y*s+ox,y=v.X*s+v.Y*c+oy,a=Math.Round(Math.Atan2(y,x)/step)*step,localX=x*Math.Cos(a)+y*Math.Sin(a),localY=-x*Math.Sin(a)+y*Math.Cos(a);if(localX>shoulder+1e-5&&Math.Abs(localY)<half-1e-5)return false;}}}return true;
  }
  public static void Apply(CadAssembly model,Result r){if(r.P.Kind!=0||r.P.CycloidLayout==1||!r.P.IntegratedRing)return;var p=r.P;var opening=Opening(r);if(!ClearsBridges(r,72))throw new Exception("Диск задевает связки печатных зацепов в проверенных положениях. Уменьшите ширину связки или эксцентриситет либо выберите отдельные ролики.");
   var wall=model.Parts.First(q=>q.Id.EndsWith("_CASE_WALL")||q.Id.EndsWith("_FIXED_CASE_WALL"));wall.Label=p.RingOutput?"Выходной корпус со встроенными зацепами":"Неподвижный корпус со встроенными зацепами";foreach(var section in wall.Sections)section.Holes[0]=opening;foreach(var cover in model.Parts){if(cover.Id.EndsWith("_ROLLER_REAR_COVER"))cover.Label="Задняя крышка печатного корпуса";if(cover.Id.EndsWith("_ROLLER_FRONT_COVER"))cover.Label="Передняя крышка печатного корпуса";}wall.Note="Зацепы и стенка — одно связное тело. Зацепы неподвижны относительно корпуса; контакт со скольжением. Ширина связки "+Engine.F(Bridge(p))+" мм. Материал, износ и прочность не рассчитаны.";
   model.Parts.RemoveAll(q=>q.Id.Contains("RING_ROLLER")||q.Id.Contains("ROLLER_AXLE"));foreach(var part in model.Parts.Where(q=>q.Role=="housing"&&q!=wall))foreach(var section in part.Sections)section.Holes.RemoveAll(h=>h.Circle&&Math.Abs(h.R-p.ModelRollerAxle/2)<1e-6&&Enumerable.Range(0,r.Pins).Any(i=>{var v=Engine.Polar(p.Radius,Engine.Tau*i/r.Pins);return Math.Abs(v.X-h.X)<1e-4&&Math.Abs(v.Y-h.Y)<1e-4;}));
   model.Notes.Add("Печатное кольцо: "+r.Pins+" зацепов объединены со стенкой корпуса. Отдельные ролики, оси и их отверстия удалены. Связки проверены по контуру дисков в 72 фазах; это дискретная проверка, не непрерывный динамический анализ. Скользящий контакт требует проверки материала, зазоров и износа.");
  }
 }
}
