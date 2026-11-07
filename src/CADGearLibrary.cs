using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;using System.Windows.Forms.Integration;

namespace GearLab {

 public sealed class CADGearPreset {public string Name,Group,Description;public Dictionary<string,string> Values;public override string ToString(){return Name;}}

 public static class CADGearCatalog {

  public static readonly List<CADGearPreset> All=Create();

  static CADGearPreset Make(string name,string group,string type,int z,double module,double width,double bore,double helix=20){var p=new CADGearPreset{Name=name,Group=group,Description="Эвольвентный профиль · угол зацепления 20° · размеры в мм",Values=new Dictionary<string,string>{{"Type",type},{"Teeth",z.ToString()},{"Module",Engine.N(module)},{"Thickness",Engine.N(width)},{"Bore",Engine.N(bore)},{"Pressure","20"},{"Backlash","0.05"}}};if(type=="helical"||type=="herringbone"){p.Values["Helix"]=Engine.N(helix);p.Description="Торцевой модуль и угол зацепления. Наклон зубьев "+Engine.F(helix)+"°. Подшипники и прочность проверяются отдельно.";}if(type=="internal"){p.Values["Outer"]=Engine.N(module*(z+10));p.Values.Remove("Bore");p.Description="Внутренний зубчатый венец · редактируемая толщина кольца.";}if(type=="rack"){p.Values["Base"]=Engine.N(Math.Max(6,module*5));p.Values.Remove("Bore");p.Description="Прямая рейка · шаг π × модуль · основание и ширина редактируются.";}return p;}

  static List<CADGearPreset> Create(){var a=new List<CADGearPreset>();a.Add(Make("Малая 20T · m0,5 · Ø10", "Прямозубые","spur",20,.5,6,3));a.Add(Make("Шестерня 20T · m1 · Ø20","Прямозубые","spur",20,1,8,5));a.Add(Make("Шестерня 24T · m1 · Ø24","Прямозубые","spur",24,1,10,5));a.Add(Make("Шестерня 32T · m1 · Ø32","Прямозубые","spur",32,1,10,8));a.Add(Make("Шестерня 40T · m1 · Ø40","Прямозубые","spur",40,1,12,8));a.Add(Make("Шестерня 60T · m1 · Ø60","Прямозубые","spur",60,1,12,10));a.Add(Make("Силовая 24T · m2 · Ø48","Прямозубые","spur",24,2,16,10));a.Add(Make("Силовая 40T · m2 · Ø80","Прямозубые","spur",40,2,20,12));

   foreach(int z in new[]{20,24,32,40}){a.Add(Make("Правая "+z+"T · m1 · β20°","Косозубые","helical",z,1,12,5,20));a.Add(Make("Левая "+z+"T · m1 · β−20°","Косозубые","helical",z,1,12,5,-20));}foreach(int z in new[]{20,24,32,40})a.Add(Make("Шеврон "+z+"T · m1 · β25°","Шевронные","herringbone",z,1,14,6,25));foreach(int z in new[]{20,24,32,40})a.Add(Make("Ответный шеврон "+z+"T · m1 · β−25°","Шевронные","herringbone",z,1,14,6,-25));foreach(int z in new[]{36,48,60,80})a.Add(Make("Кольцо "+z+"T · m1","Внутренние венцы","internal",z,1,12,0));foreach(int z in new[]{8,12,20,30})a.Add(Make("Рейка "+z+" зубьев · m1","Зубчатые рейки","rack",z,1,10,0));return a;}

 }

 public sealed class CADGearLibrary:Form {

  readonly CADKernel kernel;readonly Func<CADFeature,Task<bool>> apply;readonly Dictionary<string,string> parameters;readonly ListBox list=new ListBox();readonly ComboBox group=new EditorComboBox();readonly TextBox search=new TextBox();readonly Panel editorHost=new Panel();readonly Label info=new Label();readonly Button build=new Button(),insert=new Button();readonly WpfScene scene=new WpfScene();readonly ElementHost host=new ElementHost();CADValueEditor editor;CADGearPreset selected;int revision;bool busy,disposed,previewRunning,previewPending;

  public CADGearLibrary(CADKernel worker,Func<CADFeature,Task<bool>> callback,Dictionary<string,string> parentParameters=null){kernel=worker;apply=callback;parameters=parentParameters??new Dictionary<string,string>();Text="Библиотека зубчатых деталей · "+CADGearCatalog.All.Count+" пресетов";Font=new Font("Segoe UI",10);Size=new Size(1160,900);MinimumSize=new Size(960,720);StartPosition=FormStartPosition.CenterParent;var split=new SplitContainer{Size=new Size(1120,850),Dock=DockStyle.Fill,SplitterDistance=380,Panel1MinSize=300,Panel2MinSize=550};Controls.Add(split);var top=new Panel{Dock=DockStyle.Top,Height=84,Padding=new Padding(8)};group.Dock=DockStyle.Top;group.DropDownStyle=ComboBoxStyle.DropDownList;group.Items.Add("Все типы");group.Items.AddRange(CADGearCatalog.All.Select(p=>p.Group).Distinct().ToArray());group.SelectedIndex=0;search.Dock=DockStyle.Bottom;top.Controls.Add(group);top.Controls.Add(search);list.Dock=DockStyle.Fill;list.DrawMode=DrawMode.OwnerDrawFixed;list.ItemHeight=76;list.BorderStyle=BorderStyle.None;list.DrawItem+=DrawPreset;split.Panel1.Controls.Add(list);split.Panel1.Controls.Add(top);group.SelectedIndexChanged+=(s,e)=>Filter();search.TextChanged+=(s,e)=>Filter();list.SelectedIndexChanged+=(s,e)=>SelectPreset();

   var right=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,Size=new Size(720,850),SplitterDistance=350,Panel1MinSize=240,Panel2MinSize=280};split.Panel2.Controls.Add(right);host.Dock=DockStyle.Fill;host.Child=scene.Root;right.Panel1.Controls.Add(host);editorHost.Dock=DockStyle.Fill;editorHost.AutoScroll=true;right.Panel2.Controls.Add(editorHost);info.Dock=DockStyle.Bottom;info.Height=78;info.Padding=new Padding(10);right.Panel2.Controls.Add(info);var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=58,Padding=new Padding(10)};build.Text="Обновить 3D";build.Width=150;build.Height=34;build.Click+=(s,e)=>Preview();insert.Text="Вставить в модель";insert.Width=185;insert.Height=34;insert.Click+=async(s,e)=>{if(busy||selected==null)return;busy=true;try{if(await apply(ReadFeature())){busy=false;DialogResult=DialogResult.OK;Close();}}finally{busy=false;}};var cancel=new Button{Text="Закрыть",Width=110,Height=34,DialogResult=DialogResult.Cancel};actions.Controls.AddRange(new Control[]{build,insert,cancel});Controls.Add(actions);CancelButton=cancel;Filter();Shown+=(s,e)=>Preview();FormClosing+=(s,e)=>{if(busy){e.Cancel=true;info.Text="Дождитесь построения.";}};}

  void Filter(){var old=selected;list.BeginUpdate();list.Items.Clear();foreach(var p in CADGearCatalog.All.Where(p=>(group.SelectedIndex==0||p.Group==group.Text)&&p.Name.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0))list.Items.Add(p);list.EndUpdate();if(list.Items.Count>0)list.SelectedIndex=0;}

  void SelectPreset(){selected=list.SelectedItem as CADGearPreset;if(selected==null)return;revision++;editorHost.Controls.Clear();editor=new CADValueEditor(selected.Values.Where(p=>p.Key!="Type").ToDictionary(p=>p.Key,p=>p.Value)){Dock=DockStyle.Top};editorHost.Controls.Add(editor);info.Text=selected.Description;Preview();}

  CADFeature ReadFeature(){return new CADFeature{Kind="gear",Name=selected.Name,Values=editor.Values().Concat(new[]{new KeyValuePair<string,string>("Type",selected.Values["Type"])}).ToDictionary(p=>p.Key,p=>p.Value)};}

  async void Preview(){if(selected==null||editor==null||disposed)return;if(previewRunning){previewPending=true;revision++;return;}previewRunning=true;previewPending=false;int ticket=++revision;var f=ReadFeature();f.Id="gear_preview";var doc=new CADDocument{Parameters=new Dictionary<string,string>(parameters)};doc.Features.Add(f);build.Enabled=false;insert.Enabled=false;info.Text="Построение CAD-шестерни…";try{var result=await Task.Factory.StartNew(()=>kernel.Run(doc));if(disposed||ticket!=revision)return;scene.SetResult(new Result{P=new Parameters{Kind=9},Model=result.Assembly(doc)},true);var b=result.Bodies[0];info.Text=selected.Description+"\nОбъём "+Engine.F(b.Volume)+" мм³ · "+b.Faces.Count+" граней · делительный Ø = z × m";}catch(Exception ex){if(!disposed&&ticket==revision)info.Text=ex.GetBaseException().Message;}finally{previewRunning=false;if(!disposed){if(previewPending)Preview();else{build.Enabled=true;insert.Enabled=true;}}}}

  void DrawPreset(object sender,DrawItemEventArgs e){if(e.Index<0)return;var p=(CADGearPreset)list.Items[e.Index];bool active=(e.State&DrawItemState.Selected)!=0;e.Graphics.FillRectangle(active?new SolidBrush(Color.FromArgb(225,238,252)):Brushes.White,e.Bounds);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float cx=e.Bounds.X+35,cy=e.Bounds.Y+36;var points=new List<PointF>();for(int i=0;i<48;i++){double a=Engine.Tau*i/48,r=i%4<2?24:18;points.Add(new PointF(cx+(float)(r*Math.Cos(a)),cy+(float)(r*Math.Sin(a))));}e.Graphics.FillPolygon(Brushes.SteelBlue,points.ToArray());e.Graphics.FillEllipse(Brushes.White,cx-7,cy-7,14,14);using(var font=new Font(Font,FontStyle.Bold))e.Graphics.DrawString(p.Name,font,CADBrush.Text,e.Bounds.X+70,e.Bounds.Y+12);e.Graphics.DrawString(p.Group,Font,Brushes.SlateGray,e.Bounds.X+70,e.Bounds.Y+40);e.DrawFocusRectangle();}

  protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;revision++;scene.Dispose();host.Child=null;}base.Dispose(disposing);}

 }

 static class CADBrush {public static Brush Text=new SolidBrush(Color.FromArgb(36,58,83));}

}





