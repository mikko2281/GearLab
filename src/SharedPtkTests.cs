using System;using System.IO;using System.Linq;using System.Collections.Generic;
namespace GearLab {
 public static class SharedPtkTests {
  public static int Run(string folder){Directory.CreateDirectory(folder);var failures=new List<string>();int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)failures.Add(message);};try{
   foreach(string joint in new[]{"CommonShaft","D","Spline"})foreach(bool reverse in new[]{false,true}){
    var t=CompoundReducer.PtkExample();t.SharedHousing.Joint=joint;if(reverse){t.Stages.Reverse();t.Stages[0].Parent=null;t.Stages[1].Parent=t.Stages[0].Id;t.Stages[1].Parameters.Shaft=8;t.Stages[0].Parameters.OutputShaft=8;}
    // Both flags are deliberately true: only the cycloidal implementation consumes this setting.
    foreach(var n in t.Stages)n.Parameters.RingOutput=true;
    var s=TrainSolver.Solve(t);string name=joint+(reverse?"-reverse":"");check(s.Valid,name+": "+string.Join(";",s.Errors));if(!s.Valid)continue;
    check(Math.Abs(s.TotalRatio+68)<1e-9,name+" signed ratio -68");check(s.Scene.Model.Parts.Count(q=>q.Id.Contains("wave_ball"))==17,name+" all 17 balls retained");
    var carrier=s.Scene.Model.Parts.Single(q=>q.SectionUnion);check(carrier.Sections.Count>=19,name+" separator webs retained");check(s.Scene.Model.Parts.Count(q=>q.Reference)==1,name+" single motor");
    foreach(var p in s.Scene.Model.Parts.Where(PrintExport.Candidate)){try{var mesh=PrintExport.Mesh(p);check(Math.Abs(mesh.SignedVolume()/p.Volume()-1)<.02,name+" mesh volume "+p.Id);}catch(Exception ex){check(false,name+" mesh "+p.Id+": "+ex.Message);}}
    string stem="ptk-"+name;t.Save(Path.Combine(folder,stem+".train.json"));check(TrainSolver.Solve(TransmissionTrain.Read(Path.Combine(folder,stem+".train.json"))).Valid,name+" roundtrip");
    File.WriteAllText(Path.Combine(folder,stem+".assembly.json"),CADDocument.Serializer().Serialize(TransmissionExport.Spec(s.Scene.Model)));
    if(joint=="CommonShaft"&&!reverse){CompoundExport.PrintZip(t,s,Path.Combine(folder,"ptk-print.zip"));for(int i=1;i<=3;i++){var moved=new CadAssembly();foreach(var q in s.Scene.Model.Parts)moved.Parts.Add(q.WithPlacement(q.Placement*PartMotion.Bind(s.Scene,q).At(i*Math.PI/2,0)));File.WriteAllText(Path.Combine(folder,stem+"-phase-"+i+".assembly.json"),CADDocument.Serializer().Serialize(TransmissionExport.Spec(moved)));}}
   }
   var planet=CompoundReducer.Example();planet.Stages[0].Parameters.RingOutput=true;check(TrainSolver.Solve(planet).Valid,"planetary ignores unused RingOutput");planet.Stages[1].Parameters.RingOutput=true;check(!TrainSolver.Solve(planet,false).Valid,"cycloidal rotating ring remains rejected");
  }catch(Exception ex){failures.Add(ex.ToString());}File.WriteAllText(Path.Combine(folder,"shared-ptk-results.json"),CADDocument.Serializer().Serialize(new{Passed=failures.Count==0,Checks=checks,Failures=failures}));return failures.Count==0?0:1;}
 }
}
