using System;using System.Linq;using System.Windows.Forms;

namespace GearLab {

 public sealed partial class RobotDesignerForm {

  void EditInCAD(){var entry=Entry;if(entry==null||entry.Guide){WorkspaceHost.Navigate(this,"cad");return;}try{var edit=Document.BodyEdits.FirstOrDefault(e=>e.PartId==entry.Id);CADDocument source;if(edit!=null&&edit.NativeProject!=null){source=CADDocument.Parse(edit.NativeProject);if(edit.NativeShift!=null)CADNativeBridge.AppendMove(source,RobotMath.Translation(edit.NativeShift[0],edit.NativeShift[1],edit.NativeShift[2]));}else source=CADNativeBridge.FromLegacyPart(entry.Part);string jointId=entry.JointId,partId=entry.Id;var shape=entry.Shape==null?null:entry.Shape.CloneShape();Func<CADNative,bool> callback=shape!=null&&entry.Category=="Корпус"?(Func<CADNative,bool>)(native=>Mutate(()=>{if(Document.Find(jointId)==null)throw new Exception("Звено удалено. Откройте редактор для существующей детали.");var target=Document.BodyEdits.FirstOrDefault(e=>e.PartId==partId);if(target==null){target=new RobotBodyEdit{PartId=partId};Document.BodyEdits.Add(target);}target.Shape=shape;target.Mesh=null;target.NativeProject=native.Project;target.NativeStep=native.Step;target.NativePreview=native.Preview;target.NativeVolume=native.Volume;target.NativeCentre=native.Center;target.NativeShift=new double[3];})):null;new CADStudioForm(source,callback).ShowWorkspace(this);}catch(Exception ex){UiMessage.Show(this,ex.Message,"Открытие CAD-детали");}}

  bool NativeEditOnly(){if(Entry==null||Entry.Part.NativeStep==null)return false;UiMessage.Show(this,"Эта деталь содержит операции CAD Studio. Откройте «CAD Studio: деталь», чтобы изменить эскизы, отверстия, фаски или скругления.");return true;}

 }

}

