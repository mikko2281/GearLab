using System;using System.IO;using System.Text;using System.Threading;using System.Drawing;using System.Runtime.InteropServices;using System.Windows.Forms;
namespace GearLab {
 // Служебная команда --ui-shots <папка>: снимки основных разделов в тёмной и светлой теме для проверки оформления.
 internal static class UiShots {
  [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
  [DllImport("user32.dll")]static extern bool EnableWindow(IntPtr window,bool enable);
  static void Pump(int milliseconds){var until=DateTime.UtcNow.AddMilliseconds(milliseconds);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(15);}}
  static bool HasContent(Bitmap image){int first=image.GetPixel(0,0).ToArgb();for(int y=0;y<image.Height;y+=Math.Max(1,image.Height/32))for(int x=0;x<image.Width;x+=Math.Max(1,image.Width/32))if(image.GetPixel(x,y).ToArgb()!=first)return true;return false;}
  static void Capture(Form form,string path){using(var bitmap=new Bitmap(form.Width,form.Height)){bool ok=false;using(var g=Graphics.FromImage(bitmap)){IntPtr dc=g.GetHdc();try{ok=PrintWindow(form.Handle,dc,2);}finally{g.ReleaseHdc(dc);}}if(!ok||!HasContent(bitmap))throw new InvalidOperationException("Window capture failed or is blank; an interactive desktop is required. DrawToBitmap cannot capture the complete WPF/WebGL scene.");bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
  static System.Collections.Generic.IEnumerable<Control> All(Control c){yield return c;foreach(Control child in c.Controls)foreach(var next in All(child))yield return next;}
  static void Click(Control root,string text){foreach(var c in All(root)){var b=c as Button;if(b!=null&&I18n.Raw(b)==text){b.PerformClick();return;}}}
  public static int Run(string folder){folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);Environment.SetEnvironmentVariable("GEARLAB_PROFILE_DIR",Path.Combine(folder,"profile"));var report=new StringBuilder();bool failed=false;try{
   AppSettings.Commit(new AppSettings());
   using(var shell=new WorkspaceShell(new ReducerWorkspaceForm())){shell.StartPosition=FormStartPosition.Manual;shell.Location=new Point(40,30);shell.Size=new Size(1500,940);shell.Show();EnableWindow(shell.Handle,false);Pump(2500);
    try{SceneAnimation.Enabled=true;shell.Navigate("train");for(int i=0;i<8;i++){Pump(230);Capture(shell,Path.Combine(folder,"anim-assembly-"+i+".png"));}Pump(1500);Click(shell,"Разнести");int frame=0;foreach(int wait in new[]{280,280,500}){Pump(wait);Capture(shell,Path.Combine(folder,"anim-explode-"+(frame++)+".png"));}Pump(600);Capture(shell,Path.Combine(folder,"anim-exploded.png"));Click(shell,"Собрать");Pump(400);Capture(shell,Path.Combine(folder,"anim-collapse.png"));Pump(1200);report.AppendLine("OK animation frames");}catch(Exception ex){failed=true;report.AppendLine("FAIL animation: "+ex);}finally{SceneAnimation.Enabled=false;}
    foreach(var theme in new[]{GearTheme.Тёмная,GearTheme.Светлая}){var settings=AppSettings.Current.Clone();settings.Theme=theme;AppSettings.Commit(settings);string tag=theme==GearTheme.Тёмная?"dark":"light";
     foreach(string key in new[]{"reducer","train","settings","robot"}){try{shell.Navigate(key);Pump(key=="robot"?9000:1800);Capture(shell,Path.Combine(folder,tag+"-"+key+".png"));report.AppendLine("OK "+tag+"-"+key+" | "+(shell.Pages.SelectedTab==null?"":shell.Pages.SelectedTab.Text));}catch(Exception ex){failed=true;report.AppendLine("FAIL "+tag+"-"+key+": "+ex);}}
    }
    EnableWindow(shell.Handle,true);shell.ApproveTestClose();shell.Close();}
   File.WriteAllText(Path.Combine(folder,"ui-shots.txt"),report.ToString(),Encoding.UTF8);return failed?1:0;
  }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"ui-shots-error.txt"),report+ex.ToString(),Encoding.UTF8);return 1;}}
 }
}
