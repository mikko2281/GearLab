using System.Windows.Media.Media3D;
namespace GearLab {public sealed partial class RobotDesignerForm {
 bool webDragSnapped;
 double SnapRobotDrag(double value){return webDragSnapped?value:AppSettings.Snap(value);}
 void CommitWebDrag(string id,Vector3D delta){webDragSnapped=true;try{CommitDrag(id,delta);}finally{webDragSnapped=false;}}
}}
