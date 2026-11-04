using System;using System.Linq;
namespace GearLab {
 public static class TransmissionSizing {
  public static double MotionRpm(Parameters p){return p.Kind==8&&p.FixedMember==3?Math.Max(Math.Abs(p.InputRpm),Math.Abs(p.SecondInputRpm)):p.InputRpm;}
  public static bool Fit(Parameters p){if(p.Overall<=0)return Engine.Calculate(p).Valid;var copy=Engine.Clone(p);copy.Overall=0;double lo=.15,hi=10,best=0;for(int i=0;i<48;i++){double mid=(lo+hi)/2;copy.Module=mid;var r=Engine.Calculate(copy);if(r.Valid&&Math.Max(r.Width,r.Height)<=p.Overall){best=mid;lo=mid;}else hi=mid;}if(best==0)return false;copy.Module=Math.Floor(best*10000)/10000;copy.Overall=p.Overall;if(!Engine.Calculate(copy).Valid)return false;p.Module=copy.Module;return true;}
  public static double RackPhase(Parameters p,double phase){double rad=p.Module*p.Teeth/2,limit=p.RackTravel/2;if(limit<=0)return 0;double desired=p.RackPosition+phase*rad,span=4*limit;double x=((desired+limit)%span+span)%span;double pos=x<=2*limit?x-limit:3*limit-x;return (pos-p.RackPosition)/rad;}
 }
}
