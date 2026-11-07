using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace GearLab {
 public static class IndustrialRobot66 {
  // Engineering dimensions and joint axes are defined here; no imported mesh or rig.
  public static RobotDocument Create(double height=1000,double payload=.3){
   if(!MechanismCad.Finite(height)||height<900||height>1400||!MechanismCad.Finite(payload)||payload<0||payload>2)throw new Exception("Высота: 900–1400 мм; груз: 0–2 кг.");
   var d=RobotDocument.EnclosedExample();d.Name="Промышленный манипулятор · "+Engine.F(height)+" мм · параметрическая конструкция";d.SafetyFactor=2;
   double useful=height-460;for(int i=0;i<6;i++){
    var j=d.Joints[i];j.Id="industrial66_j"+(i+1);j.ParentId=i==0?null:d.Joints[i-1].Id;j.Name=new[]{"J1 · Поворот основания","J2 · Плечо","J3 · Локоть","J4 · Поворот предплечья","J5 · Наклон кисти","J6 · Фланец инструмента"}[i];
    j.Position=0;j.Minimum=i==0?-150:-90;j.Maximum=i==0?150:90;j.Speed=5;
    j.OffsetX=j.OffsetY=0;j.OffsetZ=i==0?150:0;j.EndX=i==5?65:0;j.EndY=0;j.EndZ=i==0?110:i==1?useful*.55:i==2?useful*.45:i==3?100:i==4?100:0;
    j.MotorTemplate=RobotMotorTemplate.NEMA17_шаблон;j.ApplyMotorTemplate();j.MotorLength=44;j.MotorMass=.29;j.MotorTorque=0;
    j.GearboxDiameter=76;j.GearboxLength=48;j.BearingInner=i<3?20:12;j.BearingOuter=i<3?47:28;j.BearingWidth=i<3?14:8;
    j.SkinDiameter=76;j.BodyStyle=RobotBodyStyle.Разъёмный_корпус;j.Appearance=RobotAppearance.Технический;j.Wall=6;j.Density=1270;j.Fit=.2;j.Cables=false;j.Payload=i==5?payload:0;
    j.BodyGenerator=new ProceduralBodySettings{Connected=true,ReferenceDesign=true,IndustrialDesign=true,FrameWall=4,MinWall=3.2,SkinWall=2.4,Clearance=.3,InsertLength=6,AllowableStressMPa=8,Density=1270,Variant="Reference",Style=new RobotStyleParameters{Slimness=.94,Taper=.92,Roundness=.65,AccentColorCoverage=.18,Profile="rounded_rectangle"}};
   }
   var errors=d.Validate();if(errors.Count>0)throw new Exception(string.Join("; ",errors));return d;
  }
  public static RobotDocument ForWorkspace(double height,double payload){
   var d=Create(height,payload);string root=Path.GetDirectoryName(Application.ExecutablePath);string file=Path.Combine(root,"examples","Industrial_1000mm.robot.json");if(!File.Exists(file))file=Path.Combine(Path.GetDirectoryName(root),"examples","Industrial_1000mm.robot.json");
   if(File.Exists(file)){var cached=RobotDocument.Parse(File.ReadAllText(file));var parameters=cached.Clone();foreach(var j in parameters.Joints)j.GeneratedBody=null;if(parameters.Serialize()==d.Serialize()&&cached.Joints.All(j=>j.GeneratedBody!=null&&j.GeneratedBody.Result.GeometryValid&&j.GeneratedBody.Fingerprint==ProceduralBodySpec.Fingerprint(cached,j,j.BodyGenerator)))return cached;}
   return d;
  }
  public static int Export(string folder){Directory.CreateDirectory(folder);try{var d=ForWorkspace(1000,.3);var b=RobotCad.Build(d);PrintExport.Zip(b.ForExport(true,null),Path.Combine(folder,"Industrial_PROTOTYPE_STL.zip"));return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"industrial66-export-error.txt"),ex.ToString(),Encoding.UTF8);return 1;}}
  public static int Generate(string folder){Directory.CreateDirectory(folder);try{var project=Path.Combine(folder,"Industrial_1000mm.robot.json");var d=File.Exists(project)?RobotDocument.Parse(File.ReadAllText(project)):Create();d.Save(Path.Combine(folder,"Industrial_start.robot.json"));using(var kernel=new CADKernel())foreach(var j in d.Ordered().AsEnumerable().Reverse()){
    File.AppendAllText(Path.Combine(folder,"progress.txt"),j.Name+"\r\n",Encoding.UTF8);File.WriteAllText(Path.Combine(folder,"spec-"+j.Id+".json"),CADDocument.Serializer().Serialize(ProceduralBodySpec.Build(d,j,j.BodyGenerator)),Encoding.UTF8);if(j.GeneratedBody==null||!j.GeneratedBody.Result.GeometryValid||j.GeneratedBody.Fingerprint!=ProceduralBodySpec.Fingerprint(d,j,j.BodyGenerator))j.GeneratedBody=ProceduralBodySpec.Generate(kernel,d,j,j.BodyGenerator);if(!j.GeneratedBody.Result.GeometryValid)throw new Exception(j.Name+": "+string.Join("; ",j.GeneratedBody.Result.Issues.Where(i=>i.Status=="error").Select(i=>i.Message)));d.Save(Path.Combine(folder,"Industrial_1000mm.robot.json"));
   }
   var b=RobotCad.Build(d);var a=RobotSolver.Solve(b);File.WriteAllText(Path.Combine(folder,"industrial-generation-results.json"),CADDocument.Serializer().Serialize(new{Geometry=b.Model.Valid,Errors=b.Model.Errors,Connections=RobotConnections.Validate(b),MassEstimate=a.TotalMass,Loads=a.Joints,Bounds=b.Model.Bounds(),Parts=b.Entries.Count,Native=d.Joints.Select(j=>new{j.Id,j.GeneratedBody.Result.GeometryValid,j.GeneratedBody.Result.Issues})}),Encoding.UTF8);
   if(File.Exists(Path.Combine(folder,"industrial-generation-error.txt")))File.Delete(Path.Combine(folder,"industrial-generation-error.txt"));return b.Model.Valid?0:1;
  }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"industrial-generation-error.txt"),ex.ToString(),Encoding.UTF8);return 1;}}
 }
 public sealed partial class RobotDesignerForm {
  void OpenIndustrial66(){using(var f=new Form{Text=I18n.T("Промышленный манипулятор"),Size=new Size(660,390),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false}){
   var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(18),FlowDirection=FlowDirection.TopDown,WrapContents=false};f.Controls.Add(flow);
   flow.Controls.Add(new Label{Text=I18n.T("Номинальная высота по осям, мм"),AutoSize=true});var height=new NumericUpDown{Minimum=900,Maximum=1400,Value=1000,Increment=25,Width=220};flow.Controls.Add(height);
   flow.Controls.Add(new Label{Text=I18n.T("Полезная нагрузка, г"),AutoSize=true});var payload=new NumericUpDown{Minimum=0,Maximum=2000,Value=300,Increment=50,Width=220};flow.Controls.Add(payload);
   flow.Controls.Add(new Label{Text=I18n.T("Предварительный проект корпусов и опор. NEMA17, PETG, P1S. Передачу и прочность ещё нужно согласовать; рабочий момент мотора не задан."),Width=590,Height=95});
   var b=new Button{Text=I18n.T("Создать параметрический робот"),Width=300,Height=36};flow.Controls.Add(b);b.Click+=(s,e)=>{try{Replace(IndustrialRobot66.ForWorkspace((double)height.Value,(double)payload.Value/1000),null);SetStage(2);f.DialogResult=DialogResult.OK;}catch(Exception ex){UiMessage.Show(f,ex.Message);}};f.ShowLocalizedDialog(this);
  }}
 }
}
