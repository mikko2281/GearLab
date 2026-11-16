using System;using System.Collections.Generic;using System.Drawing;
namespace GearLab {
 public sealed class OverloadVisualState {
  public bool Enabled,Failed,Smooth;public double OutputPhase,Scale=1;
  public void Advance(double inputDelta){double target=Enabled&&Failed?0:1;Scale=Smooth?Scale+(target-Scale)*.12:target;OutputPhase+=inputDelta*Scale;}
 }
 public sealed partial class WpfScene {
  // 6.10.0: окраска по запасу поверх внешнего вида материала. Детали без окраски возвращают
  // свой материал (MaterialLook). glow — детали за пределом: светятся, чтобы их было видно в сборке.
  public void SetEngineeringColors(Dictionary<string,Color> colors){SetEngineeringColors(colors,null);}
  public void SetEngineeringColors(Dictionary<string,Color> colors,ICollection<string> glow){foreach(var node in Nodes){Color color;node.Material=colors!=null&&colors.TryGetValue(node.Part.Id,out color)?HeatMaterial(color,glow!=null&&glow.Contains(node.Part.Id)):PartMaterial(node.Part);node.Surface.Material=node.Part.Id==SelectedId?GetMaterial(Color.FromArgb(246,189,70)):node.Material;}}
  System.Windows.Media.Media3D.Material PartMaterial(CadPart p){var look=MaterialLook.For(p);return look==null?GetMaterial(p.Color):look.Wpf();}
  static System.Windows.Media.Media3D.Material HeatMaterial(Color c,bool glow){var g=new System.Windows.Media.Media3D.MaterialGroup();var brush=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R,c.G,c.B));brush.Freeze();g.Children.Add(new System.Windows.Media.Media3D.DiffuseMaterial(brush));var spec=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120,120,120));spec.Freeze();g.Children.Add(new System.Windows.Media.Media3D.SpecularMaterial(spec,30));if(glow){var e=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(c.R*.55),(byte)(c.G*.35),(byte)(c.B*.35)));e.Freeze();g.Children.Add(new System.Windows.Media.Media3D.EmissiveMaterial(e));}g.Freeze();return g;}
  // Основной цвет материала (для проверок): первый DiffuseMaterial в группе.
  public static System.Windows.Media.Color DiffuseColor(System.Windows.Media.Media3D.Material m){var d=m as System.Windows.Media.Media3D.DiffuseMaterial;if(d!=null)return ((System.Windows.Media.SolidColorBrush)d.Brush).Color;var g=m as System.Windows.Media.Media3D.MaterialGroup;if(g!=null)foreach(var child in g.Children){var c=child as System.Windows.Media.Media3D.DiffuseMaterial;if(c!=null)return ((System.Windows.Media.SolidColorBrush)c.Brush).Color;}return System.Windows.Media.Colors.Transparent;}
 }
}
