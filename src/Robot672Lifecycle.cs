using System;using System.IO;using System.Text;using System.Threading.Tasks;
namespace GearLab {
 public sealed partial class RobotDesignerForm {
  internal static Action<string> CloseTrace;
  int recoveryRevision=-1;string recoveredDocument;
  internal int RecoverySerializationCount,RecoveryWriteCount;
  internal static RobotDocument LoadInitialRobotDocument(string file,Func<RobotDocument> createDefault){return file==null?createDefault():RobotDocument.Parse(File.ReadAllText(file));}
  void InitializeRobotRecoveryCheckpoint(){recoveryRevision=revision;recoveredDocument=saved;}
  internal bool TryRecoverRobot(){
   if(disposed||!AppSettings.Current.AutoSave||recoveryRevision==revision)return false;
   RecoverySerializationCount++;string snapshot=Document.Serialize();
   if(snapshot==recoveredDocument||(snapshot==saved&&RecoveryWriteCount==0)){recoveryRevision=revision;recoveredDocument=snapshot;return false;}
   AppSettings.Recovery("robot",snapshot,recoveryKey);RecoveryWriteCount++;recoveryRevision=revision;recoveredDocument=snapshot;return true;
  }
  bool exportBusy,stlExportBusy;
  internal bool RobotExportBusy{get{return exportBusy||stlExportBusy;}}
  internal CadAssembly CaptureRobotExportSnapshot(bool printable,string id){CapturePose();if(Build==null||!Build.Model.Valid)throw new InvalidOperationException("Исправьте CAD-модель.");return Build.ForExport(printable,id);}
  internal async Task WriteRobotExportAsync(CadAssembly snapshot,string file,bool stl,Action<CadAssembly,string> writer){
   if(RobotExportBusy)throw new InvalidOperationException("Дождитесь завершения экспорта.");
   exportBusy=true;stlExportBusy=stl;
   try{summary.Text=I18n.T(stl?"Подготовка точных STL в фоне…":"Подготовка STEP в фоне…");await Task.Run(()=>writer(snapshot,file));if(!disposed)notes.AppendText("\r\n"+I18n.T(stl?"STL сохранены: ":"Экспорт сохранён: ")+file);}
   finally{exportBusy=stlExportBusy=false;}
  }
  internal static void WriteRobotStepSnapshot(CadAssembly snapshot,string path){
   string full=Path.GetFullPath(path),folder=Path.GetDirectoryName(full);if(!Directory.Exists(folder))throw new DirectoryNotFoundException(folder);
   string staging=Path.Combine(folder,".gearlab-step-"+Guid.NewGuid().ToString("N")+".step");
   try{StepExport.Write(snapshot,staging);if(File.Exists(full))File.Replace(staging,full,null);else File.Move(staging,full);}
   finally{if(File.Exists(staging))File.Delete(staging);}
  }
 }
}
