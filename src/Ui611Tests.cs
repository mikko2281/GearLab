using System;using System.IO;using System.Linq;using System.Text;using System.Threading;using System.Diagnostics;using System.Collections.Generic;using System.Windows.Forms;using System.Windows.Media.Media3D;
namespace GearLab {
 // 6.11.0: свои редукторы в роботе — «＋ В робота», значки редукторов на суставах и их настройка.
 public static class Ui611Tests {
  static readonly List<string> lines=new List<string>();static int failures;static string progress;
  static void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);if(!ok)failures++;if(progress!=null)File.WriteAllLines(progress,lines);}
  static void Wait(Func<bool> f,double seconds){var w=Stopwatch.StartNew();while(!f()&&w.Elapsed.TotalSeconds<seconds){Application.DoEvents();Thread.Sleep(10);}}
  static IEnumerable<Control> All(Control root){foreach(Control c in root.Controls){yield return c;foreach(var d in All(c))yield return d;}}
  static SavedReducer Cycloid(){var p=Engine.Starter(0);Engine.SetPreset(p,2);p.Ratio=30;p.RingOutput=true;p.Overall=90;Engine.FitOverall(p);return new SavedReducer{Name="Циклоидальный 30",Reducer=p,Saved=DateTime.Now};}
  static SavedReducer Planetary(){var p=Engine.Starter(2);return new SavedReducer{Name="Планетарный",Reducer=p,Saved=DateTime.Now};}
  public static int Run(string folder){Directory.CreateDirectory(folder);progress=Path.Combine(folder,"ui611-progress.txt");var old=AppSettings.Current;var messages=new List<string>();var oldSink=UiMessage.TestSink;
   try{AppSettings.Current=new AppSettings{AutoSave=false};UiMessage.TestSink=m=>messages.Add(m);RobotReducerKit.LibraryOverride=Path.Combine(folder,"my-reducers.json");if(File.Exists(RobotReducerKit.LibraryOverride))File.Delete(RobotReducerKit.LibraryOverride);
    // Библиотека «Мои редукторы».
    var cycloid=Cycloid();var planetary=Planetary();RobotReducerKit.Remember(cycloid);RobotReducerKit.Remember(planetary);RobotReducerKit.Remember(cycloid);var saved=RobotReducerKit.Load();Check(saved.Count==2&&saved[0].Reducer.Kind==0&&saved[1].Reducer.Kind==2,"library keeps newest first without duplicates");
    var train=TransmissionTrain.Example();var tsol=TrainSolver.Solve(train);var leaf=tsol.States.Values.First(st=>!train.Stages.Any(n=>n.Parent==st.Id||n.SecondParent==st.Id));var trainEntry=new SavedReducer{Name="Цепь",Train=train,Output=leaf.Id,Saved=DateTime.Now};RobotReducerKit.Remember(trainEntry);Check(RobotReducerKit.Load().Count==3&&RobotReducerKit.Load()[0].Train!=null,"stage train is stored in the library");
    File.WriteAllText(RobotReducerKit.LibraryOverride,"{broken",Encoding.UTF8);Check(RobotReducerKit.Load().Count==0,"broken library file does not crash");
    string empty=Path.Combine(folder,"empty.robot.json");File.WriteAllText(empty,new RobotDocument{Name="Пусто"}.Serialize(),Encoding.UTF8);using(var robot=new RobotDesignerForm(empty))using(var shell=new WorkspaceShell(robot)){shell.Show();Application.DoEvents();Action quiet=()=>{foreach(var jj in robot.Document.Joints)if(jj.BodyGenerator!=null)jj.BodyGenerator.AutoRegenerate=false;};
     // Новый робот из редуктора.
     Check(robot.PlaceReducer(cycloid,null,true),"new robot from a user reducer");var d=robot.Document;Check(d.Joints.Count==1,"robot has one joint");var j1=d.Joints[0];Check(j1.ReducerSettings!=null&&j1.ReducerSettings.Kind==0,"joint carries the cycloid reducer");
     var r=Engine.Calculate(Engine.Clone(j1.ReducerSettings));r.Model=CadBuilder.Build(r);Check(Math.Abs(j1.Reduction-r.Actual)<1e-9,"joint ratio equals the reducer ratio");Check(j1.Mode==RobotJointMode.Вращение_Z&&j1.EndZ>0,"first joint is a vertical base rotation");Check(j1.GearboxDiameter>=Math.Min(250,r.Model.Bounds()[1]*2)-1e-6,"gearbox envelope fits the reducer");Check(robot.ReducerMode,"reducer view opens after adding");
     // Следующее звено.
     Check(robot.PlaceReducer(planetary,null,false),"second reducer becomes the next joint");d=robot.Document;Check(d.Joints.Count==2&&d.Joints[1].ParentId==d.Joints[0].Id,"next joint is attached to the end of the arm");var poses=RobotKinematics.Evaluate(d,0,false);Check((poses[d.Joints[1].Id].Origin-poses[d.Joints[0].Id].Tip).Length<1e-6,"next joint starts at the parent tip");Check(d.Joints[1].ReducerSettings!=null&&d.Joints[1].ReducerSettings.Kind==2,"second joint carries the planetary reducer");
     // Значки и инспектор.
     var badges=robot.ReducerBadges();Check(badges.Count==2&&badges.All(b=>b.HasModel&&b.Status!="none"),"each joint has a reducer badge");Check(badges[0].Label.StartsWith("1 · ")&&badges[1].Detail.Contains("i "),"badge shows order and ratio");
     string second=d.Joints[1].Id;robot.PickReducer(d.Joints[0].Id);robot.PickReducer(second);var rows=All(robot).OfType<ReducerRow>().ToList();Check(rows.Count==2&&rows.Count(x=>x.Selected)==1&&rows[1].Selected,"clicking a badge selects that reducer in the list");
     var inspector=(Control)typeof(RobotDesignerForm).GetField("quickInspector",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(robot);quiet();Application.DoEvents();if(inspector.Width>0&&inspector.Height>0)using(var bmp=new System.Drawing.Bitmap(inspector.Width,inspector.Height)){inspector.DrawToBitmap(bmp,new System.Drawing.Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(folder,"ui611-inspector.png"));}
     var buttons=All(robot).OfType<Button>().Select(b=>I18n.Raw(b)).ToList();Check(buttons.Contains("Настроить редуктор…")&&buttons.Contains("Заменить на  ▾"),"reducer card offers configure and replace");
     // Замена на цепь ступеней и отмена.
     string first=d.Joints[0].Id;Check(robot.PlaceReducer(trainEntry,first,false),"replace a joint reducer with a stage train");var live=robot.Document.Find(first);Check(live.TrainSettings!=null&&live.ReducerSettings==null&&live.TrainOutput==leaf.Id,"joint now uses the stage train");Check(Math.Abs(live.Reduction-1/Math.Abs(leaf.OutputGain))<1e-6,"train ratio applied to the joint");
     typeof(RobotDesignerForm).GetMethod("Undo",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(robot,null);live=robot.Document.Find(first);Check(live!=null&&live.ReducerSettings!=null&&live.TrainSettings==null,"undo restores the previous reducer");
     // Сохранение проекта.
     var copy=RobotDocument.Parse(robot.Document.Serialize());Check(copy.Joints.Count==2&&copy.Joints[0].ReducerSettings!=null&&copy.Joints[1].ReducerSettings.Kind==2,"robot with user reducers survives save and load");
     // Сустав без модели редуктора.
     var plain=RobotDocument.Example();var badge=RobotReducerKit.Badge(plain,plain.Joints[0],null);Check(!badge.HasModel&&badge.Status=="none","joint without a reducer model is marked as number only");
     // Кнопки в редакторах.
     // Процедурный корпус в Wine не строится (нет ядра), сообщение об этом в скрытой вкладке роняет Wine — тест отключает только автоперестроение.
     quiet();
     shell.Navigate("reducer");Wait(()=>shell.Existing("reducer") is ReducerWorkspaceForm&&((ReducerWorkspaceForm)shell.Existing("reducer")).MainView!=null,20);var workspace=shell.Existing("reducer") as ReducerWorkspaceForm;var main=workspace==null?null:workspace.MainView;Check(main!=null&&All(main).OfType<Button>().Any(b=>I18n.Raw(b)=="＋ Добавить в робота  ▾"),"reducer editor has «＋ Добавить в робота»");
     if(main!=null){int before=robot.Document.Joints.Count;Check(RobotReducerKit.Place(main,cycloid,null,false),"reducer editor places into the open robot");quiet();Check(robot.Document.Joints.Count==before+1&&shell.Existing("robot")==robot,"open robot gained a joint instead of being replaced");}
     using(var trainForm=new TrainDesignerForm()){Check(All(trainForm).OfType<Button>().Any(b=>I18n.Raw(b)=="＋ В робота  ▾"),"stage editor has «＋ В робота»");}
     using(var linked=new TrainDesignerForm()){linked.ConfigureForRobot((t,o)=>true);Check(All(linked).OfType<Button>().Where(b=>I18n.Raw(b)=="＋ В робота  ▾").All(b=>!b.Visible),"robot-linked stage editor hides «＋ В робота»");}
     Check(messages.Count==0,"no error messages: "+string.Join(" | ",messages.Take(3)));
     robot.ShowReducers(false);Check(!robot.ReducerMode,"reducer view can be closed");
     shell.ApproveTestClose();}
    lines.Add((failures==0?"PASS ":"FAIL ")+lines.Count+" assertions");File.WriteAllLines(Path.Combine(folder,"ui611-results.txt"),lines);return failures==0?0:1;}
   catch(Exception ex){lines.Add("ERROR "+ex);File.WriteAllLines(Path.Combine(folder,"ui611-results.txt"),lines);return 1;}
   finally{AppSettings.Current=old;UiMessage.TestSink=oldSink;RobotReducerKit.LibraryOverride=null;}}
 }
}
