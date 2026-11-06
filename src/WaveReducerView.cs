using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;using System.Windows.Forms.Integration;

namespace GearLab {

 public sealed partial class WaveReducerForm:Form {

  readonly CADKernel kernel=new CADKernel();readonly WpfScene scene=new WpfScene();readonly ElementHost host=new ElementHost();readonly WaveDiagram diagram=new WaveDiagram();readonly Timer timer=new Timer();readonly Label metrics=new Label(),status=new Label(),heading=new Label();readonly CADValueEditor editor;readonly FlowLayoutPanel actions=new FlowLayoutPanel();readonly ComboBox view=new ComboBox();readonly DataGridView parts=new DataGridView();readonly TextBox notes=new TextBox();readonly List<Button> nativeButtons=new List<Button>();readonly NumericUpDown speed=new NumericUpDown();readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();

  WaveParameters model;CADDocument document;bool busy,playing,closing,disposed;double phase,last;Func<CADFeature,Task<bool>> insert;CADKernel shared;Action returnToTarget;bool hasTargetButton;

  internal bool TestReady{get{return model!=null&&diagram.Parameters!=null&&!busy;}}internal bool NativeReady{get{return document!=null&&scene.Ready&&scene.Nodes.Count>0&&!busy;}}internal WaveParameters Parameters{get{return model;}}internal WaveDiagram Diagram{get{return diagram;}}internal bool IsBusy{get{return busy;}}

  public WaveReducerForm():this(null,null){}

  public WaveReducerForm(CADKernel sharedKernel,Func<CADFeature,Task<bool>> callback){shared=sharedKernel;insert=callback;Text="Редукторы · Волновой";Font=new Font("Segoe UI",10);Size=new Size(1480,940);BackColor=Color.White;

   var split=new SplitContainer{Dock=DockStyle.Fill,Size=new Size(1450,870),SplitterDistance=325,FixedPanel=FixedPanel.Panel1,Panel1MinSize=300,Panel2MinSize=500};var left=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(241,245,249)};split.Panel1.Controls.Add(left);

   var fields=new Dictionary<string,string>{{"Ratio","50"},{"Module","0.6"},{"Thickness","8"},{"Wall","0.8"},{"RingWall","4"},{"Bore","5"},{"OutputBore","8"},{"Clearance","0.15"},{"BearingWall","1.5"},{"InputTorque","0.35"},{"InputRpm","60"},{"Efficiency","0.7"},{"LoadTorque","5"}};

   editor=new CADValueEditor(fields){Dock=DockStyle.Top,Height=fields.Count*38+10};foreach(Control field in editor.Controls){if(field is TextBox)field.TextChanged+=(s,e)=>InvalidateNative();var label=field as Label;if(label!=null&&label.Text=="Стенка чашки, мм")label.Text="Стенка гибкого колеса, мм";if(label!=null&&label.Text=="Выходное отверстие Ø, мм")label.Text="Выходной вал Ø, мм";if(label!=null&&label.Text=="Толщина, мм")label.Text="Ширина ряда зубьев, мм";}

   var presets=new FlowLayoutPanel{Dock=DockStyle.Top,Height=43,Padding=new Padding(8,3,0,0)};foreach(int ratio in new[]{50,80,100}){int r=ratio;var b=new Button{Text=r+":1",Width=90,Height=30};b.Click+=(s,e)=>{if(busy)return;editor.SetValue("Ratio",r.ToString());Calculate();};presets.Controls.Add(b);}var caption=new Label{Dock=DockStyle.Top,Height=52,Padding=new Padding(12,6,0,0),Text="ПЛОСКИЙ ВОЛНОВОЙ РЕДУКТОР\nРазмеры и нагрузка",ForeColor=Color.FromArgb(34,70,102)};left.Controls.Add(editor);left.Controls.Add(presets);left.Controls.Add(caption);

   var right=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(16,8,12,8)};right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));right.RowStyles.Add(new RowStyle(SizeType.Absolute,36));right.RowStyles.Add(new RowStyle(SizeType.Absolute,100));right.RowStyles.Add(new RowStyle(SizeType.Absolute,82));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));right.RowStyles.Add(new RowStyle(SizeType.Absolute,38));split.Panel2.Controls.Add(right);

   heading.Dock=DockStyle.Fill;heading.Font=new Font("Segoe UI",14,FontStyle.Bold);heading.ForeColor=Color.FromArgb(20,65,105);right.Controls.Add(heading,0,0);metrics.Dock=DockStyle.Fill;metrics.Padding=new Padding(10,6,6,6);metrics.BackColor=Color.FromArgb(234,244,242);right.Controls.Add(metrics,0,1);

   actions.Dock=DockStyle.Fill;Add("Рассчитать",()=>Calculate());Add("Построить 3D",Build);Add("Запустить",Toggle);Add("Вписать",()=>{diagram.Zoom=1;diagram.Invalidate();if(document!=null)scene.Fit();});Add("Сохранить…",Save);Add("Открыть…",Open);Add("SVG",()=>ExportDiagram(false));Add("DXF",()=>ExportDiagram(true));Add("CSV деталей",ExportCsv);NativeButton("STEP",()=>Export("export_step"));NativeButton("STL",()=>Export("export_stl"));Add("Отмена расчёта",()=>kernel.Cancel());view.DropDownStyle=ComboBoxStyle.DropDownList;view.Width=220;view.Items.AddRange(new[]{"Рабочая схема сверху","Осевая схема","3D сборка","Разнесённая 3D сборка"});view.SelectedIndex=0;view.SelectedIndexChanged+=(s,e)=>{diagram.Axial=view.SelectedIndex==1;diagram.Visible=view.SelectedIndex<2;host.Visible=!diagram.Visible;scene.Exploded=view.SelectedIndex==3;if(host.Visible){host.BringToFront();if(document==null)Build();else scene.UpdatePose(phase);}else diagram.BringToFront();diagram.Invalidate();};actions.Controls.Add(view);speed.Minimum=.01m;speed.Maximum=10;speed.DecimalPlaces=2;speed.Value=.25m;speed.Width=65;actions.Controls.Add(new Label{Text="Скорость ×",AutoSize=true,Padding=new Padding(2,6,0,0)});actions.Controls.Add(speed);right.Controls.Add(actions,0,2);

   var tabs=new EditorTabs{Dock=DockStyle.Fill};TabPage drawing=new TabPage("Чертёж"),details=new TabPage("Детали и размеры"),checks=new TabPage("Проверки и допущения");tabs.TabPages.AddRange(new[]{drawing,details,checks});diagram.Dock=DockStyle.Fill;drawing.Controls.Add(diagram);host.Dock=DockStyle.Fill;host.Child=scene.Root;host.Visible=false;drawing.Controls.Add(host);parts.Dock=DockStyle.Fill;parts.AllowUserToAddRows=false;parts.ReadOnly=true;parts.RowHeadersVisible=false;parts.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;parts.Columns.Add("part","Деталь");parts.Columns.Add("size","Размер / назначение");parts.Columns.Add("count","Количество");details.Controls.Add(parts);notes.Dock=DockStyle.Fill;notes.Multiline=true;notes.ReadOnly=true;notes.ScrollBars=ScrollBars.Vertical;notes.BackColor=Color.White;checks.Controls.Add(notes);right.Controls.Add(tabs,0,3);status.Dock=DockStyle.Fill;status.Padding=new Padding(4,6,0,0);right.Controls.Add(status,0,4);Controls.Add(split);

   timer.Interval=50;timer.Tick+=(s,e)=>{double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.15,now-last);last=now;if(playing&&model!=null&&!busy){phase+=dt*model.InputRpm*Math.PI/30*(double)speed.Value;diagram.Phase=phase;diagram.Invalidate();if(host.Visible&&document!=null)scene.UpdatePose(phase);}};timer.Start();Shown+=(s,e)=>Calculate();FormClosing+=(s,e)=>{if(busy&&!closing){status.Text="Сначала дождитесь построения или отмените расчёт.";e.Cancel=true;}};Calculate();

  }

  void InvalidateNative(){if(model==null)return;document=null;playing=false;foreach(var b in nativeButtons)b.Enabled=false;status.Text="Параметры изменены. Нажмите «Рассчитать».";}

  void Add(string text,Action action){var b=new Button{Text=text,AutoSize=true,Height=30,Margin=new Padding(2,2,4,2)};b.Click+=(s,e)=>action();actions.Controls.Add(b);}

  void NativeButton(string text,Action action){Add(text,action);var b=(Button)actions.Controls[actions.Controls.Count-1];nativeButtons.Add(b);b.Enabled=false;}

  WaveParameters Read(){var v=editor.Values();Func<string,double> n=k=>CADExpression.Evaluate(v[k],null);double ratio=n("Ratio");if(ratio!=Math.Round(ratio))throw new Exception("Передаточное число должно быть целым.");var p=new WaveParameters{Flat=true,Ratio=(int)ratio,Module=n("Module"),Thickness=n("Thickness"),Wall=n("Wall"),RingWall=n("RingWall"),Bore=n("Bore"),OutputBore=n("OutputBore"),Clearance=n("Clearance"),BearingWall=n("BearingWall"),InputTorque=n("InputTorque"),InputRpm=n("InputRpm"),Efficiency=n("Efficiency"),LoadTorque=n("LoadTorque")};p.Validate();return p;}

  internal bool Calculate(){if(busy)return false;try{var p=Read();document=null;model=p;phase=0;diagram.Parameters=p;diagram.Phase=0;diagram.Invalidate();heading.Text="Волновой / "+p.Ratio+":1 · плоская компоновка";metrics.ForeColor=Color.FromArgb(29,89,78);metrics.Text="Зубья: гибкий венец "+p.FlexTeeth+", неподвижный "+p.RingTeeth+", выходной "+p.FlexTeeth+".\nВыход: "+Engine.F(p.OutputRpm)+" об/мин · расчётный момент "+Engine.F(p.OutputTorque)+" Н·м · вход "+Engine.F(p.InputPower)+" Вт.\nДля нагрузки "+Engine.F(p.LoadTorque)+" Н·м требуется "+Engine.F(p.RequiredTorque)+" Н·м на входе. "+(p.OutputTorque>=p.LoadTorque?"Заданного момента достаточно.":"Заданного момента недостаточно.");parts.Rows.Clear();foreach(var row in WaveDiagram.Rows(p))parts.Rows.Add(row);notes.Text="Плоская схема: неподвижный венец z+2, гибкое колесо z и выходной венец z.\r\nВыход вращается противоположно входу: ωвых/ωвх = −2/z = −1/i.\r\nГенератор волн задаёт эллиптическую деформацию; гибкое колесо соединяет два осевых ряда зубьев.\r\nЗубья — обычная эвольвента для компоновки. Упругая деформация, контакт и допустимая нагрузка не рассчитаны.\r\nКПД задаётся пользователем; момент двигателя на оборотах берётся из его характеристики.\r\nКрепёж неподвижного фланца не связывает его с вращающимся выходом.\r\n3D строится отдельно по кнопке и экспортируется точными B-rep телами.\r\nИсточник принципа: https://www.harmonicdrive.net/products/component-sets/pancake/fb-2/fb-25-50-2-gr";foreach(var b in nativeButtons)b.Enabled=false;status.ForeColor=Color.SlateGray;status.Text="Схема рассчитана. Для объёмной модели нажмите «Построить 3D».";return true;}catch(Exception ex){Error(ex);return false;}}

  void Toggle(){if(model==null)return;playing=!playing;last=watch.Elapsed.TotalSeconds;}

  internal async void Build(){if(busy||!Calculate())return;busy=true;editor.Enabled=false;view.SelectedIndex=2;playing=false;status.Text="Построение точных тел… Схема доступна во вкладке «Чертёж».";try{var p=model;var doc=p.Document();var result=await Task.Factory.StartNew(()=>kernel.Run(doc));if(disposed)return;document=doc;scene.SetResult(new Result{P=new Parameters{Kind=4},Actual=p.Ratio,Model=result.Assembly(doc),Wave=p},true);var ready=System.Diagnostics.Stopwatch.StartNew();while(!scene.Ready&&scene.Error==null&&ready.Elapsed.TotalSeconds<120&&!disposed)await Task.Delay(30);if(disposed)return;if(!scene.Ready)throw new Exception(scene.Error??"Не удалось подготовить сцену 3D.");scene.Fit();foreach(var b in nativeButtons)b.Enabled=true;status.Text="Готово: "+result.Bodies.Count+" точных тел. STEP и STL доступны.";}catch(Exception ex){Error(ex);}finally{busy=false;if(!disposed)editor.Enabled=true;}}

  internal void SaveNativePreview(string path){scene.SavePng(path,1100,800);}

  void Error(Exception ex){status.ForeColor=Color.Firebrick;status.Text=ex.GetBaseException().Message;}

  internal void ConfigureTarget(CADKernel k,Func<CADFeature,Task<bool>> callback,Action done){shared=k;insert=callback;returnToTarget=done;if(!hasTargetButton){NativeButton("Добавить в сборку",Insert);hasTargetButton=true;}}

  CADDocument Current(){var d=document.Clone();d.Features[0].Values["Phase"]=Engine.N(phase*180/Math.PI);return d;}

  void Save(){if(busy||!Calculate())return;using(var d=new SaveFileDialog{Filter="Проект волнового редуктора|*.wave.json",FileName="Волновой_"+model.Ratio+".wave.json"})if(d.ShowLocalizedDialog(this)==DialogResult.OK)try{File.WriteAllText(d.FileName,CADDocument.Serializer().Serialize(model),Encoding.UTF8);}catch(Exception ex){Error(ex);}}

  void Open(){if(busy)return;using(var d=new OpenFileDialog{Filter="Проект волнового редуктора|*.wave.json"})if(d.ShowLocalizedDialog(this)==DialogResult.OK)try{if(new FileInfo(d.FileName).Length>65536)throw new Exception("Файл проекта слишком большой.");var p=CADDocument.Serializer().Deserialize<WaveParameters>(File.ReadAllText(d.FileName));p.Validate();foreach(string key in editor.Values().Keys){var f=typeof(WaveParameters).GetField(key);editor.SetValue(key,Convert.ToString(f.GetValue(p),System.Globalization.CultureInfo.InvariantCulture));}Calculate();}catch(Exception ex){Error(ex);}}

  async void Export(string mode){if(busy||document==null)return;playing=false;using(var d=new SaveFileDialog{Filter=mode=="export_step"?"STEP|*.step":"STL|*.stl",FileName="Волновой_"+model.Ratio+(mode=="export_step"?".step":".stl")})if(d.ShowLocalizedDialog(this)==DialogResult.OK){busy=true;try{var doc=Current();await Task.Factory.StartNew(()=>kernel.Run(doc,mode,d.FileName));status.Text="Сохранено: "+d.FileName;}catch(Exception ex){Error(ex);}finally{busy=false;}}}

  void ExportDiagram(bool dxf){if(busy||!Calculate())return;using(var d=new SaveFileDialog{Filter=dxf?"DXF|*.dxf":"SVG|*.svg",FileName="Волновой_"+model.Ratio+(dxf?".dxf":".svg")})if(d.ShowLocalizedDialog(this)==DialogResult.OK)try{WaveDiagram.Export(model,d.FileName,dxf);}catch(Exception ex){Error(ex);}}

  void ExportCsv(){if(busy||!Calculate())return;using(var d=new SaveFileDialog{Filter="CSV|*.csv",FileName="Волновой_детали.csv"})if(d.ShowLocalizedDialog(this)==DialogResult.OK)try{File.WriteAllText(d.FileName,"Деталь;Размер / назначение;Количество\r\n"+string.Join("\r\n",WaveDiagram.Rows(model).Select(row=>string.Join(";",row))),Encoding.UTF8);}catch(Exception ex){Error(ex);}}

  async void Insert(){if(busy||document==null||insert==null)return;busy=true;try{if(await insert(Current().Features[0]))if(returnToTarget!=null)returnToTarget();}catch(Exception ex){Error(ex);}finally{busy=false;}}

  protected override void Dispose(bool disposing){if(disposing&&!disposed){disposed=true;timer.Dispose();kernel.Dispose();scene.Dispose();host.Child=null;}base.Dispose(disposing);}

 }

}

