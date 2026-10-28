using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Collections.Generic;using System.Web.Script.Serialization;
namespace GearLab {
 static class V4Tests {
  static int checks;
  static void Check(bool ok,string what){checks++;if(!ok)throw new Exception("v4: "+what);}
  static double Distance(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}
  static double Mod(double x){return Math.Abs(x/Engine.Tau-Math.Round(x/Engine.Tau));}
  static bool Inside(PointF p,PointF[] poly){bool yes=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[i];var b=poly[j];if((a.Y>p.Y)!=(b.Y>p.Y)&&p.X<(b.X-a.X)*(p.Y-a.Y)/(b.Y-a.Y)+a.X)yes=!yes;}return yes;}
  static double EdgeDistance(PointF p,PointF[] poly){double best=double.MaxValue;for(int j=0;j<poly.Length;j++){var a=poly[j];var b=poly[(j+1)%poly.Length];double x=b.X-a.X,y=b.Y-a.Y,den=x*x+y*y,t=den==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/den));double dx=p.X-a.X-t*x,dy=p.Y-a.Y-t*y;best=Math.Min(best,Math.Sqrt(dx*dx+dy*dy));}return best;}
  internal static void Clearance(PointF[] a,PointF[] b,bool cavity,string what){foreach(var v in a)if(Inside(v,b)!=cavity)Check(EdgeDistance(v,b)<.004,what+" overlap at "+v);}
  public static void Run(string folder,StringBuilder report){
   for(int kind=0;kind<4;kind++)for(int preset=1;preset<=4;preset++){
    var p=Engine.Starter(kind);Engine.SetPreset(p,preset);bool found=Engine.FitOverall(p);var r=Engine.Calculate(p);
    Check(found&&r.Valid,"starter/preset "+kind+"/"+preset+": "+string.Join(";",r.Errors));Check(Math.Max(r.Width,r.Height)<=p.Overall+.025,"size constraint");
    double measure=kind==0?p.Radius:kind==2?p.Center:p.Module;var larger=Engine.Clone(p);larger.Overall+=40;Check(Engine.FitOverall(larger),"larger fit");Check((kind==0?larger.Radius:kind==2?larger.Center:larger.Module)>measure,"size changes physical geometry");
    var small=Engine.Clone(p);small.Overall=10;double oldR=small.Radius,oldM=small.Module,oldC=small.Center;Check(!Engine.FitOverall(small),"impossible size blocked");Check(small.Radius==oldR&&small.Module==oldM&&small.Center==oldC,"failed fit preserves inputs");
    if(preset==2){string name=new[]{"Циклоидальный_два_диска","Зубчатая_пара","Зубчатый_ремень","Планетарный"}[kind];File.WriteAllText(Path.Combine(folder,name+".gear.json"),new JavaScriptSerializer().Serialize(p),Encoding.UTF8);Files.Csv(r,Path.Combine(folder,name+".csv"));Files.Dxf(r,Path.Combine(folder,name+".dxf"));Files.Svg(r,Path.Combine(folder,name+".svg"));Check(File.ReadAllText(Path.Combine(folder,name+".dxf")).Contains("LWPOLYLINE"),"all kinds DXF geometry");
     using(var drawing=new Canvas{Result=r,Size=new Size(1000,760)})using(var b=new Bitmap(1000,760)){drawing.Phase=1.1;drawing.DrawToBitmap(b,new Rectangle(0,0,1000,760));b.Save(Path.Combine(folder,name+".png"));}
    }
   }
   foreach(bool ring in new[]{false,true})foreach(int ratio in new[]{8,9,20,30}){
    var p=Engine.Starter(0);p.Overall=120;p.Ratio=ratio;p.RingOutput=ring;Check(Engine.FitOverall(p),"twin fit");var r=Engine.Calculate(p);Check(r.Valid&&r.P.Discs==2&&r.Actual==ratio,"one-stage twin ratio");
    foreach(double phase in new[]{0.0,.43,1.7,3.2,5.8}){
     double disc=Engine.SecondDiscAngle(p,phase),cx=-p.Eccentric*Math.Cos(phase),cy=-p.Eccentric*Math.Sin(phase),ringAngle=Engine.RingAngle(p,phase);
     var world=Engine.Transform(r.Profile,cx,cy,disc);
     foreach(var v in world)for(int j=0;j<r.Pins;j++){var roller=Engine.Polar(p.Radius,Engine.Tau*j/r.Pins+ringAngle);Check(Distance(v,roller)>=p.Roller/2-.002,"disc B roller collision");}
     for(int h=0;h<p.OutputPins;h++){var fixedPin=Engine.Polar(p.OutputRadius,Engine.Tau*h/p.OutputPins+Engine.CarrierAngle(p,phase));var hole=new PointF((float)(fixedPin.X+cx),(float)(fixedPin.Y+cy));Check(Distance(hole,fixedPin)+p.OutputPin/2<=r.Hole/2+1e-5,"B hole phase");}
    }
    List<SketchLoop> loops;List<SketchCircle> circles;DrawingExport.Geometry(r,out loops,out circles);Check(loops.Count==2&&circles.Count==2*(p.OutputPins+1),"both exported profiles/holes");
   }
   var bearing=Engine.Starter(0);double dm=(bearing.Bore+bearing.BearingInner)/2,ball=bearing.BallDiameter/2;
   foreach(double inner in new[]{-2.0,0,1.0,3.0})foreach(double outer in new[]{-1.0,0,1.0,2.0}){var a=Engine.BearingAngles(bearing,inner,outer);Check(Math.Abs(a[0]*dm/2-a[1]*ball-inner*(dm/2-ball))<1e-10,"inner contact no slip");Check(Math.Abs(a[0]*dm/2+a[1]*ball-outer*(dm/2+ball))<1e-10,"outer contact no slip");}
   var planet=Engine.Calculate(Engine.Starter(3));foreach(double phase in new[]{0.0,.6,2.0,7.1})for(int j=0;j<planet.P.Planets;j++){
    double ca=phase/planet.Actual,lambda=Engine.Tau*j/planet.P.Planets,pa=Engine.PlanetAngle(planet,phase,j);
    Check(Mod(planet.Z1*(phase-ca-lambda)+planet.Zp*(pa-ca-lambda)-(planet.Zp-1)*Math.PI)<1e-10,"sun/planet mesh phase");
    Check(Mod(planet.Z2*(Engine.RingInitial(planet)-ca-lambda)-planet.Zp*(pa-ca-lambda)-Math.PI)<1e-10,"planet/ring mesh phase");
   }
   var external=Engine.Calculate(Engine.Starter(1));for(int n=0;n<12;n++){double phase=Engine.Tau*n/12+.017;var a=Engine.Transform(external.Gear1,0,0,phase);var b=Engine.Transform(external.Gear2,external.Spacing,0,Math.PI-Math.PI/external.Z2-phase/external.Actual);Clearance(a,b,false,"external teeth");Clearance(b,a,false,"external teeth reverse");
    var sun=Engine.Transform(planet.Gear1,0,0,phase);var ring=Engine.Transform(planet.Gear2,0,0,Engine.RingInitial(planet));for(int j=0;j<planet.P.Planets;j++){var pos=Engine.Polar(planet.P.Module*(planet.Z1+planet.Zp)/2,Engine.Tau*j/planet.P.Planets+phase/planet.Actual);var pg=Engine.Transform(planet.PlanetGear,pos.X,pos.Y,Engine.PlanetAngle(planet,phase,j));Clearance(pg,sun,false,"planet/sun");Clearance(sun,pg,false,"sun/planet");Clearance(pg,ring,true,"planet/internal ring");}
   }
   var free=Engine.Starter(0);free.Overall=0;free.Ratio=3;Check(Engine.FitOverall(free)&&Engine.Calculate(free).Valid,"unbounded size respects bearing bore");
   var belt=Engine.Calculate(Engine.Starter(2));double len=0;for(int j=0;j<belt.BeltPath.Length;j++)len+=Distance(belt.BeltPath[j],belt.BeltPath[(j+1)%belt.BeltPath.Length]);double r1=belt.P.Pitch*belt.Z1/(2*Math.PI),r2=belt.P.Pitch*belt.Z2/(2*Math.PI),delta=r2-r1,c=belt.P.Center,exact=2*Math.Sqrt(c*c-delta*delta)+Math.PI*(r1+r2)+2*delta*Math.Asin(delta/c);Check(Math.Abs(len-exact)<.02,"belt tangent length");
   var high=Engine.Starter(0);high.Ratio=60;Check(!Engine.FitOverall(high),"crowded 60/80mm rollers rejected");
   report.AppendLine("PASS v4: "+checks+" assertions, 16 preset/type combinations, overall fit/failure preservation, 8 twin-disc configurations at 5 phases, bearing no-slip contacts, planetary tooth phases, all-kind exports and renderings.");
  }
 }
}
