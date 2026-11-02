using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;
namespace GearLab {
 public static class StepTests {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static double Distance(double x,double y,PointF a,PointF b){double dx=b.X-a.X,dy=b.Y-a.Y,t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(y-a.Y)*dy)/(dx*dx+dy*dy)));dx=x-a.X-t*dx;dy=y-a.Y-t*dy;return Math.Sqrt(dx*dx+dy*dy);}
  static void Profile(CadLoop loop){var spans=StepProfile.Build(loop);for(int i=0;i<spans.Count;i++){var s=spans[i];var next=spans[(i+1)%spans.Count];Check(Math.Abs(s.X[s.End]-next.X[0])+Math.Abs(s.Y[s.End]-next.Y[0])<1e-10,"STEP loop not closed");if(s.Line)continue;for(int k=0;k<=32;k++){double t=k/32.0,u=1-t,x=s.X[0]*u*u*u+3*s.X[1]*u*u*t+3*s.X[2]*u*t*t+s.X[3]*t*t*t,y=s.Y[0]*u*u*u+3*s.Y[1]*u*u*t+3*s.Y[2]*u*t*t+s.Y[3]*t*t*t;double d=double.MaxValue;for(int j=0;j<loop.Points.Length;j++)d=Math.Min(d,Distance(x,y,loop.Points[j],loop.Points[(j+1)%loop.Points.Length]));Check(d<=StepProfile.Tolerance+1e-7,"STEP spline outside tolerance: "+d);}}}
  public static int Run(string folder){Directory.CreateDirectory(folder);var log=new StringBuilder();try{
   foreach(int kind in new[]{0,1,2,3}){var p=Engine.Starter(kind);var r=Engine.Calculate(p);var m=CadBuilder.Build(r);Check(m.Valid,"invalid source model");foreach(var loop in m.Parts.SelectMany(a=>a.Sections).SelectMany(s=>new[]{s.Outer}.Concat(s.Holes)).Where(l=>!l.Circle).Distinct())Profile(loop);string file=Path.Combine(folder,"step-"+kind+".step");StepExport.Write(m,file);string text=File.ReadAllText(file);Check(text.Split(new[]{"MANIFOLD_SOLID_BREP("},StringSplitOptions.None).Length-1==m.Parts.Count,"missing STEP solid");log.AppendLine("PASS type "+kind+": closed fitted loops, sampled 0.002 mm tolerance, "+m.Parts.Count+" solids exported.");}
   foreach(int segments in new[]{360,2400,20000}){var p=MicroPreset.Starter();p.Ratio=25;p.Segments=segments;Engine.FitOverall(p);var r=Engine.Calculate(p);Profile(CadLoop.Polygon(r.Profile));log.AppendLine("PASS Micro 25:1 profile, segments="+segments);}
   foreach(bool integrated in new[]{false,true}){var p=MicroPreset.Starter();p.Ratio=25;p.IntegratedRing=integrated;Engine.FitOverall(p);var r=Engine.Calculate(p);var m=CadBuilder.Build(r);Check(m.Valid,"invalid Micro");StepExport.Write(m,Path.Combine(folder,integrated?"micro-25-integrated.step":"micro-25-rollers.step"));}
   var rect=CadLoop.Polygon(new[]{new PointF(0,0),new PointF(10,0),new PointF(10,2),new PointF(0,2)});Check(StepProfile.Build(rect).All(s=>s.Line),"rectangle corners must stay exact");
   File.WriteAllText(Path.Combine(folder,"step-results.txt"),log.ToString(),Encoding.UTF8);return 0;
  }catch(Exception e){File.WriteAllText(Path.Combine(folder,"step-error.txt"),log+"\n"+e,Encoding.UTF8);return 1;}}
 }
}
