using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace GearLab {
 public class Canvas:Control {
  System.Drawing.PointF[] ringContour;Result result;int view;bool hideHousing;GearViewerControl viewer;
  public double Phase,Zoom=1;public bool ShowMotion;public int StageIndex;public bool ShowMotor=true;public event Action VisibilityChanged;
  public Result Result{get{return result;}set{result=value;ringContour=null;if(value!=null&&value.Valid&&value.P.Kind==0&&value.P.IntegratedRing){var wall=value.Model==null?null:value.Model.Parts.FirstOrDefault(q=>q.Id.EndsWith("_CASE_WALL")||q.Id.EndsWith("_FIXED_CASE_WALL"));try{ringContour=wall!=null?wall.Sections.SelectMany(q=>q.Holes).Where(h=>!h.Circle).Select(h=>h.Points).FirstOrDefault():IntegratedRing.Opening(value).Points;}catch{ringContour=null;}}if(viewer!=null){viewer.SetResult(value,true);viewer.Present(Phase);}}}
  public int View{get{return view;}set{if(view==value)return;view=value;if(view>=5){EnsureViewer();viewer.Visible=true;viewer.SetResult(result,true);viewer.SetExploded(view==6);viewer.Present(Phase);}else if(viewer!=null)viewer.Visible=false;Invalidate();}}
  public bool HideHousing{get{return viewer==null?hideHousing:viewer.HousingHidden;}set{hideHousing=value;if(viewer!=null)viewer.SetHousingHidden(value);}}
  internal GearViewerControl Viewer{get{EnsureViewer();return viewer;}}
  void EnsureViewer(){if(viewer!=null)return;viewer=new GearViewerControl();Controls.Add(viewer);viewer.VisibilityChanged+=()=>{hideHousing=viewer.HousingHidden;if(VisibilityChanged!=null)VisibilityChanged();};viewer.SetResult(result,true);viewer.SetHousingHidden(hideHousing);}
  public void PresentFrame(){if(view>=5&&viewer!=null)viewer.Present(Phase);else Invalidate();}
  public void FitView(){if(view>=5&&viewer!=null)viewer.Fit();else{Zoom=1;Invalidate();}}
  Color blue=Color.FromArgb(20,111,172),orange=Color.FromArgb(213,116,30),green=Color.FromArgb(30,137,117),gray=Color.FromArgb(112,127,147);
  public Canvas(){DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable,true);BackColor=Color.FromArgb(248,250,252);MouseWheel+=(s,e)=>{if(View>=5)return;Zoom=Math.Max(.25,Math.Min(5,Zoom*(e.Delta>0?1.12:1/1.12)));Invalidate();};}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);if(View>=5)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var grid=new Pen(Color.FromArgb(231,236,242))){for(int x=0;x<Width;x+=28)g.DrawLine(grid,x,0,x,Height);for(int y=0;y<Height;y+=28)g.DrawLine(grid,0,y,Width,y);}
   if(Result==null)return;var r=Result;var p=r.P;
   if(!r.Valid){using(var b=new SolidBrush(Color.FromArgb(255,243,239)))g.FillRectangle(b,22,20,Math.Max(100,Width-44),Math.Max(100,Height-40));using(var f=new Font("Segoe UI",14,FontStyle.Bold))g.DrawString("Геометрия не построена",f,Brushes.Firebrick,38,38);using(var f=new Font("Segoe UI",10))g.DrawString(string.Join("\n\n",r.Errors.Take(4))+"\n\nИсправьте выделенные параметры или задайте больший общий габарит.",f,Brushes.Maroon,new RectangleF(38,92,Width-76,Height-115));return;}
   string head=View==4?"Подшипник · идеальное качение без скольжения":View==3?"Осевая компоновка · условный разрез":p.Kind==0?Engine.Scheme(p):p.Kind==1?"Внешнее зацепление · эвольвентные боковые поверхности":p.Kind==2?"Открытый зубчатый ремень · движение по делительной линии":"Планетарная схема · солнце → водило, кольцо закреплено";
   if(p.Kind==3&&r.Stages.Count>1&&View==0)head="Ступень "+(Math.Min(StageIndex,r.Stages.Count-1)+1)+" из "+r.Stages.Count+" · суммарное i="+Engine.F(r.Actual)+" · ступени вдоль общей оси";
   bool compact=Height<420;using(var f=new Font("Segoe UI",9)){g.DrawString(head,f,Brushes.SlateGray,new RectangleF(16,12,Width-32,compact?22:34));if(!compact)g.DrawString("Габарит "+Engine.F(r.Width)+" × "+Engine.F(r.Height)+" мм   Пакет ≈ "+Engine.F(r.Depth)+" мм   Вход "+Engine.F(p.InputRpm)+" об/мин",f,Brushes.SlateGray,new RectangleF(16,43,Width-32,24));}
   double[] bounds=Engine.Bounds(r);if(View==4)bounds=new[]{-p.Bore*.7,p.Bore*.7,-p.Bore*.7,p.Bore*.7};if(View==3)bounds=new[]{-p.MotorLength-6,r.Depth+6,-r.Height/2-6,r.Height/2+6};
   double bw=bounds[1]-bounds[0],bh=bounds[3]-bounds[2],mx=(bounds[1]+bounds[0])/2,my=(bounds[3]+bounds[2])/2;int top=compact?42:82,bottom=compact?32:74;float scale=(float)(Math.Min((Width-48)/(bw+8),(Height-top-bottom)/(bh+8))*Zoom);if(scale<=0||float.IsNaN(scale)||float.IsInfinity(scale))return;
   g.TranslateTransform(Width/2f-(float)mx*scale,(top+Height-bottom)/2f+(float)my*scale);g.ScaleTransform(scale,-scale);
   if(View==4){double outside=p.Kind==0?Engine.DiscAngle(p,Phase):0;Bearing(g,p,0,0,Phase,outside,scale);}
   else if(View==3){if(p.Kind==3)AxialTrain(g,r,scale);else Axial(g,r,scale);}
   else{
    using(var a=new Pen(Color.FromArgb(191,203,215),1/scale)){g.DrawLine(a,(float)bounds[0]-5,0,(float)bounds[1]+5,0);g.DrawLine(a,0,(float)bounds[2]-5,0,(float)bounds[3]+5);}
    if(p.Kind==0)Cycloid(g,r,scale);else if(p.Kind==3){int stage=Math.Max(0,Math.Min(StageIndex,r.Stages.Count-1));Planetary(g,r.Stages.Count>1?r.Stages[stage]:r,scale,r.Stages.Count>1?Engine.StageInput(r,Phase,stage):Phase);}else Pair(g,r,scale);
    if(ShowMotor&&p.MotorFrame>0&&(p.Kind!=3||StageIndex==0))Motor(g,p,scale);
   }
   g.ResetTransform();string note=View==4?"Размеры колец заданы; число/диаметр шариков — модель. Нагрузки, проскальзывание и ресурс не рассчитываются.":View==3?(p.Kind==3?"Водило ступени вращает солнце следующей; кольца закреплены. Соединения и опоры требуют CAD.":"Два противоположных эксцентрика; междисковый зазор задан отдельно. Опоры и крепёж требуют CAD."):p.Kind==0?(p.Discs==2?"Синий — диск A; оранжевый — диск B (другой осевой слой). ":"Синий — диск A. ")+"Кольцо — охра; пальцы — зелёный.":p.Kind==2?"Зубья шкивов показаны условно: конкретный профиль ремня нужно выбрать по каталогу.":"Корень зуба упрощён, без галтели и силового расчёта. Эскиз для компоновки, не готовая технология изготовления.";
   using(var f=new Font("Segoe UI",9)){if(!compact)g.DrawString(note,f,Brushes.SlateGray,new RectangleF(16,Height-61,Width-32,37));g.DrawString(compact?"мм · колесо мыши — масштаб · размеры и проверки — на соседних вкладках":"Щёлкните чертёж и крутите колесо мыши для масштаба. Размеры в мм.",f,Brushes.SlateGray,new RectangleF(16,Height-23,Width-32,23));}
  }
  void Cycloid(Graphics g,Result r,float sc){
   var p=r.P;double ring=Engine.RingAngle(p,Phase),carrier=Engine.CarrierAngle(p,Phase),outer=p.Radius+p.Roller/2+p.Wall;
   using(var pen=new Pen(p.RingOutput?orange:gray,1.7f/sc)){Circle(g,pen,0,0,outer);Line(g,pen,(outer-3)*Math.Cos(ring),(outer-3)*Math.Sin(ring),outer*Math.Cos(ring),outer*Math.Sin(ring));if(p.IntegratedRing&&ringContour!=null){var contour=Engine.Transform(ringContour,0,0,ring);using(var path=new GraphicsPath(FillMode.Alternate)){path.AddEllipse((float)-outer,(float)-outer,(float)(2*outer),(float)(2*outer));path.AddPolygon(contour);using(var fill=new SolidBrush(Color.FromArgb(28,p.RingOutput?orange:gray)))g.FillPath(fill,path);}g.DrawPolygon(pen,contour);}else for(int j=0;j<r.Pins;j++){var c=Engine.Polar(p.Radius,Engine.Tau*j/r.Pins+ring);Circle(g,pen,c.X,c.Y,p.Roller/2);}}
   int[] discs=p.Discs==2?(View==1?new[]{0}:View==2?new[]{1}:new[]{1,0}):new[]{0};
   foreach(int disk in discs){double crank=Phase+disk*Math.PI,spin=disk==0?Engine.DiscAngle(p,Phase):Engine.SecondDiscAngle(p,Phase),x=p.Eccentric*Math.Cos(crank),y=p.Eccentric*Math.Sin(crank);Color color=disk==0?blue:orange;
    PointF[] points=Engine.Transform(r.Profile,x,y,spin);using(var b=new SolidBrush(Color.FromArgb(p.Discs==2&&View==0?20:35,color)))g.FillPolygon(b,points);using(var pen=new Pen(color,1.6f/sc)){g.DrawPolygon(pen,points);Circle(g,pen,x,y,p.Bore/2);for(int j=0;j<p.OutputPins;j++){var v=Engine.Polar(p.OutputRadius,Engine.Tau*j/p.OutputPins+carrier);Circle(g,pen,x+v.X,y+v.Y,r.Hole/2);}Line(g,pen,x,y,x+(p.Bore/2+2)*Math.Cos(spin),y+(p.Bore/2+2)*Math.Sin(spin));}
    if(p.BearingSimulation)Bearing(g,p,x,y,Phase+disk*Math.PI,spin,sc);using(var pen=new Pen(Color.FromArgb(153,169,79),1.1f/sc))Line(g,pen,0,0,x,y);
   }
   using(var pen=new Pen(green,1.8f/sc)){Circle(g,pen,0,0,p.Shaft/2);for(int j=0;j<p.OutputPins;j++){var v=Engine.Polar(p.OutputRadius,Engine.Tau*j/p.OutputPins+carrier);Circle(g,pen,v.X,v.Y,p.OutputPin/2);}}
  }
  void Pair(Graphics g,Result r,float sc){
   var p=r.P;double c=p.Kind==1?r.Spacing:p.Center,angle=Phase,second=p.Kind==1?Math.PI-Math.PI/r.Z2-Phase/r.Actual:Phase/r.Actual;
   if(p.Kind==1){Gear(g,r.Gear1,0,0,angle,blue,sc);Gear(g,r.Gear2,c,0,second,green,sc);}
   else{
    double ra=p.Pitch*r.Z1/(2*Math.PI),rb=p.Pitch*r.Z2/(2*Math.PI);Pulley(g,0,0,ra,r.Z1,angle,blue,sc);Pulley(g,c,0,rb,r.Z2,second,green,sc);
    if(r.BeltPath!=null&&r.BeltPath.Length>3){using(var belt=new Pen(Color.FromArgb(80,88,100),2.4f/sc))g.DrawPolygon(belt,r.BeltPath);DrawBeltMarkers(g,r.BeltPath,-Phase*ra,sc);}
   }
   using(var pen=new Pen(gray,1.3f/sc)){Circle(g,pen,0,0,p.Shaft/2);Circle(g,pen,c,0,p.OutputShaft/2);}
   if(p.BearingSimulation)Bearing(g,p,0,0,Phase,0,sc);
  }
  void Planetary(Graphics g,Result r,float sc,double input){
   var p=r.P;double carrier=input/r.Actual,dist=p.Module*(r.Z1+r.Zp)/2;
   var ring=Engine.Transform(r.Gear2,0,0,Engine.RingInitial(r));using(var shape=new GraphicsPath(FillMode.Alternate)){float radius=(float)(p.Module*(r.Z2+2.5)/2+p.Wall);shape.AddEllipse(-radius,-radius,2*radius,2*radius);shape.AddPolygon(ring);using(var b=new SolidBrush(Color.FromArgb(28,gray)))g.FillPath(b,shape);}using(var pen=new Pen(gray,1.7f/sc))g.DrawPolygon(pen,ring);
   using(var arm=new Pen(Color.FromArgb(130,green),2/sc)){for(int j=0;j<p.Planets;j++){var c=Engine.Polar(dist,Engine.Tau*j/p.Planets+carrier);Line(g,arm,0,0,c.X,c.Y);}}
   Gear(g,r.Gear1,0,0,input,blue,sc);for(int j=0;j<p.Planets;j++){var c=Engine.Polar(dist,Engine.Tau*j/p.Planets+carrier);Gear(g,r.PlanetGear,c.X,c.Y,Engine.PlanetAngle(r,input,j),orange,sc);using(var pen=new Pen(green,1.5f/sc))Circle(g,pen,c.X,c.Y,p.PlanetPin/2);}
   if(p.BearingSimulation)Bearing(g,p,0,0,input,0,sc);using(var pen=new Pen(blue,1.3f/sc))Circle(g,pen,0,0,p.Shaft/2);
  }
  void AxialTrain(Graphics g,Result r,float sc){var p=r.P;double layer=Math.Max(p.Thickness,p.BearingSimulation?p.BearingWidth:0);using(var pen=new Pen(gray,1.5f/sc)){Line(g,pen,-p.MotorLength,0,r.Depth+5,0);g.DrawRectangle(pen,(float)-p.MotorLength,(float)-p.MotorFrame/2,(float)p.MotorLength,(float)p.MotorFrame);g.DrawRectangle(pen,0,(float)-r.Height/2,(float)r.Depth,(float)r.Height);}
   for(int j=0;j<r.Stages.Count;j++){double x=p.Wall+j*(layer+p.StageGap),radius=p.Module*(r.Stages[j].Z2+2.5)/2;using(var brush=new SolidBrush(Color.FromArgb(35,j%2==0?blue:orange)))g.FillRectangle(brush,(float)x,(float)-radius,(float)layer,(float)(2*radius));using(var pen=new Pen(j%2==0?blue:orange,1.7f/sc))g.DrawRectangle(pen,(float)x,(float)-radius,(float)layer,(float)(2*radius));if(j<r.Stages.Count-1)using(var pen=new Pen(green,2/sc))Line(g,pen,x+layer,0,x+layer+p.StageGap,0);}
  }
  void Gear(Graphics g,PointF[] shape,double x,double y,double angle,Color color,float sc){if(shape==null||shape.Length<3)return;var points=Engine.Transform(shape,x,y,angle);using(var b=new SolidBrush(Color.FromArgb(35,color)))g.FillPolygon(b,points);using(var pen=new Pen(color,1.5f/sc))g.DrawPolygon(pen,points);}
  void Pulley(Graphics g,double x,double y,double radius,int z,double angle,Color color,float sc){using(var pen=new Pen(color,1.5f/sc)){Circle(g,pen,x,y,radius);for(int j=0;j<z;j++){double a=Engine.Tau*j/z+angle;Line(g,pen,x+(radius-.5)*Math.Cos(a),y+(radius-.5)*Math.Sin(a),x+(radius+.5)*Math.Cos(a),y+(radius+.5)*Math.Sin(a));}Line(g,pen,x,y,x+radius*.7*Math.Cos(angle),y+radius*.7*Math.Sin(angle));}}
  void Bearing(Graphics g,Parameters p,double x,double y,double inner,double outer,float sc){
   double pitch=(p.Bore+p.BearingInner)/2,ball=p.BallDiameter/2;var motion=Engine.BearingAngles(p,inner,outer);using(var pen=new Pen(Color.FromArgb(120,80,101,130),1.15f/sc)){Circle(g,pen,x,y,p.Bore/2);Circle(g,pen,x,y,p.BearingInner/2);Circle(g,pen,x,y,pitch/2-ball);Circle(g,pen,x,y,pitch/2+ball);Line(g,pen,x+p.BearingInner/2*Math.Cos(inner),y+p.BearingInner/2*Math.Sin(inner),x+(pitch/2-ball)*Math.Cos(inner),y+(pitch/2-ball)*Math.Sin(inner));Line(g,pen,x+(pitch/2+ball)*Math.Cos(outer),y+(pitch/2+ball)*Math.Sin(outer),x+p.Bore/2*Math.Cos(outer),y+p.Bore/2*Math.Sin(outer));}
   for(int j=0;j<p.BallCount;j++){var c=Engine.Polar(pitch/2,Engine.Tau*j/p.BallCount+motion[0]);using(var b=new SolidBrush(Color.FromArgb(235,241,248)))g.FillEllipse(b,(float)(x+c.X-ball),(float)(y+c.Y-ball),(float)(2*ball),(float)(2*ball));using(var pen=new Pen(Color.FromArgb(75,100,130),1.1f/sc)){Circle(g,pen,x+c.X,y+c.Y,ball);double a=motion[1]+Engine.Tau*j/p.BallCount;Line(g,pen,x+c.X-ball*.6*Math.Cos(a),y+c.Y-ball*.6*Math.Sin(a),x+c.X+ball*.6*Math.Cos(a),y+c.Y+ball*.6*Math.Sin(a));}}
  }
  void Motor(Graphics g,Parameters p,float sc){using(var pen=new Pen(Color.FromArgb(110,117,131,147),1/sc)){pen.DashStyle=DashStyle.Dash;float half=(float)p.MotorFrame/2;g.DrawRectangle(pen,-half,-half,2*half,2*half);Circle(g,pen,0,0,p.MotorPilot/2);for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)Circle(g,pen,i*p.MotorMount/2,j*p.MotorMount/2,p.MotorHole/2);}}
  void Axial(Graphics g,Result r,float sc){var p=r.P;double thick=p.Kind==0?Math.Max(p.Thickness,p.BearingSimulation?p.BearingWidth:0):p.Thickness;using(var pen=new Pen(gray,1.5f/sc)){g.DrawRectangle(pen,(float)-p.MotorLength,(float)-p.MotorFrame/2,(float)p.MotorLength,(float)p.MotorFrame);Line(g,pen,0,-r.Height/2,r.Depth,-r.Height/2);Line(g,pen,0,r.Height/2,r.Depth,r.Height/2);g.DrawRectangle(pen,0,(float)-r.Height/2,(float)p.Wall,(float)r.Height);g.DrawRectangle(pen,(float)(r.Depth-p.Wall),(float)-r.Height/2,(float)p.Wall,(float)r.Height);Line(g,pen,-5,0,r.Depth+5,0);}
   int count=p.Kind==0?p.Discs:1;for(int j=0;j<count;j++){double x=p.Wall+j*(thick+p.DiscGap),cy=p.Kind==0?p.Eccentric*Math.Sin(Phase+j*Math.PI):0,radius=p.Kind==0?p.Radius-p.Roller/2:p.Kind==3?p.Module*r.Z1/2:p.Module*r.Z1/2;Color color=j==0?blue:orange;using(var b=new SolidBrush(Color.FromArgb(42,color)))g.FillRectangle(b,(float)x,(float)(cy-radius),(float)thick,(float)(2*radius));using(var pen=new Pen(color,1.6f/sc)){g.DrawRectangle(pen,(float)x,(float)(cy-radius),(float)thick,(float)(2*radius));if(p.BearingSimulation)g.DrawRectangle(pen,(float)x,(float)(cy-p.Bore/2),(float)p.BearingWidth,(float)p.Bore);}}
  }
  void DrawBeltMarkers(Graphics g,PointF[] path,double shift,float sc){var lengths=new double[path.Length];double total=0;for(int i=0;i<path.Length;i++){var a=path[i];var b=path[(i+1)%path.Length];total+=Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));lengths[i]=total;}using(var pen=new Pen(orange,3/sc)){for(int m=0;m<18;m++){double distance=((total*m/18+shift)%total+total)%total;int j=Array.FindIndex(lengths,segmentEnd=>segmentEnd>=distance);double before=j==0?0:lengths[j-1],t=(distance-before)/(lengths[j]-before);var a=path[j];var b=path[(j+1)%path.Length];double x=a.X+t*(b.X-a.X),y=a.Y+t*(b.Y-a.Y);Circle(g,pen,x,y,.35);}}}
  static void Circle(Graphics g,Pen pen,double x,double y,double radius){if(radius>0)g.DrawEllipse(pen,(float)(x-radius),(float)(y-radius),(float)(2*radius),(float)(2*radius));}
  static void Line(Graphics g,Pen p,double a,double b,double c,double d){g.DrawLine(p,(float)a,(float)b,(float)c,(float)d);}
 }
}
