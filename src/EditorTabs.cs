using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.InteropServices;
namespace GearLab {
 // Native tabs keep their keyboard, accessibility and page lifecycle. Paint only their unused chrome.
 public class EditorTabs:TabControl {
  int hot=-1;
  int HotIndex(Point p){for(int i=0;i<TabCount;i++)if(GetTabRect(i).Contains(p))return i;return -1;}
  void SetHot(int index){if(hot==index)return;hot=index;if(IsHandleCreated)Invalidate(new Rectangle(0,0,ClientSize.Width,Math.Max(1,DisplayRectangle.Top)));}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);SetHot(HotIndex(e.Location));}
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);SetHot(-1);}
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(!IsHandleCreated)return;if(m.Msg==0x000F){using(var g=Graphics.FromHwnd(Handle))PaintChrome(g);}else if(m.Msg==0x0318&&m.WParam!=IntPtr.Zero){using(var g=Graphics.FromHdc(m.WParam))PaintChrome(g);}}
  // Заголовки вкладок рисуем сами: плоские, с подчёркиванием выбранной. Клавиатура и страницы остаются системными.
  void PaintChrome(Graphics g){Color window=UiPalette.Get("Window"),ink=UiPalette.Get("Text");var page=DisplayRectangle;using(var b=new SolidBrush(window)){
    if(Alignment!=TabAlignment.Top||Multiline){int right=TabCount==0?0:GetTabRect(TabCount-1).Right+2;g.FillRectangle(b,right,0,Math.Max(0,ClientSize.Width-right),Math.Max(0,page.Top-2));g.FillRectangle(b,0,page.Top-2,ClientSize.Width,2);}
    else g.FillRectangle(b,0,0,ClientSize.Width,Math.Max(0,page.Top));
    g.FillRectangle(b,0,page.Top,Math.Max(0,page.Left),Math.Max(0,ClientSize.Height-page.Top));g.FillRectangle(b,page.Right,page.Top,Math.Max(0,ClientSize.Width-page.Right),Math.Max(0,ClientSize.Height-page.Top));g.FillRectangle(b,0,page.Bottom,ClientSize.Width,Math.Max(0,ClientSize.Height-page.Bottom));}
   if(Alignment!=TabAlignment.Top||Multiline||TabCount==0)return;
   using(var line=new Pen(UiPalette.Blend(window,UiPalette.Get("Border"),.7)))g.DrawLine(line,0,page.Top-1,ClientSize.Width,page.Top-1);
   var old=g.SmoothingMode;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   for(int i=0;i<TabCount;i++){var r=GetTabRect(i);if(r.Width<8||r.Height<8||r.Right<0||r.Left>ClientSize.Width)continue;bool current=i==SelectedIndex,hover=i==hot&&!current;
    var chip=new RectangleF(r.X+2.5f,r.Y+2.5f,r.Width-5f,r.Height-5f);
    if(current||hover)using(var path=UiAppearance.Rounded(chip,6))using(var fill=new SolidBrush(UiPalette.Blend(window,ink,current?.10:.05)))g.FillPath(fill,path);
    if(current)using(var accent=new SolidBrush(UiPalette.Get("Accent")))using(var mark=UiAppearance.Rounded(new RectangleF(r.X+12,page.Top-3,Math.Max(4,r.Width-24),3),1.5f))g.FillPath(accent,mark);
    TextRenderer.DrawText(g,TabPages[i].Text,Font,r,UiPalette.Get(current?"Text":"Muted"),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine);}
   g.SmoothingMode=old;}
 }
 public static class NativeAppearance {
  sealed class State{public IntPtr Handle;public bool Dark;public int Revision=-1;}
  static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control,State> styled=new System.Runtime.CompilerServices.ConditionalWeakTable<Control,State>();
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int a,ref int v,int size);
  [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr h,string app,string list);
  // Windows 11: заголовок окна в цвет верхней панели. На прежних системах вызов ничего не меняет.
  static int Ref(Color c){return c.R|c.G<<8|c.B<<16;}
  static void Caption(IntPtr handle){int back=Ref(UiPalette.Get("Header")),text=Ref(UiPalette.Get("Text")),border=Ref(UiPalette.Get("Border"));DwmSetWindowAttribute(handle,35,ref back,4);DwmSetWindowAttribute(handle,36,ref text,4);DwmSetWindowAttribute(handle,34,ref border,4);}
  public static void Apply(Control c){if(!c.IsHandleCreated)return;bool dark=AppSettings.Dark;var state=styled.GetValue(c,k=>new State());bool top=c is Form&&((Form)c).TopLevel;int revision=AppSettings.AppearanceRevision;if(state.Handle==c.Handle&&state.Dark==dark&&(!top||state.Revision==revision))return;state.Handle=c.Handle;state.Dark=dark;state.Revision=revision;try{var f=c as Form;if(f!=null&&f.TopLevel){int enabled=dark?1:0;DwmSetWindowAttribute(c.Handle,20,ref enabled,4);Caption(c.Handle);}if(c is TreeView||c is ListBox||c is TextBoxBase||c is ScrollableControl)SetWindowTheme(c.Handle,dark?"DarkMode_Explorer":"Explorer",null);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
 }
 public class EditorTrackBar:TrackBar {
  [StructLayout(LayoutKind.Sequential)]struct NativeRect {public int Left,Top,Right,Bottom;}
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,ref NativeRect rect);
  protected override void OnValueChanged(EventArgs e){base.OnValueChanged(e);Invalidate();}
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg!=15||!IsHandleCreated)return;var thumb=new NativeRect();var channel=new NativeRect();SendMessage(Handle,0x419,IntPtr.Zero,ref thumb);SendMessage(Handle,0x41A,IntPtr.Zero,ref channel);using(var g=Graphics.FromHwnd(Handle))using(var background=new SolidBrush(UiPalette.Get("Panel")))using(var rail=new Pen(UiPalette.Get("Border"),4))using(var active=new Pen(UiPalette.Get("Accent"),3))using(var handle=new SolidBrush(UiPalette.Get(Enabled?"Accent":"Muted"))){g.Clear(background.Color);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;if(Orientation==Orientation.Horizontal){float y=ClientSize.Height/2f,x=(thumb.Left+thumb.Right)/2f;g.DrawLine(rail,8,y,Math.Max(8,Width-8),y);g.DrawLine(active,8,y,Math.Max(8,x),y);g.FillEllipse(handle,x-5,y-7,10,14);}else{float x=ClientSize.Width/2f,y=(thumb.Top+thumb.Bottom)/2f;g.DrawLine(rail,x,8,x,Math.Max(8,Height-8));g.FillEllipse(handle,x-7,y-5,14,10);}if(Focused)ControlPaint.DrawFocusRectangle(g,ClientRectangle,UiPalette.Get("Text"),background.Color);}}
 }
}
