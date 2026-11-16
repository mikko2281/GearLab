using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Windows.Forms;using System.Windows.Media.Media3D;
using W=System.Windows;using WC=System.Windows.Controls;using WM=System.Windows.Media;
namespace GearLab {
 // 6.11.0: редуктор, сделанный пользователем в разделе «Редукторы» или «Ступени», который можно поставить в сустав робота.
 // Ровно одно из полей задано: Reducer (одиночный редуктор) или Train + Output (последовательная цепь ступеней).
 public sealed class SavedReducer {
  public string Name{get;set;}public DateTime Saved{get;set;}public Parameters Reducer{get;set;}public TransmissionTrain Train{get;set;}public string Output{get;set;}
  internal bool Valid{get{return Reducer!=null||Train!=null&&Train.Stages!=null&&Train.Stages.Count>0&&!string.IsNullOrEmpty(Output);}}
 }
 // Сводка для значка на суставе и карточки в инспекторе.
 public sealed class ReducerBadge {public string JointId,Label,Detail,Status,Tooltip,Type;public bool HasModel;}
 public static class RobotReducerKit {
  internal static string LibraryOverride;
  public static string LibraryPath{get{return LibraryOverride??Path.Combine(AppSettings.Folder,"my-reducers.json");}}
  public static string TypeName(int kind){var t=TransmissionCatalog.All.FirstOrDefault(x=>x.Id==kind);return t==null?"Редуктор":t.Name;}
  // Одиночный редуктор в сустав: КПД, i и габарит берутся из рассчитанной модели (раньше это было внутри RobotDesignerForm.Reducer).
  public static void ApplyParameters(RobotJoint joint,Parameters p){var r=Engine.Calculate(p);r.Model=CadBuilder.Build(r);RobotTransmission60.ValidateJoint(joint,r);joint.Efficiency=r.Engineering==null?p.Efficiency:r.Engineering.Efficiency;if(p.Kind==12)joint.ScrewLead=Math.PI*p.Module*p.Teeth;joint.TrainSettings=null;joint.TrainOutput=null;joint.ReducerSettings=p;joint.CustomTransmission=null;joint.TransmissionOutput=null;joint.Reduction=r.Actual;var bounds=r.Model.Bounds();joint.GearboxDiameter=Math.Max(joint.GearboxDiameter,Math.Min(250,2*Math.Max(Math.Max(Math.Abs(bounds[0]),Math.Abs(bounds[1])),Math.Max(Math.Abs(bounds[2]),Math.Abs(bounds[3])))+2*joint.Wall));}
  public static void Apply(RobotJoint joint,SavedReducer r){if(r==null||!r.Valid)throw new Exception("Редуктор не задан.");if(r.Reducer!=null){var p=Engine.Clone(r.Reducer);p.UseUniversalBase=false;ApplyParameters(joint,p);}else RobotTrainIntegration.Apply(joint,r.Train,r.Output);}
  // Новое звено подстраивается под редуктор: габарит — от редуктора и мотора, длина звена растёт с крупным редуктором.
  public static void PrepareFresh(RobotJoint j){j.GearboxDiameter=Math.Max(40,j.MotorFrame+24);}
  public static void FitLink(RobotJoint j){var end=new Vector3D(j.EndX,j.EndY,j.EndZ);double length=end.Length,wanted=Math.Max(140,Math.Round(j.GearboxDiameter*1.7/10)*10);if(length<1e-6||length>=wanted)return;end*=wanted/length;j.EndX=end.X;j.EndY=end.Y;j.EndZ=end.Z;}
  public static bool HasModel(RobotJoint j){return j.ReducerSettings!=null||j.TrainSettings!=null||j.CustomTransmission!=null;}
  public static SavedReducer FromJoint(RobotJoint j){if(j.ReducerSettings!=null)return new SavedReducer{Name=TypeName(j.ReducerSettings.Kind)+" · i "+Engine.F(j.Reduction),Reducer=Engine.Clone(j.ReducerSettings),Saved=DateTime.Now};if(j.TrainSettings!=null&&j.TrainOutput!=null)return new SavedReducer{Name=TrainName(j.TrainSettings,j.Reduction),Train=Copy(j.TrainSettings),Output=j.TrainOutput,Saved=DateTime.Now};return null;}
  public static string TrainName(TransmissionTrain t,double ratio){var names=t.Stages.Where(s=>s!=null).Select(s=>TypeName(s.Parameters==null?-1:s.Parameters.Kind)).ToArray();string text=string.Join(" → ",names);if(text.Length>48)text=names.Length+" ступ.";return text+" · i "+Engine.F(ratio);}
  internal static TransmissionTrain Copy(TransmissionTrain t){var s=CADDocument.Serializer();return s.Deserialize<TransmissionTrain>(s.Serialize(t));}
  public static string Describe(RobotJoint j){if(j.ReducerSettings!=null)return TypeName(j.ReducerSettings.Kind);if(j.TrainSettings!=null)return "Ступени ×"+j.TrainSettings.Stages.Count;if(j.CustomTransmission!=null)return "Своя передача";return "Без модели редуктора";}
  public static ReducerBadge Badge(RobotDocument d,RobotJoint j,RobotJointLoad load){int index=d.Ordered().IndexOf(j);var b=new ReducerBadge{JointId=j.Id,HasModel=HasModel(j),Type=Describe(j)};b.Label=(index+1)+" · "+j.Name;b.Detail=I18n.T(b.Type)+" · i "+Engine.F(j.Reduction);b.Status=Status(load,b.HasModel);b.Tooltip=load==null?"":I18n.T(load.Message)+(load.ReducerLimit.HasValue?"\n"+I18n.T("Предел редуктора")+" ≈ "+Engine.F(load.ReducerLimit.Value)+" "+I18n.T("Н·м"):"");return b;}
  // error — мотору или редуктору не хватает момента; warn — запас меньше 20 %; none — у сустава нет модели редуктора, только число i.
  public static string Status(RobotJointLoad load,bool model){if(load!=null&&!load.Enough)return "error";if(load!=null&&load.ReducerLimit.HasValue&&load.Required>load.ReducerLimit.Value+1e-9)return "error";if(!model)return "none";if(load!=null&&load.Available>0&&load.Required>.8*load.Available)return "warn";return "ok";}
  public static bool Shown(RobotJoint j){return j.Mode!=RobotJointMode.Неподвижный;}
  // «Мои редукторы»: последние 30 редукторов, добавленных в робота или сохранённых вручную. Файл в папке настроек.
  public static List<SavedReducer> Load(){try{if(!File.Exists(LibraryPath))return new List<SavedReducer>();var list=CADDocument.Serializer().Deserialize<List<SavedReducer>>(File.ReadAllText(LibraryPath,Encoding.UTF8));return list==null?new List<SavedReducer>():list.Where(x=>x!=null&&x.Valid).ToList();}catch{return new List<SavedReducer>();}}
  static string Key(SavedReducer r){var s=CADDocument.Serializer();return r.Reducer!=null?s.Serialize(r.Reducer):s.Serialize(r.Train)+"|"+r.Output;}
  public static void Remember(SavedReducer r){if(r==null||!r.Valid)return;var list=Load();string key=Key(r);list.RemoveAll(x=>Key(x)==key);list.Insert(0,r);if(list.Count>30)list.RemoveRange(30,list.Count-30);Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));File.WriteAllText(LibraryPath,CADDocument.Serializer().Serialize(list),Encoding.UTF8);}
  public static void Forget(SavedReducer r){var list=Load();string key=Key(r);list.RemoveAll(x=>Key(x)==key);File.WriteAllText(LibraryPath,CADDocument.Serializer().Serialize(list),Encoding.UTF8);}
  // Меню «＋ В робота» из редакторов. Робот берётся из открытой вкладки «Робототехника»; если её нет — создаётся.
  public static void ShowPlacementMenu(Control owner,Control anchor,SavedReducer entry){var shell=WorkspaceHost.Find(owner);var robot=shell==null?null:shell.Existing("robot") as RobotDesignerForm;bool has=robot!=null&&robot.Document.Joints.Count>0;var menu=new ContextMenuStrip();
   menu.Items.Add(I18n.T(has?"Следующим звеном робота":"Новый робот с этим редуктором"),null,(s,e)=>Place(owner,entry,null,!has));
   if(has){var into=new ToolStripMenuItem(I18n.T("Вместо редуктора звена"));foreach(var j in robot.Document.Ordered().Where(Shown)){string id=j.Id;into.DropDownItems.Add((robot.Document.Ordered().IndexOf(j)+1)+" · "+j.Name+"  ("+I18n.T(Describe(j))+", i "+Engine.F(j.Reduction)+")",null,(s,e)=>Place(owner,entry,id,false));}menu.Items.Add(into);menu.Items.Add(I18n.T("Новый робот с этим редуктором"),null,(s,e)=>Place(owner,entry,null,true));}
   menu.Items.Add(new ToolStripSeparator());menu.Items.Add(I18n.T("Только сохранить в «Мои редукторы»"),null,(s,e)=>{try{Remember(entry);UiMessage.Show(owner,I18n.T("Сохранено")+": "+entry.Name,"GearLab");}catch(Exception ex){UiMessage.Show(owner,ex.Message);}});
   AppSettings.Apply(menu);menu.Show(anchor,new Point(0,anchor.Height));}
  internal static bool Place(Control owner,SavedReducer entry,string target,bool newRobot){try{Remember(entry);}catch{}var shell=WorkspaceHost.Find(owner);if(shell==null){UiMessage.Show(owner,"Откройте программу через главное окно, чтобы добавить редуктор в робота.");return false;}bool existed=shell.Existing("robot")!=null;shell.Navigate("robot");var robot=shell.Existing("robot") as RobotDesignerForm;if(robot==null)return false;return robot.PlaceReducer(entry,target,newRobot||!existed);}
 }
 public sealed partial class RobotDesignerForm {
  bool reducerMode;ToolStripButton reducerButton;
  internal bool ReducerMode{get{return reducerMode;}}
  void InitializeReducerKit(ToolStrip top){reducerButton=new ToolStripButton("⚙ Редукторы"){Padding=new Padding(6),CheckOnClick=false,ToolTipText="Показать редукторы на модели. Щёлкните значок — откроются настройки редуктора."};reducerButton.Click+=(s,e)=>ShowReducers(!reducerMode);top.Items.Add(reducerButton);}
  internal void ShowReducers(bool on){reducerMode=on;if(reducerButton!=null)reducerButton.Checked=on;RefreshQuickInspector();RefreshBoneOverlay();UpdateWebPose();}
  internal void PickReducer(string id){if(Document.Find(id)==null)return;if(!reducerMode){reducerMode=true;if(reducerButton!=null)reducerButton.Checked=true;}Select(id,null);}
  RobotJointLoad LoadOf(string id){return Analysis==null?null:Analysis.Joints.FirstOrDefault(x=>x.Id==id);}
  internal List<ReducerBadge> ReducerBadges(){return Document.Ordered().Where(RobotReducerKit.Shown).Select(j=>RobotReducerKit.Badge(Document,j,LoadOf(j.Id))).ToList();}
  // Значки на Three.js-сцене: позиция — начало звена, где стоит мотор с редуктором.
  void SendReducerPins(Dictionary<string,RobotPose> poses){if(!robotWeb.Ready)return;if(!reducerMode){robotWeb.Send(new{type="reducers",enabled=false});return;}robotWeb.Send(new{type="reducers",enabled=true,items=ReducerBadges().Where(b=>poses.ContainsKey(b.JointId)).Select(b=>new{id=b.JointId,label=b.Label,detail=b.Detail,status=b.Status,tooltip=b.Tooltip,selected=b.JointId==selectedJoint,position=new[]{poses[b.JointId].Origin.X,poses[b.JointId].Origin.Y,poses[b.JointId].Origin.Z}}).ToArray()});}
  // Те же значки поверх WPF-сцены, если WebView2 недоступен.
  void AddReducerPins(){if(!reducerMode)return;var poses=RobotKinematics.Evaluate(Document,phase,Math.Abs(phase)>1e-9);foreach(var b in ReducerBadges()){if(!poses.ContainsKey(b.JointId))continue;var at=ProjectBone(poses[b.JointId].Origin);var color=UiPalette.Get(b.Status=="error"?"Error":b.Status=="warn"?"Warning":b.Status=="none"?"Muted":"Success");var text=new WC.TextBlock{Text="⚙ "+b.Label+"\n"+b.Detail,Foreground=WM.Brushes.White,FontSize=12};var pin=new WC.Button{Content=text,Padding=new W.Thickness(8,3,10,3),Background=new WM.SolidColorBrush(WM.Color.FromArgb(235,color.R,color.G,color.B)),BorderBrush=b.JointId==selectedJoint?WM.Brushes.White:WM.Brushes.Transparent,BorderThickness=new W.Thickness(2),ToolTip=string.IsNullOrEmpty(b.Tooltip)?null:b.Tooltip,Cursor=System.Windows.Input.Cursors.Hand};string id=b.JointId;pin.Click+=(s,e)=>PickReducer(id);WC.Canvas.SetLeft(pin,at.X+16);WC.Canvas.SetTop(pin,at.Y-18);boneOverlay.Children.Add(pin);}}
  // Конец цепочки: от выбранного звена (или основания) по первым потомкам до последнего звена.
  RobotJoint ChainEnd(){var current=Joint??Document.Ordered().FirstOrDefault();if(current==null)return null;for(int guard=0;guard<Document.Joints.Count;guard++){var child=Document.Joints.FirstOrDefault(x=>x.ParentId==current.Id);if(child==null)break;current=child;}return current;}
  // Ставит редуктор: новым звеном в конец руки, в существующее звено или первым звеном нового робота. Одно действие — один шаг отмены.
  internal bool PlaceReducer(SavedReducer entry,string target,bool newRobot){if(entry==null||!entry.Valid)return false;Stop();if(newRobot){Replace(new RobotDocument{Name=I18n.T("Робот из моих редукторов")},null);if(Document.Joints.Count>0)return false;}CapturePose();bool ok=Mutate(()=>{RobotJoint j;bool fresh=target==null;if(fresh){var end=ChainEnd();j=NewJointAfter(end);if(end==null){j.Mode=RobotJointMode.Вращение_Z;j.EndX=0;j.EndY=0;j.EndZ=140;}RobotReducerKit.PrepareFresh(j);}else{j=Document.Find(target);if(j==null)throw new Exception("Звено удалено.");}RobotReducerKit.Apply(j,entry);if(fresh)RobotReducerKit.FitLink(j);selectedJoint=j.Id;selectedPart=null;});if(ok){fitNext=fitNext||newRobot;ShowReducers(true);}return ok;}
  void OpenReducerEditor(RobotJoint j){if(j==null)return;if(j.TrainSettings!=null)TrainTransmission();else if(j.CustomTransmission!=null)Transmission();else Reducer();}
  void BuildReducerInspector(FlowLayoutPanel flow,RobotJoint j){
   flow.Controls.Add(QuickTitle("Редукторы робота"));
   QuickHint(flow,"Значки ⚙ на модели показывают, где стоит редуктор. Щёлкните значок или строку — ниже откроются его настройки.");
   var badges=ReducerBadges();
   if(badges.Count==0){flow.Controls.Add(QuickNote("В роботе пока нет подвижных звеньев. Сделайте редуктор в разделе «Редукторы» или «Ступени» и нажмите «＋ В робота»."));UiAppearance.Primary(QuickButton(flow,"Открыть раздел «Редукторы»",()=>WorkspaceHost.Navigate(this,"reducer")));QuickButton(flow,"Открыть раздел «Ступени»",()=>WorkspaceHost.Navigate(this,"train"));return;}
   foreach(var b in badges){var row=new ReducerRow{Title=b.Label,Detail=b.Detail,Status=b.Status,Selected=j!=null&&b.JointId==j.Id};string id=b.JointId;row.Click+=(s,e)=>PickReducer(id);flow.Controls.Add(row);}
   if(j==null||!RobotReducerKit.Shown(j)){flow.Controls.Add(QuickNote("Выберите сустав в списке или на модели."));return;}
   var load=LoadOf(j.Id);var badge=RobotReducerKit.Badge(Document,j,load);
   var card=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Width=285,Padding=new Padding(0,6,0,0),Margin=new Padding(0,10,0,0)};flow.Controls.Add(card);
   card.Controls.Add(QuickTitle(j.Name));
   Action<string,string> line=(caption,value)=>{var l=new Label{AutoSize=true,MaximumSize=new Size(285,0),Margin=new Padding(0,1,0,1),Text=I18n.T(caption)+": "+value};card.Controls.Add(l);};
   line("Тип",I18n.T(badge.Type));line("Передаточное число","i "+Engine.F(j.Reduction));line("КПД",Engine.F(j.Efficiency));line("Габарит редуктора","Ø"+Engine.F(j.GearboxDiameter)+" × "+Engine.F(j.GearboxLength)+" "+I18n.T("мм"));
   line("Предел редуктора",load!=null&&load.ReducerLimit.HasValue?Engine.F(load.ReducerLimit.Value)+" "+I18n.T("Н·м"):I18n.T("не определено"));
   if(load!=null){var state=new StatusLabel{MaximumSize=new Size(285,0),Margin=new Padding(0,6,0,4),Font=new Font("Segoe UI Semibold",10f)};state.Set(I18n.T("Нужно")+" "+Engine.F(load.Required)+" / "+I18n.T("доступно")+" "+Engine.F(load.Available)+" "+I18n.T(load.Unit)+(badge.Status=="error"?" · "+I18n.T("не хватает"):badge.Status=="warn"?" · "+I18n.T("мало запаса"):""),badge.Status=="error"?"Error":badge.Status=="warn"?"Warning":"Success");card.Controls.Add(state);}
   if(!badge.HasModel)card.Controls.Add(QuickNote("У сустава задано только передаточное число — 3D-модели и предела редуктора нет. Поставьте свой редуктор."));
   UiAppearance.Primary(QuickButton(card,badge.HasModel?"Настроить редуктор…":"Спроектировать редуктор…",()=>OpenReducerEditor(j)));
   var swap=QuickButton(card,"Заменить на  ▾",()=>{});swap.Click+=(s,e)=>ShowSwapMenu(swap,j.Id);
   if(badge.HasModel&&j.CustomTransmission==null)QuickButton(card,"Сохранить в «Мои редукторы»",()=>{try{RobotReducerKit.Remember(RobotReducerKit.FromJoint(j));summary.Text=I18n.T("Сохранено в «Мои редукторы»")+": "+RobotReducerKit.FromJoint(j).Name;}catch(Exception ex){UiMessage.Show(this,ex.Message);}});
   if(badge.HasModel){var clear=QuickButton(card,"Убрать модель, оставить i",()=>{string id=j.Id;Mutate(()=>{var live=Document.Find(id);live.ReducerSettings=null;live.TrainSettings=null;live.TrainOutput=null;live.CustomTransmission=null;live.TransmissionOutput=null;});});clear.ForeColor=Color.Firebrick;}
   flow.Controls.Add(QuickNote("Новый редуктор: раздел «Редукторы» или «Ступени» → «＋ В робота»."));}
  void ShowSwapMenu(Control anchor,string id){var menu=new ContextMenuStrip();var saved=RobotReducerKit.Load();if(saved.Count==0)menu.Items.Add(new ToolStripMenuItem(I18n.T("«Мои редукторы» пока пусты")){Enabled=false});foreach(var item in saved){var entry=item;menu.Items.Add(entry.Name,null,(s,e)=>{var j=Document.Find(id);if(j==null)return;CapturePose();Mutate(()=>RobotReducerKit.Apply(Document.Find(id),entry));});}
   menu.Items.Add(new ToolStripSeparator());menu.Items.Add(I18n.T("Новый редуктор в редакторе…"),null,(s,e)=>{Select(id,null);Reducer();});menu.Items.Add(I18n.T("Цепь ступеней…"),null,(s,e)=>{Select(id,null);TrainTransmission();});
   AppSettings.Apply(menu);menu.Show(anchor,new Point(0,anchor.Height));}
 }
 // Строка списка редукторов: круглый значок состояния, название сустава и тип редуктора.
 public sealed class ReducerRow:Control {
  public string Title,Detail,Status;public bool Selected;bool hover;
  public ReducerRow(){SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.ResizeRedraw,true);Size=new Size(285,50);Margin=new Padding(0,0,0,4);Cursor=Cursors.Hand;}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);var box=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);var fill=Selected?UiPalette.Get("Selection"):hover?UiPalette.Get("Field"):UiPalette.Blend(UiPalette.Get("Panel"),UiPalette.Get("Field"),.45);using(var path=UiAppearance.Rounded(box,8))using(var brush=new SolidBrush(fill))using(var pen=new Pen(Selected?UiPalette.Get("Accent"):UiPalette.Get("Border"),Selected?1.6f:1f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
   var color=UiPalette.Get(Status=="error"?"Error":Status=="warn"?"Warning":Status=="none"?"Muted":"Success");var circle=new RectangleF(9,(Height-30)/2f,30,30);if(Status=="none")using(var pen=new Pen(color,1.5f){DashStyle=DashStyle.Dash})g.DrawEllipse(pen,circle);else using(var brush=new SolidBrush(color))g.FillEllipse(brush,circle);TextRenderer.DrawText(g,"⚙",new Font("Segoe UI Symbol",13f),Rectangle.Round(circle),Status=="none"?color:Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   using(var bold=new Font("Segoe UI Semibold",9.75f))TextRenderer.DrawText(g,Title,bold,new Rectangle(48,6,Width-54,20),UiPalette.Get("Text"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);TextRenderer.DrawText(g,Detail,Font,new Rectangle(48,26,Width-54,19),UiPalette.Get("Muted"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);}
 }
 public sealed partial class WorkspaceShell {
  internal Form Existing(string key){TabPage page;return sections.TryGetValue(key,out page)&&!page.IsDisposed?page.Tag as Form:null;}
 }
 public partial class MainForm {
  // «＋ Добавить в робота» в разделе «Редукторы»: текущий редуктор ставится в сустав робота.
  internal void AddRobotButton(){var b=new Button{Text="＋ Добавить в робота  ▾",Width=280,Height=40,Margin=new Padding(0,2,0,10)};UiAppearance.Primary(b);b.Click+=(s,e)=>{if(!ReadControls())return;Recalculate();if(result==null||!result.Valid||result.Model==null||!result.Model.Valid){dialogs.Info(this,"Исправьте геометрию перед добавлением в робота.","Робот");return;}var copy=Engine.Clone(p);copy.UseUniversalBase=false;RobotReducerKit.ShowPlacementMenu(this,b,new SavedReducer{Name=RobotReducerKit.TypeName(copy.Kind)+" · i "+Engine.F(result.Actual),Reducer=copy,Saved=DateTime.Now});};controls.Controls.Add(b);controls.Controls.SetChildIndex(b,0);}
 }
 public sealed partial class TrainDesignerForm {
  Button addToRobot;
  // «＋ В робота» в разделе «Ступени»: последовательная цепь с одним выходом ставится в сустав.
  void AddTrainToRobot(){Calculate();if(solution==null||!solution.Valid){status.Text=I18n.T("Сначала исправьте цепь.");return;}var leaves=solution.States.Values.Where(st=>!document.Stages.Any(n=>n.Parent==st.Id||n.SecondParent==st.Id)).ToList();if(leaves.Count!=1){status.Text=I18n.T("Для сустава нужен один выход.");return;}double ratio=1/Math.Max(1e-12,Math.Abs(leaves[0].OutputGain));RobotReducerKit.ShowPlacementMenu(this,addToRobot,new SavedReducer{Name=RobotReducerKit.TrainName(document,ratio),Train=RobotReducerKit.Copy(document),Output=leaves[0].Id,Saved=DateTime.Now});}
 }
}
