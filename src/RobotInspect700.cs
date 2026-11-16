using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;using System.Windows.Media.Media3D;
namespace GearLab {
 // 7.0.0: режим «Просмотр» и экономная передача поз в Three.js-сцену.
 public sealed class InspectJoint {public string id{get;set;}public string label{get;set;}public string[] parts{get;set;}public double[] axis{get;set;}}
 public sealed class InspectMessage {public string type{get{return "inspect";}}public bool enabled{get;set;}public InspectJoint[] joints{get;set;}}
 public sealed partial class RobotDesignerForm {
  bool inspectMode;ToolStripButton inspectButton;
  internal bool InspectMode{get{return inspectMode;}}
  void InitializeInspect(ToolStrip top){inspectButton=new ToolStripButton("◎ Просмотр"){Padding=new Padding(6),CheckOnClick=false,ToolTipText="Наведите курсор на звено — оно разлетится на детали, чтобы было видно, что внутри. Уберите курсор — звено соберётся."};inspectButton.Click+=(s,e)=>ShowInspect(!inspectMode);top.Items.Add(inspectButton);}
  internal void ShowInspect(bool on){if(on&&move!=null&&move.Checked)move.Checked=false;inspectMode=on;if(inspectButton!=null)inspectButton.Checked=on;if(on&&!robotWeb.Ready)summary.Text=I18n.T("Режим «Просмотр» работает в Three.js-сцене (нужен Microsoft Edge WebView2).");SendInspect();}
  // Детали каждого звена: id деталей, подпись и ось шарнира. Отправляется только при изменении.
  internal InspectMessage InspectPayload(){if(!inspectMode||Build==null)return new InspectMessage{enabled=false};var poses=RobotKinematics.Evaluate(Document,0,false);var ordered=Document.Ordered();var index=new Dictionary<string,int>();for(int i=0;i<ordered.Count;i++)index[ordered[i].Id]=i;return new InspectMessage{enabled=true,joints=ordered.Select(j=>new InspectJoint{id=j.Id,label=(index[j.Id]+1)+" · "+j.Name,parts=Build.Entries.Values.Where(e=>e.JointId==j.Id&&!e.Guide).Select(e=>e.Id).ToArray(),axis=poses.ContainsKey(j.Id)?new[]{poses[j.Id].Axis.X,poses[j.Id].Axis.Y,poses[j.Id].Axis.Z}:new[]{0.0,0,1}}).Where(x=>x.parts.Length>0).ToArray()};}
  void SendInspect(){if(robotWeb.Ready)SendIfChanged("inspect",InspectPayload());}
  // Повторное одинаковое сообщение не отправляется: сцена не пересоздаёт кости и значки без нужды.
  readonly Dictionary<string,string> sentMessages=new Dictionary<string,string>();int sentUploads=-1;readonly Dictionary<string,double[]> sentPoses=new Dictionary<string,double[]>();
  internal int WebMessagesSkipped,WebPosesSent;
  void ResetWebCacheIfReloaded(){if(robotWeb.PartUploadCount!=sentUploads||robotWeb.ClearCount!=sentClears){sentUploads=robotWeb.PartUploadCount;sentClears=robotWeb.ClearCount;sentMessages.Clear();sentPoses.Clear();}}int sentClears=-1;
  void SendIfChanged(string key,object message){ResetWebCacheIfReloaded();string json=CADDocument.Serializer().Serialize(message);string old;if(sentMessages.TryGetValue(key,out old)&&old==json){WebMessagesSkipped++;return;}sentMessages[key]=json;robotWeb.Send(message);}
  // Только матрицы деталей, которые изменились с прошлой отправки (обычно двигаются лишь потомки одного шарнира).
  internal static List<object> PoseDelta(RobotBuild build,double phase,bool animate,Dictionary<string,double[]> sent){var list=new List<object>();foreach(var e in build.Entries.Values){if(e.Guide)continue;var m=RobotWebViewport.Matrix(build.Pose(e.Id,phase,animate));double[] old;if(sent.TryGetValue(e.Id,out old)&&Same(old,m))continue;sent[e.Id]=m;list.Add(new{id=e.Id,matrix=m});}return list;}
  static bool Same(double[] a,double[] b){for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
  void SendPoses(bool animate){ResetWebCacheIfReloaded();var delta=PoseDelta(Build,phase,animate,sentPoses);if(delta.Count==0){WebMessagesSkipped++;return;}WebPosesSent+=delta.Count;robotWeb.Send(new{type="poses",parts=delta.ToArray()});}
 }
}
