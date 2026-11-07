using System.ComponentModel;
namespace GearLab {
// Only controls implemented by the reference-layout CAD generator are exposed.
public sealed class Reference67Settings {
 readonly ProceduralBodySettings settings;
 public Reference67Settings(ProceduralBodySettings value){settings=value;}
 [DisplayName("Стенка каркаса, мм")]public double FrameWall{get{return settings.FrameWall;}set{settings.FrameWall=value;}}
 [DisplayName("Стенка крышки плеча, мм")]public double SkinWall{get{return settings.SkinWall;}set{settings.SkinWall=value;}}
 [DisplayName("Монтажный зазор, мм")]public double Clearance{get{return settings.Clearance;}set{settings.Clearance=value;}}
 [DisplayName("Плотность PETG, кг/м³")]public double Density{get{return settings.Density;}set{settings.Density=value;}}
}}
