using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Media.Media3D;
namespace GearLab {
 public sealed partial class WpfScene {
  readonly Dictionary<string,Vector3D> offsets=new Dictionary<string,Vector3D>();
  public bool MoveParts;public int MoveAxis=-1;public event Action PlacementChanged;
  string movingPart;Vector3D dragOffset;Point dragPoint;
  public string SelectedId{get{return selected;}}
  public Vector3D Offset(string id){Vector3D v;return id!=null&&offsets.TryGetValue(id,out v)?v:new Vector3D();}
  public bool SetOffset(string id,Vector3D value){if(!Ready||id==null||!Nodes.Any(n=>n.Part.Id==id)||!Finite(value.X)||!Finite(value.Y)||!Finite(value.Z)||Math.Abs(value.X)>10000||Math.Abs(value.Y)>10000||Math.Abs(value.Z)>10000)return false;if(value.LengthSquared<1e-18)offsets.Remove(id);else offsets[id]=value;UpdatePose(Phase);NotifyPlacement();return true;}
  static bool Finite(double v){return !double.IsNaN(v)&&!double.IsInfinity(v);}
  void NotifyPlacement(){if(PlacementChanged!=null)PlacementChanged();}
  public void ResetPlacements(){movingPart=null;offsets.Clear();UpdatePose(Phase);NotifyPlacement();}
  public void SetMoveMode(bool enabled){MoveParts=enabled;if(!enabled)EndPartDrag(false);help.Text=enabled?"ЛКМ по детали — переместить · пустое место — камера · Shift+ЛКМ / СКМ — сдвиг камеры · Esc — отменить сдвиг":"ЛКМ — вращение · СКМ / Shift+ЛКМ — сдвиг · колесо — масштаб · F — вписать";}
  internal bool BeginPartDrag(string id,Point point){if(!Ready||id==null||!Nodes.Any(n=>n.Part.Id==id&&Visible(n)))return false;movingPart=id;dragOffset=Offset(id);dragPoint=point;return true;}
  internal Vector3D DragDelta(Point from,Point to){double scale=Camera.Width/Math.Max(1,Root.ActualWidth),dx=to.X-from.X,dy=to.Y-from.Y;if(MoveAxis<0)return Camera.Right*(dx*scale)-Camera.Up*(dy*scale);var axis=MoveAxis==0?new Vector3D(1,0,0):MoveAxis==1?new Vector3D(0,1,0):new Vector3D(0,0,1);double a=Vector3D.DotProduct(axis,Camera.Right),b=Vector3D.DotProduct(axis,Camera.Up),denom=a*a+b*b;if(denom<.01)return new Vector3D();return axis*((dx*a-dy*b)*scale/denom);}
  internal void DragPart(Point point){if(movingPart!=null)SetOffset(movingPart,dragOffset+DragDelta(dragPoint,point));}
  internal void EndPartDrag(bool cancel){string id=movingPart;movingPart=null;if(cancel&&id!=null)SetOffset(id,dragOffset);}
 }
}
