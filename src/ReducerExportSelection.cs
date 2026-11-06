using System;using System.Linq;
namespace GearLab {
 public static class ReducerExportSelection {
  public static CadAssembly ForScope(CadAssembly source,int scope){
   if(scope==0)return source;if(scope==2)return PrintExport.Select(source,null);
   if(scope!=1)throw new ArgumentException("Неизвестный состав экспорта.");
   var selected=new CadAssembly();selected.Parts.AddRange(source.Parts.Where(p=>!p.Reference&&(p.Role=="gear"||p.Role=="magnet"||p.Role=="roller"||p.Role=="pin"||p.Id=="wave_ring"||p.Id.StartsWith("wave_ball"))));
   if(!selected.Valid)throw new Exception("Нет рабочих профилей для выбранного состава.");selected.Notes.Add("Только рабочие детали передачи; мотор, корпус, опоры, крепёж и отдельные валы исключены.");return selected;
  }
 }
}
