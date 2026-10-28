using System;using System.IO;using System.Linq;using System.Text;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace GearLab {
 public sealed class TestDialogs:IUserDialogs {
  public Queue<string> SavePaths=new Queue<string>(),OpenPaths=new Queue<string>();public Queue<DialogResult> Answers=new Queue<DialogResult>();public List<string> Messages=new List<string>();public int Helps,UnsavedCalls;
  public string Save(IWin32Window owner,string ext,string suggested){return SavePaths.Count==0?null:SavePaths.Dequeue();}
  public string Open(IWin32Window owner){return OpenPaths.Count==0?null:OpenPaths.Dequeue();}
  public DialogResult Unsaved(IWin32Window owner){UnsavedCalls++;return Answers.Count==0?DialogResult.No:Answers.Dequeue();}
  public void Info(IWin32Window owner,string text,string title){Messages.Add(title+": "+text);}
  public void Help(IWin32Window owner){Helps++;}
 }
 static class ReleaseTests {
  static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Release: "+message);}
  static void Render(Result r,int view,int stage,string path){using(var c=new Canvas{Result=r,Size=new Size(1100,800),View=view,StageIndex=stage,Phase=.73})using(var b=new Bitmap(1100,800)){c.DrawToBitmap(b,new Rectangle(0,0,1100,800));b.Save(path);}}
  public static void Run(string folder,StringBuilder report){
   var sample=Engine.Starter(3);sample.Ratio=60;sample.Overall=222.82;sample.Module=.9922;sample.MotorPreset=0;sample.AutoFit=false;var r=Engine.Calculate(sample);
   Check(r.Valid&&r.Stages.Count==2&&Math.Abs(r.Actual-60)<1e-12,"screenshot 60:1, manual module: "+string.Join(";",r.Errors));
   Check(r.Stages[0].Actual==7.5&&r.Stages[1].Actual==8,"balanced exact 60 train");
   foreach(int ratio in new[]{4,8,9,20,30,60,100,120,200})foreach(int count in new[]{2,3,4}){
    var p=Engine.Starter(3);p.Ratio=ratio;p.Planets=count;p.Overall=222.82;bool fit=Engine.FitOverall(p);var t=Engine.Calculate(p);Check(fit&&t.Valid,"ratio/planets "+ratio+"/"+count+": "+string.Join(";",t.Errors));
    var stages=t.Stages.Count>0?t.Stages:new List<Result>{t};double product=1;foreach(var s in stages){product*=s.Actual;Check(s.Z2==s.Z1+2*s.Zp&&(s.Z1+s.Z2)%count==0,"stage assembly");Check((s.Z1+s.Zp)*Math.Sin(Math.PI/count)>s.Zp+2,"stage clearance");Check(s.P.Shaft<p.Module*(s.Z1-2.5),"stage bore");Check(p.PlanetPin<p.Module*(s.Zp-2.5),"stage pin");}
    Check(Math.Abs(t.Actual/product-1)<1e-12&&Math.Abs(t.Actual/p.Ratio-1)*100<=p.RatioTolerance+1e-8,"total ratio/tolerance");Check(t.Width<=p.Overall+.025,"train diameter");
   }
   sample.PlanetStages=1;Check(!Engine.Calculate(sample).Valid,"60:1 impossible single three-planet not drawn");sample.PlanetStages=0;
   for(int preset=1;preset<=4;preset++)foreach(int ratio in new[]{8,9,20,30,60,120,200}){var p=Engine.Starter(3);Engine.SetPreset(p,preset);p.Ratio=ratio;bool fit=Engine.FitOverall(p);Check(fit&&Engine.Calculate(p).Valid,"planet ratio/preset "+ratio+"/"+preset);}
   var triple=Engine.Starter(3);triple.Ratio=200;triple.Overall=120;triple.PlanetStages=3;Check(Engine.FitOverall(triple)&&Engine.Calculate(triple).Stages.Count==3,"three-stage 200:1");
   var failed=Engine.Clone(sample);failed.Overall=10;string before=ReleaseSupport.Serialize(failed);Check(!Engine.FitOverall(failed)&&ReleaseSupport.Serialize(failed)==before,"failed fit transactional");
   foreach(var invalid in new[]{"null","[]","{\"Teeth\":24.5}","{\"Shaft\":\"5\"}","{\"AutoFit\":1}","{\"Missing\":5}","{\"FormatVersion\":99}","{\"PlanetStages\":4}","{\"Ratio\":{}}","{broken"}){bool rejected=false;try{ReleaseSupport.Parse(invalid);}catch{rejected=true;}Check(rejected,"invalid JSON rejected "+invalid);}
   var legacy=ReleaseSupport.Parse("{\"Kind\":3,\"Ratio\":60,\"Teeth\":24}");Check(legacy.PlanetStages==0&&!legacy.AutoFit,"legacy new defaults");
   for(int j=0;j<r.Stages.Count;j++){double phase=Engine.StageInput(r,Engine.Tau*60,j),outAngle=phase/r.Stages[j].Actual;Check(Math.Abs(outAngle-(j==r.Stages.Count-1?Engine.Tau:Engine.StageInput(r,Engine.Tau*60,j+1)))<1e-10,"series stage speeds");Render(r,0,j,Path.Combine(folder,"Планетарный_60_ступень_"+(j+1)+".png"));}
   Render(r,3,0,Path.Combine(folder,"Планетарный_60_осевой.png"));Render(r,4,0,Path.Combine(folder,"Подшипник.png"));
   var longTrain=Engine.Calculate(triple);Render(longTrain,0,2,Path.Combine(folder,"Планетарный_200_ступень_3.png"));
   foreach(var train in new[]{r,longTrain})for(int stageIndex=0;stageIndex<train.Stages.Count;stageIndex++)for(int n=0;n<8;n++){
    var st=train.Stages[stageIndex];double phase=Engine.StageInput(train,.19+Engine.Tau*n/8,stageIndex);var sun=Engine.Transform(st.Gear1,0,0,phase);var ring=Engine.Transform(st.Gear2,0,0,Engine.RingInitial(st));for(int j=0;j<st.P.Planets;j++){var pos=Engine.Polar(st.P.Module*(st.Z1+st.Zp)/2,Engine.Tau*j/st.P.Planets+phase/st.Actual);var pg=Engine.Transform(st.PlanetGear,pos.X,pos.Y,Engine.PlanetAngle(st,phase,j));V4Tests.Clearance(pg,sun,false,"train planet/sun");V4Tests.Clearance(sun,pg,false,"train sun/planet");V4Tests.Clearance(pg,ring,true,"train internal ring");}checks++;
   }
   foreach(var pair in new[]{new KeyValuePair<string,Result>("Планетарный_60",r),new KeyValuePair<string,Result>("Планетарный_200",longTrain)}){
    string file=Path.Combine(folder,pair.Key);ReleaseSupport.WriteText(file+".gear.json",ReleaseSupport.Serialize(pair.Value.P),Encoding.UTF8);Files.Csv(pair.Value,file+".csv");Files.Svg(pair.Value,file+".svg");Files.Dxf(pair.Value,file+".dxf");Check(File.ReadAllText(file+".dxf").Contains("STAGE_2_"),"stage export layers");}
   string atomic=Path.Combine(folder,"atomic.txt");ReleaseSupport.WriteText(atomic,"first",Encoding.UTF8);ReleaseSupport.WriteText(atomic,"second",Encoding.UTF8);Check(File.ReadAllText(atomic)=="second"&&!Directory.GetFiles(folder,".gearlab-*.tmp").Any(),"atomic overwrite cleanup");
   using(var form=new MainForm(new TestDialogs()))form.ReleaseRegression(folder,report);
   report.AppendLine("PASS release engine: "+checks+" checks; exact 60:1 screenshot, 27 ratio/planet combinations, 3 stages, invalid JSON, export layers, speed cascade, package/render checks.");
  }
 }
}
