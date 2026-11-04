using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Web.Script.Serialization;using System.Text.RegularExpressions;using System.Drawing;using System.Windows.Media.Media3D;

namespace GearLab {

 public sealed class CADEntity {public string Id{get;set;}public string Kind{get;set;}public List<double[]> Points{get;set;}public string Radius{get;set;}public string Width{get;set;}public string Height{get;set;}public string Count{get;set;}public bool Closed{get;set;}public bool Construction{get;set;}public CADEntity(){Id="e"+Guid.NewGuid().ToString("N");Kind="line";Points=new List<double[]>();Radius="10";Width="40";Height="30";Count="6";}public override string ToString(){return CADTools.EntityName(Kind);}}

 public sealed class CADConstraint {public string Kind{get;set;}public string A{get;set;}public string B{get;set;}public int PointA{get;set;}public int PointB{get;set;}public string Value{get;set;}public List<double[]> Points{get;set;}public override string ToString(){return CADTools.ConstraintName(Kind)+(string.IsNullOrEmpty(Value)?"":" = "+Value);}}

 public sealed class CADSketch {public string Plane{get;set;}public double[] Origin{get;set;}public string FaceBody{get;set;}public int Face{get;set;}public List<CADEntity> Entities{get;set;}public List<CADConstraint> Constraints{get;set;}public CADSketch(){Plane="XY";Origin=new double[3];Entities=new List<CADEntity>();Constraints=new List<CADConstraint>();}}

 public sealed class CADFeature {public string Id{get;set;}public string Name{get;set;}public string Kind{get;set;}public string Target{get;set;}public string Operation{get;set;}public bool Suppressed{get;set;}public Dictionary<string,string> Values{get;set;}public List<string> Sketches{get;set;}public List<string> Tools{get;set;}public List<int> Edges{get;set;}public List<int> Faces{get;set;}public double[] Point{get;set;}public string Data{get;set;}public double[] Transform{get;set;}public CADSketch Sketch{get;set;}public CADFeature(){Id="f"+Guid.NewGuid().ToString("N");Name="Операция";Kind="box";Operation="new";Values=new Dictionary<string,string>();Sketches=new List<string>();Tools=new List<string>();Edges=new List<int>();Faces=new List<int>();}public override string ToString(){return Name;}}

 public sealed class CADMaterial {public string Name{get;set;}public double Density{get;set;}public int Color{get;set;}public CADMaterial(){Name="Алюминий";Density=2700;Color=System.Drawing.Color.FromArgb(165,186,204).ToArgb();}}

 public sealed class CADDocument {

  public int FormatVersion{get;set;}public string Name{get;set;}public Dictionary<string,string> Parameters{get;set;}public List<CADFeature> Features{get;set;}public Dictionary<string,CADMaterial> Materials{get;set;}public List<string> Hidden{get;set;}

  public CADDocument(){FormatVersion=1;Name="Новая CAD-модель";Parameters=new Dictionary<string,string>();Features=new List<CADFeature>();Materials=new Dictionary<string,CADMaterial>();Hidden=new List<string>();}

  internal static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=128*1024*1024,RecursionLimit=100};}

  public string Serialize(){return Serializer().Serialize(this);}public CADDocument Clone(){return Serializer().Deserialize<CADDocument>(Serialize());}

  public void Validate(){if(FormatVersion!=1||Features==null||Parameters==null||Materials==null||Hidden==null||Features.Count>300)throw new Exception("Неподдерживаемый или слишком большой CAD-проект.");var ids=new HashSet<string>();foreach(var f in Features){if(f==null||!Regex.IsMatch(f.Id??"","^[A-Za-z0-9_]{1,64}$")||!ids.Add(f.Id)||f.Values==null)throw new Exception("Повреждена история операций.");if(f.Kind=="sketch"&&(f.Sketch==null||f.Sketch.Entities==null||f.Sketch.Constraints==null||f.Sketch.Entities.Count>90))throw new Exception("Повреждён эскиз.");}foreach(string key in Parameters.Keys)if(!Regex.IsMatch(key,"^[A-Za-z_][A-Za-z0-9_]*$"))throw new Exception("Имена параметров: латинские буквы, цифры и подчёркивания.");}

  public static CADDocument Parse(string text){if(text==null||text.Length>128*1024*1024)throw new Exception("CAD-проект слишком большой.");var d=Serializer().Deserialize<CADDocument>(text);if(d==null)throw new Exception("Не удалось прочитать CAD-проект.");d.Validate();return d;}

  public void Save(string path){Validate();ReleaseSupport.WriteText(path,Serialize(),new UTF8Encoding(true));}

  public static CADDocument ImportStep(byte[] bytes,string name){var d=new CADDocument{Name=name};d.Features.Add(new CADFeature{Kind="import",Name=name,Data=Convert.ToBase64String(bytes)});return d;}

  public static CADDocument Example(){var d=new CADDocument{Name="Параметрический кронштейн"};d.Parameters["width"]="60";d.Parameters["depth"]="40";d.Parameters["height"]="18";var a=CADTools.Feature("box");a.Name="Основание";a.Values["Width"]="width";a.Values["Depth"]="depth";a.Values["Height"]="height";d.Features.Add(a);var f=CADTools.Feature("fillet");f.Name="Скругление вертикальных рёбер";f.Target=a.Id;f.Edges.AddRange(new[]{0,2,4,6});f.Values["Radius"]="3";d.Features.Add(f);return d;}

 }

 public sealed class CADFace {public int Id{get;set;}public string Kind{get;set;}public double[] Center{get;set;}public double[] Normal{get;set;}public double Area{get;set;}public double Radius{get;set;}public override string ToString(){return "Грань "+(Id+1)+" · "+(Kind=="plane"?"плоскость":Kind=="cylinder"?"цилиндр":"поверхность");}}

 public sealed class CADEdge {public int Id{get;set;}public double Length{get;set;}public double[][] Points{get;set;}public override string ToString(){return "Ребро "+(Id+1)+" · "+Engine.F(Length)+" мм";}}

 public sealed class CADBody {public string Id{get;set;}public double Volume{get;set;}public double[] Center{get;set;}public double[] Bounds{get;set;}public List<CADFace> Faces{get;set;}public List<CADEdge> Edges{get;set;}public string Mesh{get;set;}public int Triangles{get;set;}[ScriptIgnore]public SceneMesh Display;[ScriptIgnore]public int[] TriangleFaces;public void ReadMesh(){using(var r=new BinaryReader(File.OpenRead(Mesh))){if(r.ReadInt32()!=0x34444143)throw new Exception("Неверный формат предпросмотра.");int n=r.ReadInt32();if(n<0||n>3000000)throw new Exception("Слишком сложный предпросмотр.");var p=new Point3D[n*3];var normals=new Vector3D[n*3];var indices=new int[n*3];TriangleFaces=new int[n];for(int i=0;i<n;i++){TriangleFaces[i]=r.ReadInt32();for(int k=0;k<3;k++){int j=i*3+k;p[j]=new Point3D(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());normals[j]=new Vector3D(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());indices[j]=j;}}Display=new SceneMesh{Points=p,Normals=normals,Indices=indices};}}}

 public sealed class CADKernelResult {public TransmissionAuditResult Audit{get;set;}public RobotMotionReport Motion{get;set;}public ProceduralBodyResult Procedural{get;set;}public bool Ok{get;set;}public string Error{get;set;}public List<CADBody> Bodies{get;set;}public Dictionary<string,double> Parameters{get;set;}public Dictionary<string,double[][]> SketchFrames{get;set;}public string Path{get;set;}public string Step{get;set;}public double Distance{get;set;}public CADSketch Sketch{get;set;}public Dictionary<string,double> ConstraintStatus{get;set;}

  public CadAssembly Assembly(CADDocument doc){var a=new CadAssembly();foreach(var b in Bodies??new List<CADBody>()){b.ReadMesh();CADMaterial mat;if(!doc.Materials.TryGetValue(b.Id,out mat))mat=new CADMaterial();var p=new CadPart(b.Id,CADTools.BodyName(doc,b.Id),"cad",Color.FromArgb(mat.Color)){MeshOverride=b.Display};var bounds=b.Bounds;p.Sections.Add(new CadSection(bounds[2],bounds[5],CadLoop.Polygon(new[]{new PointF((float)bounds[0],(float)bounds[1]),new PointF((float)bounds[3],(float)bounds[1]),new PointF((float)bounds[3],(float)bounds[4]),new PointF((float)bounds[0],(float)bounds[4])}),null));a.Parts.Add(p);}return a;}}

 public static class CADTools {

  public static string BodyName(CADDocument doc,string id){var f=doc.Features.FirstOrDefault(x=>x.Id==id||id.StartsWith(x.Id+"_",StringComparison.Ordinal));return f==null?"Тело":f.Kind=="wave"?f.Name+" · "+(id.EndsWith("_ring")?"жёсткий венец":id.EndsWith("_flex")?"гибкое колесо":id.EndsWith("_output")?"выходной венец и фланец":id.Contains("_bolt")?"крепёж фланца":id.EndsWith("_generator")?"генератор волн":"подшипник"):f.Name+(id==f.Id?"":" · копия");}

  public static string EntityName(string k){switch(k){case "line":return "Линия";case "polyline":return "Ломаная";case "rectangle":return "Прямоугольник";case "circle":return "Окружность";case "arc":return "Дуга по 3 точкам";case "polygon":return "Многоугольник";case "slot":return "Паз";case "spline":return "Сплайн";default:return k;}}

  public static string ConstraintName(string k){switch(k){case "horizontal":return "Горизонтальность";case "vertical":return "Вертикальность";case "coincident":return "Совпадение точек";case "concentric":return "Концентричность";case "parallel":return "Параллельность";case "perpendicular":return "Перпендикулярность";case "equal":return "Равенство";case "tangent":return "Касание";case "fixed":return "Зафиксировать";case "distance":return "Расстояние";case "length":return "Длина";case "radius":return "Радиус";case "angle":return "Угол";default:return k;}}

  public static string Name(string k){switch(k){case "sketch":return "Эскиз";case "box":return "Параллелепипед";case "cylinder":return "Цилиндр";case "sphere":return "Сфера";case "extrude":return "Выдавливание";case "revolve":return "Вращение";case "loft":return "Лофт";case "sweep":return "Протяжка по пути";case "hole":return "Отверстие";case "presspull":return "Нажать / вытянуть";case "fillet":return "Скругление";case "chamfer":return "Фаска";case "shell":return "Оболочка";case "draft":return "Литейный уклон";case "combine":return "Объединить / вырезать";case "split":return "Разделить тело";case "move":return "Переместить / копировать";case "scale":return "Масштаб";case "mirror":return "Зеркало";case "linear_pattern":return "Линейный массив";case "circular_pattern":return "Круговой массив";case "wave":return "Волновой редуктор";case "gear":return "Шестерня";case "import":return "Импорт STEP";default:return k;}}

  public static CADFeature Feature(string k){var f=new CADFeature{Kind=k,Name=Name(k)};Action<string,string> add=(a,b)=>f.Values[a]=b;switch(k){case "box":add("Width","40");add("Depth","30");add("Height","20");break;case "cylinder":add("Radius","15");add("Height","30");break;case "sphere":add("Radius","15");break;case "extrude":case "presspull":add("Distance",k=="extrude"?"20":"5");break;case "revolve":add("Angle","360");add("AxisX","0");break;case "fillet":case "chamfer":add("Radius","2");break;case "shell":add("Thickness","2");break;case "hole":add("Diameter","5");add("Depth","20");add("Through","false");break;case "draft":add("Angle","3");add("NeutralZ","0");break;case "combine":add("Mode","join");add("KeepTools","false");break;case "move":foreach(string key in new[]{"X","Y","Z","RX","RY","RZ"})add(key,"0");add("Copy","false");break;case "scale":add("Factor","1.1");foreach(string key in new[]{"X","Y","Z"})add(key,"0");add("Copy","false");break;case "mirror":case "split":add("Plane","YZ");foreach(string key in new[]{"X","Y","Z"})add(key,"0");if(k=="mirror")add("Copy","true");break;case "linear_pattern":add("Count","4");add("X","30");add("Y","0");add("Z","0");break;case "circular_pattern":add("Count","6");add("Angle","360");foreach(string key in new[]{"X","Y","Z"})add(key,"0");break;}return f;}

  public static string Field(string key){switch(key){case "Ratio":return "Передача |i|";case "CupDepth":return "Глубина чашки, мм";case "Wall":return "Стенка чашки, мм";case "RingWall":return "Стенка венца, мм";case "OutputBore":return "Выходное отверстие Ø, мм";case "Clearance":return "Зазор, мм";case "BearingWall":return "Толщина подшипника, мм";case "Phase":return "Угол входа, °";case "InputTorque":return "Входной момент, Н·м";case "InputRpm":return "Вход, об/мин";case "Efficiency":return "КПД (0–1)";case "LoadTorque":return "Нагрузка выхода, Н·м";case "Width":return "Ширина X, мм";case "Depth":return "Глубина, мм";case "Height":return "Высота, мм";case "Radius":return "Радиус, мм";case "Distance":return "Расстояние, мм";case "Diameter":return "Диаметр, мм";case "Thickness":return "Толщина, мм";case "Angle":return "Угол, °";case "Count":return "Количество";case "Through":return "Сквозное";case "Copy":return "Создать копию";case "KeepTools":return "Сохранить инструменты";case "Plane":return "Плоскость";case "Mode":return "Операция";case "AxisX":return "Ось вращения: X в эскизе";case "NeutralZ":return "Нейтральная плоскость Z, мм";case "Factor":return "Коэффициент";case "Teeth":return "Число зубьев";case "Module":return "Модуль, мм";case "Pressure":return "Угол зацепления, °";case "Backlash":return "Боковой зазор, мм";case "Helix":return "Наклон зуба, °";case "Bore":return "Отверстие Ø, мм";case "Outer":return "Наружный Ø кольца, мм";case "Base":return "Высота основания, мм";default:return key+(key.Length<3?" (мм / °)":"");}}

 }

}



