using System;using System.IO;using System.Linq;using System.ComponentModel;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Text;using System.Windows.Media.Media3D;

namespace GearLab {

 public enum RobotJointMode {Вращение_X,Вращение_Y,Вращение_Z,Линейный_X,Линейный_Y,Линейный_Z,Неподвижный}

 public enum RobotBodyStyle {Открытая_рама,Разъёмный_корпус,Облегчённые_пластины,Закрытый_трубчатый}

 public enum RobotMotorTemplate {NEMA17_шаблон,NEMA23_шаблон,Вручную,STEPPERONLINE_по_вашему_STEP}

 public enum RobotAppearance {Технический,Чёрный_оранжевый}

 public sealed class RobotJoint {

  [Browsable(false)]public string Id{get;set;}[Browsable(false)]public string ParentId{get;set;}

  [Category("Звено"),DisplayName("Название")]public string Name{get;set;}

  [Category("Шарнир"),DisplayName("Тип и ось")]public RobotJointMode Mode{get;set;}

  [Category("Шарнир"),DisplayName("Обратить направление оси и привода")]public bool AxisReversed{get;set;}

  [Category("Шарнир"),DisplayName("Угол ° / ход мм")]public double Position{get;set;}

  [Category("Шарнир"),DisplayName("Нижний предел")]public double Minimum{get;set;}

  [Category("Шарнир"),DisplayName("Верхний предел")]public double Maximum{get;set;}

  [Category("Шарнир"),DisplayName("Скорость °/с или мм/с")]public double Speed{get;set;}

  [Category("Начало звена относительно родителя, мм"),DisplayName("Смещение X")]public double OffsetX{get;set;}

  [Category("Начало звена относительно родителя, мм"),DisplayName("Смещение Y")]public double OffsetY{get;set;}

  [Category("Начало звена относительно родителя, мм"),DisplayName("Смещение Z")]public double OffsetZ{get;set;}

  [Category("Конец кости в локальных осях, мм"),DisplayName("Конец X")]public double EndX{get;set;}

  [Category("Конец кости в локальных осях, мм"),DisplayName("Конец Y")]public double EndY{get;set;}

  [Category("Конец кости в локальных осях, мм"),DisplayName("Конец Z")]public double EndZ{get;set;}

  [ScriptIgnore,Category("Звено"),DisplayName("Длина, мм"),ReadOnly(true)]public double Length{get{return new Vector3D(EndX,EndY,EndZ).Length;}}

  [Category("Мотор и передача"),DisplayName("Шаблон мотора")]public RobotMotorTemplate MotorTemplate{get;set;}

  [Category("Мотор и передача"),DisplayName("Момент на рабочих оборотах, Н·м"),Description("Введите доступный момент из характеристики выбранного мотора. Значение шаблона условное; NEMA задаёт габарит, а не момент.")]public double MotorTorque{get;set;}

  [Category("Мотор и передача"),DisplayName("Рабочие обороты мотора, максимум")]public double MotorMaxRpm{get;set;}

  [Category("Мотор и передача"),DisplayName("Масса мотора, кг")]public double MotorMass{get;set;}

  [Category("Мотор и передача"),DisplayName("Передаточное число")]public double Reduction{get;set;}

  [Category("Мотор и передача"),DisplayName("КПД всей передачи")]public double Efficiency{get;set;}

  [Category("Мотор и передача"),DisplayName("Ход винта за оборот, мм")]public double ScrewLead{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Сторона фланца")]public double MotorFrame{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Длина мотора")]public double MotorLength{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Шаг четырёх винтов")]public double MotorBoltPitch{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Монтажное отверстие Ø")]public double MotorBolt{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Центрирующий бурт Ø")]public double MotorPilot{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Вал мотора Ø")]public double MotorShaft{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Выступ вала от фланца")]public double MotorShaftLength{get;set;}

  [Category("Мотор: монтажные размеры, мм"),DisplayName("Высота бурта")]public double MotorPilotHeight{get;set;}

  [Category("Внешний вид"),DisplayName("Цветовая схема")]public RobotAppearance Appearance{get;set;}

  [Category("Положение мотора в плите, мм"),DisplayName("Смещение мотора X")]public double MotorOffsetX{get;set;}

  [Category("Положение мотора в плите, мм"),DisplayName("Смещение мотора Y")]public double MotorOffsetY{get;set;}

  [Category("Корпус и крепление"),DisplayName("Вариант корпуса")]public RobotBodyStyle BodyStyle{get;set;}

  [Category("Закрытый трубчатый корпус"),DisplayName("Диаметр звена, мм")]public double SkinDiameter{get;set;}

  [Category("Закрытый трубчатый корпус"),DisplayName("Поворот крышки вокруг звена, °")]public double SkinPanelRotation{get;set;}

  [Category("Закрытый трубчатый корпус"),DisplayName("Сервисная крышка, угол °")]public double SkinPanelAngle{get;set;}

  [Category("Корпус и крепление"),DisplayName("Стенка, мм")]public double Wall{get;set;}

  [Category("Корпус и крепление"),DisplayName("Ширина балки, мм")]public double BeamWidth{get;set;}

  [Category("Корпус и крепление"),DisplayName("Толщина балки, мм")]public double BeamThickness{get;set;}

  [Category("Корпус и крепление"),DisplayName("Габарит передачи Ø, мм")]public double GearboxDiameter{get;set;}

  [Category("Корпус и крепление"),DisplayName("Длина габарита передачи, мм")]public double GearboxLength{get;set;}

  [Category("Корпус и крепление"),DisplayName("Подшипник: наружный Ø, мм")]public double BearingOuter{get;set;}

  [Category("Корпус и крепление"),DisplayName("Подшипник: внутренний Ø, мм")]public double BearingInner{get;set;}

  [Category("Корпус и крепление"),DisplayName("Подшипник: ширина, мм")]public double BearingWidth{get;set;}

  [Category("Корпус и крепление"),DisplayName("Болт корпуса Ø, мм")]public double BoltDiameter{get;set;}

  [Category("Корпус и крепление"),DisplayName("Зазор посадок, мм")]public double Fit{get;set;}

  [Category("Корпус и крепление"),DisplayName("Плотность корпуса, кг/м³")]public double Density{get;set;}

  [Category("Проводка"),DisplayName("Показывать проводку")]public bool Cables{get;set;}

  [Category("Проводка"),DisplayName("Пучок проводов Ø, мм")]public double CableDiameter{get;set;}

  [Category("Проводка"),DisplayName("Зазор проводки, мм")]public double CableClearance{get;set;}

  [Category("Нагрузка инструмента"),DisplayName("Полезная нагрузка, кг")]public double Payload{get;set;}

  [Category("Нагрузка инструмента"),DisplayName("Внешняя сила X, Н")]public double ForceX{get;set;}

  [Category("Нагрузка инструмента"),DisplayName("Внешняя сила Y, Н")]public double ForceY{get;set;}

  [Category("Нагрузка инструмента"),DisplayName("Внешняя сила Z, Н")]public double ForceZ{get;set;}

  [Browsable(false)]public ProceduralBodySettings BodyGenerator{get;set;}[Browsable(false)]public RobotGeneratedBody GeneratedBody{get;set;}

  [Browsable(false)]public Parameters ReducerSettings{get;set;}[Browsable(false)]public TransmissionTrain TrainSettings{get;set;}[Browsable(false)]public string TrainOutput{get;set;}

  [Browsable(false)]public MechanismProject CustomTransmission{get;set;}

  [Browsable(false)]public string TransmissionOutput{get;set;}

  public RobotJoint(){Id=Guid.NewGuid().ToString("N");Name="Звено";Mode=RobotJointMode.Вращение_Y;Minimum=-70;Maximum=70;Speed=20;EndX=160;Reduction=40;Efficiency=.7;ScrewLead=8;MotorTorque=.35;MotorMaxRpm=400;MotorMass=.28;MotorFrame=42.3;MotorLength=40;MotorBoltPitch=31;MotorBolt=3;MotorPilot=22;MotorShaft=5;MotorShaftLength=15;MotorPilotHeight=2;SkinDiameter=60;SkinPanelAngle=100;SkinPanelRotation=-90;Wall=6;BeamWidth=34;BeamThickness=8;GearboxDiameter=76;GearboxLength=32;BearingOuter=22;BearingInner=8;BearingWidth=7;BoltDiameter=3;Fit=.2;Density=1250;Cables=true;CableDiameter=4;CableClearance=1;BodyStyle=RobotBodyStyle.Разъёмный_корпус;}

  public void ApplyMotorTemplate(){if(MotorTemplate==RobotMotorTemplate.Вручную)return;if(MotorTemplate==RobotMotorTemplate.STEPPERONLINE_по_вашему_STEP){MotorFrame=41.9;MotorLength=37.5;MotorBoltPitch=31.3;MotorBolt=3;MotorPilot=22;MotorShaft=4.65;MotorShaftLength=23.2;MotorPilotHeight=2;GearboxDiameter=Math.Max(GearboxDiameter,MotorFrame+24);return;}bool big=MotorTemplate==RobotMotorTemplate.NEMA23_шаблон;MotorFrame=big?57:42.3;MotorLength=big?56:40;MotorBoltPitch=big?47.14:31;MotorBolt=big?5:3;MotorPilot=big?38.1:22;MotorShaft=big?6.35:5;MotorShaftLength=big?21:15;MotorPilotHeight=big?1.6:2;MotorMass=big?.7:.28;MotorTorque=big?1.2:.35;GearboxDiameter=Math.Max(GearboxDiameter,MotorFrame+24);}

  public MechanismProject CreateTransmissionTemplate(){var project=MechanismProject.Example(true);if(Math.Abs(Reduction-40)<1e-6){var gears=project.Parts.Where(p=>p.Kind=="gear").ToArray();foreach(var gear in gears)gear.Module=.75;foreach(var shaft in project.Parts.Where(p=>p.Kind=="shaft"))shaft.Diameter=8;gears[1].Teeth=100;gears[3].Teeth=160;project.Links[1].PlacementAngle=180;project.Name="Две ступени 40:1 · складная компоновка";MechanismCad.AutoArrange(project);}return project;}

  [ScriptIgnore]public bool Linear{get{return Mode==RobotJointMode.Линейный_X||Mode==RobotJointMode.Линейный_Y||Mode==RobotJointMode.Линейный_Z;}}

  [ScriptIgnore]public Vector3D Axis{get{double s=AxisReversed?-1:1;return Mode==RobotJointMode.Вращение_X||Mode==RobotJointMode.Линейный_X?new Vector3D(s,0,0):Mode==RobotJointMode.Вращение_Y||Mode==RobotJointMode.Линейный_Y?new Vector3D(0,s,0):new Vector3D(0,0,s);}}

  public override string ToString(){return Name;}

 }

 public sealed class RobotBodyEdit {public string PartId{get;set;}public MechanismPart Shape{get;set;}public MeshDocument Mesh{get;set;}public string NativeProject{get;set;}public string NativeStep{get;set;}public string NativePreview{get;set;}public double NativeVolume{get;set;}public double[] NativeCentre{get;set;}public double[] NativeShift{get;set;}}

 public sealed class RobotDocument {

  public bool ReferenceGripper{get;set;}public double GripperOpening{get;set;}

  public int FormatVersion{get;set;}public string Name{get;set;}public double SafetyFactor{get;set;}public double Gravity{get;set;}public List<RobotJoint> Joints{get;set;}public List<RobotBodyEdit> BodyEdits{get;set;}

  public RobotDocument(){GripperOpening=20;FormatVersion=1;Name="Новый робот";SafetyFactor=1.5;Gravity=9.81;Joints=new List<RobotJoint>();BodyEdits=new List<RobotBodyEdit>();}

  public RobotJoint Find(string id){return Joints.FirstOrDefault(j=>j.Id==id);}

  public RobotJoint Add(string parent){var j=new RobotJoint{ParentId=parent,Name="Звено "+(Joints.Count+1)};Joints.Add(j);return j;}

  public static RobotDocument Example(){var d=new RobotDocument{Name="Манипулятор · 3 шарнира · 0,5 кг"};var a=d.Add(null);a.Name="Плечо";a.EndX=180;a.OffsetZ=95;var b=d.Add(a.Id);b.Name="Предплечье";b.EndX=140;b.OffsetY=100;var c=d.Add(b.Id);c.Name="Кисть";c.EndX=100;c.OffsetY=100;c.Payload=.5;return d;}

  public static RobotDocument SuppliedMotorExample(){var d=Example();d.Name="Манипулятор · мотор по STEP пользователя";foreach(var j in d.Joints){j.MotorTemplate=RobotMotorTemplate.STEPPERONLINE_по_вашему_STEP;j.ApplyMotorTemplate();j.Appearance=RobotAppearance.Чёрный_оранжевый;}return d;}

  public static RobotDocument EnclosedExample(){var d=new RobotDocument{ReferenceGripper=true,Name="Манипулятор · 6 осей · корпус по референсу"};var names=new[]{"J1 · Основание","J2 · Плечо","J3 · Локоть","J4 · Поворот запястья","J5 · Наклон запястья","J6 · Фланец инструмента"};var modes=new[]{RobotJointMode.Вращение_Z,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_Z,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_X};double[] lengths={110,220,170,100,100,65},poses={0,-45,90,0,60,0};string parent=null;for(int i=0;i<6;i++){var j=d.Add(parent);j.Name=names[i];j.Mode=modes[i];j.EndX=i==5?lengths[i]:0;j.EndZ=i==5?0:lengths[i];j.OffsetZ=i==0?130:0;j.Position=poses[i];j.Minimum=-120;j.Maximum=120;j.Speed=8;j.Reduction=i<3?80:40;j.MotorTemplate=RobotMotorTemplate.STEPPERONLINE_по_вашему_STEP;j.ApplyMotorTemplate();j.BodyStyle=RobotBodyStyle.Закрытый_трубчатый;j.Appearance=RobotAppearance.Чёрный_оранжевый;j.BodyGenerator=new ProceduralBodySettings{Connected=true,ReferenceDesign=true};j.Payload=i==5?.5:0;parent=j.Id;}return d;}

  public static RobotDocument BlenderExample(){

   // Coordinates measured from the author's mesh seams, not the nine helper bones.

   // 800 mm / original 4.3765735626 Blender units. Original file is never modified.

   double scale=800/4.37657356262207,baseY=.2247911543,baseZ=-2.1889286041;

   double[,] seam={{baseY,-1.718},{-.015015,-1.30153},{-.0142,-.16128},{.0004,.97973},{-.2536,1.265},{.13045,1.97443}};

   var d=new RobotDocument{Name="РОБОТ1 · авторская форма · NEMA 17 · 800 мм",ReferenceGripper=true};

   var names=new[]{"J1 · Основание Z","J2 · Плечо −Y","J3 · Локоть +Y","J4 · Кисть −Y","J5 · Поворот кисти Z","J6 · Инструмент +Y"};

   var modes=new[]{RobotJointMode.Вращение_Z,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_Y,RobotJointMode.Вращение_Z,RobotJointMode.Вращение_Y};

   string parent=null;for(int i=0;i<6;i++){

    var j=d.Add(parent);j.Name=names[i];j.Mode=modes[i];j.AxisReversed=i==1||i==3;j.Position=0;j.Minimum=-100;j.Maximum=100;j.Speed=8;

    j.EndX=0;j.EndY=i<5?(seam[i+1,0]-seam[i,0])*scale:20;j.EndZ=i<5?(seam[i+1,1]-seam[i,1])*scale:0;if(i==0)j.EndZ-=10;

    if(i==3)j.EndY-=16.6; // Space for the perpendicular NEMA 17 barrel and intact output bolt circle.

    j.OffsetY=i==0?(seam[0,0]-baseY)*scale:0;j.OffsetZ=i==0?(seam[0,1]-baseZ)*scale+10:0;

    j.MotorTemplate=RobotMotorTemplate.STEPPERONLINE_по_вашему_STEP;j.ApplyMotorTemplate();j.Cables=false;j.GearboxDiameter=54;j.GearboxLength=i==0?25:i==1?56:32;

    j.SkinDiameter=.425682962*scale;j.Reduction=i<3?80:40;j.BodyStyle=RobotBodyStyle.Закрытый_трубчатый;j.Appearance=RobotAppearance.Чёрный_оранжевый;

    j.BodyGenerator=new ProceduralBodySettings{Connected=true,ReferenceDesign=true,BlenderDesign=true,FrameWall=6.4};j.Payload=i==5?.5:0;parent=j.Id;

   }return d;

  }

  public static RobotDocument DefaultReference(){string path=Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);foreach(string folder in new[]{Path.Combine(path,"examples"),Path.Combine(Path.GetDirectoryName(path),"examples")}){foreach(string name in new[]{"Robot1_800mm.robot.json","SixAxis_Connected.robot.json"}){string file=Path.Combine(folder,name);if(File.Exists(file))try{return Parse(File.ReadAllText(file));}catch{}}}return BlenderExample();}

  public static RobotDocument CassetteReference(){string path=Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);foreach(string folder in new[]{Path.Combine(path,"examples"),Path.Combine(Path.GetDirectoryName(path),"examples")}){string file=Path.Combine(folder,"SixAxis_Connected.robot.json");if(File.Exists(file))try{return Parse(File.ReadAllText(file));}catch{}}return EnclosedExample();}

  public List<RobotJoint> Ordered(){var result=new List<RobotJoint>();var done=new HashSet<string>();for(int i=0;i<Joints.Count;i++){bool progress=false;foreach(var j in Joints)if(!done.Contains(j.Id)&&(string.IsNullOrEmpty(j.ParentId)||done.Contains(j.ParentId))){done.Add(j.Id);result.Add(j);progress=true;}if(!progress)break;}return result;}

  public bool Descendant(string node,string ancestor){var seen=new HashSet<string>();while(node!=null&&seen.Add(node)){if(node==ancestor)return true;var j=Find(node);node=j==null?null:j.ParentId;}return false;}

  public List<string> Validate(){var e=new List<string>();if(FormatVersion!=1||Joints==null||BodyEdits==null){e.Add("Неподдерживаемый проект робота.");return e;}if(Joints.Count>20){e.Add("Максимум 20 звеньев в этой версии.");return e;}if(!MechanismCad.Finite(SafetyFactor)||SafetyFactor<1||SafetyFactor>10||!MechanismCad.Finite(Gravity)||Gravity<0||Gravity>30)e.Add("Неверный запас / ускорение тяжести.");if(!MechanismCad.Finite(GripperOpening)||GripperOpening<0||GripperOpening>40)e.Add("Раскрытие захвата: 0–40 мм.");var ids=new HashSet<string>();foreach(var j in Joints){if(j==null){e.Add("Пустое звено.");continue;}if(string.IsNullOrWhiteSpace(j.Id)||!ids.Add(j.Id)||string.IsNullOrWhiteSpace(j.Name))e.Add("Идентификаторы / названия звеньев некорректны.");foreach(var p in typeof(RobotJoint).GetProperties().Where(p=>p.PropertyType==typeof(double)))if(!MechanismCad.Finite((double)p.GetValue(j,null)))e.Add(j.Name+": некорректное число "+p.Name);if(!Enum.IsDefined(typeof(RobotJointMode),j.Mode)||!Enum.IsDefined(typeof(RobotBodyStyle),j.BodyStyle)||!Enum.IsDefined(typeof(RobotMotorTemplate),j.MotorTemplate)||!Enum.IsDefined(typeof(RobotAppearance),j.Appearance))e.Add(j.Name+": неизвестный режим.");if(j.Length<20||j.Length>1500||new[]{j.OffsetX,j.OffsetY,j.OffsetZ}.Any(v=>Math.Abs(v)>3000)||j.Minimum>j.Maximum||j.Position<j.Minimum||j.Position>j.Maximum||Math.Abs(j.Minimum)>720||Math.Abs(j.Maximum)>720||j.Speed<.1||j.Speed>360)e.Add(j.Name+": неверные длина, пределы или скорость.");if(j.Reduction<1||j.Reduction>10000||j.Efficiency<=0||j.Efficiency>1||j.MotorTorque<0||j.MotorTorque>1000||j.MotorMaxRpm<1||j.MotorMaxRpm>60000||j.MotorMass<0||j.MotorMass>100||j.ScrewLead<.1||j.ScrewLead>100)e.Add(j.Name+": неверные данные мотора / передачи.");if(j.MotorFrame<15||j.MotorFrame>150||j.MotorLength<10||j.MotorLength>300||j.MotorBoltPitch<10||j.MotorBoltPitch>=j.MotorFrame-3||j.MotorPilot<5||j.MotorPilot>j.MotorFrame-5||j.MotorBolt<1||j.MotorBolt>10||j.MotorShaft<1||j.MotorShaft>30||j.MotorShaftLength<1||j.MotorShaftLength>150||j.MotorPilotHeight<.1||j.MotorPilotHeight>15||Math.Abs(j.MotorOffsetX)>200||Math.Abs(j.MotorOffsetY)>200)e.Add(j.Name+": неверные монтажные размеры мотора.");if(j.SkinDiameter<30||j.SkinDiameter>200||j.SkinPanelAngle<60||j.SkinPanelAngle>150||Math.Abs(j.SkinPanelRotation)>360||j.SkinDiameter<2*j.Wall+16)e.Add(j.Name+": неверный диаметр трубчатого корпуса или угол крышки.");if(j.Wall<3||j.Wall>20||j.BeamWidth<16||j.BeamWidth>180||j.BeamThickness<3||j.BeamThickness>30||j.GearboxDiameter<j.MotorFrame+12||j.GearboxDiameter>250||j.GearboxLength<15||j.GearboxLength>180||j.BearingInner<3||j.BearingOuter<=j.BearingInner+2||j.BearingOuter>j.GearboxDiameter-12||j.BearingWidth<2||j.BearingWidth>25||j.BoltDiameter<2||j.BoltDiameter>8||j.Fit<.05||j.Fit>1||j.Density<100||j.Density>20000)e.Add(j.Name+": неверные размеры корпуса / опоры.");if(j.CableDiameter<1||j.CableDiameter>20||j.CableClearance<.1||j.CableClearance>10||j.Payload<0||j.Payload>100||new[]{j.ForceX,j.ForceY,j.ForceZ}.Any(v=>Math.Abs(v)>10000))e.Add(j.Name+": неверная проводка / нагрузка.");if(!string.IsNullOrEmpty(j.ParentId)&&Find(j.ParentId)==null)e.Add(j.Name+": родитель не найден.");if(j.BodyGenerator!=null)try{j.BodyGenerator.Validate();}catch(Exception ex){e.Add(j.Name+": "+ex.Message);}if(j.CustomTransmission!=null){var errors=MechanismCad.Validate(j.CustomTransmission);if(errors.Count>0)e.Add(j.Name+": "+errors[0]);var s=MechanismSolver.Solve(j.CustomTransmission);if(!s.Valid||!s.Shafts.ContainsKey(j.TransmissionOutput??"")||!s.Shafts[j.TransmissionOutput].Connected)e.Add(j.Name+": выберите связанный выходной вал передачи.");}}

   if(BodyEdits.Count>2000||BodyEdits.Any(b=>b==null||string.IsNullOrEmpty(b.PartId))||BodyEdits.Where(b=>b!=null).GroupBy(b=>b.PartId).Any(g=>g.Count()>1))e.Add("Некорректные переопределения деталей.");

   if(e.Count==0&&Ordered().Count!=Joints.Count)e.Add("В скелете есть замкнутая цепь родителей.");return e;}

  static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=128*1024*1024,RecursionLimit=64};}

  public string Serialize(){return Serializer().Serialize(this);}public RobotDocument Clone(){return Serializer().Deserialize<RobotDocument>(Serialize());}

  public static RobotDocument Parse(string text){if(text==null||text.Length>128*1024*1024)throw new Exception("Файл робота слишком большой.");var d=Serializer().Deserialize<RobotDocument>(text);if(d==null)throw new Exception("Не удалось прочитать проект.");var e=d.Validate();if(e.Count>0)throw new Exception(string.Join("\n",e.Take(8)));return d;}

  public void Save(string path){ReleaseSupport.WriteText(path,Serialize(),new UTF8Encoding(true));}

  public void Remove(string id){var removed=new HashSet<string>(Joints.Where(j=>Descendant(j.Id,id)).Select(j=>j.Id));Joints.RemoveAll(j=>removed.Contains(j.Id));BodyEdits.RemoveAll(b=>removed.Any(k=>b.PartId.StartsWith(k+"_",StringComparison.Ordinal)));}

 }

 public static class RobotMath {

  public static Matrix3D Translation(double x,double y,double z){var m=Matrix3D.Identity;m.Translate(new Vector3D(x,y,z));return m;}

  public static Matrix3D Rotation(Vector3D axis,double degrees){var m=Matrix3D.Identity;m.Rotate(new Quaternion(axis,degrees));return m;}

  public static Matrix3D Frame(Vector3D z){z.Normalize();var x=Vector3D.CrossProduct(Math.Abs(z.Y)<.9?new Vector3D(0,1,0):new Vector3D(0,0,1),z);x.Normalize();var y=Vector3D.CrossProduct(z,x);return new Matrix3D(x.X,x.Y,x.Z,0,y.X,y.Y,y.Z,0,z.X,z.Y,z.Z,0,0,0,0,1);}

  public static Matrix3D Beam(Vector3D x,Vector3D normal){x.Normalize();var z=normal-x*Vector3D.DotProduct(normal,x);if(z.LengthSquared<1e-8)z=Vector3D.CrossProduct(x,Math.Abs(x.Z)<.8?new Vector3D(0,0,1):new Vector3D(0,1,0));z.Normalize();var y=Vector3D.CrossProduct(z,x);return new Matrix3D(x.X,x.Y,x.Z,0,y.X,y.Y,y.Z,0,z.X,z.Y,z.Z,0,0,0,0,1);}

  public static Point3D Point(Vector3D v){return new Point3D(v.X,v.Y,v.Z);}public static Vector3D Vector(Point3D p){return new Vector3D(p.X,p.Y,p.Z);}

 }

 public sealed class RobotPose {public Matrix3D Base,Output,End;public Point3D Origin,Tip;public Vector3D Axis;public double Value;}

 public static class RobotKinematics {

  public static Dictionary<string,RobotPose> Evaluate(RobotDocument d,double phase,bool animate,Dictionary<string,double> overrides=null){var result=new Dictionary<string,RobotPose>();int index=0;foreach(var j in d.Ordered()){var parent=string.IsNullOrEmpty(j.ParentId)?Matrix3D.Identity:result[j.ParentId].End;var basis=RobotMath.Translation(j.OffsetX,j.OffsetY,j.OffsetZ)*parent;double value=overrides!=null&&overrides.ContainsKey(j.Id)?overrides[j.Id]:j.Position;if(animate&&j.Mode!=RobotJointMode.Неподвижный){double amplitude=(j.Maximum-j.Minimum)/2,centre=(j.Maximum+j.Minimum)/2;if(amplitude>1e-9){double start=Math.Asin(Math.Max(-1,Math.Min(1,(j.Position-centre)/amplitude)));value=centre+amplitude*Math.Sin(start+phase*j.Speed/amplitude);}}value=Math.Max(j.Minimum,Math.Min(j.Maximum,value));var motion=j.Mode==RobotJointMode.Неподвижный?Matrix3D.Identity:j.Linear?RobotMath.Translation(j.Axis.X*value,j.Axis.Y*value,j.Axis.Z*value):RobotMath.Rotation(j.Axis,value);var output=motion*basis;var end=RobotMath.Translation(j.EndX,j.EndY,j.EndZ)*output;var axis=basis.Transform(j.Axis);axis.Normalize();result[j.Id]=new RobotPose{Base=basis,Output=output,End=end,Origin=basis.Transform(new Point3D()),Tip=end.Transform(new Point3D()),Axis=axis,Value=value};index++;}return result;}

 }

}





