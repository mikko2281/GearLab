using System;using System.Threading.Tasks;using System.Drawing;using System.Windows.Forms;

namespace GearLab {

 public sealed class ReducerWorkspaceForm:Form,IWorkspaceDocument {

  readonly ComboBox family=new ComboBox();readonly Panel surface=new Panel();MainForm ordinary;WaveReducerForm wave;Form active;

  public ReducerWorkspaceForm(){Text="Редукторы";Font=new Font("Segoe UI",10);Size=new Size(1500,980);var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=43,Padding=new Padding(6)};bar.Controls.Add(new Label{Text="Тип редуктора",AutoSize=true,Padding=new Padding(0,5,8,0)});family.DropDownStyle=ComboBoxStyle.DropDownList;family.Width=340;family.Items.AddRange(new[]{"Циклоидальный","Зубчатая пара","Зубчатый ремень","Планетарный","Волновой"});bar.Controls.Add(family);surface.Dock=DockStyle.Fill;Controls.Add(surface);Controls.Add(bar);family.SelectedIndexChanged+=(s,e)=>ActivateFamily();Shown+=(s,e)=>{if(family.SelectedIndex<0)family.SelectedIndex=0;else ActivateFamily();};}

  public void ConfigureTarget(CADKernel kernel,Func<CADFeature,Task<bool>> insert,Action done){SelectWave();if(wave==null){wave=new WaveReducerForm();Embed(wave);}wave.ConfigureTarget(kernel,insert,done);}

  public void SelectWave(){family.SelectedIndex=4;if(Visible)ActivateFamily();}

  void ActivateFamily(){if(!Visible)return;if(ordinary==null){ordinary=new MainForm();ordinary.ConfigureHostedKind();Embed(ordinary);}ordinary.SetHostedKind(Math.Max(0,family.SelectedIndex));active=ordinary;active.Show();active.BringToFront();active.PerformLayout();((IWorkspaceDocument)ordinary).WorkspaceActivate(true);Text="Редукторы · "+family.Text;}
  void Embed(Form form){form.TopLevel=false;form.FormBorderStyle=FormBorderStyle.None;form.MinimumSize=Size.Empty;form.Dock=DockStyle.Fill;surface.Controls.Add(form);}
  public bool WorkspaceCanClose(){return ordinary==null||((IWorkspaceDocument)ordinary).WorkspaceCanClose();}
  public void WorkspaceApproveClose(){if(ordinary!=null)((IWorkspaceDocument)ordinary).WorkspaceApproveClose();}
  public void WorkspaceActivate(bool value){if(ordinary!=null)((IWorkspaceDocument)ordinary).WorkspaceActivate(value);}
  internal void TestSelect(int index){family.SelectedIndex=index;}internal MainForm MainView{get{return ordinary;}}internal WaveReducerForm WaveView{get{return wave;}}internal void SelectOrdinary(){family.SelectedIndex=0;}
 }
}
