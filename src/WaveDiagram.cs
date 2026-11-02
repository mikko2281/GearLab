using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Windows.Forms;

namespace GearLab {

 public sealed class WaveDiagram:Control {

  public WaveParameters Parameters;public double Phase,Zoom=1;public bool Axial;internal int PaintedProfiles;

  public WaveDiagram(){DoubleBuffered=true;BackColor=Color.FromArgb(248,250,252);MouseWheel+=(s,e)=>{Zoom=Math.Max(.3,Math.Min(5,Zoom*Math.Exp(e.Delta*.001)));Invalidate();};MouseDown+=(s,e)=>Focus();Resize+=(s,e)=>Invalidate();}

  public static List<string[]> Rows(WaveParameters p){double d=p.Module*p.FlexTeeth;return new List<string[]>{new[]{"Неподвижный венец",p.RingTeeth+" зубьев · m="+Engine.F(p.Module)+" мм","1"},new[]{"Гибкое колесо",p.FlexTeeth+" зубьев · два осевых ряда · стенка "+Engine.F(p.Wall)+" мм","1"},new[]{"Выходной венец и фланец",p.FlexTeeth+" зубьев · выходной вал Ø"+Engine.F(p.OutputBore)+" мм","1"},new[]{"Эллиптический генератор","Входное отверстие Ø"+Engine.F(p.Bore)+" мм","1"},new[]{"Гибкий подшипник","Стенка "+Engine.F(p.BearingWall)+" мм · заданная деформация","1"},new[]{"Крепёж неподвижного фланца","Шесть проходных отверстий и втулок","6"},new[]{"Крепёж выходного фланца","Шесть винтов; вращаются вместе с выходом","6"},new[]{"Рабочий пакет","Два венца по "+Engine.F(p.Thickness)+" мм; делительный Ø"+Engine.F(d)+" мм","1"}};}

  public static PointF[] Profile(WaveParameters p,bool ring,double phase){var a=Engine.Involute(ring?p.RingTeeth:p.FlexTeeth,p.Module,ring);if(ring)return a;double c=Math.Cos(phase),s=Math.Sin(phase),spin=-phase/p.Ratio-phase;return a.Select(q=>{double x=q.X*Math.Cos(spin)-q.Y*Math.Sin(spin),y=q.X*Math.Sin(spin)+q.Y*Math.Cos(spin);x*=1+1.0/p.Ratio;y*=1-1.0/p.Ratio;return new PointF((float)(c*x-s*y),(float)(s*x+c*y));}).ToArray();}

  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);PaintedProfiles=0;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(Color.FromArgb(231,236,242))) {for(int x=0;x<Width;x+=28)g.DrawLine(pen,x,0,x,Height);for(int y=0;y<Height;y+=28)g.DrawLine(pen,0,y,Width,y);}var p=Parameters;if(p==null)return;using(var f=new Font("Segoe UI",9)){g.DrawString(Axial?"Осевой разрез · два ряда зубьев, генератор и выходной фланец":"Волновая схема · жёсткий венец закреплён; эллипс — вход, фланец — выход",f,Brushes.SlateGray,new RectangleF(16,12,Width-32,40));g.DrawString("Вход "+Engine.F(Phase*180/Math.PI)+"°   Выход "+Engine.F(-Phase/p.Ratio*180/Math.PI)+"°   i = −"+p.Ratio,f,Brushes.SlateGray,16,52);}

   double outer=p.Module*(p.FlexTeeth+4.5)/2+p.RingWall;float sc=(float)(Math.Min((Width-60)/(2*outer+10),(Height-155)/(2*outer+10))*Zoom);if(sc<=0)return;g.TranslateTransform(Width/2f,(Height+15)/2f);g.ScaleTransform(sc,-sc);if(Axial)DrawAxial(g,p,sc,outer);else DrawRadial(g,p,sc,outer);g.ResetTransform();using(var f=new Font("Segoe UI",9))g.DrawString("Колесо мыши — масштаб. Схема деформации задана; контакт и прочность не рассчитаны.",f,Brushes.SlateGray,new RectangleF(16,Height-43,Width-32,40));

  }

  internal void DrawRadial(Graphics g,WaveParameters p,float sc,double outer){Color gray=Color.FromArgb(112,127,147),orange=Color.FromArgb(213,116,30),blue=Color.FromArgb(20,111,172);var ring=Profile(p,true,0);var flex=Profile(p,false,Phase);double root=p.Module*(p.FlexTeeth/2.0-1.25)-p.Wall;using(var shape=new GraphicsPath(FillMode.Alternate)){shape.AddEllipse((float)-outer,(float)-outer,(float)(2*outer),(float)(2*outer));shape.AddPolygon(ring);using(var b=new SolidBrush(Color.FromArgb(32,gray)))g.FillPath(b,shape);}using(var pen=new Pen(gray,1.5f/sc)){g.DrawPolygon(pen,ring);Circle(g,pen,outer);double br=outer-Math.Max(1.2,Math.Min(2,p.RingWall*.30))-1;for(int i=0;i<6;i++){double a=Engine.Tau*i/6;g.DrawEllipse(pen,(float)(br*Math.Cos(a)-1.2),(float)(br*Math.Sin(a)-1.2),2.4f,2.4f);}}

   using(var b=new SolidBrush(Color.FromArgb(35,orange)))g.FillPolygon(b,flex);using(var pen=new Pen(orange,1.5f/sc))g.DrawPolygon(pen,flex);PaintedProfiles=2;

   var state=g.Save();g.RotateTransform((float)(Phase*180/Math.PI));double rx=root*(1+1.0/p.Ratio)-p.Clearance,ry=root*(1-1.0/p.Ratio)-p.Clearance;using(var b=new SolidBrush(Color.FromArgb(35,blue)))g.FillEllipse(b,(float)-rx,(float)-ry,(float)(2*rx),(float)(2*ry));using(var pen=new Pen(blue,1.4f/sc)){g.DrawEllipse(pen,(float)-rx,(float)-ry,(float)(2*rx),(float)(2*ry));g.DrawEllipse(pen,(float)(-rx+p.BearingWall),(float)(-ry+p.BearingWall),(float)(2*(rx-p.BearingWall)),(float)(2*(ry-p.BearingWall)));g.DrawLine(pen,(float)-rx,0,(float)rx,0);}g.Restore(state);

   using(var pen=new Pen(Color.SeaGreen,1.7f/sc)){double hr=Math.Max(p.OutputBore/2+5,p.Module*p.FlexTeeth/2*.30);Circle(g,pen,hr);Circle(g,pen,p.OutputBore/2);double a=-Phase/p.Ratio;g.DrawLine(pen,0,0,(float)(hr*Math.Cos(a)),(float)(hr*Math.Sin(a)));for(int i=0;i<6;i++){double t=a+Engine.Tau*i/6;double r=hr*.78;g.DrawEllipse(pen,(float)(r*Math.Cos(t)-1),(float)(r*Math.Sin(t)-1),2,2);}}using(var pen=new Pen(blue,1.5f/sc))Circle(g,pen,p.Bore/2);

  }

  static void Circle(Graphics g,Pen p,double r){g.DrawEllipse(p,(float)-r,(float)-r,(float)(2*r),(float)(2*r));}

  internal void DrawAxial(Graphics g,WaveParameters p,float sc,double outer){double w=p.Thickness,off=-w-p.Clearance/2;using(var gray=new Pen(Color.SlateGray,1.5f/sc))using(var orange=new Pen(Color.DarkOrange,1.5f/sc))using(var blue=new Pen(Color.SteelBlue,1.5f/sc)){float root=(float)(p.Module*p.FlexTeeth/2-1.25*p.Module);g.DrawRectangle(gray,(float)off,(float)-outer,(float)w,(float)(outer-root));g.DrawRectangle(gray,(float)off,root,(float)w,(float)(outer-root));g.DrawRectangle(orange,(float)(off+w+p.Clearance),(float)-outer,(float)w,(float)(outer-root));g.DrawRectangle(orange,(float)(off+w+p.Clearance),root,(float)w,(float)(outer-root));g.DrawRectangle(orange,(float)off,-root,(float)(2*w+p.Clearance),(float)p.Wall);g.DrawRectangle(orange,(float)off,root-(float)p.Wall,(float)(2*w+p.Clearance),(float)p.Wall);g.DrawRectangle(blue,(float)off,-root+(float)(p.Wall+p.BearingWall),(float)(2*w+p.Clearance),(float)(2*(root-p.Wall-p.BearingWall)));g.DrawLine(gray,(float)(off-8),0,(float)(off+2*w+20),0);g.DrawRectangle(orange,(float)(off+2*w+2*p.Clearance),(float)-outer,(float)Math.Max(2,p.RingWall*.75),(float)(2*outer));}PaintedProfiles=2;}

  public static void Export(WaveParameters p,string path,bool dxf){var profiles=new[]{Profile(p,true,0),Profile(p,false,0)};var b=new StringBuilder();if(dxf){b.Append("0\nSECTION\n2\nENTITIES\n");for(int j=0;j<profiles.Length;j++){var a=profiles[j];b.Append("0\nLWPOLYLINE\n8\n"+(j==0?"FIXED_RING":"FLEX_SPLINE")+"\n90\n"+a.Length+"\n70\n1\n");foreach(var q in a)b.Append("10\n"+Engine.N(q.X)+"\n20\n"+Engine.N(q.Y)+"\n");}b.Append("0\nENDSEC\n0\nEOF\n");}else{double radius=p.Module*(p.FlexTeeth+4.5)/2+p.RingWall+2;b.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+Engine.N(2*radius)+"mm\" height=\""+Engine.N(2*radius)+"mm\" viewBox=\""+Engine.N(-radius)+" "+Engine.N(-radius)+" "+Engine.N(2*radius)+" "+Engine.N(2*radius)+"\"><title>Strain-wave layout; not manufacturing tooth geometry</title><g fill=\"none\" stroke-width=\"0.15\">");for(int j=0;j<profiles.Length;j++)b.Append("<polygon stroke=\""+(j==0?"#708093":"#d5741e")+"\" points=\""+string.Join(" ",profiles[j].Select(q=>Engine.N(q.X)+","+Engine.N(-q.Y)))+"\"/>");b.Append("</g></svg>");}File.WriteAllText(path,b.ToString(),Encoding.UTF8);}

 }

}

