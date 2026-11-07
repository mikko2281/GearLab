using System;using System.IO;using System.Text;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Globalization;using System.Reflection;using System.Windows.Forms;using System.Web.Script.Serialization;

namespace GearLab {

 public interface IUserDialogs {

  string Save(IWin32Window owner,string extension,string suggested);string Open(IWin32Window owner);DialogResult Unsaved(IWin32Window owner);void Info(IWin32Window owner,string text,string title);void Help(IWin32Window owner);

 }

 public sealed class UserDialogs:IUserDialogs {

  public string Save(IWin32Window owner,string extension,string suggested){using(var d=new SaveFileDialog{Filter=extension=="gear.json"?"Проект редуктора (*.gear.json)|*.gear.json":extension.ToUpper()+" (*."+extension+")|*."+extension,DefaultExt=extension,AddExtension=true,OverwritePrompt=true,RestoreDirectory=true,InitialDirectory=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),FileName=suggested})return d.ShowLocalizedDialog(owner)==DialogResult.OK?d.FileName:null;}

  public string Open(IWin32Window owner){string examples=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Примеры");using(var d=new OpenFileDialog{Filter="Проект редуктора (*.gear.json;*.json)|*.gear.json;*.json",CheckFileExists=true,RestoreDirectory=true,InitialDirectory=Directory.Exists(examples)?examples:Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)})return d.ShowLocalizedDialog(owner)==DialogResult.OK?d.FileName:null;}

  public DialogResult Unsaved(IWin32Window owner){return UiMessage.Show(owner,"Сохранить изменения текущего проекта?\n«Нет» продолжит без сохранения.","Несохранённый проект",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);}

  public void Info(IWin32Window owner,string text,string title){UiMessage.Show(owner,text,title,MessageBoxButtons.OK,MessageBoxIcon.Information);}

  public void Help(IWin32Window owner){using(var f=new Form{Text="Gear Lab — справка",Size=new Size(780,650),MinimumSize=new Size(550,400),StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false}){

   var box=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,WordWrap=true,Font=new Font("Segoe UI",11),BackColor=Color.White,Text=I18n.English?ReleaseSupport.EnglishHelp:ReleaseSupport.HelpText};f.Controls.Add(box);f.ShowLocalizedDialog(owner);}}

 }

 public static class ReleaseSupport {

  public const string Version="6.7.1";

  public const string HelpText=@"GEARLAB 6.7.1 — КАК ПОЛЬЗОВАТЬСЯ

РЕДУКТОРЫ
В одной вкладке доступны 13 типов: циклоидальный, зубчатая пара, зубчатый ремень, планетарный, волновой, червячный, конический, RV, дифференциальный планетарный, гипоидный, глобоидный, магнитный и реечный. Используйте категории и поиск. Переключение типа сохраняет его текущие параметры.

Введите размеры и нажмите «Рассчитать» или Enter. Точка и запятая допустимы. Красные поля и сообщение объясняют несовместимые размеры. До применения ввода экспорт отключён. Общий габарит ограничивает рабочий узел в плоскости; полная сборка с мотором показана отдельно.

При автоподборе соответствующие размеры заблокированы: модуль, межосевое или радиус. Для ручного изменения выключите «Подбирать размеры автоматически». Подбор не изменяет параметры при отказе. В RV подбираются внешний радиус и модуль, в магнитном — внешний радиус. Некоторые размеры являются минимальными: например крышка не может быть тоньше размещённых опор.

Входной момент и КПД доступны для расчёта нагрузок. У червячного/глобоидного КПД вычисляется из трения, q и заходов; задавайте эти поля. Вторые обороты доступны только в режиме двух входов дифференциала. Рейка показывает скорость в мм/с и усилие в Н. Поля Lewis и допускаемого изгиба показаны только для поддержанной прямозубой пары/рейки 20°, z≥17. Для остальных типов прочность неизвестна.

Профиль, осевой вид и подшипник крупно выбираются в списке видов. В ПТК диаметр рабочих шариков определяет профиль и тела качения. Все 3D-опоры содержат кольца с дорожками, шарики и сепаратор. Параметры нескольких планетарных ступеней действуют только при нескольких ступенях. Неприменимые параметры корпуса скрыты.

3D И ДВИЖЕНИЕ
Полная/разнесённая 3D-сборка, стандартные виды, дерево и поиск деталей, скрытие/изоляция и сечения доступны в общем просмотрщике. ЛКМ вращает камеру, СКМ или Shift+ЛКМ сдвигает, колесо масштабирует, F/Home вписывает. «Запустить»/«Пауза» управляют анимацией. Поле «Скорость ×» рядом с ними меняет скорость от 0,01 до 10× сразу и сохраняет её. Перемещение отдельной детали — режим просмотра: оно не изменяет расчётный проект или STEP/STL. Esc отменяет перемещение, «Вернуть» восстанавливает положение.

ЭКСПОРТ
Меню «Экспорт»: STEP, SVG, DXF, CSV и отдельные STL. Состав в панели параметров справа применяется к экспортам: все детали / рабочие профили / печатные заготовки. Для новых передач рабочие профили исключают корпус, опоры, мотор и отдельные валы. Диалог «Для печати…» позволяет выбрать конкретные детали. Каждый STL — одна деталь в мм, нижняя точка Z=0. Скрытие и экранное сечение не меняют состав экспорта.

Точный STEP создаётся в фоне и может занимать минуты; кнопка отмены останавливает операцию. Быстрый 3D-просмотр не требует запуска CAD-ядра. ПТК и подшипники экспортируются из той же параметрической модели; сферы и тела вращения строятся в CAD-ядре. Проверьте единицы мм при импорте в CAD/слайсер.

СТУПЕНИ
Добавьте ступени кнопкой «＋ Ступень», выберите карточку и задайте отношение и размеры. Изменения применяются автоматически; стрелки ↑ ↓ меняют порядок. Сверху всегда видны итоговые отношение, скорость и момент. Дополнительные связи находятся под «Ветвления и два входа»; сумма на ветвлении не должна превышать 1. Два входа разрешены для соответствующей схемы дифференциала с совместимым балансом. Циклы и неподдержанная циркулирующая мощность отклоняются. Общая сборка, таблица скоростей/моментов, STEP и STL рассчитываются с текущими параметрами редактора. Перед экспортом сборка обновляется автоматически.

РОБОТОТЕХНИКА
Создайте дерево суставов, задайте оси, пределы, моторы, нагрузки и размеры. Контроллеры/кольца/стрелки меняют экранную позу с undo/redo и обновлением статики. Корпус после изменения геометрии требует перестроения; устаревший корпус нельзя экспортировать. Один сустав принимает последовательную цепь с одним конечным выходом; промежуточную ступень выбирать нельзя. Физические моторы программа не подключает.

Переключайтесь между редукторами, ступенями, робототехникой и настройками на верхней панели. Раздел «Свой механизм», отдельные CAD Studio и редактор полигонов удалены из пользовательского интерфейса.

ПРОЕКТЫ И НАСТРОЙКИ
Ctrl+S — сохранить, Ctrl+O — открыть. Форматы: .gear.json, .train.json, .robot.json; старые .wave.json мигрируют в общий редактор. Сохраняйте пользовательские проекты в свою папку. Настройки: оформление в стиле Blender, 17 цветов интерфейса и сцены, русский / английский язык, качество 3D, чувствительность камеры, привязка к сетке, скорость анимации и автосохранение проекта робота. Выберите раздел слева, внесите изменения и нажмите «Применить». «Отмена» отменяет только несохранённые правки; «Стандартные» готовит исходные значения до применения. Язык меняется сразу при выборе, в том числе кнопкой RU/EN в правом верхнем углу. Цвета применяются кнопкой «Применить». Оба параметра сохраняются между запусками. Панель параметров редуктора отделяется перетаскиванием заголовка или кнопкой ↗. Её размер меняется границей окна либо разделителем; ←/→ закрепляют панель слева/справа, крестик возвращает её. Кнопка ≡ показывает шкалы. Размер и положение сохраняются. Автовосстановление не заменяет обычное сохранение всех форматов.

ОГРАНИЧЕНИЯ
Теоретический T·i·КПД не равен допустимому моменту. Червячные/конические/RV/гипоидные/глобоидные профили содержат компоновочные приближения. ПТК использует жёсткое кольцо и шарики; контактные нагрузки, износ и упругость не решаются. Магнитный предельный момент неизвестен без электромагнитного расчёта. Положительный объём и валидный STEP не доказывают сопряжение зубьев или изготовимость.

B-rep-проверка относится к текущей позе и имеет лимит 250 пар; программа явно сообщает неполный результат. Полный диапазон движения, ресурс, динамика, нагрев, посадки, шпонки, крепёж, материалы и прочность требуют отдельной инженерной проверки. Подробные формулы, ограничения и отчёты находятся в docs рядом с программой.";

  public const string EnglishHelp=@"GEARLAB 6.7.1 — HELP

REDUCERS
Choose one of 13 transmission types using categories and search. Each type retains its edited parameters. Enter dimensions, then click Calculate or press Enter. Both decimal separators are accepted. Invalid fields are highlighted; exports are disabled while edits are pending. Automatic fitting locks dimensions it controls; turn it off to enter those dimensions manually. Failed fitting preserves your inputs.

The overall size limits the working mechanism in its plane; the complete motor assembly size is reported separately. Input torque and efficiency affect load calculations. Worm efficiency is calculated from friction and geometry. Rack outputs use mm/s and N. Strength estimates apply only to supported spur/rack cases; other strength limits are unknown.

The PTK reducer uses a fixed rigid ring, circular eccentric, balls and an output separator. All bearing previews include grooved races, balls and cages. These are dimensional models, with no assigned catalog load rating or service life.

VIEWPORT AND ANIMATION
Choose complete or exploded 3D, top profile, axial section or bearing close-up in the view list. Left-drag orbits; middle / Shift+left-drag pans; wheel zooms; F/Home frames all. Play/Pause controls motion. Set playback speed directly beside Play using Speed × (0.01–10×); it is saved and synchronized across workspaces. The component tree supports search, visibility and isolation. Sections and individual part offsets affect the preview; STEP/STL use assembly positions. Esc cancels dragging. Reset part/all restores positions.

EXPORT
The Export menu provides STEP, SVG, DXF, CSV and separate printable STL parts. Set the export scope in the right properties panel. Preview visibility and clipping do not change export scope. Exact STEP runs in the background and supports cancellation. The preview does not need the native CAD worker. Dimensions are in millimeters. Confirm units when importing into CAD or a slicer.

STAGES
Set motor speed and torque on the left. Add a Stage, select its card and edit ratio or dimensions on the right. Results update automatically and show output ratio, speed and torque above the assembly. Use arrows to reorder; Duplicate/Delete and Undo/Redo manage the sequence. All stage dimensions opens the full editor. Branches and two inputs contains advanced connections. Cycles and unsupported circulating power are rejected. Exports rebuild from the current editor values.

ROBOTICS
Build a joint tree and set axes, limits, motors, loads and dimensions. Joint controllers change the displayed pose with undo/redo and update static loads. Rebuild housing after geometric changes; stale housing cannot be exported. A joint accepts a sequential train with one final output. Physical motors are not connected by this application. Robot autosave creates recovery copies; save your main project file separately.

PREFERENCES
Appearance provides dark/light Blender-inspired themes. Colors provides 17 editable interface/viewport colors using swatches or #RRGGBB. 3D and animation controls preview quality, camera sensitivity, snapping and playback speed. Language switches Russian/English immediately, including the RU/EN menu in the top right. Saving configures robot recovery copies. Drag the reducer properties header or click ↗ to detach it. Resize with the splitter/window edges; ←/→ dock it left/right and X returns it to the workspace. Layout persists. Click Apply to save other preferences. Cancel discards subsequent unsaved changes. Restore defaults prepares defaults until you click Apply. Colors and language persist between sessions. Project names and numeric parameters retain their original data.

PROJECTS
Use Ctrl+S to save and Ctrl+O to open. Formats: .gear.json, .train.json, .robot.json. Older wave projects migrate to the shared PTK editor. Keep user projects in your own folder. Recovery copies do not replace normal saving. The old custom mechanism, CAD Studio and polygon editors are no longer in the user interface.

LIMITATIONS
Theoretical torque T × ratio × efficiency is not an allowable load. Worm, bevel, RV, hypoid and globoid shapes include layout approximations. PTK contact loads, wear and elasticity require separate analysis. Magnetic torque limits require electromagnetic analysis. Positive volumes and valid STEP solids do not prove tooth conjugacy or manufacturability. B-rep checks apply to the current pose and are limited to 250 pairs; incomplete results are explicitly reported. Validate strength, motion range, life, dynamics, heat, fits, fasteners and materials separately. See docs for formulas, reports and limitations.";

  public static void WriteText(string path,string text,Encoding encoding){string full=Path.GetFullPath(path),dir=Path.GetDirectoryName(full);if(!Directory.Exists(dir))throw new IOException("Папка назначения не существует.");string temp=Path.Combine(dir,".gearlab-"+Guid.NewGuid().ToString("N")+".tmp");try{File.WriteAllText(temp,text,encoding);if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}}

  public static string Serialize(Parameters p){var fields=new SortedDictionary<string,object>(StringComparer.Ordinal);foreach(var prop in typeof(Parameters).GetProperties())fields[prop.Name]=prop.GetValue(p,null);return new JavaScriptSerializer().Serialize(fields);}

  public static Parameters Parse(string text){

   if(string.IsNullOrWhiteSpace(text)||text.Length>131072)throw new Exception("Пустой или слишком большой файл проекта (максимум 128 КБ). Проект не изменён.");

   var json=new JavaScriptSerializer{MaxJsonLength=131072,RecursionLimit=16};var raw=json.DeserializeObject(text) as Dictionary<string,object>;if(raw==null)throw new Exception("Проект должен быть объектом JSON.");

   foreach(var item in raw){var property=typeof(Parameters).GetProperty(item.Key);if(property==null)throw new Exception("Неизвестное поле проекта: "+item.Key+". Проверьте версию программы.");object value=item.Value;if(property.PropertyType==typeof(bool)){if(!(value is bool))throw new Exception("Поле "+item.Key+" должно быть true/false.");}else{if(value==null||value is bool||value is string||value is Array||value is Dictionary<string,object>)throw new Exception("Поле "+item.Key+" должно содержать число.");double number=Convert.ToDouble(value,CultureInfo.InvariantCulture);if(double.IsNaN(number)||double.IsInfinity(number)||property.PropertyType==typeof(int)&&number!=Math.Truncate(number))throw new Exception("Некорректное число: "+item.Key);}}

   var p=json.Deserialize<Parameters>(text);if(p.CycloidLayout<0||p.CycloidLayout>1||p.Kind<0||p.Kind>12||p.MotorPreset<0||p.MotorPreset>=Engine.PresetNames.Length||p.PlanetStages<0||p.PlanetStages>3||p.FormatVersion!=1||p.ExportScope<0||p.ExportScope>2||p.WaveScheme<0||p.WaveScheme>1)throw new Exception("Неподдерживаемая версия, тип или набор параметров проекта.");if(p.Kind==4)WaveFastModel.Upgrade(p);return p;

  }

  public static void Export(Result result,string type,string path){if(type=="csv"&&!result.Valid){Files.Csv(result,path);return;}if(result.P.Kind==4&&type!="step"&&result.P.ExportScope==1){if(type=="csv")Files.Csv(result,path);else WaveDiagram.Export(result.Wave,path,type=="dxf");return;}var model=result.Model??CadBuilder.Build(result);if(result.P.Kind>=5){model=ReducerExportSelection.ForScope(model,result.P.ExportScope);if(type=="step")TransmissionExport.Write(model,path);else FullDrawingExport.Write(model,type,path);return;}if(result.P.ExportScope==2)model=PrintExport.Select(model,null);if(type=="step"){StepExport.Write(model,path);return;}if(result.P.ExportScope!=1){FullDrawingExport.Write(model,type,path);return;}if(type=="csv")Files.Csv(result,path);else if(type=="svg")Files.Svg(result,path);else if(type=="dxf")Files.Dxf(result,path);else throw new Exception("Неизвестный формат экспорта.");}

  public static void Report(Exception ex){string path="";try{path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GearLab","logs");Directory.CreateDirectory(path);path=Path.Combine(path,"error-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".txt");File.WriteAllText(path,"Gear Lab "+Version+Environment.NewLine+(ex==null?"Unknown error":ex.ToString()),Encoding.UTF8);}catch{path="Не удалось записать журнал.";}UiMessage.Show("Не удалось выполнить действие. Сохраните проект под другим именем, если программа продолжает работу.\nЖурнал ошибки: "+path,"Gear Lab — ошибка",MessageBoxButtons.OK,MessageBoxIcon.Error);}

 }

}


