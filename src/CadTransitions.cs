using System;using System.Linq;using System.Collections.Generic;
namespace GearLab {
 public sealed class CadFacePlan {public CadLoop Outer;public List<CadLoop> Holes;public bool Up;public CadFacePlan(CadLoop outer,IEnumerable<CadLoop> holes,bool up){Outer=outer;Holes=holes.ToList();Up=up;}}
 public static class CadTransitions {
  static bool Inside(CadLoop a,CadLoop b){return a.Sample(64).All(p=>b.Contains(p.X,p.Y));}
  // A large recess can terminate in several smaller holes, e.g. motor adapter to nut traps.
  public static IEnumerable<CadFacePlan> HoleFaces(CadSection a,CadSection b){var matched=new HashSet<int>();var consumed=new HashSet<CadLoop>();foreach(var old in a.Holes.OrderByDescending(h=>h.SurfaceArea())){if(consumed.Contains(old))continue;int index=b.Holes.FindIndex(h=>CadLoop.Length(h.Center(),old.Center())<1e-5);if(index>=0){matched.Add(index);var current=b.Holes[index];if(old.Key()==current.Key())continue;bool shrink=Inside(current,old),grow=Inside(old,current);if(!shrink&&!grow)throw new Exception("Отверстия осевых сечений не вложены; требуется CAD Boolean.");var cutouts=new List<CadLoop>{shrink?current:old};if(shrink){for(int j=0;j<b.Holes.Count;j++)if(j!=index&&Inside(b.Holes[j],old)){cutouts.Add(b.Holes[j]);matched.Add(j);}}else{foreach(var h in a.Holes)if(h!=old&&Inside(h,current)){cutouts.Add(h);consumed.Add(h);}}yield return new CadFacePlan(shrink?old:current,cutouts,!shrink);}else{var c=old.Center();if(b.Outer.Contains(c.X,c.Y)&&!b.Holes.Any(h=>Inside(old,h)))yield return new CadFacePlan(old,new CadLoop[0],false);}}
   for(int j=0;j<b.Holes.Count;j++)if(!matched.Contains(j)){var h=b.Holes[j];var c=h.Center();if(a.Outer.Contains(c.X,c.Y)&&!a.Holes.Any(old=>Inside(h,old)))yield return new CadFacePlan(h,new CadLoop[0],true);}
  }
 }
}
