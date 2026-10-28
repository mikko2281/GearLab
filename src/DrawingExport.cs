using System;using System.IO;using System.Text;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace GearLab {
 public class SketchLoop {public string Name;public PointF[] Points;public SketchLoop(string name,PointF[] points){Name=name;Points=points;}}
 public class SketchCircle {public string Name;public double X,Y,R;public SketchCircle(string name,double x,double y,double r){Name=name;X=x;Y=y;R=r;}}
 public static class DrawingExport {
  public static void Geometry(Result r,out List<SketchLoop> loops,out List<SketchCircle> circles){
   if(!r.Valid)throw new Exception("Исправьте ошибки геометрии до экспорта.");var p=r.P;loops=new List<SketchLoop>();circles=new List<SketchCircle>();
   if(p.Kind==3&&r.Stages.Count>1){double offset=0;for(int n=0;n<r.Stages.Count;n++){List<SketchLoop> sl;List<SketchCircle> sc;Geometry(r.Stages[n],out sl,out sc);string prefix="STAGE_"+(n+1)+"_";foreach(var loop in sl)loops.Add(new SketchLoop(prefix+loop.Name,Engine.Transform(loop.Points,offset,0,0)));foreach(var c in sc)circles.Add(new SketchCircle(prefix+c.Name,c.X+offset,c.Y,c.R));offset+=r.Width+12;}return;}
   if(p.Kind==0){
    for(int disk=0;disk<p.Discs;disk++){double x=disk*(2*p.Radius+p.Roller+2*p.Wall+10),angle=disk==0?0:-Math.PI/r.Lobes;string prefix=p.Discs==1?"DISC":"DISC_"+(disk==0?"A":"B");loops.Add(new SketchLoop(prefix+"_PROFILE",Engine.Transform(r.Profile,x,0,angle)));circles.Add(new SketchCircle(prefix+"_BORE",x,0,p.Bore/2));for(int j=0;j<p.OutputPins;j++){var v=Engine.Polar(p.OutputRadius,Engine.Tau*j/p.OutputPins);circles.Add(new SketchCircle(prefix+"_PIN_HOLES",x+v.X,v.Y,r.Hole/2));}}
   }else if(p.Kind==1){loops.Add(new SketchLoop("DRIVE_GEAR",r.Gear1));loops.Add(new SketchLoop("DRIVEN_GEAR",Engine.Transform(r.Gear2,r.Spacing,0,Math.PI-Math.PI/r.Z2)));circles.Add(new SketchCircle("DRIVE_BORE",0,0,p.Shaft/2));circles.Add(new SketchCircle("DRIVEN_BORE",r.Spacing,0,p.OutputShaft/2));}
   else if(p.Kind==2){circles.Add(new SketchCircle("DRIVE_PITCH_REFERENCE",0,0,p.Pitch*r.Z1/(2*Math.PI)));circles.Add(new SketchCircle("DRIVEN_PITCH_REFERENCE",p.Center,0,p.Pitch*r.Z2/(2*Math.PI)));circles.Add(new SketchCircle("DRIVE_BORE",0,0,p.Shaft/2));circles.Add(new SketchCircle("DRIVEN_BORE",p.Center,0,p.OutputShaft/2));loops.Add(new SketchLoop("BELT_PITCH_REFERENCE",r.BeltPath));}
   else{
    loops.Add(new SketchLoop("SUN_GEAR",r.Gear1));loops.Add(new SketchLoop("INTERNAL_RING",Engine.Transform(r.Gear2,0,0,Engine.RingInitial(r))));circles.Add(new SketchCircle("RING_OUTSIDE",0,0,p.Module*(r.Z2+2.5)/2+p.Wall));circles.Add(new SketchCircle("INPUT_BORE",0,0,p.Shaft/2));
    for(int j=0;j<p.Planets;j++){var c=Engine.Polar(p.Module*(r.Z1+r.Zp)/2,Engine.Tau*j/p.Planets);loops.Add(new SketchLoop("PLANET_"+(j+1),Engine.Transform(r.PlanetGear,c.X,c.Y,Engine.PlanetAngle(r,0,j))));circles.Add(new SketchCircle("PLANET_PIN_"+(j+1),c.X,c.Y,p.PlanetPin/2));}
   }
  }
  public static void Svg(Result r,string file){List<SketchLoop> loops;List<SketchCircle> circles;Geometry(r,out loops,out circles);FullDrawingExport.Svg(loops,circles,file);}
  public static void Dxf(Result r,string file){List<SketchLoop> loops;List<SketchCircle> circles;Geometry(r,out loops,out circles);var b=new StringBuilder();b.Append("0\r\nSECTION\r\n2\r\nHEADER\r\n9\r\n$ACADVER\r\n1\r\nAC1015\r\n9\r\n$INSUNITS\r\n70\r\n4\r\n0\r\nENDSEC\r\n0\r\nSECTION\r\n2\r\nENTITIES\r\n");
   foreach(var s in loops){b.Append("0\r\nLWPOLYLINE\r\n100\r\nAcDbEntity\r\n8\r\n"+s.Name+"\r\n100\r\nAcDbPolyline\r\n90\r\n"+s.Points.Length+"\r\n70\r\n1\r\n");foreach(var v in s.Points)b.Append("10\r\n"+Engine.N(v.X)+"\r\n20\r\n"+Engine.N(v.Y)+"\r\n");}
   foreach(var c in circles)b.Append("0\r\nCIRCLE\r\n100\r\nAcDbEntity\r\n8\r\n"+c.Name+"\r\n100\r\nAcDbCircle\r\n10\r\n"+Engine.N(c.X)+"\r\n20\r\n"+Engine.N(c.Y)+"\r\n30\r\n0\r\n40\r\n"+Engine.N(c.R)+"\r\n");b.Append("0\r\nENDSEC\r\n0\r\nEOF\r\n");ReleaseSupport.WriteText(file,b.ToString(),Encoding.ASCII);
  }
 }
}
