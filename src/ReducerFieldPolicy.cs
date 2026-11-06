using System;using System.Linq;
namespace GearLab {
 public static class ReducerFieldPolicy {
  public static bool Strength(Parameters p){return (p.Kind==1||p.Kind==12)&&p.Teeth>=17&&Math.Abs(p.PressureAngle-20)<1e-8;}
  public static bool Derived(Parameters p,string key){if(!p.AutoFit)return false;if(p.Kind==0)return new[]{"Radius","Eccentric","OutputRadius"}.Contains(key);if(p.Overall<=0)return false;if(p.Kind==4)return key=="Radius";if(p.Kind==2)return key=="Center";if(p.Kind==7)return key=="Module"||key=="Radius";if(p.Kind==11)return key=="Radius";return key=="Module";}
  public static bool ModelField(Parameters p,string key){
   if(p.Kind>=5)return new[]{"ModelInputBearingD","ModelInputBearingB"}.Contains(key)||key=="ModelGap"&&p.UseUniversalBase||key=="ModelBolt"&&(p.UseUniversalBase||p.Kind!=12);
   if(p.Kind==4)return false;
   if(key=="ModelBeltThickness")return p.Kind==2;
   if(key=="ModelBushWall")return p.Kind==3;
   if(key=="ModelRollerAxle")return p.Kind==0&&!p.IntegratedRing;
   if(key=="ModelBoltCount")return p.Kind==0||p.Kind==3;
   return key.StartsWith("Model");
  }
 }
}
