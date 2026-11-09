using System;using System.Linq;using System.Collections.Generic;
namespace GearLab {
 public sealed class RobotPoseFrame672 {
  public string[] Ids{get;set;}public double[] Positions{get;set;}public double Opening{get;set;}
  public static RobotPoseFrame672 Capture(RobotDocument d){return new RobotPoseFrame672{Ids=d.Joints.Select(j=>j.Id).ToArray(),Positions=d.Joints.Select(j=>j.Position).ToArray(),Opening=d.GripperOpening};}
  public bool Same(RobotPoseFrame672 p){return p!=null&&Opening==p.Opening&&Ids.SequenceEqual(p.Ids)&&Positions.SequenceEqual(p.Positions);}
  public bool ValidFor(RobotDocument d){return Ids!=null&&Positions!=null&&Ids.Length==d.Joints.Count&&Positions.Length==Ids.Length&&MechanismCad.Finite(Opening)&&Opening>=0&&Opening<=40&&Ids.SequenceEqual(d.Joints.Select(j=>j.Id))&&Positions.Where((p,i)=>!MechanismCad.Finite(p)||p<d.Joints[i].Minimum||p>d.Joints[i].Maximum).Count()==0;}
 }
 public sealed partial class RobotDesignerForm {
  const string PoseHistoryPrefix="@gearlab-pose-v1:";string poseSavedSource,poseStructuralSource;RobotPoseFrame672 savedPoseFrame;bool structureMatchesSaved;
  internal long HistoryBytes{get{return undo.Concat(redo).Sum(s=>(long)s.Length*2);}}
  internal int UndoCount{get{return undo.Count;}}
  string CapturePoseHistory(){return PoseHistoryPrefix+CADDocument.Serializer().Serialize(RobotPoseFrame672.Capture(Document));}
  void PushRobotHistory(Stack<string> stack,string value){stack.Push(value);var newest=stack.ToArray();long bytes=0;int keep=0;foreach(var s in newest){if(keep>0&&(keep>=128||bytes+(long)s.Length*2>64L*1024*1024))break;bytes+=(long)s.Length*2;keep++;}if(keep==newest.Length)return;stack.Clear();for(int i=keep-1;i>=0;i--)stack.Push(newest[i]);}
  void EnsurePoseSaveBaseline(){bool changed=!ReferenceEquals(poseSavedSource,saved);if(changed){poseSavedSource=saved;savedPoseFrame=last==saved?RobotPoseFrame672.Capture(Document):RobotPoseFrame672.Capture(RobotDocument.Parse(saved));}if(!changed&&ReferenceEquals(poseStructuralSource,last))return;poseStructuralSource=last;structureMatchesSaved=last==saved;if(!structureMatchesSaved&&savedPoseFrame.ValidFor(Document)){var current=RobotPoseFrame672.Capture(Document);try{Document.GripperOpening=savedPoseFrame.Opening;for(int i=0;i<Document.Joints.Count;i++)Document.Joints[i].Position=savedPoseFrame.Positions[i];structureMatchesSaved=Document.Serialize()==saved;}finally{Document.GripperOpening=current.Opening;for(int i=0;i<Document.Joints.Count;i++)Document.Joints[i].Position=current.Positions[i];}}}
  void UpdatePoseTitle(){bool dirty=!structureMatchesSaved||savedPoseFrame==null||!savedPoseFrame.Same(RobotPoseFrame672.Capture(Document));Text="GearLab 6.7.2 · "+Document.Name+(dirty?" *":"");}
  void RestoreRobotHistory(Stack<string> source,Stack<string> destination){if(source.Count==0)return;Stop();string value=source.Peek();if(value.StartsWith(PoseHistoryPrefix,StringComparison.Ordinal)){var p=CADDocument.Serializer().Deserialize<RobotPoseFrame672>(value.Substring(PoseHistoryPrefix.Length));if(!p.ValidFor(Document))throw new InvalidOperationException("История положения не соответствует текущему роботу.");EnsurePoseSaveBaseline();string before=CapturePoseHistory();double delta=(p.Opening-Document.GripperOpening)/2;Document.GripperOpening=p.Opening;for(int i=0;i<p.Positions.Length;i++)Document.Joints[i].Position=p.Positions[i];foreach(var e in Build.Entries.Values.Where(e=>e.GripperSide!=0))e.Shape.X+=e.GripperSide*delta;source.Pop();PushRobotHistory(destination,before);RefreshManualPose();UpdatePoseTitle();}else{var target=RobotDocument.Parse(value);string before=Document.Serialize();source.Pop();PushRobotHistory(destination,before);Document=target;Rebuild();}}
 }
}
