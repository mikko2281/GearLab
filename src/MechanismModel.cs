using System;using System.IO;using System.Linq;using System.ComponentModel;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Text;
namespace GearLab {
 public sealed class MechanismPoint {public double X{get;set;}public double Y{get;set;}public MechanismPoint(){}public MechanismPoint(double x,double y){X=x;Y=y;}}
 public sealed class MechanismHole {
  public string Name{get;set;}public double X{get;set;}public double Y{get;set;}public double Diameter{get;set;}public double Depth{get;set;}
  public MechanismHole(){Name="Отверстие";Diameter=3;}
 }
 public sealed class MechanismStep {public double Length{get;set;}public double Diameter{get;set;}public double Bore{get;set;}public MechanismStep(){Length=20;Diameter=10;}}
 public sealed class MechanismPart {
  [Browsable(false)]public string Id{get;set;}
  [Browsable(false)]public string Kind{get;set;}
  [Category("Деталь"),DisplayName("Название")]public string Name{get;set;}
  [ScriptIgnore,Category("Деталь"),DisplayName("Тип"),ReadOnly(true)]public string TypeName{get{return GearForm=="helical"?"Косозубая шестерня":GearForm=="herringbone"?"Шевронная шестерня":GearForm=="internal"?"Внутренний венец · CAD":GearForm=="rack"?"Зубчатая рейка · CAD":KindName(Kind);}}
  [Category("Положение, мм"),DisplayName("X")]public double X{get;set;}
  [Category("Положение, мм"),DisplayName("Y")]public double Y{get;set;}
  [Category("Положение, мм"),DisplayName("Z — начало детали")]public double Z{get;set;}
  [Category("Положение, мм"),DisplayName("Поворот вокруг Z, °")]public double Angle{get;set;}
  [Browsable(false)]public string ShaftId{get;set;}
  [Category("Посадка на вал"),DisplayName("Положение по валу, мм")]public double OffsetZ{get;set;}
  [Category("Посадка на вал"),DisplayName("Отверстие по диаметру вала")]public bool MatchShaft{get;set;}
  [Category("Размеры, мм"),DisplayName("Наружный диаметр")]public double Diameter{get;set;}
  [Category("Размеры, мм"),DisplayName("Длина / выдавливание по Z")]public double Length{get;set;}
  [Category("Размеры, мм"),DisplayName("Центральное отверстие Ø (0 — нет)")]public double Bore{get;set;}
  [Category("Зубчатый венец"),DisplayName("Число зубьев")]public int Teeth{get;set;}
  [Category("Зубчатый венец"),DisplayName("Модуль, мм")]public double Module{get;set;}
  [ScriptIgnore,Category("Зубчатый венец"),DisplayName("Делительный диаметр, мм"),ReadOnly(true)]public double PitchDiameter{get{return Kind=="gear"?Teeth*Module:Diameter;}}
  [Category("Размеры, мм"),DisplayName("Ширина X")]public double SizeX{get;set;}
  [Category("Размеры, мм"),DisplayName("Ширина Y")]public double SizeY{get;set;}
  [Category("Материал"),DisplayName("Плотность, кг/м³")]public double Density{get;set;}
  [Category("Нагрузка"),DisplayName("Сопротивляющий момент, Н·м"),Description("Нагрузка на этом валу. Моменты нескольких выходов суммируются с учётом передаточных чисел и КПД.")]public double LoadTorque{get;set;}
  [Browsable(false)]public List<MechanismHole> Holes{get;set;}
  [Browsable(false)]public List<MechanismStep> Steps{get;set;}
  [Browsable(false)]public List<MechanismPoint> Outline{get;set;}
  [Browsable(false)]public string NativeProject{get;set;}
  [Browsable(false)]public string NativeStep{get;set;}
  [Browsable(false)]public string NativePreview{get;set;}
  [Browsable(false)]public double NativeVolume{get;set;}
  [Browsable(false)]public double[] NativeCentre{get;set;}
  [Browsable(false)]public double NativeBaseX{get;set;}
  [Browsable(false)]public double NativeBaseY{get;set;}
  [Browsable(false)]public double NativeBaseZ{get;set;}
  [Browsable(false)]public double NativeBaseAngle{get;set;}
  [Browsable(false)]public string GearForm{get;set;}
  [Category("Зубчатый венец"),DisplayName("Угол зацепления, °"),ReadOnly(true)]public double PressureAngle{get;set;}
  [Category("Зубчатый венец"),DisplayName("Угол наклона зуба, °"),ReadOnly(true)]public double HelixAngle{get;set;}
  public MechanismPart(){Id=Guid.NewGuid().ToString("N");Kind="shaft";Name="Вал";Diameter=10;Length=80;Bore=0;Teeth=20;Module=1;PressureAngle=20;GearForm="spur";SizeX=40;SizeY=30;Density=7850;MatchShaft=true;Holes=new List<MechanismHole>();Steps=new List<MechanismStep>();Outline=new List<MechanismPoint>();}
  public static string KindName(string kind){switch(kind){case "shaft":return "Вал";case "gear":return "Прямозубая шестерня";case "pulley":return "Шкив";case "bearing":return "Втулка / опора";case "block":return "Блок / корпус";case "sketch":return "Выдавленный эскиз";default:return "Неизвестная деталь";}}
  public override string ToString(){return Name;}
 }
 public sealed class MechanismLink {
  public string Id{get;set;}public string FromId{get;set;}public string ToId{get;set;}public string Kind{get;set;}public double Efficiency{get;set;}public double PlacementAngle{get;set;}
  public MechanismLink(){Id=Guid.NewGuid().ToString("N");Kind="gear";Efficiency=.97;}
 }
 public sealed class MechanismProject {
  public int FormatVersion{get;set;}public string Name{get;set;}public List<MechanismPart> Parts{get;set;}public List<MechanismLink> Links{get;set;}public string DriveShaftId{get;set;}public double InputRpm{get;set;}public double InputTorque{get;set;}
  public MechanismProject(){FormatVersion=1;Name="Новый механизм";Parts=new List<MechanismPart>();Links=new List<MechanismLink>();InputRpm=300;InputTorque=2;}
  public MechanismPart Find(string id){return Parts.FirstOrDefault(p=>p.Id==id);}
  public MechanismPart Shaft(MechanismPart p){return p==null?null:p.Kind=="shaft"?p:Find(p.ShaftId);}
  public MechanismPart Add(string kind){var p=new MechanismPart{Kind=kind,Name=MechanismPart.KindName(kind)+" "+(Parts.Count+1)};if(kind!="shaft"){p.Length=kind=="bearing"?12:8;p.Diameter=kind=="bearing"?22:30;p.Bore=kind=="bearing"?10:0;p.Density=kind=="block"||kind=="sketch"?1250:7850;}Parts.Add(p);if(kind=="shaft"&&string.IsNullOrEmpty(DriveShaftId))DriveShaftId=p.Id;return p;}
  public double[] Position(MechanismPart p){var shaft=p.Kind=="shaft"?null:Find(p.ShaftId);return shaft==null?new[]{p.X,p.Y,p.Z}:new[]{shaft.X,shaft.Y,shaft.Z+p.OffsetZ};}
  public double ShaftDiameter(MechanismPart shaft,double z){if(shaft.Steps.Count==0)return shaft.Diameter;double at=0;foreach(var step in shaft.Steps){if(z<at+step.Length-1e-8)return step.Diameter;at+=step.Length;}return shaft.Steps.Last().Diameter;}
  public double ShaftLength(MechanismPart shaft){return shaft.Steps.Count==0?shaft.Length:shaft.Steps.Sum(s=>s.Length);}
  public double ActualBore(MechanismPart part){var shaft=part.Kind=="shaft"?null:Find(part.ShaftId);return shaft!=null&&part.MatchShaft?ShaftDiameter(shaft,part.OffsetZ):part.Bore;}
  static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=128*1024*1024,RecursionLimit=32};}
  public string Serialize(){return Serializer().Serialize(this);}
  public static MechanismProject Parse(string text){if(text==null||text.Length>128*1024*1024)throw new Exception("Файл механизма слишком большой.");var p=Serializer().Deserialize<MechanismProject>(text);if(p==null||p.FormatVersion!=1||p.Parts==null||p.Links==null)throw new Exception("Неподдерживаемый файл механизма.");var errors=MechanismCad.Validate(p);if(errors.Count>0)throw new Exception(string.Join("\n",errors.Take(8)));return p;}
  public MechanismProject Clone(){return Serializer().Deserialize<MechanismProject>(Serialize());}
  public void Save(string path){ReleaseSupport.WriteText(path,Serialize(),new UTF8Encoding(true));}
  public void Remove(string id){var part=Find(id);if(part==null)return;foreach(var child in Parts.Where(p=>p.ShaftId==id)){var pos=Position(child);child.X=pos[0];child.Y=pos[1];child.Z=pos[2];child.ShaftId=null;}Parts.Remove(part);Links.RemoveAll(l=>l.FromId==id||l.ToId==id||Find(l.FromId)==null||Find(l.ToId)==null||Shaft(Find(l.FromId))==null||Shaft(Find(l.ToId))==null);if(DriveShaftId==id)DriveShaftId=Parts.Where(p=>p.Kind=="shaft").Select(p=>p.Id).FirstOrDefault();}
  public static MechanismProject Example(bool compound){var p=new MechanismProject{Name=compound?"Две ступени 6:1":"Зубчатая передача 3:1"};var a=p.Add("shaft");a.Name="Входной вал";var b=p.Add("shaft");b.Name=compound?"Промежуточный вал":"Выходной вал";var g1=p.Add("gear");g1.Name="Ведущая 20 зубьев";g1.ShaftId=a.Id;g1.OffsetZ=10;g1.Module=2;var g2=p.Add("gear");g2.Name="Ведомая 60 зубьев";g2.ShaftId=b.Id;g2.OffsetZ=10;g2.Teeth=60;g2.Module=2;p.Links.Add(new MechanismLink{FromId=g1.Id,ToId=g2.Id});if(compound){var c=p.Add("shaft");c.Name="Выходной вал";var g3=p.Add("gear");g3.ShaftId=b.Id;g3.Name="Вторая ведущая 20";g3.OffsetZ=35;g3.Module=2;var g4=p.Add("gear");g4.Name="Вторая ведомая 40";g4.ShaftId=c.Id;g4.OffsetZ=10;g4.Module=2;g4.Teeth=40;p.Links.Add(new MechanismLink{FromId=g3.Id,ToId=g4.Id});c.LoadTorque=5;}else b.LoadTorque=3;MechanismCad.AutoArrange(p);return p;}
 }
 // Filter the property panel to fields which actually affect the selected part.
 public sealed class MechanismProperties:ICustomTypeDescriptor {
  readonly MechanismPart part;public MechanismProperties(MechanismPart p){part=p;}
  public PropertyDescriptorCollection GetProperties(Attribute[] attributes){var hide=new HashSet<string>();if(part.NativeStep!=null)hide.UnionWith(new[]{"Diameter","Length","Bore","Teeth","Module","SizeX","SizeY","MatchShaft"});if(part.Kind!="gear")hide.UnionWith(new[]{"PressureAngle","HelixAngle"});bool mounted=!string.IsNullOrEmpty(part.ShaftId);if(mounted){hide.UnionWith(new[]{"X","Y","Z"});if(part.MatchShaft)hide.Add("Bore");}else hide.UnionWith(new[]{"OffsetZ","MatchShaft"});if(part.Kind!="shaft")hide.Add("LoadTorque");if(part.Kind!="gear")hide.UnionWith(new[]{"Teeth","Module","PitchDiameter"});if(part.Kind=="gear"||part.Kind=="sketch"||part.Kind=="block")hide.Add("Diameter");if(part.Kind!="block")hide.UnionWith(new[]{"SizeX","SizeY"});if(part.Kind=="shaft"&&part.Steps.Count>0)hide.UnionWith(new[]{"Length","Diameter","Bore"});return new PropertyDescriptorCollection(TypeDescriptor.GetProperties(part,attributes).Cast<PropertyDescriptor>().Where(p=>p.IsBrowsable&&!hide.Contains(p.Name)).ToArray());}
  public PropertyDescriptorCollection GetProperties(){return GetProperties(new Attribute[0]);}public object GetPropertyOwner(PropertyDescriptor p){return part;}public AttributeCollection GetAttributes(){return TypeDescriptor.GetAttributes(part);}public string GetClassName(){return part.TypeName;}public string GetComponentName(){return part.Name;}public TypeConverter GetConverter(){return TypeDescriptor.GetConverter(part);}public EventDescriptor GetDefaultEvent(){return null;}public PropertyDescriptor GetDefaultProperty(){return null;}public object GetEditor(Type t){return null;}public EventDescriptorCollection GetEvents(Attribute[] a){return EventDescriptorCollection.Empty;}public EventDescriptorCollection GetEvents(){return EventDescriptorCollection.Empty;}
 }
}

