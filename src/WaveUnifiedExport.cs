using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows.Forms;
namespace GearLab {
 public static class WaveProjectMigration {
  public static bool IsWaveFile(string path){return path.EndsWith(".wave.json",StringComparison.OrdinalIgnoreCase);}
  public static Parameters Read(string path){if(new FileInfo(path).Length>65536)throw new Exception("Проект волнового редуктора слишком большой.");var w=CADDocument.Serializer().Deserialize<WaveParameters>(File.ReadAllText(path));w.Validate();if(w.Rolling)return w.RollingParameters();var p=Engine.Starter(4);p.WaveScheme=0;p.Ratio=w.Ratio;p.Module=w.Module;p.Thickness=w.Thickness;p.WaveWall=w.Wall;p.Wall=w.RingWall;p.Shaft=w.Bore;p.OutputShaft=w.OutputBore;p.Clearance=w.Clearance;p.WaveBearingWall=w.BearingWall;p.InputTorque=w.InputTorque;p.InputRpm=w.InputRpm;p.Efficiency=w.Efficiency;p.LoadTorque=w.LoadTorque;p.Overall=0;p.AutoFit=false;return WaveFastModel.Upgrade(p);}
 }
 public static class WaveExactExport {
  public static void Write(Parameters p,string path,CADKernel kernel,IEnumerable<string> ids=null){var rr=Engine.Calculate(p);var model=CadBuilder.Build(rr);var filtered=ids==null?ReducerExportSelection.ForScope(model,p.ExportScope):new CadAssembly();if(ids!=null)filtered.Parts.AddRange(model.Parts.Where(q=>ids.Contains(q.Id)));TransmissionExport.Write(filtered,path,kernel);
  }
 }
 public partial class MainForm {
  static int WaveQuickRatio(int key){return key==8?8:key==9?12:key==20?17:key==30?20:30;}
  readonly CADKernel waveExportKernel=new CADKernel();bool waveExportBusy;Button waveCancel;
  async void ExportWaveStep(IEnumerable<string> selected=null,string chosenPath=null){if(waveExportBusy||!ReadControls())return;Recalculate();if(!result.Valid||!result.Model.Valid)return;string path=chosenPath??dialogs.Save(this,"step","Редуктор_Волновой_"+Engine.N(p.Ratio)+"к1.step");if(path==null)return;var snapshot=Engine.Clone(p);waveExportBusy=true;canvas.ShowMotion=false;string temporary=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),".gearlab-wave-"+Guid.NewGuid().ToString("N")+".step");try{if(waveCancel==null){waveCancel=new Button{Text="Отменить STEP",Width=130,Height=32};waveCancel.Click+=(s,e)=>waveExportKernel.Cancel();step.Parent.Controls.Add(waveCancel);}waveCancel.Visible=true;footer.Text="Экспорт точного STEP… Предпросмотр и камера остаются доступны.";await Task.Factory.StartNew(()=>WaveExactExport.Write(snapshot,temporary,waveExportKernel,selected));if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);if(!IsDisposed)footer.Text="STEP сохранён: "+path;}catch(Exception ex){if(!IsDisposed)footer.Text="STEP не сохранён: "+ex.GetBaseException().Message;}finally{if(File.Exists(temporary))File.Delete(temporary);waveExportBusy=false;if(!IsDisposed&&waveCancel!=null)waveCancel.Visible=false;}}
  void InitializeWaveExport(){FormClosing+=(s,e)=>{if(waveExportBusy){footer.Text="Дождитесь STEP-экспорта или нажмите «Отменить STEP».";e.Cancel=true;}};FormClosed+=(s,e)=>waveExportKernel.Dispose();}
 }
}
