using System;using System.Linq;
namespace GearLab {
 public static class TransmissionSizing {
  public static double MotionRpm(Parameters p){return p.Kind==8&&p.FixedMember==3?Math.Max(Math.Abs(p.InputRpm),Math.Abs(p.SecondInputRpm)):p.InputRpm;}
  public static bool Fit(Parameters p){
   if(p==null)return false;if(p.Overall<=0)return Engine.Calculate(p).Valid;
   if(!MechanismCad.Finite(p.Overall))return false;
   var copy=Engine.Clone(p);
   // These layouts are sized by the outer radius, rather than by tooth module alone.
   if(p.Kind==7||p.Kind==11){copy.Radius=Math.Floor((p.Overall/2-12-(p.Kind==7?p.Roller/2:0))*100)/100;if(copy.Radius<5||copy.Radius>1000)return false;}
   Func<double,bool> fits=m=>{copy.Module=m;var r=Engine.Calculate(copy);return r.Valid&&Math.Max(r.Width,r.Height)<=p.Overall+.000001;};
   if(p.Kind==11){if(!fits(p.Module))return false;p.Radius=copy.Radius;return true;}
   double best=fits(p.Module)?p.Module:0,step=(10-.15)/192;
   // Feasibility has both lower bounds (bores/face width) and upper bounds.
   // Bracket valid intervals first; never discard a known feasible input.
   for(int i=192;i>=0;i--){double m=.15+i*step;if(m<=best)break;if(fits(m)){best=m;break;}}
   if(best==0)return false;
   double hi=Math.Min(10,.15+(Math.Floor((best-.15)/step)+1)*step),lo=best;
   if(fits(hi))best=hi;else for(int i=0;i<32;i++){double mid=(lo+hi)/2;if(fits(mid)){best=mid;lo=mid;}else hi=mid;}
   double rounded=Math.Floor(best*10000)/10000;
   if(!fits(rounded)){rounded=Math.Round(best,4);if(!fits(rounded)){rounded=p.Module;if(!fits(rounded))return false;}}
   p.Module=rounded;p.Radius=copy.Radius;return true;
  }
  public static double RackPhase(Parameters p,double phase){double rad=p.Module*p.Teeth/2,limit=p.RackTravel/2;if(limit<=0)return 0;double desired=p.RackPosition+phase*rad,span=4*limit;double x=((desired+limit)%span+span)%span;double pos=x<=2*limit?x-limit:3*limit-x;return (pos-p.RackPosition)/rad;}
 }
}

