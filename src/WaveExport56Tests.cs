using System;using System.IO;using System.Text;using System.Diagnostics;
namespace GearLab {
 public static class WaveExport56Tests {
  public static int Run(string folder){Directory.CreateDirectory(folder);try{var p=Engine.Starter(4);using(var kernel=new CADKernel()){var w=Stopwatch.StartNew();WaveExactExport.Write(p,Path.Combine(folder,"Wave56-full.step"),kernel);double first=w.Elapsed.TotalSeconds;w.Restart();WaveExactExport.Write(p,Path.Combine(folder,"Wave56-print.step"),kernel,new[]{"wave_ring","wave_flex","wave_output"});File.WriteAllText(Path.Combine(folder,"wave-export-results.json"),CADDocument.Serializer().Serialize(new{full_seconds=first,cached_subset_seconds=w.Elapsed.TotalSeconds,worker_count=CADKernel.StartedWorkers}),Encoding.UTF8);}return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"wave-export-error.txt"),ex.ToString(),Encoding.UTF8);return 1;}}
 }
}
