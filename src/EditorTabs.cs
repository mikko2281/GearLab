using System;using System.Drawing;using System.Windows.Forms;using System.Runtime.InteropServices;
namespace GearLab {
 // Native tabs keep their keyboard, accessibility and page lifecycle. Paint only their unused chrome.
 public class EditorTabs:TabControl {
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg!=0x000F||!IsHandleCreated)return;using(var g=Graphics.FromHwnd(Handle))using(var b=new SolidBrush(UiPalette.Get("Window"))){var page=DisplayRectangle;int right=TabCount==0?0:GetTabRect(TabCount-1).Right+2;g.FillRectangle(b,right,0,Math.Max(0,ClientSize.Width-right),Math.Max(0,page.Top-2));g.FillRectangle(b,0,page.Top-2,ClientSize.Width,2);g.FillRectangle(b,0,page.Top,Math.Max(0,page.Left),Math.Max(0,ClientSize.Height-page.Top));g.FillRectangle(b,page.Right,page.Top,Math.Max(0,ClientSize.Width-page.Right),Math.Max(0,ClientSize.Height-page.Top));g.FillRectangle(b,0,page.Bottom,ClientSize.Width,Math.Max(0,ClientSize.Height-page.Bottom));}}
 }
 public static class NativeAppearance {
  sealed class State{public IntPtr Handle;public bool Dark;}
  static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control,State> styled=new System.Runtime.CompilerServices.ConditionalWeakTable<Control,State>();
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int a,ref int v,int size);
  [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr h,string app,string list);
  public static void Apply(Control c){if(!c.IsHandleCreated)return;bool dark=AppSettings.Current.Theme==GearTheme.Тёмная;var state=styled.GetValue(c,k=>new State());if(state.Handle==c.Handle&&state.Dark==dark)return;state.Handle=c.Handle;state.Dark=dark;try{var f=c as Form;if(f!=null&&f.TopLevel){int enabled=dark?1:0;DwmSetWindowAttribute(c.Handle,20,ref enabled,4);}if(c is TreeView||c is ListBox||c is TextBoxBase||c is ScrollableControl)SetWindowTheme(c.Handle,dark?"DarkMode_Explorer":"Explorer",null);}catch(DllNotFoundException){}catch(EntryPointNotFoundException){}}
 }
 public class EditorTrackBar:TrackBar {
  [StructLayout(LayoutKind.Sequential)]struct NativeRect {public int Left,Top,Right,Bottom;}
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,ref NativeRect rect);
  protected override void OnValueChanged(EventArgs e){base.OnValueChanged(e);Invalidate();}
  protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg!=15||!IsHandleCreated)return;var thumb=new NativeRect();var channel=new NativeRect();SendMessage(Handle,0x419,IntPtr.Zero,ref thumb);SendMessage(Handle,0x41A,IntPtr.Zero,ref channel);using(var g=Graphics.FromHwnd(Handle))using(var background=new SolidBrush(UiPalette.Get("Panel")))using(var rail=new Pen(UiPalette.Get("Border"),4))using(var active=new Pen(UiPalette.Get("Accent"),3))using(var handle=new SolidBrush(UiPalette.Get(Enabled?"Accent":"Muted"))){g.Clear(background.Color);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;if(Orientation==Orientation.Horizontal){float y=ClientSize.Height/2f,x=(thumb.Left+thumb.Right)/2f;g.DrawLine(rail,8,y,Math.Max(8,Width-8),y);g.DrawLine(active,8,y,Math.Max(8,x),y);g.FillEllipse(handle,x-5,y-7,10,14);}else{float x=ClientSize.Width/2f,y=(thumb.Top+thumb.Bottom)/2f;g.DrawLine(rail,x,8,x,Math.Max(8,Height-8));g.FillEllipse(handle,x-7,y-5,14,10);}if(Focused)ControlPaint.DrawFocusRectangle(g,ClientRectangle,UiPalette.Get("Text"),background.Color);}}
 }
}
