using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace GearLab {
 // 6.10.0: проверки нового вида материалов и вкладок «Материалы / Нагрузки / Перегрузка».
 // Запуск: GearLab.exe --ui610-test <папка>. Пишет ui610-results.txt (или ui610-error.txt), ui610-progress.txt
 // и снимок схемы вала ui610-shaft.png. 3D-вид здесь не переносится между вкладками,
 // поэтому набор работает и там, где WPF-3D недоступен; перенос 3D проверяет --overload-ui-test.
 public static class Ui610Tests {
  public static int Run(string folder){folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);var report=new StringBuilder();int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;report.AppendLine("PASS "+label);File.WriteAllText(Path.Combine(folder,"ui610-progress.txt"),report.ToString(),Encoding.UTF8);};
   try{
    // 1. Внешний вид материалов: у каждого встроенного — свой узнаваемый вид, переопределения работают.
    var defaults=MaterialLibrary.Defaults();var looks=defaults.Select(m=>MaterialLook.For(m)).ToList();check(looks.All(l=>Array.IndexOf(MaterialLook.Finishes,l.Finish)>=0),"every default material has a known finish");check(looks.Select(l=>MaterialLook.Hex(l.Color)+l.Finish).Distinct().Count()==defaults.Count,"default materials look different from each other");
    check(MaterialLook.For(defaults.First(m=>m.Name=="PETG")).Finish=="Gloss"&&MaterialLook.For(defaults.First(m=>m.Name=="PLA")).Finish=="Matte"&&MaterialLook.For(defaults.First(m=>m.Name=="Сталь 45")).Finish=="Metal","PETG glossy, PLA matte, steel metallic");
    var custom=MaterialLibrary.Clone(defaults[0]);custom.Color="#112233";custom.Finish="Matte";var cl=MaterialLook.For(custom);check(MaterialLook.Hex(cl.Color)=="#112233"&&cl.Finish=="Matte","user colour and finish override the typical look");
    bool rejected=false;custom.Color="red";try{MaterialLibrary.Validate(custom);}catch{rejected=true;}check(rejected,"invalid colour rejected");custom.Color=null;custom.Finish="Chrome";rejected=false;try{MaterialLibrary.Validate(custom);}catch{rejected=true;}check(rejected,"unknown finish rejected");custom.Finish=null;MaterialLibrary.Validate(custom);check(true,"empty look fields are valid");
    var fresh=MaterialLook.For(new TrainMaterial{Name="Мой пластик",Kind="FDM",Density=1200,LayerFactor=.5,Source="x"});check(fresh!=null&&fresh.Finish=="Satin","unknown FDM material gets a neutral satin look");
    var json=CADDocument.Serializer().Serialize(new TrainMaterial{Name="A",Kind="Metal",Density=7800,LayerFactor=1,Source="s",Color="#ABCDEF",Finish="Metal"});var back=CADDocument.Serializer().Deserialize<TrainMaterial>(json);check(back.Color=="#ABCDEF"&&back.Finish=="Metal","look fields survive save/load");var old=CADDocument.Serializer().Deserialize<TrainMaterial>("{\"Name\":\"B\",\"Kind\":\"FDM\",\"Density\":1200,\"LayerFactor\":0.5,\"Source\":\"s\"}");check(old.Color==null&&old.Finish==null&&MaterialLook.For(old)!=null,"older material files load with the typical look");
    // 2. Назначение материалов окрашивает детали; старые проекты без материалов выглядят как раньше.
    var legacy=TrainSolver.Solve(CompoundReducer.Example());check(legacy.Valid&&legacy.Scene.Model.Parts.All(p=>p.MaterialColor==null),"projects without materials keep the old colours");
    var t=CompoundReducer.Example();MaterialLibrary.ApplyDefaults(t);var s=TrainSolver.Solve(t);var housing=s.Scene.Model.Parts.First(p=>!p.Reference&&p.Role=="housing");var shaft=s.Scene.Model.Parts.First(p=>!p.Reference&&MaterialLibrary.Role(p.Role)=="shaft");check(housing.MaterialColor==MaterialLook.Hex(MaterialLook.Typical("PETG","FDM").Color)&&housing.MaterialFinish=="Gloss","PETG housing carries the PETG look");check(shaft.MaterialFinish=="Metal","steel shaft carries the metal look");
    t.Materials.Roles["housing"]="PLA";s=TrainSolver.Solve(t);housing=s.Scene.Model.Parts.First(p=>!p.Reference&&p.Role=="housing");check(housing.MaterialName=="PLA"&&housing.MaterialFinish=="Matte","switching the housing to PLA changes its look");
    // 3. Форма: поля пишут в проект, история отмены работает, перегрузка видна.
    Application.EnableVisualStyles();var example=CompoundReducer.Example();MaterialLibrary.ApplyDefaults(example);example.OutputLoad=new TrainOutputLoad{RadialForce=25,ForceOffset=30};example.Overload=new TrainOverloadSettings{Enabled=true};
    using(var form=new TrainDesignerForm(example)){form.ShowInTaskbar=false;form.Opacity=0;form.Show();Application.DoEvents();form.Ui610Verify(folder,check);form.WorkspaceApproveClose();form.Close();}
    File.WriteAllText(Path.Combine(folder,"ui610-results.txt"),report+"PASS "+checks+" assertions",Encoding.UTF8);return 0;
   }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"ui610-error.txt"),report+ex.ToString(),Encoding.UTF8);return 1;}}
 }
 public sealed partial class TrainDesignerForm {
  internal void Ui610Verify(string folder,Action<bool,string> check){Calculate();check(solution!=null&&solution.Valid&&solution.Strength!=null,"materials example calculates");
   check(((Pick)roleChoices["housing"].Box.SelectedItem).Key=="PETG"&&((Pick)roleChoices["shaft"].Box.SelectedItem).Key=="Сталь 45","role choices show the assigned materials");check(roleSwatches["housing"].Look!=null&&roleSwatches["housing"].Look.Finish=="Gloss","housing swatch shows the PETG look");
   check(!string.IsNullOrEmpty(materialSource.Text)&&materialNumbers["Density"].Value==1270,"material card shows PETG properties and source");
   foreach(Pick p in roleChoices["housing"].Box.Items)if(p.Key=="PLA")roleChoices["housing"].Box.SelectedItem=p;typeof(ChoiceField).GetMethod("RaiseChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(roleChoices["housing"],null);Calculate();check(document.Materials.Roles["housing"]=="PLA"&&solution.Scene.Model.Parts.First(p=>!p.Reference&&p.Role=="housing").MaterialName=="PLA","choosing PLA for the housing updates the project and the model");
   History(false);check(document.Materials.Roles["housing"]=="PETG","undo restores the previous material");
   editedMaterial="PETG";ShowMaterial(MaterialLibrary.Resolve(document).First(m=>m.Name=="PETG"));foreach(Pick p in materialFinish.Box.Items)if(p.Key=="Matte")materialFinish.Box.SelectedItem=p;CommitMaterial();Calculate();check(document.Materials.Custom.Any(m=>m.Name=="PETG"&&m.Finish=="Matte")&&solution.Scene.Model.Parts.First(p=>!p.Reference&&p.Role=="housing").MaterialFinish=="Matte","editing the finish restyles parts made of that material");
   materialNumbers["Density"].Box.Text="-5";string before=Snapshot();CommitMaterial();check(Snapshot()==before,"invalid density is not applied");materialNumbers["Density"].Value=1270;
   loadFields["RadialForce"].Box.Text="40";CommitLoad();Calculate();check(document.OutputLoad.RadialForce==40&&solution.Strength.Shaft.Reaction2>0,"radial force field updates the shaft load");
   var arrows=LoadArrows(true);check(arrows!=null&&arrows.Count>=2&&arrows.Any(a=>a.Torque),"load arrows are built for force and torque");
   overloadTorque.Value=1200;ApplyOverload();check(overloadHeadline.Text.Contains(I18n.T("Сломается"))&&overloadVisual.Failed,"120 % of the limit reports a failure");overloadTorque.Value=600;ApplyOverload();check(!overloadVisual.Failed&&overloadHeadline.Text.Contains(I18n.T("Держит")),"60 % of the limit holds");
   check(modeChart.Items.Count>0&&modeChart.Items[0].Limit==solution.Strength.LimitTorque,"weakest mode is first in the chart");
   using(var bmp=new Bitmap(760,420))using(var panel=new ShaftLoadPanel{Size=new Size(760,420),Solution=solution}){AppSettings.Apply(panel);panel.DrawToBitmap(bmp,panel.ClientRectangle);bmp.Save(Path.Combine(folder,"ui610-shaft.png"));}
   check(File.Exists(Path.Combine(folder,"ui610-shaft.png")),"shaft diagram snapshot written");overloadTorque.Value=0;}
 }
}
