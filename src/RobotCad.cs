using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Threading;using System.Windows.Media.Media3D;

namespace GearLab {

 public sealed class RobotCadEntry {

  public string Id,JointId,OwnerId,Category;public MechanismPart Shape;public CadPart Part;public Matrix3D Local=Matrix3D.Identity,NativeBasePlacement=Matrix3D.Identity;public bool InputSide,Guide,Printable;public double Density=1250,MassOverride=-1;public PartMotion NativeMotion;public double NativeRatio=1,BearingGain;

 }

 public sealed class RobotBuild {

  public RobotDocument Document;public CadAssembly Model=new CadAssembly();public Dictionary<string,RobotCadEntry> Entries=new Dictionary<string,RobotCadEntry>();public List<string> Notes=new List<string>();public Dictionary<string,RobotPose> Current;double cachedPhase=double.NaN;bool cachedAnimation;Dictionary<string,Matrix3D> poses;

  public Matrix3D Pose(string id,double phase,bool animate){if(poses==null||cachedPhase!=phase||cachedAnimation!=animate){var fk=RobotKinematics.Evaluate(Document,phase,animate);poses=new Dictionary<string,Matrix3D>();foreach(var entry in Entries.Values){var p=fk[entry.JointId];var local=entry.Local;if(entry.BearingGain!=0)local=RobotMath.Rotation(new Vector3D(0,0,1),p.Value*entry.BearingGain)*local;if(entry.NativeMotion!=null)local=entry.NativeBasePlacement*entry.NativeMotion.At(p.Value*Math.PI/180*entry.NativeRatio,0)*local;poses[entry.Id]=local*(entry.InputSide?p.Base:p.Output);}cachedPhase=phase;cachedAnimation=animate;}Matrix3D result;return poses.TryGetValue(id,out result)?result:Matrix3D.Identity;}

  public void InvalidatePose(){poses=null;cachedPhase=double.NaN;cachedAnimation=false;}

  public double Mass(RobotCadEntry e){return e.Guide?0:e.MassOverride>=0?e.MassOverride:e.Part.Volume()*1e-9*e.Density;}

  public static Point3D Centre(CadPart p){if(p.NativeStep!=null)return p.NativeCentre;if(p.MeshOverride!=null){var mesh=p.MeshOverride;double signed=0;var centreVector=new Vector3D();for(int i=0;i<mesh.Indices.Length;i+=3){var a=RobotMath.Vector(mesh.Points[mesh.Indices[i]]);var b=RobotMath.Vector(mesh.Points[mesh.Indices[i+1]]);var c=RobotMath.Vector(mesh.Points[mesh.Indices[i+2]]);double v=Vector3D.DotProduct(a,Vector3D.CrossProduct(b,c))/6;signed+=v;centreVector+=(a+b+c)*(v/4);}return Math.Abs(signed)>1e-9?RobotMath.Point(centreVector/signed):new Point3D();}double volume=0,x=0,y=0,z=0;foreach(var s in p.Sections){Func<CadLoop,Point3D> centre=l=>{if(l.Circle)return new Point3D(l.X,l.Y,0);double polyArea=0,cx=0,cy=0;for(int i=0;i<l.Points.Length;i++){var a=l.Points[i];var b=l.Points[(i+1)%l.Points.Length];double cross=(double)a.X*b.Y-(double)b.X*a.Y;polyArea+=cross;cx+=(a.X+b.X)*cross;cy+=(a.Y+b.Y)*cross;}return Math.Abs(polyArea)<1e-12?new Point3D():new Point3D(cx/(3*polyArea),cy/(3*polyArea),0);};double outer=s.Outer.SurfaceArea(),area=outer-s.Holes.Sum(h=>h.SurfaceArea());var c=centre(s.Outer);double sx=c.X*outer,sy=c.Y*outer;foreach(var h in s.Holes){c=centre(h);sx-=c.X*h.SurfaceArea();sy-=c.Y*h.SurfaceArea();}double height=s.Top-s.Bottom,v=area*height;x+=sx*height;y+=sy*height;z+=v*(s.Bottom+s.Top)/2;volume+=v;}return volume>1e-9?new Point3D(x/volume,y/volume,z/volume):new Point3D();}

  public CadAssembly ForExport(bool printableOnly,string partId){var m=new CadAssembly();foreach(var e in Entries.Values.Where(e=>!e.Guide&&(!printableOnly||e.Printable)&&(string.IsNullOrEmpty(partId)||e.Id==partId))){var j=Document.Find(e.JointId);if(e.Id.Contains("_proc_")&&j!=null&&j.BodyGenerator!=null&&j.BodyGenerator.Enabled&&j.GeneratedBody!=null&&j.GeneratedBody.Fingerprint!=ProceduralBodySpec.Fingerprint(Document,j,j.BodyGenerator))throw new Exception("Корпус "+j.Name+" ещё перестраивается. Дождитесь актуальной геометрии перед экспортом.");m.Parts.Add(e.Part);}if(m.Parts.Count==0)m.Errors.Add("Нет деталей для экспорта.");return m;}

 }

 public static partial class RobotCad {

  static MechanismPart Round(string name,double diameter,double length,double bore,double z){return new MechanismPart{Kind="shaft",Name=name,Diameter=diameter,Length=length,Bore=bore,Z=z};}

  static MechanismPart MotorRound(RobotJoint j,string name,double d,double height,double z){var p=Round(name,d,height,0,z);p.X=j.MotorOffsetX;p.Y=j.MotorOffsetY;return p;}

  static MechanismPart Box(string name,double x,double y,double z,double height){return new MechanismPart{Kind="block",Name=name,SizeX=x,SizeY=y,Length=height,Z=z};}

  static IEnumerable<MechanismHole> Pattern(double radius,double diameter,int count,double angle){return Enumerable.Range(0,count).Select(i=>new MechanismHole{Name="Крепёж "+(i+1),X=radius*Math.Cos(angle+Engine.Tau*i/count),Y=radius*Math.Sin(angle+Engine.Tau*i/count),Diameter=diameter});}

  static void MotorHoles(MechanismPart shape,RobotJoint j){for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)shape.Holes.Add(new MechanismHole{Name="Крепление мотора",X=j.MotorOffsetX+a*j.MotorBoltPitch/2,Y=j.MotorOffsetY+b*j.MotorBoltPitch/2,Diameter=j.MotorBolt+2*j.Fit});}

  static List<MechanismPoint> Capsule(double length,double left,double right){double angle=Math.Acos(Math.Max(-.95,Math.Min(.95,(left-right)/length)));var p=new List<MechanismPoint>();for(int i=0;i<=32;i++){double a=angle+(Engine.Tau-2*angle)*i/32;p.Add(new MechanismPoint(left*Math.Cos(a),left*Math.Sin(a)));}for(int i=0;i<=32;i++){double a=Engine.Tau-angle+2*angle*i/32;p.Add(new MechanismPoint(length+right*Math.Cos(a),right*Math.Sin(a)));}return p;}

  static RobotCadEntry Add(RobotBuild build,RobotJoint j,string key,MechanismPart original,Matrix3D local,bool input,string category,bool printable,double density,bool guide=false,double mass=-1){string id=j.Id+"_"+key;var edit=build.Document.BodyEdits.FirstOrDefault(e=>e.PartId==id);var shape=edit!=null&&edit.Shape!=null?edit.Shape.CloneShape():original;shape.Id=id;if(edit==null||edit.Shape==null)shape.Density=Math.Max(100,density);var project=new MechanismProject();project.Parts.Add(shape);var errors=MechanismCad.Validate(project);if(errors.Count>0)throw new Exception(j.Name+" / "+shape.Name+": "+errors[0]);var part=MechanismCad.BuildPart(project,shape);if(key=="skin_tube"||key=="skin_panel")foreach(var section in part.Sections)section.Outer.SmoothSides=true;part.Reference=!printable;part.Role=category=="Крепёж"?"fastener":category=="Опоры"?"bearing":category=="Проводка"?"wire":category=="Мотор"?"reference":category=="Передача"?part.Role:"housing";if(guide)part.Color=key.StartsWith("end")?Color.FromArgb(246,179,55):Color.FromArgb(51,143,213);else if(category=="Корпус")part.Color=j.BodyStyle==RobotBodyStyle.Открытая_рама?Color.FromArgb(91,155,205):j.BodyStyle==RobotBodyStyle.Облегчённые_пластины?Color.FromArgb(73,166,147):Color.FromArgb(115,137,182);else if(category=="Проводка")part.Color=Color.FromArgb(222,115,55);if((j.Appearance==RobotAppearance.Чёрный_оранжевый||j.BodyStyle==RobotBodyStyle.Закрытый_трубчатый)&&category=="Корпус")part.Color=key=="beam_cover"||key=="hub"||key=="skin_panel"||key=="skin_trim"||key=="skin_badge"?Color.FromArgb(240,115,30):Color.FromArgb(48,51,55);if(edit!=null&&edit.Mesh!=null){var meshErrors=edit.Mesh.Validate(true);if(meshErrors.Count>0)throw new Exception(shape.Name+": "+meshErrors[0]);part.MeshOverride=edit.Mesh.ToScene();}if(edit!=null&&edit.NativeStep!=null){if(edit.NativeVolume<=0||edit.NativeCentre==null||edit.NativeCentre.Length!=3||!edit.NativeCentre.All(MechanismCad.Finite))throw new Exception("Повреждены данные CAD-детали.");part.NativeStep=edit.NativeStep;part.NativeVolume=edit.NativeVolume;part.NativeCentre=new Point3D(edit.NativeCentre[0],edit.NativeCentre[1],edit.NativeCentre[2]);part.MeshOverride=CADMeshCodec.Decode(edit.NativePreview);if(edit.NativeShift!=null&&edit.NativeShift.Length==3)local=RobotMath.Translation(edit.NativeShift[0],edit.NativeShift[1],edit.NativeShift[2])*local;}var entry=new RobotCadEntry{Id=id,JointId=j.Id,OwnerId=input?j.ParentId:j.Id,Shape=shape,Part=part,Local=local,InputSide=input,Guide=guide,Category=category,Printable=printable,Density=shape.Density,MassOverride=mass};build.Entries[id]=entry;build.Model.Parts.Add(part);return entry;}

  static void Bolt(RobotBuild b,RobotJoint j,string key,double x,double y,double z,double length,double diameter,Matrix3D axis,bool input){var shaft=Round("Винт Ø"+Engine.F(diameter)+" · без резьбы",diameter,length,0,z);shaft.X=x;shaft.Y=y;Add(b,j,key,shaft,axis,input,"Крепёж",false,7850);var head=Round("Головка винта",diameter*1.8,diameter*.7,0,z+length);head.X=x;head.Y=y;Add(b,j,key+"_head",head,axis,input,"Крепёж",false,7850);}

  static void Cable(RobotBuild b,RobotJoint j,string key,Point3D a,Point3D c,bool input){var d=c-a;if(d.Length<.1)return;var frame=RobotMath.Frame(d)*RobotMath.Translation(a.X,a.Y,a.Z);Add(b,j,key,Round("Пучок проводов · габарит",j.CableDiameter,d.Length,0,0),frame,input,"Проводка",false,1100);}

  public static RobotBuild Build(RobotDocument document){var build=new RobotBuild{Document=document};build.Model.Errors.AddRange(document.Validate());if(build.Model.Errors.Count>0)return build;build.Current=RobotKinematics.Evaluate(document,0,false);try{foreach(var j in document.Ordered())BuildJoint(build,j);AddReferenceTool(build);ExpandBearings(build);foreach(var e in build.Entries.Values)e.Part.Placement=build.Pose(e.Id,0,false);build.Model.Errors.AddRange(RobotConnections.Validate(build));if(document.BodyEdits.Count>0)build.Notes.Add("Изменённые детали сохраняются в проекте. После смены мотора/габаритов проверьте переопределённые посадки и крепёж либо восстановите исходную деталь.");}catch(Exception ex){build.Model.Errors.Add(ex.Message);}return build;}

  static void ExpandBearings(RobotBuild build){foreach(var old in build.Entries.Values.Where(e=>e.Part.Role=="bearing"&&e.Part.BearingSource==null&&e.Part.Analytic==null&&e.Part.NativeStep==null&&e.Part.MeshOverride==null&&e.Part.Sections.Count==1&&e.Part.Sections[0].Holes.Count==1).ToArray()){
   var parts=BearingDetail.Create(old.Part);if(parts.Count==1&&parts[0]==old.Part)continue;build.Entries.Remove(old.Id);build.Model.Parts.Remove(old.Part);foreach(var q in parts){var entry=new RobotCadEntry{Id=q.Id,JointId=old.JointId,OwnerId=old.OwnerId,Part=q,Local=q.Placement*old.Local,NativeBasePlacement=old.NativeBasePlacement,InputSide=old.InputSide,Guide=old.Guide,Category=old.Category,Printable=false,Density=7850,NativeMotion=old.NativeMotion,NativeRatio=old.NativeRatio};var joint=build.Document.Find(old.JointId);if(joint!=null&&joint.BodyGenerator!=null&&joint.BodyGenerator.IndustrialDesign){if(q.BearingElement==1){entry.InputSide=false;entry.OwnerId=old.JointId;}else if(q.BearingElement==2)entry.BearingGain=.5*(1-q.BearingBall/(2*q.BearingPitch));}q.Placement=System.Windows.Media.Media3D.Matrix3D.Identity;build.Entries[entry.Id]=entry;build.Model.Parts.Add(q);}}
  }

  static void BuildJoint(RobotBuild b,RobotJoint j){if(j.BodyGenerator!=null&&j.BodyGenerator.Variant=="Reference67")BuildReference67(b,j);else BuildLegacyJoint(b,j);if(j.BodyGenerator!=null&&j.BodyGenerator.Enabled){if(j.BodyGenerator.Connected&&(j.GeneratedBody==null||j.GeneratedBody.Result.Mates==null)){foreach(string id in b.Entries.Values.Where(e=>e.JointId==j.Id&&(e.Category=="Корпус"||e.Category=="Крепёж"||e.Id.EndsWith("_output_shaft"))).Select(e=>e.Id).ToArray()){b.Model.Parts.Remove(b.Entries[id].Part);b.Entries.Remove(id);}b.Notes.Add(j.Name+": несостыкованный старый корпус скрыт; требуется построение соединённого узла.");}else if(j.GeneratedBody!=null)ApplyGenerated(b,j);}}

  static void BuildLegacyJoint(RobotBuild b,RobotJoint j){var axis=RobotMath.Frame(j.Axis);double wall=j.Wall,fit=j.Fit,outer=j.GearboxDiameter,caseR=outer/2,boltR=caseR-wall/2,boltD=j.BoltDiameter+2*fit,lid=Math.Max(wall,j.BearingWidth+2),length=j.GearboxLength;bool moving=j.Mode!=RobotJointMode.Неподвижный;

   // The body generators preserve dimensioned motor pilots, four-bolt patterns, bearing seats and wire ports.

   if(j.MotorTemplate==RobotMotorTemplate.STEPPERONLINE_по_вашему_STEP)b.Notes.Add(j.Name+": монтажный шаблон измерен по STEP пользователя: 41,9 × 37,5 мм, крепёж 31,3 мм, бурт Ø22 × 2, вал Ø4,65 × 23,2. D-срез, фаски, разъём и резьба здесь упрощены; масса и момент не определены STEP и требуют данных мотора. Сверьте реальные размеры до изготовления.");

   if(moving){if(j.TrainSettings==null&&(j.ReducerSettings==null||j.ReducerSettings.Kind<5)){var motor=Box("Мотор · монтажный габарит",j.MotorFrame,j.MotorFrame,-length-wall-j.MotorLength,j.MotorLength);motor.X=j.MotorOffsetX;motor.Y=j.MotorOffsetY;Add(b,j,"motor",motor,axis,true,"Мотор",false,7850,false,j.MotorMass);Add(b,j,"motor_pilot",MotorRound(j,"Центрирующий бурт мотора",j.MotorPilot,j.MotorPilotHeight,-length-wall),axis,true,"Мотор",false,7850,false,0);Add(b,j,"motor_shaft",MotorRound(j,"Вал мотора · круглый монтажный габарит",j.MotorShaft,j.MotorShaftLength,-length-wall),axis,true,"Мотор",false,7850,false,0);}

    var back=Round("Моторная монтажная плита",outer,wall,0,-length-wall);back.Holes.Add(new MechanismHole{Name="Бурт мотора",X=j.MotorOffsetX,Y=j.MotorOffsetY,Diameter=j.MotorPilot+2*fit});MotorHoles(back,j);back.Holes.AddRange(Pattern(boltR,boltD,4,0));if(j.Cables)back.Holes.Add(new MechanismHole{Name="Вывод проводов",X=j.MotorOffsetX,Y=j.MotorOffsetY+j.MotorFrame/2+wall/2,Diameter=j.CableDiameter+2*j.CableClearance});Add(b,j,"motor_plate",back,axis,true,"Корпус",true,j.Density);

    if(j.ReducerSettings==null&&j.CustomTransmission==null&&j.TrainSettings==null){var shell=Round("Габарит корпуса передачи · внутренний механизм не выбран",outer,Math.Max(3,length-lid),outer-2*wall,-length);shell.Holes.AddRange(Pattern(boltR,boltD,4,0));Add(b,j,"case",shell,axis,true,"Корпус",true,j.Density);b.Notes.Add(j.Name+": передача i="+Engine.F(j.Reduction)+" пока задана габаритом. Выберите реальную передачу через «Редуктор» или «Свой механизм».");}else AddTransmission(b,j,axis);

    var front=Round("Крышка с посадкой под подшипник",outer,lid,0,-lid);front.Steps.Add(new MechanismStep{Length=lid-j.BearingWidth,Diameter=outer,Bore=j.BearingInner+2*fit});front.Steps.Add(new MechanismStep{Length=j.BearingWidth,Diameter=outer,Bore=j.BearingOuter+2*fit});front.Holes.AddRange(Pattern(boltR,boltD,4,0));Add(b,j,"bearing_plate",front,axis,true,"Корпус",true,j.Density);Add(b,j,"bearing",Round("Подшипник · размерный шаблон",j.BearingOuter,j.BearingWidth,j.BearingInner,-j.BearingWidth),axis,true,"Опоры",false,7850);Add(b,j,"output_shaft",Round("Выходная ось / шейка",j.BearingInner,lid+15+(j.BodyStyle==RobotBodyStyle.Закрытый_трубчатый?3*wall:0),0,-lid-5),axis,false,"Опоры",false,7850);

    var points=Pattern(boltR,boltD,4,0).ToArray();for(int i=0;i<points.Length;i++)Bolt(b,j,"case_bolt_"+i,points[i].X,points[i].Y,-length-wall, length+wall, j.BoltDiameter,axis,true);int count=0;foreach(var h in back.Holes.Where(h=>h.Name=="Крепление мотора"))Bolt(b,j,"motor_bolt_"+(count++),h.X,h.Y,-length-wall-4,wall+4,j.MotorBolt,axis,true);

   }

   if(j.BodyStyle==RobotBodyStyle.Закрытый_трубчатый){Enclosure(b,j,axis,moving);}else{

   double rootR=Math.Max(j.BeamWidth/2,j.BearingOuter/2+j.BoltDiameter+5),endR=j.BeamWidth/2;var children=b.Document.Joints.Where(c=>c.ParentId==j.Id).ToList();foreach(var c in children)endR=Math.Max(endR,c.GearboxDiameter/2+c.Wall);double hub=4,beamZ=hub+fit;var beamFrame=RobotMath.Beam(new Vector3D(j.EndX,j.EndY,j.EndZ),j.Axis);var beam=new MechanismPart{Kind="sketch",Name="Балка / редактируемый корпус звена",Length=j.BeamThickness,Z=beamZ,Outline=Capsule(j.Length,rootR,endR)};if(moving)beam.Holes.Add(new MechanismHole{Name="Посадка звена",Diameter=j.BearingInner+2*fit});double mountR=rootR-5;beam.Holes.AddRange(Pattern(mountR,boltD,4,0));

   foreach(var c in children){double postR=c.GearboxDiameter/2+c.Wall/2;foreach(var hole in Pattern(postR,c.BoltDiameter+2*c.Fit,4,Math.PI/4))beam.Holes.Add(new MechanismHole{Name="Крепление следующего звена",X=j.Length+hole.X,Y=hole.Y,Diameter=hole.Diameter});}

   if(j.BodyStyle==RobotBodyStyle.Облегчённые_пластины&&j.Length>rootR+endR+50){double start=rootR+wall+12,end=j.Length-endR-wall-12,step=18;for(double x=start;x<end;x+=step)beam.Holes.Add(new MechanismHole{Name="Облегчение",X=x,Y=0,Diameter=Math.Min(10,j.BeamWidth-2*wall-4)});}

   if(j.BodyStyle==RobotBodyStyle.Открытая_рама){beam.Length=Math.Max(3,j.BeamThickness*.65);}

   beam.Holes=beam.Holes.GroupBy(h=>Engine.N(Math.Round(h.X,6))+","+Engine.N(Math.Round(h.Y,6))+","+Engine.N(Math.Round(h.Diameter,6))+","+Engine.N(h.Depth)).Select(g=>g.First()).ToList();Add(b,j,"beam",beam,beamFrame,false,"Корпус",true,j.Density);

   if(j.BodyStyle==RobotBodyStyle.Разъёмный_корпус){var second=beam.CloneShape();second.Name="Вторая разъёмная половина звена";second.Z=beamZ+j.BeamThickness+wall*2;second.Length=Math.Max(3,j.BeamThickness*.5);Add(b,j,"beam_cover",second,beamFrame,false,"Корпус",true,j.Density);foreach(double x in new[]{rootR+8,Math.Max(rootR+10,j.Length-endR-8)}){var rail=Box("Соединительная стенка корпуса",Math.Max(8,j.Length-rootR-endR-16),wall,beamZ+j.BeamThickness,wall*2);rail.X=(rootR+j.Length-endR)/2;rail.Y=(x==rootR+8?-1:1)*(j.BeamWidth/2-wall/2);Add(b,j,"rail_"+Engine.N(x),rail,beamFrame,false,"Корпус",true,j.Density);}}

   if(moving){var hubPart=Round("Фланец крепления балки",rootR*2,hub,j.BearingInner+2*fit,fit);hubPart.Holes.AddRange(Pattern(mountR,boltD,4,0));Add(b,j,"hub",hubPart,beamFrame,false,"Корпус",true,j.Density);var holes=Pattern(mountR,boltD,4,0).ToArray();for(int i=0;i<holes.Length;i++)Bolt(b,j,"beam_bolt_"+i,holes[i].X,holes[i].Y,fit,hub+j.BeamThickness,j.BoltDiameter,beamFrame,false);}

   if(!string.IsNullOrEmpty(j.ParentId)){var parent=b.Document.Find(j.ParentId);double offset=Vector3D.DotProduct(new Vector3D(j.OffsetX,j.OffsetY,j.OffsetZ),j.Axis),start=-offset+parent.BeamThickness+4+parent.Fit,end=-length-wall;if(end>start+2&&Math.Abs(Vector3D.DotProduct(parent.Axis,j.Axis))>.999){double radius=j.GearboxDiameter/2+j.Wall/2;var posts=Pattern(radius,j.BoltDiameter,4,Math.PI/4).ToArray();for(int i=0;i<posts.Length;i++){var post=Round("Дистанционная опора мотора",Math.Max(7,j.BoltDiameter+2*wall/2),end-start,j.BoltDiameter+2*fit,start);post.X=posts[i].X;post.Y=posts[i].Y;Add(b,j,"post_"+i,post,axis,true,"Корпус",true,j.Density);}b.Notes.Add(j.Name+": дистанционные опоры рассчитаны по осевому смещению; стыковочные поверхности и длину крепежа проверьте перед изготовлением.");}else b.Notes.Add(j.Name+": для выбранного смещения/разворота осей требуется ручной переходной кронштейн; измените смещение или доработайте корпус.");}

   if(j.Cables){double y=j.BeamWidth/2+j.CableDiameter/2+j.CableClearance;var a=beamFrame.Transform(new Point3D(rootR+5,y,beamZ+j.BeamThickness/2));var c=beamFrame.Transform(new Point3D(Math.Max(rootR+6,j.Length-endR),y,beamZ+j.BeamThickness/2));Cable(b,j,"cable_link",a,c,false);var port=axis.Transform(new Point3D(j.MotorOffsetX,j.MotorOffsetY+j.MotorFrame/2+wall/2,-length-wall));var back=axis.Transform(new Point3D(j.MotorOffsetX,j.MotorOffsetY+j.MotorFrame/2+wall/2,-length-wall-j.MotorLength*.65));Cable(b,j,"cable_motor",port,back,true);}

   }

   // Guides are rendered for skeleton editing and excluded from manufactured parts and gravity.

   Add(b,j,"joint_guide",Round("Ось шарнира",14,5,0,-2.5),axis,true,"Скелет",false,0,true);Add(b,j,"end_guide",Round("Конец кости · переместите для изменения длины",12,5,0,-2.5),axis*RobotMath.Translation(j.EndX,j.EndY,j.EndZ),false,"Скелет",false,0,true);var vector=new Vector3D(j.EndX,j.EndY,j.EndZ);Add(b,j,"bone_guide",Round("Кость",5,vector.Length,0,0),RobotMath.Frame(vector),false,"Скелет",false,0,true);

  }

  static void AddTransmission(RobotBuild b,RobotJoint j,Matrix3D axis){Result result;double ratio;Matrix3D local;

   if(j.TrainSettings!=null){var train=TrainSolver.Solve(j.TrainSettings);TrainState output;if(!train.Valid||!train.States.TryGetValue(j.TrainOutput??"",out output))throw new Exception("Граф передачи сустава не рассчитан.");result=train.Scene;ratio=j.Linear?180/(Math.PI*output.OutputGain):1/output.OutputGain;local=output.OutputFrame;local.Invert();local*=axis;}else if(j.CustomTransmission!=null){var project=j.CustomTransmission;var solution=MechanismSolver.Solve(project);if(!solution.Valid)throw new Exception("Передача "+j.Name+": "+string.Join("; ",solution.Errors));var output=project.Find(j.TransmissionOutput);var state=solution.Shafts[output.Id];ratio=1/state.SpeedRatio;var model=MechanismCad.Build(project);result=MechanismCad.ViewResult(project,solution,model);local=RobotMath.Translation(-output.X,-output.Y,-output.Z-project.ShaftLength(output))*axis;}else{var p=Engine.Clone(j.ReducerSettings);result=Engine.Calculate(p);result.Model=CadBuilder.Build(result);if(!result.Valid||!result.Model.Valid)throw new Exception("Редуктор "+j.Name+" не построен.");ratio=j.Linear&&p.Kind==12?180/(Math.PI*result.Engineering.SpeedGain):1/TrainSolver.Gain(result);if(p.Kind>=5){local=result.OutputFrame;local.Invert();local*=axis;}else{ratio=result.Actual*(p.Kind==0&&!p.RingOutput?-1:1);double top=result.Model.Parts.Where(a=>a.Role=="housing").SelectMany(a=>a.Sections).Select(s=>s.Top).DefaultIfEmpty(result.Model.Bounds()[5]).Max();local=RobotMath.Translation(0,0,-top)*axis;}}

   int index=0;foreach(var source in result.Model.Parts){if(source.Id.Contains("MOTOR_REFERENCE")&&j.TrainSettings==null&&(j.ReducerSettings==null||j.ReducerSettings.Kind<5))continue;var part=new CadPart(j.Id+"_transmission_"+(index++),j.Name+" · "+source.Label,source.Role,source.Color){Reference=source.Reference,NativeStep=source.NativeStep,NativeVolume=source.NativeVolume,NativeCentre=source.NativeCentre,SectionUnion=source.SectionUnion,Analytic=source.Analytic,BearingSource=source.BearingSource,BearingElement=source.BearingElement,BearingPitch=source.BearingPitch,BearingBall=source.BearingBall,Loft=source.Loft,MeshOverride=source.MeshOverride};part.Sections.AddRange(source.Sections);var motion=PartMotion.Bind(result,source);var entry=new RobotCadEntry{Id=part.Id,JointId=j.Id,OwnerId=RobotTransmission60.OutputPart(result,source)||source.Role=="housing"&&motion.Moving?j.Id:j.ParentId,Part=part,Local=local,InputSide=true,Category=source.Reference?"Мотор":"Передача",MassOverride=source.Reference?j.MotorMass:-1,Printable=PrintExport.Candidate(source),Density=source.Role=="gear"||source.Role=="housing"?j.Density:7850,NativeMotion=motion,NativeRatio=ratio,NativeBasePlacement=source.Placement};b.Entries[entry.Id]=entry;b.Model.Parts.Add(part);}b.Notes.Add(j.Name+": вставлена рассчитанная передача. Совпадение выходного фланца с корпусом робота требует проверки / адаптера; внутреннее движение показано кинематически.");

  }

 }

}







