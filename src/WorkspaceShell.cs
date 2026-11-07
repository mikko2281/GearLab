using System;using System.Threading.Tasks;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;

namespace GearLab {

 public interface IWorkspaceDocument {bool WorkspaceCanClose();void WorkspaceApproveClose();void WorkspaceActivate(bool active);}

 public static class WorkspaceHost {

  public static WorkspaceShell Find(Control owner){for(Control c=owner;c!=null;c=c.Parent)if(c is WorkspaceShell)return (WorkspaceShell)c;var f=owner as Form;return f!=null&&f.Owner!=null?Find(f.Owner):null;}

  public static void ShowWorkspace(this Form form,Control owner){Open(owner,form);}

  public static void Navigate(Control owner,string key){var shell=Find(owner);if(shell!=null)shell.Navigate(key);else Open(owner,WorkspaceShell.Create(key));}

  public static void OpenWave(Form owner,CADKernel kernel,Func<CADFeature,Task<bool>> insert){var shell=Find(owner);if(shell==null){var reducers=new ReducerWorkspaceForm();new WorkspaceShell(reducers).Show();reducers.ConfigureTarget(kernel,insert,null);return;}shell.Navigate("wave");var workspace=shell.Pages.SelectedTab.Tag as ReducerWorkspaceForm;if(workspace!=null)workspace.ConfigureTarget(kernel,insert,()=>shell.FocusDocument(owner));}

  public static void Open(Control owner,Form form){if(form is CADStudioForm||form is PolygonForm||form is MechanismForm){form.Dispose();throw new InvalidOperationException("Редакторы CAD и полигонов удалены. Для внешней правки используйте экспорт STEP / STL.");}var shell=Find(owner);if(shell!=null)shell.AddDocument(form,null);else new WorkspaceShell(form).Show(owner as IWin32Window);}

 }

 public sealed class WorkspaceShell:Form {

  internal readonly TabControl Pages=new EditorTabs();readonly Dictionary<string,TabPage> sections=new Dictionary<string,TabPage>();readonly List<Form> documents=new List<Form>();Form active;bool approved;

  public WorkspaceShell(Form initial){Text="GearLab 6.4.1";Font=new Font("Segoe UI",9);Size=new Size(1660,1050);MinimumSize=new Size(1180,760);StartPosition=FormStartPosition.CenterScreen;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);BackColor=Color.FromArgb(239,243,249);var nav=new ToolStrip{Dock=DockStyle.Top,GripStyle=ToolStripGripStyle.Hidden,Padding=new Padding(6),AutoSize=false,Height=34,BackColor=Color.FromArgb(224,232,244)};nav.Items.Add(new ToolStripLabel("GEARLAB 6.4.1"));foreach(var pair in new[]{new[]{"robot","Робототехника"},new[]{"reducer","Редукторы"},new[]{"train","Ступени"},new[]{"settings","Настройки"}}){string key=pair[0];var button=new ToolStripButton(pair[1]){Padding=new Padding(10,2,10,2)};button.Click+=(s,e)=>Navigate(key);nav.Items.Add(button);}nav.Items.Add(new ToolStripSeparator());var close=new ToolStripButton("Закрыть вкладку");close.Click+=(s,e)=>CloseCurrent();nav.Items.Add(close);var language=new ToolStripDropDownButton{Alignment=ToolStripItemAlignment.Right,Text=AppSettings.Current.Language==GearLanguage.English?"EN":"RU",ToolTipText="Язык / Language"};var ru=new ToolStripMenuItem("Русский");var en=new ToolStripMenuItem("English");ru.Click+=(s,e)=>{try{AppSettings.SetLanguage(GearLanguage.Русский);}catch(Exception ex){UiMessage.Show(this,ex.Message);}};en.Click+=(s,e)=>{try{AppSettings.SetLanguage(GearLanguage.English);}catch(Exception ex){UiMessage.Show(this,ex.Message);}};language.DropDownItems.AddRange(new ToolStripItem[]{ru,en});Action syncLanguage=()=>{language.Text=AppSettings.Current.Language==GearLanguage.English?"EN":"RU";ru.Checked=AppSettings.Current.Language==GearLanguage.Русский;en.Checked=!ru.Checked;};AppSettings.Changed+=syncLanguage;Disposed+=(s,e)=>AppSettings.Changed-=syncLanguage;nav.Items.Add(language);syncLanguage();Pages.Dock=DockStyle.Fill;Pages.Padding=new Point(14,5);Pages.SelectedIndexChanged+=(s,e)=>ActivatePage();Controls.Add(Pages);Controls.Add(nav);Pages.BringToFront();Pages.Selected+= (s,e)=>ActivatePage();AddDocument(initial,Key(initial));AppSettings.Apply(this);FormClosing+=(s,e)=>{if(approved)return;foreach(var form in documents.ToArray()){var doc=form as IWorkspaceDocument;if(doc!=null&&!doc.WorkspaceCanClose()){Pages.SelectedTab=Pages.TabPages.Cast<TabPage>().First(p=>p.Tag==form);e.Cancel=true;return;}}approved=true;foreach(var form in documents.ToArray()){var doc=form as IWorkspaceDocument;if(doc!=null)doc.WorkspaceApproveClose();form.Close();}};}

  static string Key(Form f){return f is TrainDesignerForm?"train":f is CADStudioForm?"cad":f is RobotDesignerForm?"robot":f is MechanismForm?"mechanism":f is MainForm||f is ReducerWorkspaceForm?"reducer":f is WaveReducerForm?"wave":f is PolygonForm?"polygon":"settings";}

  public static Form Create(string key){switch(key){case "robot":return new RobotDesignerForm();case "mechanism":throw new InvalidOperationException("Раздел «Свой механизм» удалён.");case "train":return new TrainDesignerForm();case "reducer":return new ReducerWorkspaceForm();case "wave":var reducers=new ReducerWorkspaceForm();reducers.SelectWave();return reducers;case "cad":case "polygon":throw new InvalidOperationException("Раздел моделирования удалён.");case "settings":return new SettingsForm(null);default:return new ReducerWorkspaceForm();}}

  public void Navigate(string key){if(key=="cad"||key=="polygon"||key=="mechanism")throw new InvalidOperationException("Раздел моделирования удалён.");bool wave=key=="wave";if(wave)key="reducer";TabPage page;if(sections.TryGetValue(key,out page)&&!page.IsDisposed){Pages.SelectedTab=page;if(wave&&page.Tag is ReducerWorkspaceForm)((ReducerWorkspaceForm)page.Tag).SelectWave();return;}var created=Create(key);AddDocument(created,key);if(wave&&created is ReducerWorkspaceForm)((ReducerWorkspaceForm)created).SelectWave();}

  public void AddDocument(Form form,string key){if(key!=null&&sections.ContainsKey(key)){form.Dispose();Pages.SelectedTab=sections[key];return;}var returnTo=key==null?Pages.SelectedTab:null;var page=new TabPage{Tag=form,Padding=Padding.Empty};if(key!=null)sections[key]=page;documents.Add(form);form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;form.StartPosition=FormStartPosition.Manual;form.Dock=DockStyle.Fill;form.MinimumSize=Size.Empty;page.Controls.Add(form);Action caption=()=>{page.Text=form.Text.Replace("GearLab 6.4.1 · ","").Replace("GearLab 4 · ","").Replace("GearLab 3 · ","");if(page.Text.Length>45)page.Text=page.Text.Substring(0,42)+"…";if(active==form)Text="GearLab 6.4.1 · "+page.Text;};form.TextChanged+=(s,e)=>caption();form.FormClosed+=(s,e)=>{documents.Remove(form);foreach(var k in sections.Where(p=>p.Value==page).Select(p=>p.Key).ToArray())sections.Remove(k);Pages.TabPages.Remove(page);page.Dispose();if(returnTo!=null&&Pages.TabPages.Contains(returnTo))Pages.SelectedTab=returnTo;else if(documents.Count==0&&!approved)Navigate("reducer");};Pages.TabPages.Add(page);Pages.SelectedTab=page;if(Visible)form.Show();else Shown+=(s,e)=>{if(!form.IsDisposed){form.Show();form.PerformLayout();}};caption();AppSettings.Apply(form);ActivatePage();}

  public void FocusDocument(Form form){var page=Pages.TabPages.Cast<TabPage>().FirstOrDefault(p=>p.Tag==form);if(page!=null)Pages.SelectedTab=page;}

  public void ApplyWorkspaceSettings(){AppSettings.Apply(this);foreach(var form in documents)AppSettings.Apply(form);}

  void ActivatePage(){var next=Pages.SelectedTab==null?null:Pages.SelectedTab.Tag as Form;foreach(var document in documents){if(document!=next)document.Hide();}if(next!=null&&Visible&&!next.Visible)next.Show();if(active==next)return;var previous=active as IWorkspaceDocument;if(previous!=null)previous.WorkspaceActivate(false);active=next;var current=active as IWorkspaceDocument;if(current!=null)current.WorkspaceActivate(true);if(active!=null)Text="GearLab 6.4.1 · "+Pages.SelectedTab.Text;}

  void CloseCurrent(){var form=Pages.SelectedTab==null?null:Pages.SelectedTab.Tag as Form;if(form==null)return;var doc=form as IWorkspaceDocument;if(doc!=null&&!doc.WorkspaceCanClose())return;if(doc!=null)doc.WorkspaceApproveClose();form.Close();if(documents.Count==0)Navigate("reducer");}

  internal void ApproveTestClose(){approved=true;foreach(var form in documents.ToArray()){var d=form as IWorkspaceDocument;if(d!=null)d.WorkspaceApproveClose();form.Close();}}

 }

 public sealed partial class CADStudioForm:IWorkspaceDocument {public bool WorkspaceCanClose(){return Confirm();}public void WorkspaceApproveClose(){allowClose=true;}public void WorkspaceActivate(bool active){} }

 public sealed partial class RobotDesignerForm:IWorkspaceDocument {public bool WorkspaceCanClose(){return Confirm();}public void WorkspaceApproveClose(){allowClose=true;}public void WorkspaceActivate(bool active){if(!active)Stop();}}

 public sealed partial class MechanismForm:IWorkspaceDocument {public bool WorkspaceCanClose(){return Confirm();}public void WorkspaceApproveClose(){closingApproved=true;}public void WorkspaceActivate(bool active){if(!active)Stop();}}

 public partial class MainForm:IWorkspaceDocument {public bool WorkspaceCanClose(){return !waveExportBusy&&ConfirmChanges();}public void WorkspaceApproveClose(){closingApproved=true;}public void WorkspaceActivate(bool active){if(inspectorWorkspace!=null)inspectorWorkspace.SetActive(active);if(animation!=null){if(active)animation.Start();else animation.Stop();}}}

 public sealed partial class PolygonForm:IWorkspaceDocument {public bool WorkspaceCanClose(){return Confirm();}public void WorkspaceApproveClose(){allowClose=true;}public void WorkspaceActivate(bool active){} }

 public sealed partial class WaveReducerForm:IWorkspaceDocument {public bool WorkspaceCanClose(){if(!busy)return true;UiMessage.Show(this,"Дождитесь построения или отмените расчёт.");return false;}public void WorkspaceApproveClose(){closing=true;}public void WorkspaceActivate(bool active){if(!active)playing=false;}}

}

