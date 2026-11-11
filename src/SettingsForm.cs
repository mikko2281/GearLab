using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
namespace GearLab {
 // Окно настроек: простые разделы, подписи обычным языком, расширенные цвета — отдельно.
 public sealed class SettingsForm:Form {
  int section;bool selecting;readonly Dictionary<SafeNumber,Action<decimal>> numericEdits=new Dictionary<SafeNumber,Action<decimal>>();
  ComboBox languageChoice;bool syncingLanguage;double observedSpeed;AppSettings draft;readonly Panel content=new Panel();readonly Label message=new Label();readonly ListBox sections=new ListBox();readonly Dictionary<string,TextBox> colorFields=new Dictionary<string,TextBox>();
  readonly Button save=new Button{Text="Сохранить",Width=140,Height=36};
  const int ContentWidth=560;
  internal AppSettings Draft{get{return draft;}}
  public SettingsForm(Control owner){
   draft=AppSettings.Current.Clone();observedSpeed=AppSettings.Current.AnimationScale;Text="Настройки GearLab";Size=new Size(920,720);MinimumSize=new Size(720,540);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",9.75f);
   var footer=new Panel{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(20,14,20,14)};
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Right,AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Margin=Padding.Empty};
   var reset=new Button{Text="Сбросить всё",Width=140,Height=36,Margin=new Padding(0,0,8,0)};reset.Click+=(s,e)=>{draft=new AppSettings{Language=AppSettings.Current.Language};ShowSection();Info("Стандартные значения подготовлены. Нажмите «Сохранить».");};
   var cancel=new Button{Text="Отмена",Width=120,Height=36,Margin=new Padding(0,0,8,0)};cancel.Click+=(s,e)=>{draft=AppSettings.Current.Clone();if(Modal){DialogResult=DialogResult.Cancel;Close();}else{ShowSection();Info("Несохранённые изменения отменены.");}};
   save.Margin=Padding.Empty;save.Click+=(s,e)=>ApplyDraft();UiAppearance.Primary(save);
   buttons.Controls.AddRange(new Control[]{reset,cancel,save});
   message.Dock=DockStyle.Fill;message.TextAlign=ContentAlignment.MiddleLeft;message.AutoEllipsis=true;
   footer.Controls.Add(message);footer.Controls.Add(buttons);Controls.Add(footer);CancelButton=cancel;AcceptButton=save;
   AppSettings.Changed+=SynchronizeLanguage;Disposed+=(s,e)=>AppSettings.Changed-=SynchronizeLanguage;
   sections.Dock=DockStyle.Left;sections.Width=210;sections.BorderStyle=BorderStyle.None;sections.ItemHeight=42;sections.DrawMode=DrawMode.OwnerDrawFixed;sections.IntegralHeight=false;
   sections.Items.AddRange(new[]{"Внешний вид","Управление и 3D","Сохранение","Язык","Свои цвета"});
   sections.DrawItem+=(s,e)=>{if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;var g=e.Graphics;using(var b=new SolidBrush(UiPalette.Get("Header")))g.FillRectangle(b,e.Bounds);if(selected){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=UiAppearance.Rounded(new RectangleF(e.Bounds.X+8,e.Bounds.Y+3,e.Bounds.Width-16,e.Bounds.Height-6),7))using(var b=new SolidBrush(UiPalette.Get("Selection")))g.FillPath(b,path);using(var path=UiAppearance.Rounded(new RectangleF(e.Bounds.X+8,e.Bounds.Y+12,3,e.Bounds.Height-24),1.5f))using(var b=new SolidBrush(UiPalette.Get("Accent")))g.FillPath(b,path);}TextRenderer.DrawText(g,I18n.T(sections.Items[e.Index].ToString()),Font,new Rectangle(e.Bounds.X+24,e.Bounds.Y,e.Bounds.Width-30,e.Bounds.Height),UiPalette.Get(selected?"Text":"Muted"),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};
   content.Dock=DockStyle.Fill;content.AutoScroll=true;Controls.Add(content);Controls.Add(sections);content.BringToFront();
   sections.SelectedIndexChanged+=(s,e)=>{if(selecting)return;try{CaptureEditors();section=sections.SelectedIndex;ShowSection();}catch(Exception ex){Info(ex.Message);selecting=true;sections.SelectedIndex=section;selecting=false;}};
   UiAppearance.Surface(sections,"Header");sections.SelectedIndex=0;AppSettings.Apply(this);
   if(!string.IsNullOrEmpty(AppSettings.LoadWarning))Info(AppSettings.LoadWarning);
  }
  void Info(string text){message.Text=text;}
  void SynchronizeLanguage(){if(IsDisposed)return;double speed=AppSettings.Current.AnimationScale;if(Math.Abs(speed-observedSpeed)>1e-9&&Math.Abs(draft.AnimationScale-observedSpeed)<1e-9){draft.AnimationScale=speed;foreach(var n in numericEdits.Keys)if(n.AccessibleName=="Скорость анимации ×")n.Commit((decimal)speed);}observedSpeed=speed;draft.Language=AppSettings.Current.Language;if(languageChoice!=null&&!languageChoice.IsDisposed){syncingLanguage=true;try{languageChoice.SelectedIndex=(int)draft.Language;}finally{syncingLanguage=false;}}}
  internal bool ApplyDraft(){try{CaptureEditors();AppSettings.Commit(draft);draft=AppSettings.Current.Clone();ShowSection();Info(I18n.T("Настройки сохранены."));return true;}catch(Exception ex){Info(I18n.T(ex.Message));return false;}}
  void CaptureEditors(){foreach(var edit in numericEdits){decimal value;if(!edit.Key.TryRead(out value))throw new Exception(I18n.T("Проверьте поле: ")+I18n.T(edit.Key.AccessibleName));edit.Value(value);}foreach(var pair in colorFields){if(!System.Text.RegularExpressions.Regex.IsMatch(pair.Value.Text.Trim(),"^#[0-9a-fA-F]{6}$"))throw new Exception(I18n.T("Недопустимый цвет: ")+pair.Key);if(!string.Equals(pair.Value.Text.Trim(),ColorTranslator.ToHtml(UiPalette.Get(Without(draft,pair.Key),pair.Key)),StringComparison.OrdinalIgnoreCase))draft.Colors[pair.Key]=pair.Value.Text.Trim();else draft.Colors.Remove(pair.Key);}}
  // Цвет темы без пользовательской замены: чтобы в файл попадали только действительно изменённые цвета.
  static AppSettings Without(AppSettings source,string key){var copy=source.Clone();copy.Colors.Remove(key);return copy;}
  FlowLayoutPanel Stack(){var p=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(32,26,24,30)};content.Controls.Add(p);return p;}
  void Title(FlowLayoutPanel p,string text,string about){p.Controls.Add(new Label{Text=text,AutoSize=true,Font=new Font("Segoe UI Semibold",15f),Margin=new Padding(0,0,0,4)});if(about!=null)p.Controls.Add(new Label{Text=about,MaximumSize=new Size(ContentWidth,0),AutoSize=true,Tag="muted",Margin=new Padding(1,0,0,14)});}
  void Heading(FlowLayoutPanel p,string text){p.Controls.Add(new Label{Text=text,AutoSize=true,Font=new Font("Segoe UI Semibold",10.5f),Margin=new Padding(0,18,0,6)});}
  void Note(FlowLayoutPanel p,string text){p.Controls.Add(new Label{Text=text,MaximumSize=new Size(ContentWidth,0),AutoSize=true,Tag="muted",Margin=new Padding(1,2,0,8)});}
  ComboBox Choice(FlowLayoutPanel p,string label,string[] items,int selected,Action<int> set){Heading(p,label);var c=new EditorComboBox{Width=330,DropDownStyle=ComboBoxStyle.DropDownList};c.Items.AddRange(items);c.SelectedIndex=selected;int last=selected;c.SelectionChangeCommitted+=(s,e)=>{if(c.SelectedIndex==last)return;last=c.SelectedIndex;set(last);};p.Controls.Add(c);return c;}
  void Number(FlowLayoutPanel p,string label,string unit,decimal min,decimal max,int digits,decimal value,Action<decimal> set){Heading(p,label);var row=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=Padding.Empty};var n=new SafeNumber{Minimum=min,Maximum=max,DecimalPlaces=digits,Increment=digits==0?1:.1m,Width=140,AccessibleName=label};n.Commit(value);numericEdits[n]=set;n.ValueChanged+=(s,e)=>set(n.Value);row.Controls.Add(n);row.Controls.Add(new Label{Text=(unit==""?"":unit+"   ")+min.ToString("0.##")+" … "+max.ToString("0.##"),AutoSize=true,Tag="muted",Margin=new Padding(8,5,0,0)});p.Controls.Add(row);}
  void Toggle(FlowLayoutPanel p,string text,string about,bool value,Action<bool> set){var c=new CheckBox{Text=text,Checked=value,AutoSize=true,MaximumSize=new Size(ContentWidth,0),Font=new Font("Segoe UI Semibold",10.5f),Margin=new Padding(0,18,0,2)};c.CheckedChanged+=(s,e)=>set(c.Checked);p.Controls.Add(c);if(about!=null)p.Controls.Add(new Label{Text=about,MaximumSize=new Size(ContentWidth-20,0),AutoSize=true,Tag="muted",Margin=new Padding(20,0,0,4)});}
  // Карточки выбора: крупные, с примером оформления вместо выпадающего списка.
  void Cards(FlowLayoutPanel p,string[] titles,int selected,Func<int,Graphics,Rectangle,bool> preview,Action<int> set,int width,int height){var row=new FlowLayoutPanel{AutoSize=true,WrapContents=true,MaximumSize=new Size(ContentWidth+140,0),Margin=Padding.Empty};var cards=new List<OptionCard>();for(int i=0;i<titles.Length;i++){int index=i;var card=new OptionCard{Text=titles[i],Width=width,Height=height,Margin=new Padding(0,0,10,10),Selected=i==selected,Preview=(g,r)=>preview(index,g,r)};card.Click+=(s,e)=>{foreach(var other in cards){other.Selected=other==card;other.Invalidate();}set(index);};cards.Add(card);row.Controls.Add(card);}p.Controls.Add(row);}
  void ShowSection(){
   content.SuspendLayout();foreach(Control c in content.Controls.Cast<Control>().ToArray())c.Dispose();content.Controls.Clear();colorFields.Clear();numericEdits.Clear();languageChoice=null;var p=Stack();
   switch(sections.SelectedIndex){
    case 0:
     Title(p,"Внешний вид","Тема и акцентный цвет действуют во всех разделах программы.");
     Heading(p,"Тема");
     Cards(p,new[]{"Тёмная","Светлая","Как в Windows"},draft.Theme==GearTheme.Тёмная?0:draft.Theme==GearTheme.Светлая?1:2,(i,g,r)=>{bool dark=i==0||i==2&&AppSettings.SystemDark;PaintThemePreview(g,r,dark,i==2);return true;},i=>{draft.Theme=i==0?GearTheme.Тёмная:i==1?GearTheme.Светлая:GearTheme.Системная;Info("Тема выбрана. Нажмите «Сохранить».");content.Invalidate(true);},170,118);
     Heading(p,"Акцентный цвет");
     Note(p,"Им выделяются главные кнопки, выбранные вкладки и активные элементы.");
     Cards(p,Enum.GetNames(typeof(GearAccent)),(int)draft.Accent,(i,g,r)=>{g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var b=new SolidBrush(UiPalette.AccentSample((GearAccent)i,AppSettings.IsDark(draft))))g.FillEllipse(b,r.X+(r.Width-26)/2,r.Y+(r.Height-26)/2,26,26);return true;},i=>{draft.Accent=(GearAccent)i;draft.Colors.Remove("Accent");draft.Colors.Remove("Selection");Info("Цвет выбран. Нажмите «Сохранить».");},100,84);
     Toggle(p,"Показывать подсказки","Короткие пояснения в разделе «Робототехника» и под 3D-сценой. Отключите, когда освоитесь.",draft.ShowHints,v=>draft.ShowHints=v);
     Choice(p,"Что открывать при запуске",Enum.GetNames(typeof(GearStart)),(int)draft.StartSection,i=>draft.StartSection=(GearStart)i);
     if(draft.Colors.Count>0)Note(p,"Сейчас часть цветов изменена вручную (раздел «Свои цвета»). Они сохраняются при смене темы.");
     break;
    case 1:
     Title(p,"Управление и 3D","Как выглядит и ведёт себя трёхмерная сцена.");
     Heading(p,"Качество 3D-картинки");
     Cards(p,new[]{"Быстрое","Обычное","Подробное"},(int)draft.Quality,(i,g,r)=>{g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;int sides=i==0?6:i==1?12:40;var points=Enumerable.Range(0,sides).Select(k=>new PointF(r.X+r.Width/2f+(float)Math.Cos(k*2*Math.PI/sides)*15,r.Y+r.Height/2f+(float)Math.Sin(k*2*Math.PI/sides)*15)).ToArray();using(var pen=new Pen(UiPalette.Get("Muted"),1.6f))g.DrawPolygon(pen,points);return true;},i=>draft.Quality=(GearQuality)i,170,92);
     Note(p,"Влияет только на предпросмотр. На слабом компьютере выберите «Быстрое». Файлы STEP всегда точные.");
     Number(p,"Скорость вращения камеры мышью","",1,30,1,(decimal)draft.OrbitSensitivity,v=>draft.OrbitSensitivity=(double)v);
     Number(p,"Скорость анимации ×","×",.01m,10,2,(decimal)draft.AnimationScale,v=>draft.AnimationScale=(double)v);
     Toggle(p,"Привязка перемещения к сетке","Детали и звенья двигаются шагами, а не плавно — проще попадать в ровные размеры.",draft.SnapEnabled,v=>draft.SnapEnabled=v);
     Number(p,"Шаг сетки / перемещения, мм","мм",.01m,100,2,(decimal)draft.SnapStep,v=>draft.SnapStep=(double)v);
     break;
    case 3:
     Title(p,"Язык интерфейса","Язык меняется сразу, без кнопки «Сохранить».");
     languageChoice=new EditorComboBox{Width=330,DropDownStyle=ComboBoxStyle.DropDownList};languageChoice.Items.AddRange(new[]{"Русский","English"});languageChoice.SelectedIndex=(int)draft.Language;languageChoice.SelectedIndexChanged+=(s,e)=>{if(syncingLanguage||languageChoice.SelectedIndex<0||languageChoice.SelectedIndex==(int)AppSettings.Current.Language)return;try{AppSettings.SetLanguage((GearLanguage)languageChoice.SelectedIndex);draft.Language=AppSettings.Current.Language;Info("Язык изменён и сохранён.");}catch(Exception ex){Info(ex.Message);}};p.Controls.Add(languageChoice);
     Note(p,"Названия ваших проектов и числа в них не меняются.");
     break;
    case 2:
     Title(p,"Сохранение","Запасные копии на случай сбоя или случайного закрытия.");
     Toggle(p,"Автосохранение рабочих проектов","Программа сама делает запасную копию проекта робота. Основной файл записывается только командой «Сохранить».",draft.AutoSave,v=>draft.AutoSave=v);
     Number(p,"Интервал автосохранения, с","с",15,600,0,draft.AutoSaveSeconds,v=>draft.AutoSaveSeconds=(int)v);
     Heading(p,"Папка настроек и восстановительных копий:");
     var path=new TextBox{Text=AppSettings.Folder,Name="UserPath",ReadOnly=true,Width=ContentWidth-150,Margin=new Padding(0,3,8,0)};var open=new Button{Text="Открыть папку",Width=140,Height=30,Margin=Padding.Empty};open.Click+=(s,e)=>{try{System.IO.Directory.CreateDirectory(AppSettings.Folder);System.Diagnostics.Process.Start("explorer.exe","\""+AppSettings.Folder+"\"");}catch(Exception ex){Info(ex.Message);}};var folder=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=Padding.Empty};folder.Controls.Add(path);folder.Controls.Add(open);p.Controls.Add(folder);
     break;
    case 4:
     Title(p,"Свои цвета","Для тех, кому мало готовых тем. Нажмите на образец или введите цвет в виде #RRGGBB.");
     var restore=new Button{Text="Вернуть цвета темы",Width=200,Height=32,Margin=new Padding(0,0,0,12)};restore.Click+=(s,e)=>BeginInvoke(new Action(()=>{draft.Colors.Clear();ShowSection();Info("Цвета темы подготовлены. Нажмите «Сохранить».");}));p.Controls.Add(restore);
     string[] names={"Фон окна","Панели","Заголовки и меню","Поля ввода","Основной текст","Вторичный текст","Границы","Акцент","Выделение","Фон 3D / чертежа","Сетка","Ось X","Ось Y","Ось Z","Успех","Предупреждение","Ошибка"};
     for(int i=0;i<UiPalette.Keys.Length;i++){string key=UiPalette.Keys[i];var row=new FlowLayoutPanel{Width=ContentWidth,Height=36,WrapContents=false,Margin=new Padding(0,1,0,1)};row.Controls.Add(new Label{Text=names[i],Width=240,Height=30,TextAlign=ContentAlignment.MiddleLeft});var swatch=new PaletteButton{Width=52,Height=28,Swatch=UiPalette.Get(draft,key)};var hex=new TextBox{Width=112,Text=ColorTranslator.ToHtml(swatch.Swatch),Margin=new Padding(8,4,0,0)};colorFields[key]=hex;hex.TextChanged+=(s,e)=>{try{if(System.Text.RegularExpressions.Regex.IsMatch(hex.Text,"^#[0-9a-fA-F]{6}$")){swatch.Swatch=ColorTranslator.FromHtml(hex.Text);swatch.Invalidate();}}catch{}};swatch.Click+=(s,e)=>{using(var d=new ColorDialog{Color=swatch.Swatch,FullOpen=true})if(d.ShowLocalizedDialog(this)==DialogResult.OK)hex.Text="#"+d.Color.R.ToString("X2")+d.Color.G.ToString("X2")+d.Color.B.ToString("X2");};row.Controls.Add(swatch);row.Controls.Add(hex);p.Controls.Add(row);}
     break;
   }
   AppSettings.Apply(content);sections.Invalidate();content.ResumeLayout(true);
  }
  static void PaintThemePreview(Graphics g,Rectangle r,bool dark,bool split){
   g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var sample=new AppSettings{Theme=dark?GearTheme.Тёмная:GearTheme.Светлая,Accent=AppSettings.Current.Accent};
   using(var path=UiAppearance.Rounded(r,6)){using(var b=new SolidBrush(UiPalette.Get(sample,"Window")))g.FillPath(b,path);g.SetClip(path);
    if(split){var other=new AppSettings{Theme=dark?GearTheme.Светлая:GearTheme.Тёмная};using(var b=new SolidBrush(UiPalette.Get(other,"Window")))g.FillPolygon(b,new[]{new Point(r.Right,r.Top),new Point(r.Right,r.Bottom),new Point(r.Left,r.Bottom)});}
    using(var b=new SolidBrush(UiPalette.Get(sample,"Header")))g.FillRectangle(b,r.X,r.Y,r.Width,11);
    using(var b=new SolidBrush(UiPalette.Get(sample,"Panel")))g.FillRectangle(b,r.X+6,r.Y+17,r.Width/3,r.Height-23);
    using(var b=new SolidBrush(UiPalette.Get(sample,"Muted"))){g.FillRectangle(b,r.X+12,r.Y+24,r.Width/3-14,3);g.FillRectangle(b,r.X+12,r.Y+32,r.Width/3-22,3);}
    using(var b=new SolidBrush(UiPalette.Get(sample,"Accent")))using(var pill=UiAppearance.Rounded(new RectangleF(r.Right-44,r.Bottom-18,36,11),4))g.FillPath(b,pill);
    g.ResetClip();using(var pen=new Pen(UiPalette.Get("Border")))g.DrawPath(pen,path);}
  }
  sealed class PaletteButton:Button {public Color Swatch;protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=UiAppearance.Rounded(new RectangleF(5,5,Width-10,Height-10),4))using(var b=new SolidBrush(Swatch))e.Graphics.FillPath(b,path);}}
  // Плитка выбора с небольшим рисунком-примером и подписью.
  sealed class OptionCard:Control {
   public bool Selected;public Func<Graphics,Rectangle,bool> Preview;bool hot;
   public OptionCard(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable|ControlStyles.StandardClick,true);TabStop=true;Cursor=Cursors.Hand;}
   protected override void OnMouseEnter(EventArgs e){hot=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hot=false;Invalidate();base.OnMouseLeave(e);}
   protected override void OnGotFocus(EventArgs e){Invalidate();base.OnGotFocus(e);}protected override void OnLostFocus(EventArgs e){Invalidate();base.OnLostFocus(e);}
   protected override bool IsInputKey(Keys key){return key==Keys.Space||key==Keys.Enter||base.IsInputKey(key);}
   protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
   protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?UiPalette.Get("Panel"):Parent.BackColor);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-2,Height-2);Color fill=Selected?UiPalette.Get("Selection"):hot?UiPalette.Blend(UiPalette.Get("Field"),UiPalette.Get("Accent"),.12):UiPalette.Get("Field");
    using(var path=UiAppearance.Rounded(r,9)){using(var b=new SolidBrush(fill))g.FillPath(b,path);using(var pen=new Pen(Selected||Focused&&NativeFocus.Keyboard?UiPalette.Get("Accent"):UiPalette.Get("Border"),Selected?2f:1f))g.DrawPath(pen,path);}
    int caption=26;var area=new Rectangle(10,10,Width-20,Math.Max(8,Height-caption-16));if(Preview!=null)Preview(g,area);
    TextRenderer.DrawText(g,I18n.T(Text).Replace('_',' '),Font,new Rectangle(4,Height-caption-4,Width-8,caption),UiPalette.Get("Text"),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);}
  }
 }
}
