using System;using System.IO;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;using System.Windows.Forms.Integration;using System.Windows.Media.Media3D;

namespace GearLab {

 public sealed class WaveParameters {

  public bool Rolling;public double BallDiameter=6,ProfileRadius=38,Eccentric=1.2;public Parameters RollingParameters(){var p=new Parameters{WaveScheme=1,Kind=4,Ratio=Ratio,BallDiameter=BallDiameter,Radius=ProfileRadius,Eccentric=Eccentric,Thickness=Thickness,Wall=RingWall,Shaft=Bore,OutputShaft=OutputBore,Clearance=Clearance,InputTorque=InputTorque,InputRpm=InputRpm,Efficiency=Efficiency,LoadTorque=LoadTorque,Overall=0,AutoFit=false};return p;}public bool Flat=true;public int Ratio=50;public double Module=.6,CupDepth=25,Thickness=8,Wall=.8,RingWall=4,Bore=5,OutputBore=8,Clearance=.15,BearingWall=1.5;

  public double InputTorque=.35,InputRpm=60,Efficiency=.7,LoadTorque=5;

  public int FlexTeeth{get{return 2*Ratio;}}public int RingTeeth{get{return FlexTeeth+2;}}

  public double OutputRpm{get{return -InputRpm/Ratio;}}public double OutputTorque{get{return InputTorque*Ratio*Efficiency;}}

  public double InputPower{get{return InputTorque*InputRpm*Math.PI/30;}}public double RequiredTorque{get{return LoadTorque/(Ratio*Efficiency);}}

  public void Validate(){if(Rolling){var rolling=Engine.Calculate(RollingParameters());if(!rolling.Valid)throw new Exception(string.Join(";",rolling.Errors));return;}if(Ratio<20||Ratio>120||!Finite(InputTorque)||InputTorque<0||!Finite(InputRpm)||InputRpm<0||!Finite(Efficiency)||Efficiency<=0||Efficiency>1||!Finite(LoadTorque)||LoadTorque<0)throw new Exception("Передача 20–120; момент, обороты и нагрузка ≥ 0; КПД 0–1, исключая 0.");if(new[]{Module,Thickness,Wall,RingWall,Bore,OutputBore,Clearance,BearingWall,CupDepth}.Any(x=>!Finite(x))||Module<.15||Module>2||Thickness<3||Thickness>40||Wall<.3||Wall>3||RingWall<2||RingWall>15||Bore<=0||OutputBore<=0||Clearance<.05||Clearance>.5||BearingWall<.5||BearingWall>4||!Flat&&(CupDepth<10||CupDepth>100))throw new Exception("Проверьте размеры: модуль 0,15–2; толщина венца 3–40; гибкая стенка 0,3–3; стенка корпуса 2–15; зазор 0,05–0,5; стенка подшипника 0,5–4 мм.");double inner=Module*(Ratio-1.25)-Wall;if(inner*(1-1.0/Ratio)-Clearance-BearingWall<=Math.Max(Bore,OutputBore)/2+2)throw new Exception("Отверстие не помещается внутри генератора волн.");if(Flat&&RingWall-2*Math.Max(1.2,Math.Min(2,RingWall*.30))-1<.25)throw new Exception("Увеличьте стенку венца для болтового фланца.");}

  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}

  public CADFeature Feature(double phase=0){Validate();return new CADFeature{Kind="wave",Name="Волновой редуктор "+Ratio+":1",Values=new Dictionary<string,string>{{"Layout",Flat?"flat":"cup"},{"Ratio",Ratio.ToString()},{"Module",Engine.N(Module)},{"CupDepth",Engine.N(CupDepth)},{"Thickness",Engine.N(Thickness)},{"Wall",Engine.N(Wall)},{"RingWall",Engine.N(RingWall)},{"Bore",Engine.N(Bore)},{"OutputBore",Engine.N(OutputBore)},{"Clearance",Engine.N(Clearance)},{"BearingWall",Engine.N(BearingWall)},{"Phase",Engine.N(phase)}}};}

  public CADDocument Document(double phase=0){var doc=new CADDocument{Name="Волновой редуктор "+Ratio+":1"};var f=Feature(phase);doc.Features.Add(f);ApplyMaterials(doc,f);return doc;}

  public static void ApplyMaterials(CADDocument doc,CADFeature f){doc.Materials[f.Id+"_ring"]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(80,90,103).ToArgb()};doc.Materials[f.Id+"_flex"]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(172,178,186).ToArgb()};doc.Materials[f.Id+"_output"]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(190,194,200).ToArgb()};for(int i=1;i<=6;i++){doc.Materials[f.Id+"_output_bolt"+i]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(120,124,132).ToArgb()};doc.Materials[f.Id+"_bolt"+i]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(115,120,127).ToArgb()};}doc.Materials[f.Id+"_generator"]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.FromArgb(65,135,189).ToArgb()};doc.Materials[f.Id+"_bearing"]=new CADMaterial{Name="Сталь",Density=7850,Color=Color.Silver.ToArgb()};}

 }

 public static class WaveMotion {

  // Prescribed visualization of an elastic cup. No contact or elasticity solver is implied.

  public static SceneMesh Deform(SceneMesh original,WaveParameters p,double phase){if(p.Flat){double sx=1+1.0/p.Ratio,sy=1-1.0/p.Ratio,waveAxis=phase+phase/p.Ratio,cosWave=Math.Cos(waveAxis),sinWave=Math.Sin(waveAxis),a=(sx*cosWave*cosWave+sy*sinWave*sinWave)/sx,b=(sx-sy)*cosWave*sinWave/sy,d=(sx*sinWave*sinWave+sy*cosWave*cosWave)/sy,e=(sx-sy)*cosWave*sinWave/sx;var pts=new Point3D[original.Points.Length];var ns=new Vector3D[pts.Length];for(int j=0;j<pts.Length;j++){var q=original.Points[j];pts[j]=new Point3D(a*q.X+b*q.Y,e*q.X+d*q.Y,q.Z);var n=original.Normals[j];var v=new Vector3D(d*n.X-e*n.Y,-b*n.X+a*n.Y,n.Z);if(v.LengthSquared>1e-20)v.Normalize();ns[j]=v;}return new SceneMesh{Points=pts,Normals=ns,Indices=original.Indices};}var points=new Point3D[original.Points.Length];var normals=new Vector3D[points.Length];double floor=Math.Max(1.5,2*p.Wall),depth=p.CupDepth-floor,axis=phase+phase/p.Ratio,c=Math.Cos(axis),s=Math.Sin(axis);for(int i=0;i<points.Length;i++){var q=original.Points[i];double f=p.Flat?1:Math.Max(0,Math.Min(1,(q.Z+depth)/depth)),k=f/p.Ratio;double a=1+k,b=1-k;double x=q.X/a,y=q.Y/b;double u=c*x+s*y,v=-s*x+c*y;points[i]=new Point3D(c*a*u-s*b*v,s*a*u+c*b*v,q.Z);}for(int j=0;j<original.Indices.Length;j+=3){int a=original.Indices[j],b=original.Indices[j+1],d=original.Indices[j+2];var n=Vector3D.CrossProduct(points[b]-points[a],points[d]-points[a]);if(n.LengthSquared>1e-20)n.Normalize();normals[a]+=n;normals[b]+=n;normals[d]+=n;}for(int i=0;i<normals.Length;i++)if(normals[i].LengthSquared>1e-20)normals[i].Normalize();return new SceneMesh{Points=points,Normals=normals,Indices=original.Indices};}

 }

}

