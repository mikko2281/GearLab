using System;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Windows.Media;using System.Windows.Media.Media3D;using LibTessDotNet.Double;
namespace GearLab {
 public sealed class SceneMesh {
  public Point3D[] Points;public Vector3D[] Normals;public int[] Indices;
  public MeshGeometry3D Geometry(){var g=new MeshGeometry3D{Positions=new Point3DCollection(Points),Normals=new Vector3DCollection(Normals),TriangleIndices=new Int32Collection(Indices)};g.Freeze();return g;}
  public int Triangles{get{return Indices.Length/3;}}
  public double SignedVolume(){double volume=0;for(int j=0;j<Indices.Length;j+=3){var a=Points[Indices[j]];var b=Points[Indices[j+1]];var c=Points[Indices[j+2]];volume+=Vector3D.DotProduct((Vector3D)a,Vector3D.CrossProduct((Vector3D)b,(Vector3D)c))/6;}return volume;}
 }
 public sealed class MeshWriter {
  public List<Point3D> P=new List<Point3D>();public List<Vector3D> N=new List<Vector3D>();public List<int> I=new List<int>();
  public int Vertex(Point3D p,Vector3D n){int index=P.Count;P.Add(p);N.Add(n);return index;}
  public void Triangle(Point3D a,Point3D b,Point3D c,Vector3D na,Vector3D nb,Vector3D nc){if(Vector3D.CrossProduct(b-a,c-a).LengthSquared<1e-18)return;int index=Vertex(a,na);Vertex(b,nb);Vertex(c,nc);I.Add(index);I.Add(index+1);I.Add(index+2);}
  public SceneMesh Finish(){return new SceneMesh{Points=P.ToArray(),Normals=N.ToArray(),Indices=I.ToArray()};}
 }
 public static class SceneMesher {
  public static int Builds;[ThreadStatic]static bool printQuality;
  public static SceneMesh BuildPrint(CadPart part,CancellationToken cancel){bool previous=printQuality;printQuality=true;try{return Build(part,cancel);}finally{printQuality=previous;}}

  static System.Drawing.PointF[] Sample(CadLoop l){if(!l.Circle)return l.Points;int count=printQuality?Math.Max(36,Math.Min(2048,(int)Math.Ceiling(Math.PI/Math.Acos(Math.Max(-1,1-.01/l.R))))):Math.Max(24,Math.Min(128,(int)Math.Ceiling(Math.PI*Math.Sqrt(l.R/.04))));count=(count+3)/4*4;return l.Sample(count);}
  public static SceneMesh Build(CadPart part,CancellationToken cancel){Builds++;var writer=new MeshWriter();for(int j=0;j<part.Sections.Count;j++){cancel.ThrowIfCancellationRequested();var s=part.Sections[j];Sides(writer,s.Outer,s.Bottom,s.Top,false);foreach(var hole in s.Holes)Sides(writer,hole,s.Bottom,s.Top,true);if(j==0)Cap(writer,s.Outer,s.Holes,s.Bottom,false);else Transition(writer,part.Sections[j-1],s);if(j==part.Sections.Count-1)Cap(writer,s.Outer,s.Holes,s.Top,true);}return writer.Finish();}
  public static void Cap(MeshWriter w,CadLoop outer,IEnumerable<CadLoop> holes,double z,bool up){var tess=new Tess{NoEmptyPolygons=true};const double rotate=.3713906763;double cs=Math.Cos(rotate),sn=Math.Sin(rotate);foreach(var loop in new[]{outer}.Concat(holes)){var contour=Sample(loop).Select(p=>new ContourVertex(new Vec3{X=p.X*cs-p.Y*sn,Y=p.X*sn+p.Y*cs,Z=0},p)).ToArray();tess.AddContour(contour,ContourOrientation.Original);}tess.Tessellate(WindingRule.EvenOdd,ElementType.Polygons,3,null,new Vec3{Z=1});var normal=new Vector3D(0,0,up?1:-1);int start=w.P.Count;foreach(var v in tess.Vertices){var original=v.Data is System.Drawing.PointF?(System.Drawing.PointF)v.Data:new System.Drawing.PointF((float)(v.Position.X*cs+v.Position.Y*sn),(float)(-v.Position.X*sn+v.Position.Y*cs));w.Vertex(new Point3D(original.X,original.Y,z),normal);}
   for(int j=0;j<tess.Elements.Length;j+=3){int a=tess.Elements[j],b=tess.Elements[j+1],c=tess.Elements[j+2];if(a<0||b<0||c<0)continue;var x=w.P[start+a];var y=w.P[start+b];var q=w.P[start+c];bool ccw=Vector3D.CrossProduct(y-x,q-x).Z>0;w.I.Add(start+a);w.I.Add(start+(ccw==up?b:c));w.I.Add(start+(ccw==up?c:b));}}
  static void Sides(MeshWriter w,CadLoop loop,double z0,double z1,bool hole){var pts=Sample(loop);for(int j=0;j<pts.Length;j++){var a=pts[j];var b=pts[(j+1)%pts.Length];Vector3D na,nb;if(loop.Circle){na=new Vector3D(a.X-loop.X,a.Y-loop.Y,0);nb=new Vector3D(b.X-loop.X,b.Y-loop.Y,0);na.Normalize();nb.Normalize();}else{na=new Vector3D(b.Y-a.Y,a.X-b.X,0);if(na.LengthSquared<1e-14)continue;na.Normalize();nb=na;}if(hole){na=-na;nb=-nb;}var p0=new Point3D(a.X,a.Y,z0);var p1=new Point3D(b.X,b.Y,z0);var p2=new Point3D(b.X,b.Y,z1);var p3=new Point3D(a.X,a.Y,z1);if(hole){w.Triangle(p0,p2,p1,na,nb,nb);w.Triangle(p0,p3,p2,na,na,nb);}else{w.Triangle(p0,p1,p2,na,nb,nb);w.Triangle(p0,p2,p3,na,nb,na);}}}
  static bool Same(CadLoop a,CadLoop b){return a.Key()==b.Key();}static bool Inside(CadLoop a,CadLoop b){return Sample(a).All(v=>b.Contains(v.X,v.Y));}
  static void Transition(MeshWriter w,CadSection a,CadSection b){double z=a.Top;if(!Same(a.Outer,b.Outer)){bool shrink=Inside(b.Outer,a.Outer),grow=Inside(a.Outer,b.Outer);if(!shrink&&!grow)throw new Exception("Невложенные сечения: "+z);var large=shrink?a:b;var small=shrink?b.Outer:a.Outer;var holes=new List<CadLoop>{small};holes.AddRange(large.Holes.Where(h=>{var c=h.Center();return !small.Contains(c.X,c.Y);}));Cap(w,large.Outer,holes,z,shrink);}
   foreach(var face in CadTransitions.HoleFaces(a,b))Cap(w,face.Outer,face.Holes,z,face.Up);}

 }
 public sealed class CutResult {public SceneMesh Body,Cap;}
 public static class MeshSection {
  public static CutResult Clip(SceneMesh source,Matrix3D transform,int axis,double coordinate,bool positive,double epsilon){
   var body=new MeshWriter();var cap=new MeshWriter();var segments=new List<Point3D[]>();Vector3D normal=axis==0?new Vector3D(1,0,0):axis==1?new Vector3D(0,1,0):new Vector3D(0,0,1);if(positive)normal=-normal;double offset=positive?-coordinate:coordinate;
   Func<Point3D,double> distance=p=>Vector3D.DotProduct((Vector3D)p,normal)-offset;
   for(int j=0;j<source.Indices.Length;j+=3){var input=new List<CutVertex>(3);for(int k=0;k<3;k++){int index=source.Indices[j+k];var p=transform.Transform(source.Points[index]);var n=transform.Transform(source.Normals[index]);input.Add(new CutVertex{P=p,N=n,D=distance(p)});}double min=input.Min(v=>v.D),max=input.Max(v=>v.D);if(min>epsilon)continue;
    var output=new List<CutVertex>(4);var crossing=new List<Point3D>();for(int k=0;k<3;k++){var a=input[k];var b=input[(k+1)%3];bool ia=a.D<=epsilon,ib=b.D<=epsilon;if(Math.Abs(a.D)<epsilon)crossing.Add(a.P);if(ia)output.Add(a);if(ia!=ib){double t=a.D/(a.D-b.D);var v=new CutVertex{P=a.P+(b.P-a.P)*t,N=a.N+(b.N-a.N)*t,D=0};if(v.N.LengthSquared>1e-15)v.N.Normalize();output.Add(v);crossing.Add(v.P);}}
    for(int k=1;k+1<output.Count;k++)body.Triangle(output[0].P,output[k].P,output[k+1].P,output[0].N,output[k].N,output[k+1].N);
    if(max>epsilon&&min<epsilon){var unique=new List<Point3D>();foreach(var p in crossing)if(!unique.Any(v=>(v-p).Length<epsilon*4))unique.Add(p);if(unique.Count==2&&(unique[0]-unique[1]).Length>epsilon*4)segments.Add(unique.ToArray());}
   }
   var loops=SectionLoops(segments,axis,epsilon*8);if(loops.Count>0){var tess=new Tess();foreach(var loop in loops)tess.AddContour(loop.Select(p=>new ContourVertex(new Vec3{X=p.X,Y=p.Y})).ToArray());tess.Tessellate(WindingRule.EvenOdd,ElementType.Polygons,3,null,new Vec3{Z=1});Func<Vec3,Point3D> restore=p=>axis==0?new Point3D(coordinate,p.X,p.Y):axis==1?new Point3D(p.X,coordinate,p.Y):new Point3D(p.X,p.Y,coordinate);foreach(var v in tess.Vertices)cap.Vertex(restore(v.Position),normal);
    for(int j=0;j<tess.Elements.Length;j+=3){int a=tess.Elements[j],b=tess.Elements[j+1],c=tess.Elements[j+2];if(a<0||b<0||c<0)continue;bool correct=Vector3D.DotProduct(Vector3D.CrossProduct(cap.P[b]-cap.P[a],cap.P[c]-cap.P[a]),normal)>0;cap.I.Add(a);cap.I.Add(correct?b:c);cap.I.Add(correct?c:b);}}
   return new CutResult{Body=body.Finish(),Cap=cap.Finish()};
  }
  sealed class CutVertex {public Point3D P;public Vector3D N;public double D;}
  static List<List<System.Windows.Point>> SectionLoops(List<Point3D[]> segments,int axis,double tolerance){
   var positions=new List<System.Windows.Point>();var map=new Dictionary<string,int>();Func<Point3D,int> vertex=p=>{double x=axis==0?p.Y:p.X,y=axis==2?p.Y:p.Z;string key=Math.Round(x/tolerance)+":"+Math.Round(y/tolerance);int id;if(!map.TryGetValue(key,out id)){id=positions.Count;map[key]=id;positions.Add(new System.Windows.Point(x,y));}return id;};var edges=new Dictionary<string,int[]>();foreach(var s in segments){int a=vertex(s[0]),b=vertex(s[1]);if(a==b)continue;string key=Math.Min(a,b)+":"+Math.Max(a,b);if(edges.ContainsKey(key))edges.Remove(key);else edges[key]=new[]{a,b};}
   var neighbours=new Dictionary<int,List<int>>();foreach(var e in edges.Values){if(!neighbours.ContainsKey(e[0]))neighbours[e[0]]=new List<int>();if(!neighbours.ContainsKey(e[1]))neighbours[e[1]]=new List<int>();neighbours[e[0]].Add(e[1]);neighbours[e[1]].Add(e[0]);}var used=new HashSet<string>();Func<int,int,string> keyOf=(a,b)=>Math.Min(a,b)+":"+Math.Max(a,b);var result=new List<List<System.Windows.Point>>();foreach(var e in edges.Values){if(used.Contains(keyOf(e[0],e[1])))continue;int start=e[0],previous=start,current=e[1];var loop=new List<System.Windows.Point>{positions[start]};used.Add(keyOf(start,current));bool closed=false;
    for(int k=0;k<=edges.Count;k++){loop.Add(positions[current]);if(current==start){closed=true;break;}var options=neighbours[current].Where(n=>n!=previous&&!used.Contains(keyOf(current,n))).ToList();if(options.Count==0)break;int next=options[0];used.Add(keyOf(current,next));previous=current;current=next;}if(closed&&loop.Count>=4){loop.RemoveAt(loop.Count-1);result.Add(loop);}}
   return result;
  }
 }
}
