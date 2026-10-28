using System;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace GearLab {
 public class CadLoop {
  public bool Circle;public double X,Y,R;public PointF[] Points;
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
 public class CadPart {public string Id,Label,Role,Note;public Color Color;public bool Reference;public List<CadSection> Sections=new List<CadSection>();public CadPart(string id,string label,string role,Color color){Id=id;Label=label;Role=role;Color=color;}
  public double Volume(){return Sections.Sum(s=>(s.Outer.SurfaceArea()-s.Holes.Sum(h=>h.SurfaceArea()))*(s.Top-s.Bottom));}
 }
 public class CadAssembly {public List<CadPart> Parts=new List<CadPart>();public List<string> Errors=new List<string>(),Notes=new List<string>();public bool Valid{get{return Errors.Count==0&&Parts.Count>0;}}public double[] Bounds(){var b=Parts.SelectMany(p=>p.Sections).ToList();return new[]{b.Min(s=>s.Outer.Bounds()[0]),b.Max(s=>s.Outer.Bounds()[1]),b.Min(s=>s.Outer.Bounds()[2]),b.Max(s=>s.Outer.Bounds()[3]),b.Min(s=>s.Bottom),b.Max(s=>s.Top)};}}
}
