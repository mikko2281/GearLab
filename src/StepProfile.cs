using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GearLab {
 // STEP-only approximation. The calculation, project and STL geometry stay unchanged.
 public sealed class StepSpan {
  public double[] X,Y;
  public bool Line { get { return X.Length==2; } }
  public int End { get { return X.Length-1; } }
  public string Key() { return string.Join(";",X.Select((x,i)=>CadNumbers.F(x)+","+CadNumbers.F(Y[i]))); }
 }
 public static class StepProfile {
  public const double Tolerance=.002; // mm, conservative bound against the input polyline
  const double CornerCos=.9758974493306055; // 12.6 degrees; preserve actual sharp corners
  struct V {
   public double X,Y;
   public V(double x,double y){X=x;Y=y;}
   public static V operator +(V a,V b){return new V(a.X+b.X,a.Y+b.Y);}
   public static V operator -(V a,V b){return new V(a.X-b.X,a.Y-b.Y);}
   public static V operator *(V a,double k){return new V(a.X*k,a.Y*k);}
   public double Length {get{return Math.Sqrt(X*X+Y*Y);}}
   public V Unit(){double n=Length;return n>1e-14?this*(1/n):new V(1,0);}
   public double Dot(V b){return X*b.X+Y*b.Y;}
  }
  static V Mix(V a,V b,double t){return a*(1-t)+b*t;}
  static V[] Restrict(V[] c,double a,double b){
   // Control polygon of the same cubic restricted to [a,b].
   V p=At(c,a),q=At(c,b);
   return new[]{p,p+Derivative(c,a)*((b-a)/3),q-Derivative(c,b)*((b-a)/3),q};
  }
  static V At(V[] c,double t){double s=1-t;return c[0]*(s*s*s)+c[1]*(3*s*s*t)+c[2]*(3*s*t*t)+c[3]*(t*t*t);}
  static V Derivative(V[] c,double t){double s=1-t;return (c[1]-c[0])*(3*s*s)+(c[2]-c[1])*(6*s*t)+(c[3]-c[2])*(3*t*t);}
  static bool Corner(V[] p,int i){int n=p.Length-1;V a=(p[i]-p[(i+n-1)%n]).Unit(),b=(p[(i+1)%n]-p[i]).Unit();return a.Dot(b)<CornerCos;}
  static V Tangent(V[] p,int i,bool outgoing){int n=p.Length-1,k=i%n;V a=(p[k]-p[(k+n-1)%n]).Unit(),b=(p[(k+1)%n]-p[k]).Unit();return Corner(p,k)?(outgoing?b:a):(a+b).Unit();}
  static StepSpan Span(V[] c){return new StepSpan{X=c.Select(v=>v.X).ToArray(),Y=c.Select(v=>v.Y).ToArray()};}
  static void Fit(V[] p,int first,int last,List<StepSpan> spans){
   if(last-first==1){spans.Add(Span(new[]{p[first],p[last]}));return;}
   int count=last-first+1;var u=new double[count];for(int i=1;i<count;i++)u[i]=u[i-1]+(p[first+i]-p[first+i-1]).Length;
   double length=u[count-1];for(int i=1;i<count;i++)u[i]/=length;
   V start=p[first],end=p[last],left=Tangent(p,first,true),right=Tangent(p,last,false);
   double aa=0,ab=0,bb=0,ra=0,rb=0;
   for(int i=1;i<count-1;i++){double t=u[i],s=1-t,b0=s*s*s,b1=3*s*s*t,b2=3*s*t*t,b3=t*t*t;V a=left*b1,b=right*(-b2),r=p[first+i]-start*(b0+b1)-end*(b2+b3);aa+=a.Dot(a);ab+=a.Dot(b);bb+=b.Dot(b);ra+=a.Dot(r);rb+=b.Dot(r);}
   double determinant=aa*bb-ab*ab,alpha=length/3,beta=alpha;
   if(Math.Abs(determinant)>1e-18){alpha=(ra*bb-rb*ab)/determinant;beta=(rb*aa-ra*ab)/determinant;}
   if(alpha<length*1e-6||beta<length*1e-6||alpha>length*2||beta>length*2)alpha=beta=length/3;
   V[] c={start,start+left*alpha,end-right*beta,end};
   double error=0;int split=(first+last)/2;
   for(int i=0;i<count-1;i++){
    var restricted=Restrict(c,u[i],u[i+1]);
    for(int j=0;j<4;j++){double distance=(restricted[j]-Mix(p[first+i],p[first+i+1],j/3.0)).Length;if(distance>error){error=distance;split=Math.Max(first+1,Math.Min(last-1,first+i+(j>=2?1:0)));}}
   }
   // The convex-hull property bounds the entire cubic, not just sample points.
   if(error<=Tolerance){spans.Add(Span(c));return;}
   int margin=Math.Max(1,(last-first)/4);split=Math.Max(first+margin,Math.Min(last-margin,split));
   Fit(p,first,split,spans);Fit(p,split,last,spans);
  }
  public static List<StepSpan> Build(CadLoop loop){
   if(loop.Circle)throw new ArgumentException("Circles use analytic STEP geometry.");
   var p=loop.Points.Select(v=>new V(v.X,v.Y)).ToList();p.Add(p[0]);var points=p.ToArray();var spans=new List<StepSpan>();
   if(loop.Points.Length<16){for(int i=0;i<points.Length-1;i++)spans.Add(Span(new[]{points[i],points[i+1]}));return spans;}
   var breaks=new SortedSet<int>{0,points.Length-1};for(int i=1;i<points.Length-1;i++)if(Corner(points,i))breaks.Add(i);
   // Keep fitting spans short enough for stable closed profiles and bounded recursion.
   for(int i=1;i<4;i++)breaks.Add((points.Length-1)*i/4);
   var stops=breaks.ToArray();for(int i=1;i<stops.Length;i++)Fit(points,stops[i-1],stops[i],spans);
   return spans;
  }
 }
}
