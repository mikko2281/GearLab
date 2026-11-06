using System;using System.Linq;using System.Text.RegularExpressions;using System.Collections.Generic;using System.Windows;using System.Windows.Media.Media3D;

namespace GearLab {

 public sealed class PartMotion {

  public CadPart Bearing;public PartMotion BearingBase;public TrainMotion Train;public TransmissionMotion Transmission;public string Kind="fixed";public int Stage,Planet;public Point3D Centre;public double Radius,Location,CustomRatio;public Result Result;public string RobotPartId;public Matrix3D RobotInverse=Matrix3D.Identity;

  public static PartMotion Bind(Result result,CadPart part){if(result.Robot==null&&result.Train==null&&part.BearingSource!=null)return new PartMotion{Result=result,Bearing=part,BearingBase=Bind(result,part.BearingSource),Kind="bearingDetail"};var motion=new PartMotion{Result=result};var p=result.P;string id=part.Id;var first=part.Sections[0];var centre=first.Outer.Center();motion.Centre=new Point3D(centre.X,centre.Y,0);

   if(result.Train!=null){TrainMotion tm;if(result.TrainMotions.TryGetValue(id,out tm)){motion.Train=tm;motion.Kind="train";}return motion;}if(result.Engineering!=null){TransmissionMotion tm;if(result.TransmissionMotions.TryGetValue(id,out tm)){motion.Transmission=tm;motion.Kind="transmission";}return motion;}if(result.Wave!=null){if(result.Wave.Rolling){motion.Centre=new Point3D();if(id.StartsWith("wave_ball")){motion.Kind="ptkBall";motion.Planet=int.Parse(id.Substring(9));motion.Centre=WaveFastModel.BallCentre(p,motion.Planet,0);}else motion.Kind=id=="wave_output_bearing"?"fixed":id.StartsWith("wave_output")?"waveOutput":id=="wave_generator"||id=="wave_input"||id=="wave_input_bearing"?"input":"fixed";return motion;}motion.Centre=new Point3D();motion.Kind=id.EndsWith("_output")||id.Contains("_output_bolt")?"waveOutput":id.EndsWith("_flex")?"waveFlex":id.EndsWith("_generator")||id.EndsWith("_bearing")?"input":"fixed";return motion;}if(result.Robot!=null){motion.Kind="robot";motion.RobotPartId=part.Id;motion.RobotInverse=part.Placement;motion.RobotInverse.Invert();return motion;}if(result.Mechanism!=null){var item=result.Mechanism.Find(part.Id);var shaft=result.Mechanism.Shaft(item);MechanismShaftState state;if(item!=null&&item.Kind!="bearing"&&shaft!=null&&result.MechanismSolution!=null&&result.MechanismSolution.Valid&&result.MechanismSolution.Shafts.TryGetValue(shaft.Id,out state)&&state.Connected){motion.Kind="mechanism";motion.CustomRatio=state.SpeedRatio;motion.Centre=new Point3D(shaft.X,shaft.Y,0);}return motion;}

   if(p.Kind==0){

    if(id.Contains("CYCLOID_DISC_")){motion.Kind=id.Contains("DISC_B")?"discB":"discA";motion.Centre=new Point3D(motion.Kind=="discA"?p.Eccentric:-p.Eccentric,0,0);}

    else if(id.Contains("ECCENTRIC_SHAFT")||id.Contains("ECCENTRIC_BEARING"))motion.Kind="input";

    else if(p.RingOutput&&(part.Role=="housing"||id.Contains("CASE_TIE_BOLT")||id.Contains("CASE_NUT")||id.Contains("RING_ROLLER")||id.Contains("ROLLER_AXLE")))motion.Kind="ring";

    else if(!p.RingOutput&&(id.Contains("OUTPUT_CARRIER_SHAFT")||id.Contains("OUTPUT_PIN")))motion.Kind="carrier";

   }else if(p.Kind==1||p.Kind==2){

    if(id.Contains("INPUT_SHAFT")||id.Contains("DRIVE_GEAR")||id.Contains("DRIVE_PULLEY"))motion.Kind="input";

    else if(id.Contains("OUTPUT_SHAFT")||id.Contains("DRIVEN_GEAR")||id.Contains("DRIVEN_PULLEY")){motion.Kind=p.Kind==1?"pairOutput":"beltOutput";motion.Centre=new Point3D(p.Kind==1?result.Spacing:p.Center,0,0);}

   }else{

    if(id.Contains("INPUT_SHAFT"))motion.Kind="input";

    var match=Regex.Match(id,@"STAGE_(\d+)_(SUN|PLANET_(\d+)|CARRIER_SHAFT)");

    if(match.Success){motion.Stage=int.Parse(match.Groups[1].Value)-1;string what=match.Groups[2].Value;if(what=="SUN")motion.Kind="sun";else if(what=="CARRIER_SHAFT")motion.Kind="stageCarrier";else{motion.Kind="planet";motion.Planet=int.Parse(match.Groups[3].Value)-1;}}

    else if(id.Contains("PLANET_BUSH")||id.Contains("PLANET_AXLE")){

     var suns=result.Model.Parts.Where(q=>q.Id.Contains("_SUN")).ToList();motion.Stage=suns.Select((q,i)=>new{I=i,D=Math.Abs(q.Sections[0].Bottom-first.Bottom)}).OrderBy(q=>q.D).First().I;

     motion.Kind=id.Contains("PLANET_AXLE")?"planetAxle":"planet";double angle=Math.Atan2(centre.Y,centre.X);motion.Planet=((int)Math.Round(angle/Engine.Tau*p.Planets)+p.Planets)%p.Planets;

    }

    if(motion.Kind=="planet"||motion.Kind=="planetAxle"){var st=motion.StageResult();motion.Radius=p.Module*(st.Z1+st.Zp)/2;motion.Location=Engine.Tau*motion.Planet/p.Planets;motion.Centre=new Point3D(motion.Radius*Math.Cos(motion.Location),motion.Radius*Math.Sin(motion.Location),0);}

   }return motion;

  }

  Result StageResult(){return Result.Stages.Count>0?Result.Stages[Stage]:Result;}

  public bool Moving{get{return Kind!="fixed";}}

  public Matrix3D At(double phase,double explode){if(Bearing!=null){var owner=Bearing.BearingSource;var s=owner.Sections[0];double gain=owner.Id.ToUpperInvariant().Contains("OUT")||owner.Id.ToUpperInvariant().Contains("CASE")?TrainSolver.Gain(Result):1;double factor=Bearing.BearingElement==0?0:Bearing.BearingElement==1?gain:gain*.5*(1-Bearing.BearingBall/(2*Bearing.BearingPitch));if(BearingBase.Moving)factor=0;var frame=RobotMath.Translation(s.Outer.X,s.Outer.Y,0)*owner.Placement;var inv=frame;inv.Invert();return inv*RobotMath.Rotation(new Vector3D(0,0,1),phase*factor*180/Math.PI)*frame*BearingBase.At(phase,explode);}if(Train!=null)return Train.At(phase,explode);if(Transmission!=null){var tm=Transmission.At(Result.P.Kind==12?TransmissionSizing.RackPhase(Result.P,phase):phase);tm.Translate(new Vector3D(0,0,explode));return tm;}if(Kind=="robot")return RobotInverse*Result.Robot.Pose(RobotPartId,phase,true);var p=Result.P;double angle=0,x=Centre.X,y=Centre.Y;

   if(Kind=="ptkBall"){var target=WaveFastModel.BallCentre(p,Planet,phase);return Around(-phase/Result.Actual,Centre,target,explode);}
   if(Kind=="waveFlex"||Kind=="waveOutput")angle=-phase/Result.Wave.Ratio;

   else if(Kind=="mechanism")angle=phase*CustomRatio;

   else if(Kind=="input")angle=phase;

   else if(Kind=="ring")angle=Engine.RingAngle(p,phase);

   else if(Kind=="carrier")angle=Engine.DiscAngle(p,phase);

   else if(Kind=="pairOutput")angle=-phase/Result.Actual;

   else if(Kind=="beltOutput")angle=phase/Result.Actual;

   else if(Kind=="discA"||Kind=="discB"){double sign=Kind=="discA"?1:-1;angle=Engine.DiscAngle(p,phase);x=sign*p.Eccentric*Math.Cos(phase);y=sign*p.Eccentric*Math.Sin(phase);}

   else if(Kind=="sun")angle=Engine.StageInput(Result,phase,Stage);

   else if(Kind=="stageCarrier")angle=Engine.StageInput(Result,phase,Stage)/StageResult().Actual;

   else if(Kind=="planet"||Kind=="planetAxle"){var st=StageResult();double input=Engine.StageInput(Result,phase,Stage),carrier=input/st.Actual;x=Radius*Math.Cos(Location+carrier);y=Radius*Math.Sin(Location+carrier);angle=Kind=="planetAxle"?carrier:Engine.PlanetAngle(st,input,Planet)-Engine.PlanetAngle(st,0,Planet);}

   if(Kind=="input"||Kind=="ring"||Kind=="carrier"||Kind=="sun"||Kind=="stageCarrier")return Around(angle,new Point3D(),new Point3D(),explode);

   return Around(angle,Centre,new Point3D(x,y,0),explode);

  }

  public static Matrix3D Around(double angle,Point3D centre,Point3D target,double z){double c=Math.Cos(angle),s=Math.Sin(angle);return new Matrix3D(c,s,0,0,-s,c,0,0,0,0,1,0,target.X-centre.X*c+centre.Y*s,target.Y-centre.X*s-centre.Y*c,z,1);}

 }

 public sealed class OrbitCamera {

  public event Action Changed;public Point3D Target;public double Yaw=-Math.PI/4,Pitch=Math.PI/5,Width=150,Span=100;public OrthographicCamera Camera=new OrthographicCamera();

  public Vector3D Direction{get{return new Vector3D(Math.Cos(Pitch)*Math.Cos(Yaw),Math.Cos(Pitch)*Math.Sin(Yaw),Math.Sin(Pitch));}}

  public Vector3D Right{get{var r=Vector3D.CrossProduct(-Direction,new Vector3D(0,0,1));r.Normalize();return r;}}

  public Vector3D Up{get{var u=Vector3D.CrossProduct(Right,-Direction);u.Normalize();return u;}}

  public void Update(){Camera.Position=Target+Direction*Math.Max(Span*3,10);Camera.LookDirection=-Direction;Camera.UpDirection=Up;Camera.Width=Width;Camera.NearPlaneDistance=.01;Camera.FarPlaneDistance=Math.Max(1000,Span*15);if(Changed!=null)Changed();}

  public void Fit(Rect3D bounds,double aspect){if(bounds.IsEmpty)return;Target=new Point3D(bounds.X+bounds.SizeX/2,bounds.Y+bounds.SizeY/2,bounds.Z+bounds.SizeZ/2);Span=Math.Max(1,Math.Sqrt(bounds.SizeX*bounds.SizeX+bounds.SizeY*bounds.SizeY+bounds.SizeZ*bounds.SizeZ));double horizontal=0,vertical=0;for(int i=0;i<8;i++){var corner=new Point3D(bounds.X+((i&1)!=0?bounds.SizeX:0),bounds.Y+((i&2)!=0?bounds.SizeY:0),bounds.Z+((i&4)!=0?bounds.SizeZ:0));var delta=corner-Target;horizontal=Math.Max(horizontal,Math.Abs(Vector3D.DotProduct(delta,Right)));vertical=Math.Max(vertical,Math.Abs(Vector3D.DotProduct(delta,Up)));}Width=Math.Max(1,2*Math.Max(horizontal,vertical*aspect))*1.12;Update();}

  public void Orbit(double dx,double dy){Yaw-=dx*AppSettings.Current.OrbitSensitivity/1000;Pitch=Math.Max(-Math.PI/2+.025,Math.Min(Math.PI/2-.025,Pitch+dy*AppSettings.Current.OrbitSensitivity/1000));Update();}

  public void Pan(double dx,double dy,double pixelsWide){double scale=Width/Math.Max(1,pixelsWide);Target+=Right*(-dx*scale)+Up*(dy*scale);Update();}

  public void Zoom(double wheel,double x,double y,double pixelsWide,double pixelsHigh){double old=Width;Width=Math.Max(Span*.015,Math.Min(Span*30,Width*Math.Exp(-wheel*.0014)));double difference=(old-Width)/Math.Max(1,pixelsWide);Target+=Right*((x-pixelsWide/2)*difference)+Up*((pixelsHigh/2-y)*difference);Update();}

  public Point3D ScreenPoint(double x,double y,double width,double height){return Target+Right*((x-width/2)*Width/width)+Up*((height/2-y)*Width/width);}

  public void Preset(string value){if(value=="Сверху"){Yaw=-Math.PI/2;Pitch=Math.PI/2;}else if(value=="Спереди"){Yaw=-Math.PI/2;Pitch=0;}else if(value=="Справа"){Yaw=0;Pitch=0;}else{Yaw=-Math.PI/4;Pitch=Math.PI/5;}Update();}

 }

}

