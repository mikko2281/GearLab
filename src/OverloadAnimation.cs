using System;using System.Collections.Generic;using System.Drawing;
namespace GearLab {
 public sealed class OverloadVisualState {
  public bool Enabled,Failed,Smooth;public double OutputPhase,Scale=1;
  public void Advance(double inputDelta){double target=Enabled&&Failed?0:1;Scale=Smooth?Scale+(target-Scale)*.12:target;OutputPhase+=inputDelta*Scale;}
 }
 public sealed partial class WpfScene {
  public void SetEngineeringColors(Dictionary<string,Color> colors){foreach(var node in Nodes){Color color;node.Material=GetMaterial(colors.TryGetValue(node.Part.Id,out color)?color:node.Part.Color);node.Surface.Material=node.Material;}}
 }
}
