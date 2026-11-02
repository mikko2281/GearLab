using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace GearLab {
 public sealed partial class RobotDesignerForm {
  sealed class JointControllerRow {public string Id;public NumericUpDown Value;public TrackBar Slider;public Label Load;}
  readonly Dictionary<string,JointControllerRow> jointControllers=new Dictionary<string,JointControllerRow>();
  bool syncingControllers;NumericUpDown controllerStep;CheckBox controllerHandles;bool handlesEnabled=true;
  internal void OpenJointControllers(){EndPoseEdit();Stop();SetStage(3);if(move!=null){move.Checked=false;Scene.SetMoveMode(false);robotWeb.Send(new{type="move",enabled=false});}UpdateWebPose();}
  void BuildJointControllers(FlowLayoutPanel flow){
   jointControllers.Clear();flow.Controls.Add(QuickLabel("Управляйте шарнирами, не меняя размеры деталей. Дочерние звенья следуют за выбранным суставом.",62));
   controllerHandles=new CheckBox{Text="Кольцо / стрелка шарнира в 3D",Checked=handlesEnabled,Width=285,Height=27};controllerHandles.CheckedChanged+=(s,e)=>{handlesEnabled=controllerHandles.Checked;UpdateWebPose();};flow.Controls.Add(controllerHandles);
   var stepRow=new FlowLayoutPanel{Width=285,Height=32};stepRow.Controls.Add(new Label{Text="Шаг, ° / мм",Width=105,Height=26,TextAlign=ContentAlignment.MiddleLeft});
   controllerStep=new NumericUpDown{Minimum=.01m,Maximum=30,DecimalPlaces=2,Value=1,Increment=.1m,Width=105,AccessibleName="Шаг управления шарнирами"};stepRow.Controls.Add(controllerStep);flow.Controls.Add(stepRow);
   foreach(var j in Document.Ordered()){
    string id=j.Id;var group=new Panel{Width=285,Height=117,Margin=new Padding(0,4,0,3),BorderStyle=BorderStyle.FixedSingle};
    var choose=new Button{Text=j.Name+" · "+(j.Linear?"мм":"°"),Left=3,Top=2,Width=273,Height=25,TextAlign=ContentAlignment.MiddleLeft,AccessibleName="Выбрать шарнир "+j.Name};choose.Click+=(s,e)=>{EndPoseEdit();Select(id,null);UpdateWebPose();};group.Controls.Add(choose);
    var numeric=new NumericUpDown{Left=72,Top=31,Width=125,Height=26,Minimum=(decimal)j.Minimum,Maximum=(decimal)j.Maximum,DecimalPlaces=2,Increment=1,Value=(decimal)j.Position,AccessibleName="Положение "+j.Name};
    var minus=new Button{Text="−",Left=4,Top=30,Width=60,Height=27,AccessibleName="Уменьшить "+j.Name};var plus=new Button{Text="+",Left=204,Top=30,Width=69,Height=27,AccessibleName="Увеличить "+j.Name};
    var slider=new TrackBar{Left=0,Top=59,Width=275,Height=29,AutoSize=false,TickStyle=TickStyle.None,Minimum=(int)Math.Floor(j.Minimum*100),Maximum=(int)Math.Ceiling(j.Maximum*100),Value=(int)Math.Round(j.Position*100),AccessibleName="Ползунок "+j.Name};
    var state=new Label{Left=4,Top=91,Width=270,Height=20,Font=new Font(Font.FontFamily,8),ForeColor=Color.SlateGray};
    group.Controls.AddRange(new Control[]{minus,numeric,plus,slider,state});flow.Controls.Add(group);var row=new JointControllerRow{Id=id,Value=numeric,Slider=slider,Load=state};jointControllers[id]=row;
    bool fixedJoint=j.Mode==RobotJointMode.Неподвижный;minus.Enabled=plus.Enabled=numeric.Enabled=slider.Enabled=!fixedJoint;
    numeric.Enter+=(s,e)=>{if(!syncingControllers)BeginPoseEdit();};numeric.Leave+=(s,e)=>EndPoseEdit();numeric.ValueChanged+=(s,e)=>{if(!syncingControllers)UpdateJointController(id,(double)numeric.Value,false);};
    slider.MouseDown+=(s,e)=>BeginPoseEdit();slider.MouseUp+=(s,e)=>EndPoseEdit();slider.KeyDown+=(s,e)=>BeginPoseEdit();slider.KeyUp+=(s,e)=>EndPoseEdit();slider.LostFocus+=(s,e)=>EndPoseEdit();slider.ValueChanged+=(s,e)=>{if(!syncingControllers)UpdateJointController(id,slider.Value/100.0,false);};
    minus.Click+=(s,e)=>JogJoint(id,-(double)controllerStep.Value);plus.Click+=(s,e)=>JogJoint(id,(double)controllerStep.Value);
   }
   controllerStep.ValueChanged+=(s,e)=>{foreach(var row in jointControllers.Values)row.Value.Increment=controllerStep.Value;};
   if(Document.ReferenceGripper)QuickButton(flow,"Захват · сжать / раскрыть",ToggleGripperOpening);QuickButton(flow,"Все шарниры → 0",ResetJointPose);QuickButton(flow,"▶ Анимация / пауза",()=>{EndPoseEdit();TogglePlay();UpdateWebPose();});
   QuickButton(flow,"Нагрузки · 48 поз",CheckRange);QuickButton(flow,"CAD-столкновения · диапазон",CheckCADMotion);QuickButton(flow,"Показать столкновение",ShowMotionCollision);
   flow.Controls.Add(QuickLabel("Кольцо вращает выбранный сустав. Для линейной оси появляется стрелка. Esc — отменить перетаскивание. Это управление моделью на экране.",94));SyncJointControllers();
  }
  internal void UpdateJointController(string id,double value,bool commit){if(!MechanismCad.Finite(value))throw new Exception("Некорректное положение шарнира.");var j=Document.Find(id);if(j==null||j.Mode==RobotJointMode.Неподвижный)return;BeginPoseEdit();if(selectedJoint!=id){selectedJoint=id;selectedPart=null;bool previous=updating;updating=true;try{foreach(TreeNode node in tree.Nodes){var match=FindNode(node,"j|"+id);if(match!=null){tree.SelectedNode=match;break;}}detail.Text=j.Name;properties.SelectedObject=j;Scene.Select(null);robotWeb.Select(id+"_end_guide");}finally{updating=previous;}}SetJointPose(id,value);if(commit)EndPoseEdit();}
  internal void JogJoint(string id,double step){var j=Document.Find(id);if(j==null)return;BeginPoseEdit();UpdateJointController(id,Document.Find(id).Position+step,true);}
  internal void ResetJointPose(){BeginPoseEdit();foreach(var j in Document.Joints.Where(j=>j.Mode!=RobotJointMode.Неподвижный))j.Position=Math.Max(j.Minimum,Math.Min(j.Maximum,0));RefreshManualPose();EndPoseEdit();}
  void RefreshManualPose(){if(Build==null)return;phase=0;Build.InvalidatePose();Build.Current=RobotKinematics.Evaluate(Document,0,false);Scene.UpdatePose(0);RefreshAnimatedLoads(0);UpdateWebPose();RefreshBoneOverlay();revision++;Text="GearLab 5.4 · "+Document.Name+" *";}
  void SyncJointControllers(){if(syncingControllers||jointControllers.Count==0||Build==null)return;syncingControllers=true;try{var poses=RobotKinematics.Evaluate(Document,phase,playing||Math.Abs(phase)>1e-9);foreach(var pair in jointControllers){var row=pair.Value;var j=Document.Find(row.Id);if(j==null||row.Value.IsDisposed)continue;double value=poses[j.Id].Value;row.Value.Value=Math.Max(row.Value.Minimum,Math.Min(row.Value.Maximum,(decimal)value));row.Slider.Value=Math.Max(row.Slider.Minimum,Math.Min(row.Slider.Maximum,(int)Math.Round(value*100)));var load=Analysis==null?null:Analysis.Joints.FirstOrDefault(x=>x.Id==j.Id);row.Load.Text=Engine.F(j.Minimum)+" … "+Engine.F(j.Maximum)+(load==null?"":" · "+Engine.F(load.Required)+" / "+Engine.F(load.Available)+" "+load.Unit);row.Load.ForeColor=load!=null&&!load.Enough?Color.Firebrick:Color.SlateGray;}}finally{syncingControllers=false;}}
  void SendJointController(){if(!robotWeb.Ready||Build==null)return;var poses=RobotKinematics.Evaluate(Document,phase,playing||Math.Abs(phase)>1e-9);var j=Joint;var p=j==null?null:poses[j.Id];robotWeb.Send(new{type="jointController",enabled=stage==3&&handlesEnabled&&(move==null||!move.Checked)&&!playing&&!bodyBusy&&j!=null&&j.Mode!=RobotJointMode.Неподвижный,id=j==null?null:j.Id,label=j==null?null:j.Name,linear=j!=null&&j.Linear,value=p==null?0:p.Value,minimum=j==null?0:j.Minimum,maximum=j==null?0:j.Maximum,origin=p==null?new double[3]:new[]{p.Origin.X,p.Origin.Y,p.Origin.Z},axis=p==null?new[]{0.0,0.0,1.0}:new[]{p.Axis.X,p.Axis.Y,p.Axis.Z},step=controllerStep==null||controllerStep.IsDisposed?1:(double)controllerStep.Value});}
  internal void HandleJointControlMessage(Dictionary<string,object> data){string type=Convert.ToString(data["type"]),id=Convert.ToString(data["id"]);var j=Document.Find(id);if(stage!=3||j==null||j.Id!=selectedJoint||j.Mode==RobotJointMode.Неподвижный||bodyBusy)return;
   if(type=="jointBegin"){BeginPoseEdit();return;}
   if(type=="jointCancel"){if(poseEditing&&poseBefore!=null){var old=RobotDocument.Parse(poseBefore);foreach(var item in Document.Joints){var previous=old.Find(item.Id);if(previous!=null)item.Position=previous.Position;}string restored=poseBefore;poseEditing=false;poseBefore=null;RefreshManualPose();last=restored;Text="GearLab 5.4 · "+Document.Name+(last!=saved?" *":"");}return;}
   if(type=="jointPose"){double value=Convert.ToDouble(data["value"]);UpdateJointController(id,value,false);}
   if(type=="jointEnd"){EndPoseEdit();SyncJointControllers();UpdateWebPose();}
  }
 }
}
