using System;using System.Linq;using System.Drawing;using System.Collections.Generic;
namespace GearLab {
 public static partial class Engine {
  public static string[] PresetNames={"Пользовательские размеры","NEMA 17 · компактный 60 мм","NEMA 17 · базовый 80 мм","NEMA 17 · большой 120 мм","NEMA 23 · базовый 120 мм","Micro 20:1 · NEMA 17 · 42 мм"};
  public static void ResetCadSettings(Parameters p){var defaults=new Parameters();if(p.CycloidLayout==1)MicroPreset.Configure(defaults);foreach(var property in typeof(Parameters).GetProperties())if(property.Name.StartsWith("Model")||property.Name.StartsWith("Micro"))property.SetValue(p,property.GetValue(defaults,null),null);if(p.CycloidLayout==0&&p.MotorHole>=4.5)p.ModelCover=6;p.RingBridge=0;}
  public static void SetPreset(Parameters p,int preset){
   if(preset==5){MicroPreset.Configure(p);return;}p.MotorPreset=preset;if(preset==0)return;p.CycloidLayout=0;p.ModelCover=preset==4?6:4;
   bool big=preset==4;p.MotorFrame=big?57.3:42.3;p.MotorMount=big?47.14:31;p.MotorPilot=big?38.1:22;p.MotorHole=big?5.2:3.3;p.MotorLength=big?76.5:preset==1?39:48;p.Shaft=big?6.35:5;
   p.Overall=preset==1?60:preset==2?80:120;p.Wall=preset==1?2.5:3;p.OutputShaft=big?12:8;p.Thickness=preset==1?5:preset==2?7:9;p.DiscGap=1;p.Discs=2;
   p.Roller=preset==1?2.5:preset==2?4:5;p.OutputPin=preset==1?4:preset==2?5:6;p.OutputPins=6;p.Web=preset==1?1.2:1.5;p.Teeth=p.Kind==3?24:20;p.Planets=3;p.Pitch=2;p.PlanetPin=4;
   p.BearingSimulation=true;
   if(p.Kind==0){p.BearingInner=big?15:8;p.Bore=big?32:22;p.BearingWidth=big?9:7;p.BallDiameter=big?4.25:3.5;p.BallCount=big?8:7;}
   else{p.BearingInner=p.Shaft;p.Bore=big?19:16;p.BearingWidth=big?6:5;p.BallDiameter=big?3:2.5;p.BallCount=7;}
   ResetCadSettings(p);
  }
  public static Parameters Starter(int kind){var p=new Parameters{Kind=kind,RingOutput=true,IntegratedRing=kind==0,Ratio=kind==0?20:kind==3?4:2};SetPreset(p,2);p.AutoFit=true;FitOverall(p);return p;}
  public static double SecondDiscAngle(Parameters p,double input){return DiscAngle(p,input)-Math.PI/(PinCount(p)-1);}
  public static double CarrierAngle(Parameters p,double input){return DiscAngle(p,input);}
  public static double PlanetAngle(Result r,double input,int index){double carrier=input/r.Actual,location=Tau*index/r.P.Planets;return carrier-(double)r.Z1/r.Zp*(input-carrier)+(1+(double)r.Z1/r.Zp)*location+Math.PI-Math.PI/r.Zp;}
  public static double RingInitial(Result r){return r.Zp*Math.PI/r.Z2;}
  public static double[] BearingAngles(Parameters p,double inner,double outer){double pitch=(p.Bore+p.BearingInner)/2,q=p.BallDiameter/pitch,cage=.5*((1-q)*inner+(1+q)*outer),spin=cage-pitch/(2*p.BallDiameter)*(1-q*q)*(inner-outer);return new[]{cage,spin};}
  public static Result Calculate(Parameters input){
   var r=CalculateCore(input);if(input==null)return r;var p=r.P;
   if(!r.Valid)return r;
   if(p.CycloidLayout<0||p.CycloidLayout>1||p.CycloidLayout==1&&(p.Kind!=0||!p.RingOutput||p.Discs!=2))r.Error("Micro: циклоидальный редуктор, два диска и выход наружного кольца.","CycloidLayout","Discs");
   if(p.MotorPreset<0||p.MotorPreset>=PresetNames.Length){r.Error("Неизвестный набор размеров.");return r;}
   if(p.Overall<0||p.Overall>2000)r.Error("Общий габарит: от 0 до 2000 мм; 0 отключает ограничение.","Overall");
   if(p.Wall<0||p.Wall>30)r.Error("Толщина условной стенки: от 0 до 30 мм.","Wall");
   if(p.OutputShaft<1||p.OutputShaft>100)r.Error("Выходной вал: от 1 до 100 мм.","OutputShaft");
   if(p.DiscGap<0||p.DiscGap>20)r.Error("Междисковый зазор: от 0 до 20 мм.","DiscGap");
   if(p.InputRpm<.1||p.InputRpm>3000)r.Error("Скорость входа для анимации: от 0,1 до 3000 об/мин.","InputRpm");
   if(p.MotorFrame<0||p.MotorFrame>200||p.MotorMount<0||p.MotorMount>180||p.MotorPilot<0||p.MotorPilot>150||p.MotorHole<0||p.MotorHole>20||p.MotorLength<0||p.MotorLength>300)r.Error("Некорректные размеры моторного интерфейса.","MotorFrame","MotorMount","MotorPilot","MotorHole","MotorLength");
   if(p.MotorFrame>0&&(p.MotorMount+p.MotorHole>=p.MotorFrame||p.MotorPilot>=p.MotorFrame))r.Error("Крепёж/центрирующий бурт не помещаются на торце мотора.","MotorMount","MotorHole","MotorPilot","MotorFrame");
   if(p.BearingSimulation){
    double pitch=(p.Bore+p.BearingInner)/2;
    if(p.BearingInner<=0||p.Bore<=p.BearingInner||p.BearingWidth<=0||p.BallDiameter<=0||p.BallCount<3||p.BallCount>30)r.Error("Проверьте размеры колец, ширину и число шариков подшипника.","Bore","BearingInner","BearingWidth","BallDiameter","BallCount");
    else if(p.BallDiameter>=(p.Bore-p.BearingInner)/2||p.BallDiameter>=pitch*Math.Sin(Math.PI/p.BallCount))r.Error("Шарики не помещаются между кольцами или пересекаются. Уменьшите диаметр/число шариков.","BallDiameter","BallCount","BearingInner","Bore");
    if(p.Kind==0&&p.Shaft+2*p.Eccentric>=p.BearingInner)r.Error("Вал и эксцентриситет не помещаются в выбранной шейке подшипника. Увеличьте внутренний Ø либо уменьшите вал/e.","BearingInner","Shaft","Eccentric");
    if(p.Kind!=0&&Math.Abs(p.BearingInner-p.Shaft)>.001)r.Error("В демонстрационной входной опоре внутренний Ø подшипника должен совпадать с валом.","BearingInner","Shaft");
   }
   if(p.Kind==1){if(p.OutputShaft>=p.Module*(r.Z2-2.5))r.Error("Выходной вал не помещается в ведомой шестерне.","OutputShaft","Module");r.Gear1=Involute(r.Z1,p.Module,false);r.Gear2=Involute(r.Z2,p.Module,false);}
   if(p.Kind==2){if(p.OutputShaft>=p.Pitch*r.Z2/Math.PI)r.Error("Выходной вал не помещается в ведомом шкиве.","OutputShaft","Pitch");r.BeltPath=BeltPath(r);}
   if(p.StageGap<0||p.StageGap>50||p.IntermediateShaft<1||p.IntermediateShaft>100)r.Error("Зазор ступеней: 0–50 мм; промежуточный вал: 1–100 мм.","StageGap","IntermediateShaft");
   if(p.Kind==3){
    if(p.PlanetPin<=0||p.PlanetPin>=p.Module*(r.Zp-2.5))r.Error("Ось сателлита не помещается в корневом круге.","PlanetPin","Module");
    r.Gear1=Involute(r.Z1,p.Module,false);r.Gear2=Involute(r.Z2,p.Module,true);r.PlanetGear=Involute(r.Zp,p.Module,false);
    foreach(var stage in r.Stages){stage.Gear1=Involute(stage.Z1,p.Module,false);stage.Gear2=Involute(stage.Z2,p.Module,true);stage.PlanetGear=Involute(stage.Zp,p.Module,false);double[] sb=Bounds(stage);stage.Width=sb[1]-sb[0];stage.Height=sb[3]-sb[2];}
   }
   double[] bounds=Bounds(r);r.Width=bounds[1]-bounds[0];r.Height=bounds[3]-bounds[2];r.Depth=(p.Kind==0?Math.Max(p.Thickness,p.BearingSimulation?p.BearingWidth:0)*p.Discs+(p.Discs-1)*p.DiscGap:Math.Max(p.Thickness,p.BearingSimulation?p.BearingWidth:0))+2*p.Wall;
   if(p.Kind==3&&r.Stages.Count>1)r.Depth=r.Stages.Count*Math.Max(p.Thickness,p.BearingSimulation?p.BearingWidth:0)+(r.Stages.Count-1)*p.StageGap+2*p.Wall;
   if(p.Overall>0&&Math.Max(r.Width,r.Height)>p.Overall+.025)r.Error("Узел не помещается: "+F(r.Width)+" × "+F(r.Height)+" мм при ограничении "+F(p.Overall)+" мм. Измените габарит, отношение или детали.","Overall");
   if((p.Kind==0&&p.CycloidLayout==0||p.Kind==3)&&p.MotorMount>0&&Math.Sqrt(2)*(p.MotorMount+p.MotorHole)+2*p.Wall>r.Width+.025)r.Error("Монтажные отверстия мотора не помещаются внутри круглого корпуса с заданной стенкой.","Overall","MotorMount","MotorHole");
   r.Metric("Общий габарит в плоскости",F(r.Width)+" × "+F(r.Height)+" мм");r.Metric("Максимум по заданию",p.Overall>0?F(p.Overall)+" мм":"Не ограничен");r.Metric("Осевая толщина пакета, оценка",F(r.Depth)+" мм");r.Metric("Пакет + длина мотора, оценка",F(r.Depth+p.MotorLength)+" мм");r.Metric("Выход при заданной скорости",F(p.InputRpm/r.Actual)+" об/мин");
   if(p.MotorFrame>0){r.Metric("Моторный интерфейс",F(p.MotorFrame)+" мм; шаг отверстий "+F(p.MotorMount)+" мм; бурт Ø"+F(p.MotorPilot));r.Add("Моторный адаптер","Торец "+F(p.MotorFrame)+", шаг "+F(p.MotorMount)+", бурт Ø"+F(p.MotorPilot)+", 4 отверстия Ø"+F(p.MotorHole)+" мм","1","Размерный шаблон, длины крепежа по CAD");}
   if(p.BearingSimulation){r.Metric("Подшипник: d × D × B",F(p.BearingInner)+" × "+F(p.Bore)+" × "+F(p.BearingWidth)+" мм");r.Notes.Add("Подшипники: идеальное качение без скольжения, радиальный контакт 0°. Число/диаметр шариков — параметры модели, не паспортная внутренняя геометрия конкретной марки.");if(p.Kind==0){r.Items[6].Size="d="+F(p.BearingInner)+", D="+F(p.Bore)+", B="+F(p.BearingWidth)+" мм; марку и посадки проверить";}else r.Add("Входной подшипник для модели","d="+F(p.BearingInner)+", D="+F(p.Bore)+", B="+F(p.BearingWidth)+" мм","—","Размер демонстрационной опоры; не добавлять повторно к BOM опор");}
   r.Notes.Add("Общий размер ограничивает максимум ширины/высоты в плоскости. Осевая толщина — оценка рабочего пакета с двумя условными крышками, без полного CAD опор, муфт и крепежа.");
   if(p.MotorPreset>0)r.Notes.Add("Набор "+PresetNames[p.MotorPreset]+": геометрическая стартовая компоновка. Габарит, ролики, материал и допустимый момент не следуют из типоразмера NEMA; моторный торец сверить с паспортом.");
   if(p.Kind==0&&p.CycloidLayout==1){r.Depth=MicroPreset.BodyHeight(p);r.Metric("Micro: номинальный пакет без адаптера и мотора",F(r.Depth)+" мм");r.Metric("Micro: входная шейка / моторный вал",F(p.MicroJournal)+" / "+F(p.Shaft)+" мм");r.Items[6].Size="d="+F(p.BearingInner)+", D="+F(p.Bore)+", B="+F(p.BearingWidth)+" мм";r.Items[7].Size=F(p.MicroJournal)+" × "+F(p.ModelInputBearingD)+" × "+F(p.ModelInputBearingB)+" мм";r.Items[8].Size=F(p.ModelCaseInner)+" × "+F(p.ModelCaseOuter)+" × "+F(p.ModelCaseBearingB)+" мм";r.Notes.Add("Micro — собственная параметрическая компоновка по открытому референсу "+MicroPreset.Source+". Публичные опоры и отношение приняты как ориентир; внутренние размеры назначены, характеристики оригинала не переносятся.");}
   return r;
  }
  public static double[] Bounds(Result r){
   var p=r.P;if(p.Kind==0&&p.CycloidLayout==1){double size=p.Radius+p.Roller/2+p.Wall;return new[]{-size,size,-size,size};}double motor=p.MotorFrame/2,bearing=p.BearingSimulation?p.Bore/2:0,w=p.Wall,left,right,half;
   if(p.Kind==0){double radius=Math.Max(p.Radius+p.Roller/2,Math.Max(motor,bearing));return new[]{-radius-w,radius+w,-radius-w,radius+w};}
   if(p.Kind==3){double radius=Math.Max(p.Module*(r.Z2+2.5)/2,Math.Max(motor,bearing));return new[]{-radius-w,radius+w,-radius-w,radius+w};}
   double u=p.Kind==1?p.Module:p.Pitch/Math.PI,a=p.Kind==1?r.Spacing:p.Center,ra=u*r.Z1/2+(p.Kind==1?p.Module:0),rb=u*r.Z2/2+(p.Kind==1?p.Module:0);left=-Math.Max(ra,Math.Max(motor,bearing))-w;right=a+rb+w;half=Math.Max(rb,Math.Max(ra,Math.Max(motor,bearing)))+w;return new[]{left,right,-half,half};
  }
  public static bool FitOverall(Parameters p){
   if(p.Overall<=0)return p.Kind==0?AutoSize(p):true;
   var c=Clone(p);
   if(c.Kind==0){
    if(!Integer(c.Ratio)||c.Ratio<(c.RingOutput?3:2)||c.Ratio>120)return false;
    c.Radius=Math.Floor((c.Overall-c.Roller-2*c.Wall)/2*100)/100;if(c.Radius<5||c.Radius>1000)return false;
    double room=((c.BearingSimulation?c.BearingInner:c.Bore)-(c.CycloidLayout==1?c.MicroJournal:c.Shaft))/2-.1;
    double start=Math.Min(room,Math.Min(2,.5*c.Radius/PinCount(c)));if(start<.01)return false;
    for(int k=0;k<32;k++){
     c.Eccentric=Math.Max(.01,Math.Floor(start*Math.Pow(.88,k)*100)/100);double hd=c.OutputPin+2*c.Eccentric+2*c.Clearance;
     double lo=Math.Max(c.Bore/2+hd/2+c.Web,(hd+c.Web)/(2*Math.Sin(Math.PI/c.OutputPins))),hi=Math.Min(500,c.Radius-c.Eccentric-c.Roller/2-c.Clearance-hd/2-c.Web);
     if(lo>hi)continue;c.OutputRadius=Math.Round((lo+hi)/2,2);var test=Calculate(c);if(test.Valid){p.Radius=c.Radius;p.Eccentric=c.Eccentric;p.OutputRadius=c.OutputRadius;return true;}
    }return false;
   }
   var baseline=CalculateCore(c);if(baseline.Z2<=0)return false;
   if(c.Kind==2){double a=c.Pitch*baseline.Z1/Math.PI/2,b=c.Pitch*baseline.Z2/Math.PI/2;c.Center=Math.Floor((c.Overall-2*c.Wall-Math.Max(a,c.MotorFrame/2)-b)*100)/100;if(c.Center<5||c.Center>2000)return false;}
   else{
    double lo=.1,hi=10;c.Module=lo;var small=CalculateCore(c);if(small.Z2<=0)return false;var limits=Bounds(small);if(Math.Max(limits[1]-limits[0],limits[3]-limits[2])>c.Overall+.001)return false;
    for(int k=0;k<40;k++){double mid=(lo+hi)/2;c.Module=mid;var t=CalculateCore(c);double[] b=Bounds(t);if(Math.Max(b[1]-b[0],b[3]-b[2])<=c.Overall)lo=mid;else hi=mid;}
    c.Module=Math.Floor(lo*10000)/10000;
   }
   if(!Calculate(c).Valid)return false;p.Module=c.Module;p.Center=c.Center;return true;
  }
  public static PointF[] Involute(int z,double module,bool inner){
   if(z<3)return new PointF[0];var points=new List<PointF>();double rp=module*z/2,rb=rp*Math.Cos(20*Math.PI/180),tip=inner?rp-module:rp+module,root=inner?rp+1.25*module:Math.Max(.1,rp-1.25*module),invP=Inv(rp,rb),pitch=Tau/z;
   Func<double,double> half=(radius)=>Math.PI/(2*z)+(inner?-1:1)*(invP-Inv(radius,rb));
   for(int tooth=0;tooth<z;tooth++){
    double center=tooth*pitch,from=inner?root:Math.Max(root,rb),end=tip;
    if(!inner&&root<rb)points.Add(Polar(root,center-half(from)));
    for(int k=0;k<=7;k++){double radius=from+(end-from)*k/7;points.Add(Polar(radius,center-half(radius)));}
    for(int k=1;k<=4;k++)points.Add(Polar(tip,center-half(tip)+2*half(tip)*k/4));
    for(int k=6;k>=0;k--){double radius=from+(end-from)*k/7;points.Add(Polar(radius,center+half(radius)));}
    if(!inner&&root<rb)points.Add(Polar(root,center+half(from)));
    double rootHalf=half(from);for(int k=1;k<=3;k++)points.Add(Polar(root,center+rootHalf+(pitch-2*rootHalf)*k/3));
   }return points.ToArray();
  }
  static double Inv(double r,double rb){if(r<=rb)return 0;double a=Math.Sqrt(r*r/(rb*rb)-1);return a-Math.Atan(a);}
  public static PointF Polar(double radius,double angle){return new PointF((float)(radius*Math.Cos(angle)),(float)(radius*Math.Sin(angle)));}
  public static PointF[] Transform(PointF[] input,double x,double y,double angle){double c=Math.Cos(angle),s=Math.Sin(angle);return input.Select(v=>new PointF((float)(x+v.X*c-v.Y*s),(float)(y+v.X*s+v.Y*c))).ToArray();}
  public static PointF[] BeltPath(Result r){var p=r.P;double r1=p.Pitch*r.Z1/(2*Math.PI),r2=p.Pitch*r.Z2/(2*Math.PI),c=p.Center;if(c<=Math.Abs(r2-r1))return new PointF[0];double a=Math.Acos((r1-r2)/c);var list=new List<PointF>();list.Add(Polar(r1,a));
   for(int j=0;j<=100;j++){double t=a-2*a*j/100;var v=Polar(r2,t);list.Add(new PointF((float)(c+v.X),v.Y));}
   for(int j=0;j<=100;j++){double t=-a-(Tau-2*a)*j/100;list.Add(Polar(r1,t));}return list.ToArray();}
 }
}
