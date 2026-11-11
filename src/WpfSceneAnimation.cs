using System;using System.Diagnostics;using System.Linq;using System.Collections.Generic;using System.Windows.Media.Media3D;using System.Windows.Threading;
namespace GearLab {
 // Анимации показа: сборка модели по деталям и плавный разнесённый вид.
 // Включаются только в обычном окне программы; тесты, экспорт и снимки работают без них и получают итоговое состояние сразу.
 public static class SceneAnimation {
  public static bool Enabled;
  static double Clamp(double t){return t<0?0:t>1?1:t;}
  public static double EaseOut(double t){double u=1-Clamp(t);return 1-u*u*u;}
  public static double EaseInOut(double t){t=Clamp(t);return t<.5?4*t*t*t:1-Math.Pow(-2*t+2,3)/2;}
  // Лёгкий «доезд» с небольшим перелётом в конце.
  public static double Settle(double t){t=Clamp(t);const double c=1.25;double u=t-1;return 1+(c+1)*u*u*u+c*u*u;}
 }
 public sealed partial class WpfScene {
  const double ExplodeSeconds=.85,AssemblePartSeconds=.5,AssembleSpreadSeconds=1.1;
  readonly Stopwatch motionClock=Stopwatch.StartNew();DispatcherTimer motionTimer;
  bool exploding,explodeOpening,assembling,cameraTween;double explodeStart,explodeProgress,assembleStart,assembleSeconds,assembleStep,assembleDrop;
  Dictionary<string,double> explodeFrom;int lastIndex;
  void CountIndexes(){lastIndex=0;foreach(var n in Nodes)if(n.Index>lastIndex)lastIndex=n.Index;}
  Point3D cameraFromTarget,cameraToTarget;double cameraFromWidth,cameraToWidth,cameraFromSpan,cameraToSpan;
  public bool Animating{get{return exploding||assembling;}}

  bool CanAnimate{get{return SceneAnimation.Enabled&&!disposed&&Ready&&Axis<0&&Nodes.Count>0&&Root.ActualWidth>1;}}
  void StartMotion(){if(motionTimer==null){motionTimer=new DispatcherTimer(DispatcherPriority.Render,Root.Dispatcher);motionTimer.Interval=TimeSpan.FromMilliseconds(15);motionTimer.Tick+=MotionTick;}motionTimer.Start();}
  void StopMotion(){exploding=false;assembling=false;cameraTween=false;explodeFrom=null;if(motionTimer!=null)motionTimer.Stop();}
  void FinishPresentation(){bool fit=exploding&&cameraTween;StopMotion();if(Ready){UpdatePose(Phase,true);if(fit)Fit();}}
  internal void CancelCameraTween(){cameraTween=false;}

  // Смещение детали вдоль оси сборки. Во время анимации детали расходятся по очереди, начиная с верхней.
  double ExplosionOf(SceneNode node){double full=node.Index*2.5;if(!exploding)return Exploded?full:0;double from;from=explodeFrom!=null&&explodeFrom.TryGetValue(node.Part.Id,out from)?from:(explodeOpening?0:full);double order=lastIndex<1?0:(double)node.Index/lastIndex;if(explodeOpening)order=1-order;double f=SceneAnimation.EaseInOut(explodeProgress*1.6-.6*order);return from+((Exploded?full:0)-from)*f;}

  // Положение детали на экране во время сборки: до своей очереди деталь не видна, затем опускается на место.
  Matrix3D AssemblyPose(SceneNode node,Matrix3D pose){double local=(assembleSeconds-node.Index*assembleStep)/AssemblePartSeconds;var b=node.Frozen==null?Rect3D.Empty:node.Frozen.Bounds;if(b.IsEmpty)return pose;var centre=pose.Transform(new Point3D(b.X+b.SizeX/2,b.Y+b.SizeY/2,b.Z+b.SizeZ/2));if(local<=0){pose.ScaleAt(new Vector3D(.0001,.0001,.0001),centre);return pose;}if(local>=1)return pose;double scale=.6+.4*SceneAnimation.Settle(local),lift=(1-SceneAnimation.EaseOut(local))*assembleDrop;pose.ScaleAt(new Vector3D(scale,scale,scale),centre);pose.Translate(new Vector3D(0,0,lift));return pose;}

  void BeginAssembly(){if(!CanAnimate)return;CountIndexes();exploding=false;assembleStep=Math.Min(.07,AssembleSpreadSeconds/Math.Max(1,lastIndex+1));assembleDrop=Math.Max(15,Camera.Span*.3);assembleStart=motionClock.Elapsed.TotalSeconds;assembleSeconds=0;assembling=true;UpdatePose(Phase,true);StartMotion();}

  void BeginExplode(bool enabled){CountIndexes();var from=Nodes.ToDictionary(n=>n.Part.Id,n=>ExplosionOf(n));exploding=false;assembling=false;var target=Camera.Target;double width=Camera.Width,span=Camera.Span;Exploded=enabled;UpdatePose(Phase,true);Fit();cameraToTarget=Camera.Target;cameraToWidth=Camera.Width;cameraToSpan=Camera.Span;cameraFromTarget=target;cameraFromWidth=width;cameraFromSpan=span;Camera.Target=target;Camera.Width=width;Camera.Span=span;Camera.Update();explodeOpening=enabled;explodeFrom=from;explodeProgress=0;explodeStart=motionClock.Elapsed.TotalSeconds;cameraTween=true;exploding=true;UpdatePose(Phase,true);StartMotion();}

  void MotionTick(object sender,EventArgs e){if(disposed||!Ready){StopMotion();return;}double now=motionClock.Elapsed.TotalSeconds;bool finishedExplode=false;
   if(assembling){assembleSeconds=now-assembleStart;if(assembleSeconds>=lastIndex*assembleStep+AssemblePartSeconds)assembling=false;}
   if(exploding){explodeProgress=Math.Min(1,(now-explodeStart)/ExplodeSeconds);if(cameraTween){double k=SceneAnimation.EaseInOut(explodeProgress);Camera.Target=cameraFromTarget+(cameraToTarget-cameraFromTarget)*k;Camera.Width=cameraFromWidth+(cameraToWidth-cameraFromWidth)*k;Camera.Span=cameraFromSpan+(cameraToSpan-cameraFromSpan)*k;Camera.Update();}if(explodeProgress>=1){exploding=false;finishedExplode=true;}}
   UpdatePose(Phase,!Animating);if(finishedExplode){explodeFrom=null;if(cameraTween)Fit();}if(!Animating&&motionTimer!=null)motionTimer.Stop();}
 }
}
