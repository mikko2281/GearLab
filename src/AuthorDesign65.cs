using System;using System.ComponentModel;using System.IO;using System.IO.Compression;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;using System.Windows.Media.Media3D;
namespace GearLab {
 public sealed class AuthorReferenceInfo65 {
  [DisplayName("Статус"),ReadOnly(true)]public string Status{get{return I18n.T(AuthorDesign65.Notice);}}
  [DisplayName("Высота, мм"),ReadOnly(true)]public double Height{get;set;}
  [DisplayName("Груз, г"),ReadOnly(true)]public double Payload{get;set;}
  [DisplayName("Принтер / материал"),ReadOnly(true)]public string Printer{get{return "Bambu Lab P1S / PETG";}}
  [DisplayName("Мотор: шаблон"),ReadOnly(true)]public string Motor{get{return I18n.T("NEMA17 · 42,3 × 42,3 × 44 мм (44 мм — допущение) · вал 5 мм");}}
  [DisplayName("Редактирование"),ReadOnly(true)]public string Editing{get{return I18n.T("Высота и груз — в окне нового дизайна; движение — на этапе 4.");}}
 }
 public sealed class AuthorReferencePart {public string Id{get;set;}public string Name{get;set;}public int Joint{get;set;}public bool Fixed{get;set;}public string Preview{get;set;}public int Triangles{get;set;}public string SourceType{get;set;}}
 public sealed class AuthorReferenceAsset {
  public string Id{get;set;}public int Version{get;set;}public double Height{get;set;}public string SourceSHA256{get;set;}public double[] Bounds{get;set;}public double[][] Pivots{get;set;}public double[][] Axes{get;set;}public double[] Tip{get;set;}public List<AuthorReferencePart> Parts{get;set;}public string Status{get;set;}
 }
 public sealed class AuthorDesignSettings {public string Printer{get;set;}public string Material{get;set;}public double Nozzle{get;set;}public string AssetId{get;set;}public double Height{get;set;}public AuthorReferenceAsset Asset{get;set;}public List<string> JointIds{get;set;}}
 public static class AuthorDesign65 {
  public const string Id="author-manipulator-2026-11";
  public const string Notice="Авторский дизайн: референс. Прочность, приводы, посадки и готовность к печати не подтверждены.";
  public static string Find(string relative){string root=Path.GetDirectoryName(Application.ExecutablePath);foreach(string folder in new[]{root,Path.GetDirectoryName(root)}){string p=Path.Combine(folder,relative);if(File.Exists(p))return p;}throw new Exception("В комплекте отсутствует "+relative);}
  public static AuthorReferenceAsset Load(){return CADDocument.Serializer().Deserialize<AuthorReferenceAsset>(File.ReadAllText(Find("examples/Manipulator_1000mm.reference.json")));}
  public static RobotDocument Create(double height=1000,double payload=.3){
   if(!MechanismCad.Finite(height)||height<300||height>1500||!MechanismCad.Finite(payload)||payload<0||payload>10)throw new Exception("Высота: 300–1500 мм; груз: 0–10 кг.");
   var asset=Load();double scale=height/asset.Height;var d=new RobotDocument{FormatVersion=2,Name="Авторский манипулятор · "+Engine.F(height)+" мм · референс",SafetyFactor=2};
   var settings=new AuthorDesignSettings{Printer="Bambu Lab P1S",Material="PETG",Nozzle=.4,AssetId=Id,Asset=asset,Height=height,JointIds=new List<string>()};d.AuthorDesign=settings;
   string parent=null;for(int i=0;i<7;i++){
    var j=d.Add(parent);j.Id="author65_j"+(i+1);settings.JointIds.Add(j.Id);j.Name="J"+(i+1)+" · "+new[]{"Основание","Плечо","Верхнее звено","Локоть","Кисть 1","Кисть 2","Поворот инструмента"}[i];
    j.Mode=i==0?RobotJointMode.Вращение_Z:RobotJointMode.Вращение_X;j.CustomAxis=(double[])asset.Axes[i].Clone();
    var p=asset.Pivots[i];var next=i<6?asset.Pivots[i+1]:asset.Tip;
    j.EndX=(next[0]-p[0])*scale;j.EndY=(next[1]-p[1])*scale;j.EndZ=(next[2]-p[2])*scale;
    if(i==0){j.OffsetX=p[0]*scale;j.OffsetY=p[1]*scale;j.OffsetZ=p[2]*scale;}
    j.Position=0;j.Minimum=-20;j.Maximum=20;j.Speed=5;j.MotorTorque=0;j.MotorMass=0;j.MotorTemplate=RobotMotorTemplate.NEMA17_шаблон;j.ApplyMotorTemplate();j.MotorLength=44;j.MotorMass=.29;j.MotorTorque=0;j.Payload=i==6?payload:0;j.Cables=false;
    parent=j.Id;
   }var errors=d.Validate();if(errors.Count>0)throw new Exception(string.Join("; ",errors));return d;
  }
  public static List<string> Validate(RobotDocument d){var e=new List<string>();var s=d.AuthorDesign;if(s==null)return e;
   var a=s.Asset;
   if(s.AssetId!=Id||a==null||a.Id!=Id||a.Version!=2||a.Status!="REFERENCE_NOT_MANUFACTURING_VALIDATED"||a.Height!=1000||!MechanismCad.Finite(s.Height)||s.Height<300||s.Height>1500){e.Add("Повреждён авторский референс / масштаб.");return e;}
   if(s.JointIds==null||d.Joints.Count!=7||s.JointIds.Count!=7||s.JointIds.Distinct().Count()!=7||s.JointIds.Any(k=>d.Find(k)==null)){e.Add("Для референса нужны исходные семь звеньев. Создайте новый обычный проект для изменения состава.");return e;}
   if(a.Pivots==null||a.Pivots.Length!=7||a.Axes==null||a.Axes.Length!=7||a.Bounds==null||a.Bounds.Length!=6||a.Tip==null||a.Tip.Length!=3||a.Parts==null||a.Parts.Count<17||a.Parts.Count>64){e.Add("Повреждены данные референса.");return e;}
   if(a.Pivots.Concat(a.Axes).Concat(new[]{a.Bounds,a.Tip}).Any(v=>v==null||v.Length<3||v.Any(x=>!MechanismCad.Finite(x)||Math.Abs(x)>10000)))e.Add("Повреждены координаты референса.");
   if(a.Parts.Any(p=>p==null||string.IsNullOrEmpty(p.Id)||p.Joint<0||p.Joint>=7||string.IsNullOrEmpty(p.Preview)||p.Preview.Length>2*1024*1024)||a.Parts.Where(p=>p!=null).GroupBy(p=>p.Id).Any(g=>g.Count()>1))e.Add("Повреждены детали референса.");if(a.Parts.Sum(p=>p==null||p.Preview==null?0L:p.Preview.Length)>4*1024*1024)e.Add("Референс слишком большой.");return e;
  }
  static SceneMesh Preview(AuthorReferencePart part){using(var memory=new MemoryStream(Convert.FromBase64String(part.Preview)))using(var zip=new DeflateStream(memory,CompressionMode.Decompress))using(var reader=new BinaryReader(zip)){if(reader.ReadInt32()!=0x344D5043)throw new Exception("Повреждён формат референса.");int n=reader.ReadInt32(),count=reader.ReadInt32();if(n<3||n>150000||count<3||count>150000)throw new Exception("Превышен предел геометрии референса.");}return CADMeshCodec.Decode(part.Preview);}
  public static RobotBuild Build(RobotDocument d){var b=new RobotBuild{Document=d};b.Model.Errors.AddRange(d.Validate());if(b.Model.Errors.Count>0)return b;b.Current=RobotKinematics.Evaluate(d,0,false);
   try{var s=d.AuthorDesign;double factor=s.Height/s.Asset.Height;int budget=0;foreach(var source in s.Asset.Parts){
     var mesh=Preview(source);budget+=mesh.Points.Length+mesh.Indices.Length;if(budget>600000)throw new Exception("Превышен общий предел геометрии референса.");mesh=new SceneMesh{Points=mesh.Points.Select(p=>new Point3D(p.X*factor,p.Y*factor,p.Z*factor)).ToArray(),Normals=mesh.Normals,Indices=mesh.Indices};
     string key="author65_"+source.Id;string joint=s.JointIds[source.Joint];var part=new CadPart(key,source.Name+" · референс","reference",source.SourceType=="CURVE"?Color.FromArgb(80,83,87):Color.FromArgb(156,163,171)){MeshOverride=mesh,Reference=true};
     var entry=new RobotCadEntry{Id=key,JointId=joint,OwnerId=null,Part=part,Local=Matrix3D.Identity,InputSide=source.Fixed,Category="Референс",Printable=false,MassOverride=0};b.Entries.Add(key,entry);b.Model.Parts.Add(part);
   }foreach(var entry in b.Entries.Values)entry.Part.Placement=b.Pose(entry.Id,0,false);b.Notes.Add(Notice);b.Notes.Add("Высота в исходной позе: "+Engine.F(s.Height)+" мм. Масса исходных открытых сеток неизвестна; нулевая масса референса не является расчётом массы руки.");b.Notes.Add("Анимация показывает семь групп исходной модели. Оси, пределы ±20° и удержание захвата демонстрационные; столкновения, кабели и физическая подвижность не проверены.");
   }catch(Exception ex){b.Model.Errors.Add(ex.Message);}return b;
  }
  public static string Report(RobotDocument d){var a=d.AuthorDesign;double k=a.Height/a.Asset.Height;var bounds=a.Asset.Bounds;double payload=d.Joints.Sum(j=>j.Payload);if(AppSettings.Current.Language==GearLanguage.English)return I18n.T(Notice)+"\r\n\r\nBambu Lab P1S; PETG; 256 × 256 × 256 mm; assumed nozzle 0.4 mm.\r\n\r\nSource pose height: "+Engine.F(a.Height)+" mm\r\nX × Y envelope: "+Engine.F((bounds[3]-bounds[0])*k)+" × "+Engine.F((bounds[4]-bounds[1])*k)+" mm\r\nPayload: "+Engine.F(payload*1000)+" g\r\n\r\nPayload-only gravity torque at 1 m reach: "+Engine.F(payload*d.Gravity)+" N·m; safety factor "+Engine.F(d.SafetyFactor)+": "+Engine.F(payload*d.Gravity*d.SafetyFactor)+" N·m. Height is not reach.\r\n\r\nSource: 17 meshes, 13 with open or nonmanifold edges; 350 boundary edges. Four curves show cables. The source does not provide validated printable parts or rated drives.\r\n\r\nRequired before a complete manufacturing pack: confirm nozzle and PETG brand, NEMA17 working torque curves, transmissions, bearings/shafts and table anchoring; structural redesign, arm self-weight, fits, connections and collision checks.\r\n\r\nNative fit specimens and the audit are in docs/author-design-65. Specimens are not a complete robot print pack.";return I18n.T(Notice)+"\r\n\r\n"+
   "Bambu Lab P1S; PETG; стол 256 × 256 × 256 мм; сопло 0,4 мм — допущение.\r\n\r\nВысота исходной позы: "+Engine.F(a.Height)+" мм\r\nГабарит X × Y: "+Engine.F((bounds[3]-bounds[0])*k)+" × "+Engine.F((bounds[4]-bounds[1])*k)+" мм\r\nГруз: "+Engine.F(payload*1000)+" г\r\n"+
   "Статический момент только груза на плече 1 м: "+Engine.F(payload*d.Gravity)+" Н·м; с запасом "+Engine.F(d.SafetyFactor)+": "+Engine.F(payload*d.Gravity*d.SafetyFactor)+" Н·м. Высота 1 м не равна вылету 1 м.\r\n\r\n"+
   "Исходник: 17 сеток, 13 с открытыми/некорректными краями; 350 открытых рёбер. Кривые — визуальная проводка. Готовые детали и реальные моторы из исходника не получены.\r\n\r\n"+
   "До полного печатного комплекта нужны: подтверждение сопла и марки PETG, рабочие характеристики прежних NEMA17, передачи, подшипники/валы, крепление к столу. Затем — разделение и силовая переработка корпусов, расчёт собственного веса, посадок и проверка сборки.\r\n\r\n"+
   "Проверочные образцы посадок и полный аудит находятся в docs/author-design-65. Это отдельные образцы, они не составляют готовый манипулятор.";
  }
 }
 public sealed partial class RobotDesignerForm {
  void OpenAuthorDesign65(){using(var f=new Form{Text=I18n.T("Новый дизайн манипулятора"),Size=new Size(640,410),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false}){
   var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),FlowDirection=FlowDirection.TopDown,WrapContents=false};f.Controls.Add(flow);
   flow.Controls.Add(new Label{Text=I18n.T("Высота в исходной позе, мм"),AutoSize=true});var height=new NumericUpDown{Minimum=300,Maximum=1500,Value=(decimal)(Document.AuthorDesign==null?1000:Document.AuthorDesign.Height),Increment=50,Width=220};flow.Controls.Add(height);
   flow.Controls.Add(new Label{Text=I18n.T("Полезная нагрузка, г"),AutoSize=true});var payload=new NumericUpDown{Minimum=0,Maximum=10000,Value=(decimal)(Document.AuthorDesign==null?300:Math.Min(10000,Document.Joints.Sum(j=>j.Payload)*1000)),Increment=50,Width=220};flow.Controls.Add(payload);
   flow.Controls.Add(new Label{Text=I18n.T(AuthorDesign65.Notice)+"\n"+I18n.T("Bambu Lab P1S / PETG / сопло 0,4 мм (допущение). Выбраны прежние NEMA17; рабочий момент и передачи не подтверждены. Экспорт исходных поверхностей для печати отключён."),Width=570,Height=100,Padding=new Padding(0,12,0,0)});
   var button=new Button{Text=I18n.T("Открыть новый дизайн"),Width=280,Height=36};flow.Controls.Add(button);button.Click+=(s,e)=>{try{var d=AuthorDesign65.Create((double)height.Value,(double)payload.Value/1000);Replace(d,null);f.DialogResult=DialogResult.OK;}catch(Exception ex){UiMessage.Show(f,ex.Message);}};
   f.ShowLocalizedDialog(this);
  }}
  void ShowAuthorReport65(){if(Document.AuthorDesign==null){UiMessage.Show(this,I18n.T("Сначала откройте новый дизайн манипулятора."));return;}using(var f=new Form{Text=I18n.T("Проверка нового дизайна"),Size=new Size(850,700),StartPosition=FormStartPosition.CenterParent}){f.Controls.Add(new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text=AuthorDesign65.Report(Document),Font=new Font("Segoe UI",11)});f.ShowLocalizedDialog(this);}}
 }
}
