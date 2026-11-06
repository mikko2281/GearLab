using System;using System.Threading.Tasks;using System.Drawing;using System.Windows.Forms;

namespace GearLab {

 public sealed class ReducerWorkspaceForm:Form,IWorkspaceDocument {

  readonly ComboBox family=new ComboBox(),category=new ComboBox();readonly TextBox search=new TextBox();bool syncing;int selectedKind;readonly Panel surface=new Panel();MainForm ordinary;WaveReducerForm wave;Form active;

  public ReducerWorkspaceForm(){Text="Редукторы";Font=new Font("Segoe UI",10);Size=new Size(1500,980);var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=43,Padding=new Padding(6)};bar.Controls.Add(new Label{Text="Тип редуктора",AutoSize=true,Padding=new Padding(0,5,8,0)});family.DropDownStyle=ComboBoxStyle.DropDownList;family.Width=340;family.Items.AddRange(TransmissionCatalog.All);category.DropDownStyle=ComboBoxStyle.DropDownList;category.Width=180;category.Items.Add("Все категории");foreach(var c in System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(TransmissionCatalog.All,x=>x.Category)))category.Items.Add(c);category.SelectedIndex=0;search.Width=160;search.AccessibleName="Поиск передачи";bar.Controls.Add(category);bar.Controls.Add(new Label{Text="Поиск",AutoSize=true,Padding=new Padding(0,5,0,0)});bar.Controls.Add(search);search.TextChanged+=(s,e)=>Filter();category.SelectedIndexChanged+=(s,e)=>Filter();bar.Controls.Add(family);surface.Dock=DockStyle.Fill;Controls.Add(surface);Controls.Add(bar);family.SelectedIndexChanged+=(s,e)=>{if(syncing||family.SelectedItem==null)return;selectedKind=((TransmissionType)family.SelectedItem).Id;ActivateFamily();};Shown+=(s,e)=>{if(family.SelectedIndex<0)family.SelectedIndex=0;else ActivateFamily();};}

  public void ConfigureTarget(CADKernel kernel,Func<CADFeature,Task<bool>> insert,Action done){SelectWave();if(wave==null){wave=new WaveReducerForm();Embed(wave);}wave.ConfigureTarget(kernel,insert,done);}

  public void SelectWave(){Choose(4);if(Visible)ActivateFamily();}

  void ActivateFamily(){if(!Visible)return;if(ordinary==null){ordinary=new MainForm();ordinary.ConfigureHostedKind();ordinary.HostedKindChanged+=k=>{if(selectedKind!=k){selectedKind=k;Choose(k);}};Embed(ordinary);}ordinary.SetHostedKind(selectedKind);active=ordinary;active.Show();active.BringToFront();active.PerformLayout();((IWorkspaceDocument)ordinary).WorkspaceActivate(true);Text="Редукторы · "+family.Text;}
  void Embed(Form form){form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;form.MinimumSize=Size.Empty;form.Dock=DockStyle.Fill;surface.Controls.Add(form);}
  public bool WorkspaceCanClose(){return ordinary==null||((IWorkspaceDocument)ordinary).WorkspaceCanClose();}
  public void WorkspaceApproveClose(){if(ordinary!=null)((IWorkspaceDocument)ordinary).WorkspaceApproveClose();}
  public void WorkspaceActivate(bool value){if(ordinary!=null)((IWorkspaceDocument)ordinary).WorkspaceActivate(value);}
  void Choose(int id){syncing=true;family.Items.Clear();family.Items.AddRange(TransmissionCatalog.All);family.SelectedIndex=id;selectedKind=id;syncing=false;}
  void Filter(){syncing=true;family.Items.Clear();foreach(var t in TransmissionCatalog.All)if((category.SelectedIndex==0||t.Category==Convert.ToString(category.SelectedItem))&&((t.Name.IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0||I18n.T(t.Name).IndexOf(search.Text,StringComparison.OrdinalIgnoreCase)>=0)))family.Items.Add(t);for(int i=0;i<family.Items.Count;i++)if(((TransmissionType)family.Items[i]).Id==selectedKind)family.SelectedIndex=i;syncing=false;}
  internal void TestSelect(int index){Choose(index);ActivateFamily();}internal MainForm MainView{get{return ordinary;}}internal WaveReducerForm WaveView{get{return wave;}}internal void SelectOrdinary(){Choose(0);ActivateFamily();}
 }
}
