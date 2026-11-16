using System;using System.Linq;using System.Collections.Generic;using System.Windows.Media;using System.Windows.Media.Media3D;
namespace GearLab {
 // 6.10.0. Стрелки нагрузки в 3D: радиальная и осевая сила на выходном валу и рабочий момент.
 // Это наглядная схема приложения нагрузки, а не поле напряжений: длина стрелок условная,
 // величина подписывается в интерфейсе.
 public sealed class LoadArrow {public Point3D Origin;public Vector3D Direction;public double Length,Radius;public System.Drawing.Color Color;public bool Torque;public Vector3D Axis=new Vector3D(0,0,1);}
 public sealed partial class WpfScene {
  readonly Model3DGroup loadGroup=new Model3DGroup();
  public int LoadArrowCount{get{return loadGroup.Children.Count;}}
  public void SetLoadArrows(IEnumerable<LoadArrow> arrows){loadGroup.Children.Clear();if(arrows!=null)foreach(var a in arrows){if(a==null||a.Length<=0||a.Radius<=0)continue;var mesh=a.Torque?TorqueMesh(a):ArrowMesh(a.Origin,a.Direction,a.Length,a.Radius);if(mesh==null)continue;var brush=new SolidColorBrush(Color.FromRgb(a.Color.R,a.Color.G,a.Color.B));brush.Freeze();var group=new MaterialGroup();group.Children.Add(new DiffuseMaterial(brush));var glow=new SolidColorBrush(Color.FromRgb((byte)(a.Color.R/3),(byte)(a.Color.G/3),(byte)(a.Color.B/3)));glow.Freeze();group.Children.Add(new EmissiveMaterial(glow));group.Freeze();mesh.Freeze();loadGroup.Children.Add(new GeometryModel3D(mesh,group){BackMaterial=group});}
   if(Ready)RefreshVisibility();}
  static void Basis(Vector3D d,out Vector3D u,out Vector3D v){d.Normalize();u=Vector3D.CrossProduct(d,Math.Abs(d.Z)<.9?new Vector3D(0,0,1):new Vector3D(1,0,0));u.Normalize();v=Vector3D.CrossProduct(d,u);}
  // Цилиндр-стержень и конус-наконечник; наконечник смотрит в точку приложения силы.
  static MeshGeometry3D ArrowMesh(Point3D tip,Vector3D direction,double length,double radius){if(direction.LengthSquared<1e-12)return null;direction.Normalize();Vector3D u,v;Basis(direction,out u,out v);var mesh=new MeshGeometry3D();int n=20;double head=Math.Min(length*.35,radius*5);var baseHead=tip-direction*head;var start=tip-direction*length;
   Action<Point3D,double,Point3D,double> ring=(a,ra,b,rb)=>{int o=mesh.Positions.Count;for(int i=0;i<=n;i++){double t=2*Math.PI*i/n;var off=u*Math.Cos(t)+v*Math.Sin(t);mesh.Positions.Add(a+off*ra);mesh.Positions.Add(b+off*rb);mesh.Normals.Add(off);mesh.Normals.Add(off);}for(int i=0;i<n;i++){int k=o+2*i;mesh.TriangleIndices.Add(k);mesh.TriangleIndices.Add(k+2);mesh.TriangleIndices.Add(k+1);mesh.TriangleIndices.Add(k+1);mesh.TriangleIndices.Add(k+2);mesh.TriangleIndices.Add(k+3);}};
   ring(start,radius,baseHead,radius);ring(baseHead,radius*2.6,tip,0.0001);Disc(mesh,baseHead,-direction,u,v,radius*2.6,n);Disc(mesh,start,-direction,u,v,radius,n);return mesh;}
  static void Disc(MeshGeometry3D mesh,Point3D c,Vector3D normal,Vector3D u,Vector3D v,double r,int n){int o=mesh.Positions.Count;mesh.Positions.Add(c);mesh.Normals.Add(normal);for(int i=0;i<=n;i++){double t=2*Math.PI*i/n;mesh.Positions.Add(c+(u*Math.Cos(t)+v*Math.Sin(t))*r);mesh.Normals.Add(normal);}for(int i=1;i<=n;i++){mesh.TriangleIndices.Add(o);mesh.TriangleIndices.Add(o+i+1);mesh.TriangleIndices.Add(o+i);}}
  // Дуга 270° вокруг оси вала с наконечником — направление рабочего момента.
  static MeshGeometry3D TorqueMesh(LoadArrow a){var axis=a.Axis;if(axis.LengthSquared<1e-12)return null;axis.Normalize();Vector3D u,v;Basis(axis,out u,out v);double R=a.Length,r=a.Radius;int seg=48,n=12;double sweep=1.5*Math.PI,sign=a.Direction.Z<0?-1:1;var mesh=new MeshGeometry3D();
   Func<double,Point3D> at=t=>a.Origin+(u*Math.Cos(t)+v*Math.Sin(t))*R;Func<double,Vector3D> tangent=t=>{var d=(-u*Math.Sin(t)+v*Math.Cos(t))*sign;d.Normalize();return d;};
   double end=sweep*.86;for(int i=0;i<=seg;i++){double t=sign*end*i/seg;var c=at(t);var tg=tangent(t);Vector3D a1,b1;Basis(tg,out a1,out b1);for(int k=0;k<=n;k++){double q=2*Math.PI*k/n;var off=a1*Math.Cos(q)+b1*Math.Sin(q);mesh.Positions.Add(c+off*r);mesh.Normals.Add(off);}}
   for(int i=0;i<seg;i++)for(int k=0;k<n;k++){int p=i*(n+1)+k,q=(i+1)*(n+1)+k;mesh.TriangleIndices.Add(p);mesh.TriangleIndices.Add(q);mesh.TriangleIndices.Add(p+1);mesh.TriangleIndices.Add(p+1);mesh.TriangleIndices.Add(q);mesh.TriangleIndices.Add(q+1);}
   double tipAngle=sign*sweep;var head=ArrowMesh(at(tipAngle),tangent(tipAngle),R*(sweep-end)*1.05,r);if(head!=null){int o=mesh.Positions.Count;foreach(var p in head.Positions)mesh.Positions.Add(p);foreach(var nn in head.Normals)mesh.Normals.Add(nn);foreach(int idx in head.TriangleIndices)mesh.TriangleIndices.Add(o+idx);}return mesh;}
 }
}
