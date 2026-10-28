using System;using System.Linq;using System.Collections.Generic;
namespace GearLab {
 public static partial class Engine {
  sealed class ToothSet {public int Ring,Planet;public double Ratio;}
  static readonly Dictionary<string,int[]> trainCache=new Dictionary<string,int[]>();
  static List<ToothSet> PlanetCandidates(Parameters p){var a=new List<ToothSet>();for(int zr=p.Teeth+16;zr<=2000;zr++){
   if((zr-p.Teeth)%2!=0||(zr+p.Teeth)%p.Planets!=0)continue;int zp=(zr-p.Teeth)/2;
   if((p.Teeth+zp)*Math.Sin(Math.PI/p.Planets)<=zp+2)continue;
   a.Add(new ToothSet{Ring=zr,Planet=zp,Ratio=1+(double)zr/p.Teeth});}return a;}
  static int Nearest(List<ToothSet> a,double target){int lo=0,hi=a.Count-1;while(lo<hi){int mid=(lo+hi)/2;if(a[mid].Ratio<target)lo=mid+1;else hi=mid;}return lo;}
  static int[] FindTrain(Parameters p){
   string key=p.Teeth+"/"+p.Planets+"/"+N(p.Ratio)+"/"+p.PlanetStages+"/"+N(p.RatioTolerance)+"/"+N(p.Overall)+"/"+N(p.Wall)+"/"+N(p.Shaft)+"/"+N(p.IntermediateShaft)+"/"+N(p.PlanetPin);int[] cached;if(trainCache.TryGetValue(key,out cached))return cached;
   var a=PlanetCandidates(p);if(a.Count==0)return null;int[] answer=null;
   for(int count=p.PlanetStages==0?1:p.PlanetStages;count<=(p.PlanetStages==0?3:p.PlanetStages);count++){
    double bestError=double.MaxValue;int bestMax=int.MaxValue;int[] best=null;
    Action<int[]> evaluate=indices=>{double ratio=1;int maximum=0,minPlanet=int.MaxValue;foreach(int i in indices){ratio*=a[i].Ratio;maximum=Math.Max(maximum,a[i].Ring);minPlanet=Math.Min(minPlanet,a[i].Planet);}double error=Math.Abs(ratio/p.Ratio-1);
     if(p.Overall>0){double available=(p.Overall-2*p.Wall)/(maximum+2.5),needed=Math.Max(.1,Math.Max((indices.Length>1?Math.Max(p.Shaft,p.IntermediateShaft):p.Shaft)/(p.Teeth-2.5),p.PlanetPin/(minPlanet-2.5)));if(available<=needed+1e-7)return;}
     if(error<bestError-1e-12||Math.Abs(error-bestError)<1e-12&&maximum<bestMax){bestError=error;bestMax=maximum;best=indices.Select(i=>a[i].Ring).ToArray();}};
    if(count==1)for(int n=0;n<a.Count;n++)evaluate(new[]{n});
    if(count==2)for(int j=0;j<a.Count;j++){int n=Nearest(a,p.Ratio/a[j].Ratio);evaluate(new[]{j,n});if(n>0)evaluate(new[]{j,n-1});}
    if(count==3)for(int j=0;j<a.Count;j++)for(int k=j;k<a.Count;k++){
     if(a[j].Ratio*a[k].Ratio*a[0].Ratio>p.Ratio*(1+p.RatioTolerance/100)&&best!=null)break;
     int n=Nearest(a,p.Ratio/(a[j].Ratio*a[k].Ratio));evaluate(new[]{j,k,n});if(n>0)evaluate(new[]{j,k,n-1});}
    if(best!=null&&bestError<=p.RatioTolerance/100+1e-10){answer=best.OrderBy(z=>z).ToArray();break;}
   }
   if(trainCache.Count>=128)trainCache.Clear();trainCache[key]=answer;return answer;
  }
  static void Planetary(Result r){
   var p=r.P;
   if(p.PlanetStages<0||p.PlanetStages>3||p.RatioTolerance<0||p.RatioTolerance>10){r.Error("Ступени: авто или 1–3; допуск отношения: 0–10 %.","PlanetStages","RatioTolerance");return;}
   if(p.Ratio<=2){r.Error("Схема с неподвижным кольцом требует i>2 для одной ступени. Выберите другой тип передачи либо увеличьте отношение.","Ratio");return;}
   int[] rings=FindTrain(p);
   if(rings==null){var choices=PlanetCandidates(p);string span=choices.Count==0?"Нет даже одной ступени для такого солнца и числа сателлитов.":"Одна ступень для этих зубьев допускает i от "+F(choices.First().Ratio)+" до "+F(choices.Last().Ratio)+".";
    r.Error("Нельзя получить i="+F(p.Ratio)+" в выбранном числе ступеней с допуском "+F(p.RatioTolerance)+" % либо не хватает места под валы/оси. "+span+" Выберите «Авто 1–3 ступени», измените солнце/сателлиты, габарит или допуск. Увеличение корпуса не исправляет пересечение сателлитов.","Ratio","Planets","Teeth","PlanetStages","RatioTolerance","Overall");return;}
   if(rings.Length==1){PlanetarySingle(r);if(r.Valid&&Math.Abs(r.Actual/p.Ratio-1)*100>p.RatioTolerance+1e-8)r.Error("Отклонение отношения превышает допуск.","RatioTolerance");r.Metric("Ступеней","1");return;}
   r.Actual=1;
   for(int n=0;n<rings.Length;n++){
    var sp=Clone(p);sp.PlanetStages=1;sp.Ratio=1+(double)rings[n]/p.Teeth;sp.Shaft=n==0?p.Shaft:p.IntermediateShaft;sp.BearingSimulation=n==0&&p.BearingSimulation;sp.MotorFrame=n==0?p.MotorFrame:0;sp.MotorMount=n==0?p.MotorMount:0;sp.MotorPilot=n==0?p.MotorPilot:0;
    var stage=new Result{P=sp};PlanetarySingle(stage);r.Stages.Add(stage);r.Actual*=stage.Actual;
    foreach(string error in stage.Errors)r.Error("Ступень "+(n+1)+": "+error,"Module",n==0?"Shaft":"IntermediateShaft");
    if(p.PlanetPin>=p.Module*(stage.Zp-2.5))r.Error("Ступень "+(n+1)+": ось сателлита не помещается в корневом круге.","PlanetPin","Module");
    r.Metric("Ступень "+(n+1)+": солнце / сателлит / кольцо",stage.Z1+" / "+stage.Zp+" / "+stage.Z2);r.Metric("Ступень "+(n+1)+": i",F(stage.Actual)+":1");
    for(int j=0;j<6;j++){var item=stage.Items[j];r.Add("Ст. "+(n+1)+": "+item.Name,item.Size,item.Qty,item.Status);}
    if(stage.Z1<17||stage.Zp<17)r.Notes.Add("Ступень "+(n+1)+": меньше 17 зубьев на солнце/сателлите; проверить подрезание и коррекцию.");
   }
   var largest=r.Stages.OrderByDescending(x=>x.Z2).First();r.Z1=largest.Z1;r.Z2=largest.Z2;r.Zp=largest.Zp;r.Envelope=p.Module*r.Z2;
   r.Metric("Ступеней",rings.Length.ToString());r.Metric("Фактическое суммарное отношение",F(r.Actual)+":1");r.Metric("Отклонение от запроса",F((r.Actual/p.Ratio-1)*100)+" %");
   r.Add("Межступенчатая передача","Водило предыдущей → солнце следующей, вал Ø"+F(p.IntermediateShaft)+" мм",(rings.Length-1).ToString(),"Размер принят; муфта, вал и опоры по CAD");r.Add("Опоры входа и конечного выхода","По 2 на каждый вал","4","Шаблон, марки по нагрузкам");r.Add("Корпус, крышки, крепёж","Общий соосный пакет "+rings.Length+" ступеней","—","Полный CAD не выполнен");
   r.Notes.Add("Ступени расположены последовательно вдоль общей оси. Водило предыдущей вращает солнце следующей; все кольца закреплены. Суммарное i — произведение отношений ступеней. Общий габарит ограничивает диаметр, а длина пакета растёт с числом ступеней.");
   r.Notes.Add("Во всех ступенях одинаковые солнце, модуль и число сателлитов; кольца и сателлиты подбираются отдельно. Вал между ступенями задан отдельно. Прочность водил/связей, подрезание, интерференция и распределение нагрузки не подтверждены.");
   r.Notes.Add("В каждой ступени проверены целые зубья, Zr=Zs+2Zp, (Zs+Zr)/число сателлитов и непересечение соседних сателлитов по вершинам. Подбор минимального числа ступеней с заданным допуском; при равном отклонении выбирается меньший максимум зубьев кольца.");
  }
  public static double StageInput(Result train,double angle,int index){for(int j=0;j<index&&j<train.Stages.Count;j++)angle/=train.Stages[j].Actual;return angle;}
 }
}
