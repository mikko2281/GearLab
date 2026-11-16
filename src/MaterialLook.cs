using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;
using M=System.Windows.Media;using M3=System.Windows.Media.Media3D;
namespace GearLab {
 // 6.10.0. Внешний вид материала в 3D и в образцах интерфейса. Только отображение:
 // на массу, прочность и экспорт геометрии не влияет. Цвет печатного пластика — цвет нити,
 // поэтому у каждого материала есть узнаваемый типичный вид, а пользователь может задать свой.
 public sealed class MaterialLook {
  public Color Color;public string Finish;
  public static readonly string[] Finishes={"Matte","Satin","Gloss","Metal","Clear"};
  public static readonly string[] FinishNames={"Матовая","Полуматовая","Глянцевая","Металл","Прозрачная"};
  static readonly Dictionary<string,string[]> Known=new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase){
   {"PETG",new[]{"#2F7FC1","Gloss"}},{"PLA",new[]{"#EE8636","Matte"}},{"ABS",new[]{"#3E4248","Satin"}},{"ASA",new[]{"#E3DED2","Satin"}},
   {"PA12",new[]{"#D8D1BD","Matte"}},{"PC",new[]{"#BFD9E8","Clear"}},{"POM",new[]{"#F0EDE4","Satin"}},
   {"Сталь 45",new[]{"#737A82","Metal"}},{"Нержавеющая сталь",new[]{"#BAC1C8","Metal"}},{"Алюминий 6061-T6",new[]{"#CDD4DC","Metal"}},{"Бронза",new[]{"#B5864A","Metal"}}};
  public static MaterialLook Typical(string name,string kind){string[] v;if(name!=null&&Known.TryGetValue(name.Trim(),out v))return new MaterialLook{Color=ColorTranslator.FromHtml(v[0]),Finish=v[1]};
   return kind=="Metal"?new MaterialLook{Color=ColorTranslator.FromHtml("#9EA5AD"),Finish="Metal"}:kind=="Polymer"?new MaterialLook{Color=ColorTranslator.FromHtml("#E6E2D8"),Finish="Satin"}:new MaterialLook{Color=ColorTranslator.FromHtml("#8E98A6"),Finish="Satin"};}
  public static MaterialLook For(TrainMaterial m){if(m==null)return null;var look=Typical(m.Name,m.Kind);if(!string.IsNullOrWhiteSpace(m.Color))look.Color=ColorTranslator.FromHtml(m.Color.Trim());if(!string.IsNullOrWhiteSpace(m.Finish)&&Array.IndexOf(Finishes,m.Finish)>=0)look.Finish=m.Finish;return look;}
  public static MaterialLook For(CadPart p){if(p==null||p.MaterialName==null||string.IsNullOrEmpty(p.MaterialColor))return null;return new MaterialLook{Color=ColorTranslator.FromHtml(p.MaterialColor),Finish=Array.IndexOf(Finishes,p.MaterialFinish)<0?"Satin":p.MaterialFinish};}
  public static string Hex(Color c){return "#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");}
  public string FinishName{get{int i=Array.IndexOf(Finishes,Finish);return i<0?Finish:FinishNames[i];}}

  // WPF-материал: диффузный цвет плюс блик, сила и размер которого зависят от вида поверхности.
  static readonly Dictionary<string,M3.Material> cache=new Dictionary<string,M3.Material>();
  public M3.Material Wpf(){string key=Hex(Color)+Finish;M3.Material m;if(cache.TryGetValue(key,out m))return m;var group=new M3.MaterialGroup();
   Func<Color,byte,M.SolidColorBrush> brush=(c,a)=>{var b=new M.SolidColorBrush(M.Color.FromArgb(a,c.R,c.G,c.B));b.Freeze();return b;};
   if(Finish=="Metal"){group.Children.Add(new M3.DiffuseMaterial(brush(UiPalette.Blend(Color,Color.Black,.28),255)));group.Children.Add(new M3.SpecularMaterial(brush(UiPalette.Blend(Color,Color.White,.55),255),34));}
   else if(Finish=="Clear"){group.Children.Add(new M3.DiffuseMaterial(brush(Color,120)));group.Children.Add(new M3.SpecularMaterial(brush(Color.White,255),90));}
   else{group.Children.Add(new M3.DiffuseMaterial(brush(Color,255)));byte s=(byte)(Finish=="Gloss"?215:Finish=="Satin"?95:34);double power=Finish=="Gloss"?70:Finish=="Satin"?22:7;group.Children.Add(new M3.SpecularMaterial(brush(Color.FromArgb(s,s,s),255),power));}
   group.Freeze();if(cache.Count>256)cache.Clear();cache[key]=group;return group;}

  // Образец-шарик для интерфейса: тот же цвет и характер блика, что в 3D.
  public static void PaintSwatch(Graphics g,RectangleF r,MaterialLook look){if(look==null||r.Width<4||r.Height<4)return;var old=g.SmoothingMode;g.SmoothingMode=SmoothingMode.AntiAlias;float d=Math.Min(r.Width,r.Height);var e=new RectangleF(r.X+(r.Width-d)/2,r.Y+(r.Height-d)/2,d,d);
   bool metal=look.Finish=="Metal",clear=look.Finish=="Clear";
   if(clear){using(var b=new HatchBrush(HatchStyle.LargeCheckerBoard,Color.FromArgb(60,128,128,128),Color.FromArgb(25,128,128,128)))g.FillEllipse(b,e);}
   using(var path=new GraphicsPath()){path.AddEllipse(e);using(var b=new PathGradientBrush(path)){b.CenterPoint=new PointF(e.X+e.Width*.36f,e.Y+e.Height*.32f);int a=clear?150:255;
     b.CenterColor=Color.FromArgb(a,UiPalette.Blend(look.Color,Color.White,metal?.35:look.Finish=="Matte"?.12:.22));b.SurroundColors=new[]{Color.FromArgb(a,UiPalette.Blend(look.Color,Color.Black,metal?.62:.45))};g.FillPath(b,path);}
    if(metal){using(var band=new LinearGradientBrush(e,Color.FromArgb(0,255,255,255),Color.FromArgb(90,255,255,255),70f)){band.SetBlendTriangularShape(.55f,1f);g.SetClip(path);g.FillRectangle(band,e);g.ResetClip();}}}
   float spot=look.Finish=="Gloss"||clear?.22f:metal?.3f:look.Finish=="Satin"?.34f:0;
   if(spot>0){var hs=new RectangleF(e.X+e.Width*.22f,e.Y+e.Height*.16f,e.Width*spot,e.Height*spot*.8f);using(var path=new GraphicsPath()){path.AddEllipse(hs);using(var b=new PathGradientBrush(path)){b.CenterColor=Color.FromArgb(look.Finish=="Satin"?90:metal?150:235,255,255,255);b.SurroundColors=new[]{Color.FromArgb(0,255,255,255)};g.FillPath(b,path);}}}
   using(var pen=new Pen(Color.FromArgb(70,0,0,0)))g.DrawEllipse(pen,e);g.SmoothingMode=old;}
 }
}
