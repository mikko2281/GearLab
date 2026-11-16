using System;using System.Linq;using System.Drawing;using System.Drawing.Drawing2D;using System.Collections.Generic;using System.Globalization;using System.Windows.Forms;using System.Runtime.InteropServices;
namespace GearLab {
 // 6.10.0. Строительные блоки «человеческих» форм вместо таблиц свойств: поля с единицами и подсказкой,
 // карточки-рамки, плитки с ключевыми числами, раскрывающиеся разделы, образцы материалов,
 // шкала момента и диаграмма «что сломается первым». Все цвета берутся из палитры темы.
 public static class EngineeringUi {
  public const int Width=372;
  public static Label Title(string text){return new Label{Text=text,AutoSize=true,MaximumSize=new Size(Width,0),Font=new Font("Segoe UI Semibold",12f),Margin=new Padding(0,6,0,2)};}
  public static Label Heading(string text){return new Label{Text=text,AutoSize=true,MaximumSize=new Size(Width,0),Font=new Font("Segoe UI Semibold",10f),Margin=new Padding(0,12,0,4)};}
  public static Label Hint(string text){return new Label{Text=text,AutoSize=true,MaximumSize=new Size(Width,0),Tag="muted",Font=new Font("Segoe UI",8.75f),Margin=new Padding(0,0,0,6)};}
  public static FlowLayoutPanel Column(){return new FlowLayoutPanel{FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=Padding.Empty,Padding=Padding.Empty};}
  public static FlowLayoutPanel Row(){return new FlowLayoutPanel{FlowDirection=FlowDirection.LeftToRight,WrapContents=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=new Padding(0,2,0,2),Padding=Padding.Empty};}
  public static Button Action(string text,Action click,bool primary=false,int width=0){var b=new Button{Text=text,AutoSize=width<=0,Width=Math.Max(width,60),Height=34,MinimumSize=new Size(60,34),Margin=new Padding(0,4,8,4),FlatStyle=FlatStyle.Flat,Padding=new Padding(8,0,8,0)};b.Click+=(s,e)=>click();return primary?UiAppearance.Primary(b):b;}
  // Левая колонка вкладки: прокручивается, ширина фиксирована, справа — 3D или схема.
  public static FlowLayoutPanel ScrollColumn(){return new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18,12,10,18)};}
 }

 // Числовое поле: «Название  [значение] ед.» и подсказка под ним. Пустое значение допускается
 // только у необязательных величин (null = «не задано / не определено»).
 public sealed class NumberField:FlowLayoutPanel {
  readonly Label caption=new Label(),unit=new Label(),hint=new Label();readonly string title;public readonly TextBox Box=new TextBox();readonly Timer delay=new Timer{Interval=600};
  public double Min,Max;public bool Optional;public int Digits=3;public event Action Committed;bool setting;double? last;
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,string l);
  public NumberField(string title,string units,double min,double max,bool optional=false,string help=null,string empty=null){FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,4,0,4);Min=min;Max=max;Optional=optional;this.title=title;
   var row=EngineeringUi.Row();caption.Text=title;caption.Width=196;caption.Height=28;caption.TextAlign=ContentAlignment.MiddleLeft;caption.AutoEllipsis=true;Box.Width=104;Box.TextAlign=HorizontalAlignment.Right;Box.Margin=new Padding(0,3,6,0);Box.AccessibleName=title;unit.Text=units;unit.AutoSize=true;unit.Tag="muted";unit.Margin=new Padding(0,7,0,0);row.Controls.Add(caption);row.Controls.Add(Box);row.Controls.Add(unit);Controls.Add(row);
   if(!string.IsNullOrEmpty(help)){hint.Text=help;hint.AutoSize=true;hint.MaximumSize=new Size(EngineeringUi.Width,0);hint.Tag="muted";hint.Font=new Font("Segoe UI",8.5f);hint.Margin=new Padding(0,0,0,2);Controls.Add(hint);}
   if(!string.IsNullOrEmpty(empty))Box.HandleCreated+=(s,e)=>SendMessage(Box.Handle,0x1501,(IntPtr)1,I18n.T(empty));
   Box.TextChanged+=(s,e)=>{if(setting)return;MarkValidity();delay.Stop();delay.Start();};Box.Leave+=(s,e)=>Commit();Box.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;Commit();}};delay.Tick+=(s,e)=>{delay.Stop();Commit();};Disposed+=(s,e)=>delay.Dispose();}
  public string Title{get{return title;}}
  public bool TryRead(out double? value){value=null;string t=Box.Text.Trim().Replace(',','.');if(t.Length==0)return Optional;double v;if(!double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||double.IsNaN(v)||double.IsInfinity(v)||v<Min||v>Max)return false;value=v;return true;}
  public double? Value{get{double? v;return TryRead(out v)?v:last;}set{setting=true;try{last=value;Box.Text=value.HasValue?Math.Round(value.Value,Digits).ToString("0.######",CultureInfo.InvariantCulture):"";MarkValidity();}finally{setting=false;}}}
  public bool Valid{get{double? v;return TryRead(out v);}}
  void MarkValidity(){Box.ForeColor=Valid?UiPalette.Get("Text"):UiPalette.Get("Error");}
  void Commit(){delay.Stop();double? v;if(!TryRead(out v)){MarkValidity();return;}if(v==last)return;last=v;if(Committed!=null)Committed();}
 }

 // Надпись цветом состояния палитры (Success/Warning/Error/Text): обычный ForeColor тема приводит к цвету текста.
 public sealed class StatusLabel:Label {
  public string Status="Text";public StatusLabel(){AutoSize=true;}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,UiPalette.Get(Status??"Text"),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix);}
  public void Set(string text,string status){Status=status;Text=text;Invalidate();}
 }
 // Переключатель с подписью-пояснением.
 public sealed class ToggleField:FlowLayoutPanel {
  public readonly CheckBox Box=new CheckBox();public event Action Changed;bool setting;
  public ToggleField(string title,string help=null){FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,6,0,4);Box.Text=title;Box.AutoSize=true;Box.Font=new Font("Segoe UI Semibold",9.75f);Box.Margin=new Padding(0,0,0,1);Controls.Add(Box);if(!string.IsNullOrEmpty(help)){var h=EngineeringUi.Hint(help);h.Margin=new Padding(20,0,0,2);h.MaximumSize=new Size(EngineeringUi.Width-20,0);Controls.Add(h);}Box.CheckedChanged+=(s,e)=>{if(!setting&&Changed!=null)Changed();};}
  public bool Checked{get{return Box.Checked;}set{setting=true;try{Box.Checked=value;}finally{setting=false;}}}
 }

 // Выпадающий выбор с подписью.
 public sealed class ChoiceField:FlowLayoutPanel {
  public readonly EditorComboBox Box=new EditorComboBox{DropDownStyle=ComboBoxStyle.DropDownList};public event Action Changed;bool setting;
  public ChoiceField(string title,int width=176){FlowDirection=FlowDirection.LeftToRight;WrapContents=false;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,4,0,4);Controls.Add(new Label{Text=title,Width=196,Height=30,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true});Box.Width=width;Box.AccessibleName=title;Controls.Add(Box);Box.SelectionChangeCommitted+=(s,e)=>{if(!setting&&Changed!=null)Changed();};}
  void RaiseChanged(){if(Changed!=null)Changed();}
  public void Set(IEnumerable<object> items,object selected){setting=true;try{Box.Items.Clear();foreach(var i in items)Box.Items.Add(i);Box.SelectedItem=selected;if(Box.SelectedIndex<0&&Box.Items.Count>0)Box.SelectedIndex=0;}finally{setting=false;}}
 }

 // Рамка-карточка: группирует поля, не меняя фон (детям не нужен отдельный цвет).
 public sealed class Card:FlowLayoutPanel {
  public Card(){FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Padding=new Padding(14,10,14,12);Margin=new Padding(0,6,0,8);MinimumSize=new Size(EngineeringUi.Width+28,0);DoubleBuffered=true;ResizeRedraw=true;}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=UiAppearance.Rounded(new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f),10))using(var pen=new Pen(UiPalette.Blend(UiPalette.Get("Border"),UiPalette.Get("Text"),.08)))e.Graphics.DrawPath(pen,path);}
 }

 // Раскрывающийся раздел: заголовок со стрелкой, содержимое показывается по щелчку.
 public sealed class Expander:FlowLayoutPanel {
  readonly Label header=new Label();public readonly Panel Content=new Panel();readonly string title;bool open;public event Action Toggled;
  public Expander(string title,int contentHeight,bool expanded=false){this.title=title;FlowDirection=FlowDirection.TopDown;WrapContents=false;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,6,0,6);header.AutoSize=false;header.Width=EngineeringUi.Width+28;header.Height=30;header.TextAlign=ContentAlignment.MiddleLeft;header.Cursor=Cursors.Hand;header.Font=new Font("Segoe UI Semibold",9.75f);header.Click+=(s,e)=>Expanded=!Expanded;Content.Width=EngineeringUi.Width+28;Content.Height=contentHeight;Controls.Add(header);Controls.Add(Content);Expanded=expanded;}
  public bool Expanded{get{return open;}set{open=value;Content.Visible=value;header.Text=(value?"▾  ":"▸  ")+I18n.T(title);if(Toggled!=null)Toggled();}}
  public string Caption{set{header.Text=(open?"▾  ":"▸  ")+I18n.T(value);}}
 }

 // Плитка с главным числом: подпись, значение крупно, пояснение и цветная полоска состояния.
 public sealed class KpiTile:Control {
  public string Caption,Value,Sub,Status;
  public KpiTile(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Size=new Size(178,86);Margin=new Padding(0,0,10,10);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);Color fill=UiPalette.Blend(UiPalette.Get("Panel"),UiPalette.Get("Field"),.55);
   using(var path=UiAppearance.Rounded(r,9)){using(var b=new SolidBrush(fill))g.FillPath(b,path);using(var pen=new Pen(UiPalette.Blend(UiPalette.Get("Border"),fill,.3)))g.DrawPath(pen,path);}
   if(!string.IsNullOrEmpty(Status))using(var b=new SolidBrush(UiPalette.Get(Status)))using(var strip=UiAppearance.Rounded(new RectangleF(1,10,4,Height-20),2))g.FillPath(b,strip);
   TextRenderer.DrawText(g,I18n.T(Caption??""),new Font("Segoe UI",8.5f),new Rectangle(14,9,Width-20,18),UiPalette.Get("Muted"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   TextRenderer.DrawText(g,Value??"—",new Font("Segoe UI Semibold",15f),new Rectangle(12,27,Width-16,30),string.IsNullOrEmpty(Status)||Status=="Accent"?UiPalette.Get("Text"):UiPalette.Get(Status),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   TextRenderer.DrawText(g,I18n.T(Sub??""),new Font("Segoe UI",8.25f),new Rectangle(14,Height-26,Width-20,18),UiPalette.Get("Muted"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);}
  public void Set(string value,string sub,string status){Value=value;Sub=sub;Status=status;Invalidate();}
 }

 // Шарик-образец материала (тот же вид, что в 3D).
 public sealed class MaterialSwatch:Control {
  public MaterialLook Look;public MaterialSwatch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Size=new Size(30,30);Margin=new Padding(0,2,10,2);}
  protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);if(Look==null){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(UiPalette.Get("Muted")){DashStyle=DashStyle.Dash})e.Graphics.DrawEllipse(pen,2,2,Width-5,Height-5);return;}MaterialLook.PaintSwatch(e.Graphics,new RectangleF(1,1,Width-3,Height-3),Look);}
 }

 // Шкала момента: зелёная зона до половины предела, жёлтая до предела, красная — разрушение.
 public sealed class TorqueGauge:Control {
  public double Torque;public double? Limit,Clutch;public double Scale=1.5;
  public TorqueGauge(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);Size=new Size(EngineeringUi.Width+20,58);Margin=new Padding(0,4,0,0);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;float x0=6,x1=Width-6,y=18,h=12;var track=new RectangleF(x0,y,x1-x0,h);
   using(var path=UiAppearance.Rounded(track,6))using(var b=new SolidBrush(UiPalette.Get("Field")))g.FillPath(b,path);if(!Limit.HasValue||Limit.Value<=0){TextRenderer.DrawText(g,I18n.T("Предел не определён — назначьте материалы."),Font,new Rectangle(4,(int)(y+h+4),Width-8,20),UiPalette.Get("Muted"));return;}
   double max=Limit.Value*Scale;Func<double,float> X=v=>x0+(float)(Math.Max(0,Math.Min(max,v))/max)*(x1-x0);
   g.SetClip(UiAppearance.Rounded(track,6));using(var b=new LinearGradientBrush(new PointF(x0,0),new PointF(X(Limit.Value)+1,0),UiPalette.Get("Success"),UiPalette.Get("Warning"))){b.WrapMode=WrapMode.TileFlipX;g.FillRectangle(b,x0,y,X(Limit.Value)-x0,h);}using(var b=new SolidBrush(UiPalette.Get("Error")))g.FillRectangle(b,X(Limit.Value),y,x1-X(Limit.Value),h);
   using(var b=new SolidBrush(Color.FromArgb(120,UiPalette.Get("Panel"))))g.FillRectangle(b,X(Torque),y,x1-X(Torque),h);g.ResetClip();
   using(var pen=new Pen(UiPalette.Get("Text"),2)){float lx=X(Limit.Value);g.DrawLine(pen,lx,y-5,lx,y+h+5);}TextRenderer.DrawText(g,I18n.T("предел")+" "+Engine.F(Limit.Value),new Font("Segoe UI",8f),new Rectangle((int)X(Limit.Value)-60,0,120,16),UiPalette.Get("Muted"),TextFormatFlags.HorizontalCenter);
   if(Clutch.HasValue){float cx=X(Clutch.Value);using(var pen=new Pen(UiPalette.Get("Accent"),2){DashStyle=DashStyle.Dash})g.DrawLine(pen,cx,y-4,cx,y+h+4);TextRenderer.DrawText(g,I18n.T("муфта"),new Font("Segoe UI",8f),new Rectangle((int)cx-40,(int)(y+h+4),80,16),UiPalette.Get("Accent"),TextFormatFlags.HorizontalCenter);}
   float m=X(Torque);using(var b=new SolidBrush(UiPalette.Get("Text")))g.FillPolygon(b,new[]{new PointF(m,y+h+2),new PointF(m-6,y+h+11),new PointF(m+6,y+h+11)});using(var pen=new Pen(UiPalette.Get("Panel"),1.5f))g.DrawLine(pen,m,y,m,y+h);}
 }

 // «Что сломается первым»: полоса на каждый способ разрушения, длина — предельный момент.
 // Вертикальная линия — текущий момент; полосы, которые он пересёк, красные.
 public sealed class ModeChart:Control {
  public sealed class Item {public string Mode,Part,Formula,Note,PartId;public double? Limit;}
  public List<Item> Items=new List<Item>();public double Torque;public int Selected=-1;public event Action<int> Picked;const int RowHeight=46,Top=8;
  public ModeChart(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);Width=EngineeringUi.Width+20;Cursor=Cursors.Hand;Margin=new Padding(0,2,0,4);}
  // Одинаковые способы с одинаковым пределом (например, все сателлиты ступени) сворачиваются в одну строку.
  public void SetItems(IEnumerable<Item> items){var grouped=new List<Item>();foreach(var g in items.GroupBy(i=>i.Mode+"|"+(i.Limit.HasValue?Math.Round(i.Limit.Value,6).ToString(CultureInfo.InvariantCulture):"-"))){var list=g.ToList();var first=list[0];grouped.Add(new Item{Mode=first.Mode,Limit=first.Limit,Formula=first.Formula,Note=first.Note,PartId=first.PartId,Part=list.Count==1?first.Part:first.Part+" "+I18n.T("и ещё")+" "+(list.Count-1)});}Items=grouped.OrderBy(i=>i.Limit.HasValue?0:1).ThenBy(i=>i.Limit??0).ToList();Height=Math.Max(40,Top+Items.Count*RowHeight+6);if(Selected>=Items.Count)Selected=-1;Invalidate();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);int i=(e.Y-Top)/RowHeight;if(i>=0&&i<Items.Count){Selected=i;Invalidate();if(Picked!=null)Picked(i);}}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;if(Items.Count==0){TextRenderer.DrawText(g,I18n.T("Нет проверенных способов разрушения. Назначьте материалы."),Font,ClientRectangle,UiPalette.Get("Muted"),TextFormatFlags.WordBreak);return;}
   var known=Items.Where(i=>i.Limit.HasValue).Select(i=>i.Limit.Value).ToList();double weakest=known.Count==0?1:known.Min();double max=Math.Max(weakest*3,Torque*1.2);if(max<=0)max=1;float x0=6,x1=Width-8;Func<double,float> X=v=>x0+(float)(Math.Min(max,Math.Max(0,v))/max)*(x1-x0);
   for(int i=0;i<Items.Count;i++){var it=Items[i];float y=Top+i*RowHeight;var row=new Rectangle(0,(int)y,Width,RowHeight-2);if(i==Selected)using(var b=new SolidBrush(UiPalette.Get("Selection")))using(var path=UiAppearance.Rounded(new RectangleF(0,y,Width-1,RowHeight-3),6))g.FillPath(b,path);
    TextRenderer.DrawText(g,I18n.T(it.Mode),new Font("Segoe UI Semibold",9f),new Rectangle(8,(int)y+3,Width-110,18),UiPalette.Get("Text"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    TextRenderer.DrawText(g,it.Limit.HasValue?Engine.F(it.Limit.Value)+" "+I18n.T("Н·м"):I18n.T("не определено"),new Font("Segoe UI",8.75f),new Rectangle(Width-112,(int)y+3,104,18),it.Limit.HasValue?UiPalette.Get("Text"):UiPalette.Get("Muted"),TextFormatFlags.Right|TextFormatFlags.NoPrefix);
    TextRenderer.DrawText(g,it.Part??"",new Font("Segoe UI",8.25f),new Rectangle(8,(int)y+21,Width-16,15),UiPalette.Get("Muted"),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    var bar=new RectangleF(x0,y+RowHeight-9,x1-x0,5);using(var b=new SolidBrush(UiPalette.Get("Field")))using(var path=UiAppearance.Rounded(bar,2.5f))g.FillPath(b,path);
    if(it.Limit.HasValue){double margin=Torque>0?it.Limit.Value/Torque:double.PositiveInfinity;string key=margin<1?"Error":margin<2?"Warning":"Success";var fillRect=new RectangleF(x0,bar.Y,Math.Max(5,X(it.Limit.Value)-x0),5);using(var b=new SolidBrush(UiPalette.Get(Torque>0?key:"Accent")))using(var path=UiAppearance.Rounded(fillRect,2.5f))g.FillPath(b,path);if(it.Limit.Value>max)TextRenderer.DrawText(g,"»",Font,new Rectangle((int)x1-10,(int)bar.Y-9,12,14),UiPalette.Get("Muted"));}}
   if(Torque>0){float tx=X(Torque);using(var pen=new Pen(UiPalette.Get("Text"),2f))for(int i=0;i<Items.Count;i++){float by=Top+i*RowHeight+RowHeight-9;g.DrawLine(pen,tx,by-4,tx,by+9);}}}
 }

 // Схема выходного вала: опоры, консоль с силой, реакции и эпюра изгибающего момента.
 public sealed class ShaftLoadPanel:Panel {
  public TrainSolution Solution;public ShaftLoadPanel(){DoubleBuffered=true;ResizeRedraw=true;}
  static string N(double v){double a=Math.Abs(v);return Engine.F(Math.Round(v,a>=100?0:a>=10?1:2));}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.Clear(UiPalette.Get("Panel"));g.SmoothingMode=SmoothingMode.AntiAlias;var s=Solution;
   TextRenderer.DrawText(g,I18n.T("Выходной вал: опоры, нагрузка и изгибающий момент"),new Font("Segoe UI Semibold",10f),new Rectangle(14,10,Width-28,22),UiPalette.Get("Text"),TextFormatFlags.EndEllipsis);
   if(s==null||s.Strength==null||s.Strength.Shaft==null){TextRenderer.DrawText(g,I18n.T("Включите общий корпус и задайте нагрузку слева."),Font,new Rectangle(14,40,Width-28,40),UiPalette.Get("Muted"),TextFormatFlags.WordBreak);return;}
   var b=s.Strength.Shaft;double total=Math.Max(1e-6,b.Span+b.Overhang);float left=70,right=Width-70;
   // Пролёт между опорами рисуется не короче 30 % ширины, чтобы подписи не налезали; размеры подписаны настоящие.
   float spanFrac=(float)Math.Max(.3,Math.Min(.7,b.Span/total));float sup2=left+(right-left)*spanFrac,load=right;
   float axisY=Math.Max(92,Height*.3f),shaftH=16;Color muted=UiPalette.Get("Muted"),steel=UiPalette.Blend(UiPalette.Get("Muted"),UiPalette.Get("Panel"),.25);Font small=new Font("Segoe UI",8.5f),bold=new Font("Segoe UI Semibold",9f);
   using(var br=new LinearGradientBrush(new RectangleF(left-12,axisY-shaftH/2,right-left+24,shaftH),UiPalette.Blend(steel,Color.White,.35),UiPalette.Blend(steel,Color.Black,.25),90f))using(var path=UiAppearance.Rounded(new RectangleF(left-12,axisY-shaftH/2,right-left+24,shaftH),shaftH/2))g.FillPath(br,path);
   foreach(float x in new[]{left,sup2}){var rect=new RectangleF(x-12,axisY-shaftH/2-8,24,shaftH+16);using(var br=new SolidBrush(UiPalette.Get("Field")))using(var path=UiAppearance.Rounded(rect,5))g.FillPath(br,path);using(var pen=new Pen(muted,1.5f))using(var path=UiAppearance.Rounded(rect,5))g.DrawPath(pen,path);}
   Action<float,float,float,Color,string,bool> arrow=(x,fromY,toY,color,label,labelLeft)=>{using(var pen=new Pen(color,3f){StartCap=LineCap.Round}){g.DrawLine(pen,x,fromY,x,toY-Math.Sign(toY-fromY)*10);}float dir=Math.Sign(toY-fromY);using(var br=new SolidBrush(color))g.FillPolygon(br,new[]{new PointF(x,toY),new PointF(x-7,toY-dir*12),new PointF(x+7,toY-dir*12)});var size=TextRenderer.MeasureText(label,bold);float ly=(fromY+toY)/2-size.Height/2;TextRenderer.DrawText(g,label,bold,new Rectangle(labelLeft?(int)x-size.Width-10:(int)x+10,(int)ly,size.Width+4,size.Height),color);};
   arrow(load,axisY-shaftH/2-62,axisY-shaftH/2-2,UiPalette.Get("Warning"),"F = "+N(Math.Abs(b.Reaction1+b.Reaction2))+" "+I18n.T("Н"),true);
   if(Math.Abs(b.Reaction1)>1e-9){bool up=b.Reaction1>0;arrow(left,axisY+shaftH/2+(up?60:10),axisY+shaftH/2+(up?10:60),UiPalette.Get("Accent"),"R1 = "+N(Math.Abs(b.Reaction1)),true);}
   if(Math.Abs(b.Reaction2)>1e-9){bool up=b.Reaction2>0;arrow(sup2,axisY+shaftH/2+(up?60:10),axisY+shaftH/2+(up?10:60),UiPalette.Get("Accent"),"R2 = "+N(Math.Abs(b.Reaction2)),false);}
   float dimY=axisY+shaftH/2+80;using(var pen=new Pen(muted,1f)){foreach(var seg in new[]{Tuple.Create(left,sup2,"L = "+N(b.Span)),Tuple.Create(sup2,load,"a = "+N(b.Overhang))}){g.DrawLine(pen,seg.Item1,dimY,seg.Item2,dimY);g.DrawLine(pen,seg.Item1,dimY-5,seg.Item1,dimY+5);g.DrawLine(pen,seg.Item2,dimY-5,seg.Item2,dimY+5);TextRenderer.DrawText(g,seg.Item3+" "+I18n.T("мм"),small,new Rectangle((int)seg.Item1,(int)dimY-18,(int)Math.Max(60,seg.Item2-seg.Item1),16),muted,TextFormatFlags.HorizontalCenter);}}
   float plotTop=dimY+30,plotH=Math.Max(0,Height-plotTop-34);if(plotH>24&&b.BendingMoment>0){var pts=new[]{new PointF(left,plotTop),new PointF(sup2,plotTop+plotH),new PointF(load,plotTop)};using(var br=new SolidBrush(Color.FromArgb(70,UiPalette.Get("Accent"))))g.FillPolygon(br,pts);using(var pen=new Pen(UiPalette.Get("Accent"),2f))g.DrawLines(pen,pts);using(var pen=new Pen(muted,1f))g.DrawLine(pen,left-12,plotTop,right+12,plotTop);
    TextRenderer.DrawText(g,"Mmax = "+N(b.BendingMoment)+" "+I18n.T("Н·мм"),bold,new Rectangle((int)sup2+14,(int)(plotTop+plotH-22),240,18),UiPalette.Get("Accent"));TextRenderer.DrawText(g,I18n.T("изгибающий момент"),small,new Rectangle((int)left-12,(int)plotTop+4,200,16),muted);}
   TextRenderer.DrawText(g,"Ø "+N(b.Diameter)+" "+I18n.T("мм")+" · "+I18n.T("балка на двух опорах с консолью"),small,new Rectangle(14,Height-24,Width-28,18),muted,TextFormatFlags.EndEllipsis);}
 }
}
