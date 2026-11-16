using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Diagnostics;using System.Collections.Generic;using System.Windows.Forms;
namespace GearLab {
 // 7.0.0: режим «Просмотр», экономная отправка поз и замеры производительности робота.
 public static class Ui700Tests {
  static readonly List<string> lines=new List<string>();static int failures;static string progress;
  static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;if(progress!=null)File.WriteAllLines(progress,lines);}
  static double Time(int count,Action a){a();var samples=new List<double>();for(int i=0;i<count;i++){var w=Stopwatch.StartNew();a();samples.Add(w.Elapsed.TotalMilliseconds);}samples.Sort();return samples[samples.Count/2];}
  public static int Run(string folder){Directory.CreateDirectory(folder);progress=Path.Combine(folder,"ui700-progress.txt");var old=AppSettings.Current;var messages=new List<string>();var oldSink=UiMessage.TestSink;var perf=new Dictionary<string,object>();
   try{AppSettings.Current=new AppSettings{AutoSave=false};UiMessage.TestSink=m=>messages.Add(m);
    Check(ReleaseSupport.Version=="7.0.0","version is 7.0.0");
    var doc=RobotDocument.DefaultReference();var build=RobotCad.Build(doc);int partsCount=build.Entries.Values.Count(e=>!e.Guide);
    // Отправка поз: первый раз все детали, повтор — ничего, движение последнего шарнира — только его детали и потомки.
    var sent=new Dictionary<string,double[]>();var first=RobotDesignerForm.PoseDelta(build,0,false,sent);Check(first.Count==partsCount,"first pose message carries every part ("+partsCount+")");
    Check(RobotDesignerForm.PoseDelta(build,0,false,sent).Count==0,"unchanged pose sends nothing");
    doc=build.Document;var ordered=doc.Ordered();var movable=ordered.Where(j=>j.Mode!=RobotJointMode.Неподвижный&&build.Entries.Values.Any(e=>!e.Guide&&e.JointId==j.Id)).ToList();var last=movable[Math.Min(movable.Count-1,Math.Max(0,movable.Count/2))];last.Position=last.Position+7<=last.Maximum?last.Position+7:last.Position-7;build.InvalidatePose();var moved=RobotDesignerForm.PoseDelta(build,0,false,sent);var family=new HashSet<string>(doc.Joints.Where(j=>j.Id==last.Id||doc.Descendant(j.Id,last.Id)).Select(j=>j.Id));int expected=build.Entries.Values.Count(e=>!e.Guide&&family.Contains(e.JointId));Check(moved.Count>0&&moved.Count<partsCount,"jogging the last joint sends only moved parts ("+moved.Count+" of "+partsCount+")");Check(moved.Count<=expected,"moved parts belong to that joint or its children");
    var serializer=CADDocument.Serializer();var fullMessage=new{type="poses",parts=build.Entries.Values.Where(e=>!e.Guide).Select(e=>new{id=e.Id,matrix=RobotWebViewport.Matrix(build.Pose(e.Id,0,false))}).ToArray()};
    perf["parts"]=partsCount;perf["full_pose_bytes"]=serializer.Serialize(fullMessage).Length;perf["jog_pose_bytes"]=serializer.Serialize(new{type="poses",parts=moved.ToArray()}).Length;
    perf["full_pose_ms"]=Time(15,()=>serializer.Serialize(new{type="poses",parts=build.Entries.Values.Where(e=>!e.Guide).Select(e=>new{id=e.Id,matrix=RobotWebViewport.Matrix(build.Pose(e.Id,0,false))}).ToArray()}));
    int step=0;perf["delta_pose_ms"]=Time(15,()=>{last.Position=last.Minimum+(step++%2)*.5;build.InvalidatePose();serializer.Serialize(new{type="poses",parts=RobotDesignerForm.PoseDelta(build,0,false,sent).ToArray()});});
    perf["doc_serialize_ms"]=Time(5,()=>doc.Serialize());perf["doc_bytes"]=doc.Serialize().Length;perf["doc_validate_ms"]=Time(5,()=>doc.Validate());perf["robot_build_ms"]=Time(5,()=>RobotCad.Build(doc));perf["robot_solve_ms"]=Time(5,()=>RobotSolver.Solve(build));{var fresh=new List<double>();for(int i=0;i<3;i++){var b=RobotCad.Build(doc);var w=Stopwatch.StartNew();RobotSolver.Solve(b);fresh.Add(w.Elapsed.TotalMilliseconds);}fresh.Sort();perf["first_solve_after_build_ms"]=fresh[1];}perf["check_range_64_ms"]=Time(3,()=>RobotSolver.CheckRange(build,64));var reference=RobotSolver.Solve(RobotCad.Build(doc));perf["solve_signature"]=string.Join(";",reference.Joints.Select(x=>x.Id.Substring(0,4)+"="+x.Required.ToString("R")));perf["total_mass"]=reference.TotalMass.ToString("R");
    var wide=new RobotDocument();RobotJoint parent=null;for(int i=0;i<12;i++){var j=wide.Add(parent==null?null:parent.Id);j.EndX=60;parent=j;}perf["ordered_12_joints_x1000_ms"]=Time(5,()=>{for(int i=0;i<1000;i++)wide.Ordered();});
    string empty=Path.Combine(folder,"empty.robot.json");File.WriteAllText(empty,new RobotDocument{Name="Пусто"}.Serialize(),Encoding.UTF8);
    using(var robot=new RobotDesignerForm(empty))using(var shell=new WorkspaceShell(robot)){shell.Show();Application.DoEvents();Action quiet=()=>{foreach(var jj in robot.Document.Joints)if(jj.BodyGenerator!=null)jj.BodyGenerator.AutoRegenerate=false;};
     var p=Engine.Starter(0);Engine.SetPreset(p,2);p.Ratio=30;p.RingOutput=true;p.Overall=90;Engine.FitOverall(p);var entry=new SavedReducer{Name="Ц",Reducer=p,Saved=DateTime.Now};Check(robot.PlaceReducer(entry,null,true)&&robot.PlaceReducer(Ui700Reducers.Planetary(),null,false),"robot with two reducer joints");quiet();
     robot.ShowInspect(true);Check(robot.InspectMode,"inspect mode turns on");var payload=robot.InspectPayload();var joints=payload.joints.ToList();Check(joints.Count==2,"inspect payload lists both joints");bool ownParts=true;foreach(var j in joints){foreach(string id in j.parts){RobotCadEntry e;if(!robot.Build.Entries.TryGetValue(id,out e)||e.Guide||e.JointId!=j.id)ownParts=false;}}Check(ownParts,"each joint lists only its own visible parts");Check(joints.All(j=>j.parts.Length>0&&j.axis.Length==3),"each joint has parts and an axis");
     Check(joints[1].label.StartsWith("2 · "),"joint label shows its order");
     var badges=robot.ReducerBadges();Check(badges.Count==2&&badges[1].Label.StartsWith("2 · "),"reducer badges keep order after optimisation");
     robot.ShowInspect(false);Check(!robot.InspectMode&&!robot.InspectPayload().enabled,"inspect mode turns off");
     Check(messages.Count==0,"no error messages: "+string.Join(" | ",messages.Take(3)));shell.ApproveTestClose();}
    File.WriteAllText(Path.Combine(folder,"ui700-performance.json"),CADDocument.Serializer().Serialize(perf),Encoding.UTF8);
    lines.Add((failures==0?"PASS ":"FAIL ")+lines.Count+" assertions");File.WriteAllLines(Path.Combine(folder,"ui700-results.txt"),lines);return failures==0?0:1;}
   catch(Exception ex){lines.Add("ERROR "+ex);File.WriteAllLines(Path.Combine(folder,"ui700-results.txt"),lines);return 1;}
   finally{AppSettings.Current=old;UiMessage.TestSink=oldSink;}}
 }
 static class Ui700Reducers {public static SavedReducer Planetary(){return new SavedReducer{Name="П",Reducer=Engine.Starter(2),Saved=DateTime.Now};}}
}
