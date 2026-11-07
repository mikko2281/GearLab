using System;using System.Linq;using System.Drawing;using System.Collections.Generic;

namespace GearLab {

 using System.Windows.Media.Media3D;

 public class CadLoop {

  public bool Circle,SmoothSides;public double X,Y,R;public PointF[] Points;

  public static CadLoop Round(double x,double y,double r){return new CadLoop{Circle=true,X=x,Y=y,R=r};}

  public static CadLoop Polygon(PointF[] points){var clean=new List<PointF>();foreach(var v in points)if(clean.Count==0||Length(v,clean.Last())>1e-6)clean.Add(v);if(clean.Count>2&&Length(clean[0],clean.Last())<1e-6)clean.RemoveAt(clean.Count-1);if(clean.Count<3)throw new Exception("Вырожденный CAD-контур.");if(Area(clean.ToArray())<0)clean.Reverse();return new CadLoop{Points=clean.ToArray()};}

  public PointF[] Sample(int count){if(!Circle)return Points;return Enumerable.Range(0,count).Select(i=>new PointF((float)(X+R*Math.Cos(Engine.Tau*i/count)),(float)(Y+R*Math.Sin(Engine.Tau*i/count)))).ToArray();}

  public CadLoop Move(double x,double y){return Circle?Round(X+x,Y+y,R):Polygon(Points.Select(v=>new PointF((float)(v.X+x),(float)(v.Y+y))).ToArray());}

  public double[] Bounds(){if(Circle)return new[]{X-R,X+R,Y-R,Y+R};return new[]{(double)Points.Min(v=>v.X),Points.Max(v=>v.X),Points.Min(v=>v.Y),Points.Max(v=>v.Y)};}

  public double SurfaceArea(){return Circle?Math.PI*R*R:Math.Abs(Area(Points));}

  public static double Area(PointF[] p){double a=0;for(int i=0;i<p.Length;i++){var v=p[i];var w=p[(i+1)%p.Length];a+=(double)v.X*w.Y-(double)v.Y*w.X;}return a/2;}

  public static double Length(PointF a,PointF b){return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));}

  public bool Contains(double x,double y){if(Circle)return (x-X)*(x-X)+(y-Y)*(y-Y)<R*R+1e-7;bool inside=false;for(int i=0,j=Points.Length-1;i<Points.Length;j=i++){var a=Points[i];var b=Points[j];if((a.Y>y)!=(b.Y>y)&&x<(b.X-a.X)*(y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;}return inside;}

  public PointF Center(){if(Circle)return new PointF((float)X,(float)Y);return new PointF(Points.Average(v=>v.X),Points.Average(v=>v.Y));}

  public string Key(){return Circle?"C"+CadNumbers.F(X)+","+CadNumbers.F(Y)+","+CadNumbers.F(R):"P"+string.Join(";",Points.Select(v=>CadNumbers.F(v.X)+","+CadNumbers.F(v.Y)));}

 }

 public static class CadNumbers {public static string F(double x){if(Math.Abs(x)<1e-10)x=0;string s=x.ToString("0.#########",System.Globalization.CultureInfo.InvariantCulture);return s.Contains(".")?s:s+".";}}

 public class CadSection {public double Bottom,Top;public CadLoop Outer;public List<CadLoop> Holes=new List<CadLoop>();public CadSection(double bottom,double top,CadLoop outer,IEnumerable<CadLoop> holes){Bottom=bottom;Top=top;Outer=outer;if(holes!=null)Holes.AddRange(holes);}}

 public class CadPart {public bool SectionUnion;public double BearingRequestedBall;public int BearingRequestedCount;public AnalyticSolid Analytic;public CadPart BearingSource;public int BearingElement;public double BearingPitch,BearingBall;public double[] PrintAngles;public string Id,Label,Role,Note;public Color Color;public bool Reference;public Matrix3D Placement=Matrix3D.Identity;public ParametricLoft Loft;public SceneMesh MeshOverride;public string NativeStep;public double NativeVolume=-1;public Point3D NativeCentre;public List<CadSection> Sections=new List<CadSection>();public CadPart(string id,string label,string role,Color color){Id=id;Label=label;Role=role;Color=color;}

  public CadPart WithPlacement(Matrix3D placement){var copy=(CadPart)MemberwiseClone();copy.Placement=placement;return copy;}
  public double Volume(){if(NativeStep!=null&&NativeVolume>=0)return NativeVolume;return MeshOverride==null?Sections.Sum(s=>(s.Outer.SurfaceArea()-s.Holes.Sum(h=>h.SurfaceArea()))*(s.Top-s.Bottom)):Math.Abs(MeshOverride.SignedVolume());}

 }

 public class CadAssembly {public Dictionary<string,TransmissionMotion> TransmissionMotions=new Dictionary<string,TransmissionMotion>();public Matrix3D InputFrame=Matrix3D.Identity,OutputFrame=Matrix3D.Identity;public List<CadPart> Parts=new List<CadPart>();public List<string> Errors=new List<string>(),Notes=new List<string>();public bool Valid{get{return Errors.Count==0&&Parts.Count>0;}}public double[] Bounds(){var all=new List<Point3D>();foreach(var p in Parts){if(p.MeshOverride!=null){all.AddRange(p.MeshOverride.Points.Select(v=>p.Placement.Transform(v)));continue;}foreach(var s in p.Sections){var b=s.Outer.Bounds();for(int i=0;i<8;i++)all.Add(p.Placement.Transform(new Point3D((i&1)==0?b[0]:b[1],(i&2)==0?b[2]:b[3],(i&4)==0?s.Bottom:s.Top)));}}if(all.Count==0)return new double[6];return new[]{all.Min(v=>v.X),all.Max(v=>v.X),all.Min(v=>v.Y),all.Max(v=>v.Y),all.Min(v=>v.Z),all.Max(v=>v.Z)};}}

}



