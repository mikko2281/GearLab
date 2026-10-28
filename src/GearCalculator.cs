using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace GearLab {
 public class Parameters {
  public int Kind {get;set;} public bool RingOutput {get;set;} public double Ratio {get;set;} public double Radius {get;set;}
  public double Roller {get;set;} public double Eccentric {get;set;} public double Clearance {get;set;}
  public double Bore {get;set;} public double Shaft {get;set;} public double Thickness {get;set;}
  public int Discs {get;set;} public int OutputPins {get;set;} public double OutputPin {get;set;}
  public double OutputRadius {get;set;} public double Web {get;set;} public int Segments {get;set;}
  public int Teeth {get;set;} public double Module {get;set;} public double Pitch {get;set;}
  public double Center {get;set;} public int Planets {get;set;}
  public double Overall {get;set;} public double Wall {get;set;} public int MotorPreset {get;set;}
  public double MotorFrame {get;set;} public double MotorMount {get;set;} public double MotorPilot {get;set;} public double MotorHole {get;set;} public double MotorLength {get;set;}
  public double OutputShaft {get;set;} public double DiscGap {get;set;} public double PlanetPin {get;set;}
  public bool BearingSimulation {get;set;} public double BearingInner {get;set;} public double BearingWidth {get;set;} public double BallDiameter {get;set;} public int BallCount {get;set;}
  public double InputRpm {get;set;} public int PlanetStages {get;set;} public double StageGap {get;set;} public double IntermediateShaft {get;set;} public double RatioTolerance {get;set;} public bool AutoFit {get;set;} public int FormatVersion {get;set;}
  public int CycloidLayout {get;set;} public double MicroFrontExtension {get;set;} public double MicroJournal {get;set;} public double MicroRollerLength {get;set;} public double MicroAdapterHeight {get;set;} public bool IntegratedRing {get;set;} public double RingBridge {get;set;} public int ExportScope {get;set;} public double ModelCover {get;set;} public double ModelFlange {get;set;} public double ModelGap {get;set;} public double ModelBolt {get;set;} public int ModelBoltCount {get;set;} public double ModelShaftExtension {get;set;} public double ModelInputBearingD {get;set;} public double ModelInputBearingB {get;set;} public double ModelCaseInner {get;set;} public double ModelCaseOuter {get;set;} public double ModelCaseBearingB {get;set;} public double ModelRollerAxle {get;set;} public double ModelBeltThickness {get;set;} public double ModelBushWall {get;set;}
  public Parameters(){MicroFrontExtension=.5;MicroJournal=7;MicroRollerLength=10;MicroAdapterHeight=5;Ratio=20;Radius=35;Roller=4;Eccentric=.9;Clearance=.05;Bore=22;Shaft=5;Thickness=6;Discs=1;OutputPins=6;OutputPin=5;OutputRadius=17;Web=1.5;Segments=2400;Teeth=20;Module=1;Pitch=2;Center=80;Planets=3;Wall=3;OutputShaft=8;DiscGap=1;PlanetPin=4;BearingInner=8;BearingWidth=7;BallDiameter=3.5;BallCount=7;InputRpm=30;StageGap=3;IntermediateShaft=8;RatioTolerance=1;FormatVersion=1;ModelCover=4;ModelFlange=6;ModelGap=.5;ModelBolt=3;ModelBoltCount=6;ModelShaftExtension=8;ModelInputBearingD=16;ModelInputBearingB=5;ModelCaseBearingB=7;ModelRollerAxle=2;ModelBeltThickness=1.3;ModelBushWall=1.5;}
 }
 public class Item {
  public string Name,Size,Qty,Status;
  public Item(string n,string s,string q,string st){Name=n;Size=s;Qty=q;Status=st;}
 }
 public class Result {
  public Parameters P; public int Pins,Lobes,Z1,Z2,Zp; public double Actual,Spacing,Hole,Envelope,OutRadius;
  public PointF[] Profile; public List<string> Errors=new List<string>(),Notes=new List<string>();
  public List<Item> Items=new List<Item>(); public List<string[]> Metrics=new List<string[]>();
  public HashSet<string> BadFields=new HashSet<string>();
  public PointF[] Gear1,Gear2,PlanetGear,BeltPath;public double Width,Height,Depth;public List<Result> Stages=new List<Result>();public CadAssembly Model;
  public bool Valid {get{return Errors.Count==0;}}
  public void Metric(string a,string b){Metrics.Add(new[]{a,b});}
  public void Add(string n,string s,string q,string status){Items.Add(new Item(n,s,q,status));}
  public void Error(string message,params string[] fields){Errors.Add(message);foreach(string f in fields)BadFields.Add(f);}
 }
 public static partial class Engine {
  public static string F(double v){return v.ToString("0.###",CultureInfo.GetCultureInfo("ru-RU"));}
  public static string N(double v){return v.ToString("0.######",CultureInfo.InvariantCulture);}
  public static double Tau=2*Math.PI;
  static bool Integer(double x){return Math.Abs(x-Math.Round(x))<1e-8;}
  public static Parameters Clone(Parameters p){var j=new JavaScriptSerializer();return j.Deserialize<Parameters>(j.Serialize(p));}
  public static int PinCount(Parameters p){return (int)Math.Round(p.Ratio)+(p.RingOutput?0:1);}
  public static double RingAngle(Parameters p,double input){return p.RingOutput?input/PinCount(p):0;}
  public static double DiscAngle(Parameters p,double input){return p.RingOutput?0:-input/(PinCount(p)-1);}
  public static string Scheme(Parameters p){return p.RingOutput?"Выход — наружное кольцо; центральный фланец закреплён":"Выход — центральный фланец; наружное кольцо закреплено";}
  public static bool AutoSize(Parameters p){
   if(p==null||!Integer(p.Ratio)||p.Ratio<(p.RingOutput?3:2)||p.Ratio>120||!Finite(p.Ratio))return false;
   int n=PinCount(p);double room=((p.BearingSimulation?p.BearingInner:p.Bore)-p.Shaft)/2-.1;
   if(room<.01||p.Roller<=0||p.OutputPins<3||p.OutputPins>16)return false;
   double radius=Math.Max(24,(p.Roller+Math.Max(.5,2*p.Clearance))/(2*Math.Sin(Math.PI/n))*1.03);
   for(int attempt=0;attempt<150&&radius<=1000;attempt++){
    var c=Clone(p);c.Radius=Math.Ceiling(radius*100)/100;
    c.Eccentric=Math.Max(.01,Math.Floor(Math.Min(room,Math.Min(2,.5*c.Radius/n))*100)/100);
    double hd=c.OutputPin+2*c.Eccentric+2*c.Clearance;
    double lower=Math.Max(c.Bore/2+hd/2+c.Web,(hd+c.Web)/(2*Math.Sin(Math.PI/c.OutputPins)));
    double upper=Math.Min(500,c.Radius-c.Eccentric-c.Roller/2-c.Clearance-hd/2-c.Web);
    if(lower<=upper){c.OutputRadius=Math.Round((lower+upper)/2,2);var test=Calculate(c);
     if(test.Valid){p.Radius=c.Radius;p.Eccentric=c.Eccentric;p.OutputRadius=c.OutputRadius;return true;}
    }
    radius=Math.Max(radius+1,radius*1.04);
   }
   return false;
  }
  public static PointF[] Profile(Parameters p){
   int n=PinCount(p),lobes=n-1,steps=((Math.Max(p.Segments,n*40)+lobes-1)/lobes)*lobes;
   var a=new PointF[steps];double rr=p.Roller/2+p.Clearance;
   for(int k=0;k<steps;k++){
    double t=Tau*k/steps,psi=Math.Atan2(Math.Sin((1-n)*t),p.Radius/(p.Eccentric*n)-Math.Cos((1-n)*t));
    a[k]=new PointF((float)(p.Radius*Math.Cos(t)-rr*Math.Cos(t+psi)-p.Eccentric*Math.Cos(n*t)),(float)(-p.Radius*Math.Sin(t)+rr*Math.Sin(t+psi)+p.Eccentric*Math.Sin(n*t)));
   }return a;
  }
  public static Result CalculateCore(Parameters input){
   var r=new Result{P=input??new Parameters()};
   if(input==null){r.Error("Отсутствуют параметры проекта.");return r;}
   foreach(var property in typeof(Parameters).GetProperties())if(property.PropertyType==typeof(double)&&!Finite((double)property.GetValue(input,null)))r.Error("Некорректное число в параметре "+property.Name+".",property.Name);
   if(!r.Valid)return r;
   var p=Clone(input);r.P=p;
   if(p.Kind<0||p.Kind>3)r.Error("Неизвестный тип передачи.");
   if(p.Ratio<1||p.Ratio>200)r.Error("Передаточное число должно быть от 1 до 200.","Ratio");
   if(p.Shaft<1||p.Shaft>100)r.Error("Диаметр входного вала: от 1 до 100 мм.","Shaft");
   if(p.Thickness<1||p.Thickness>100)r.Error("Толщина: от 1 до 100 мм.","Thickness");
   if(p.Kind==0){
    if(p.Radius<5||p.Radius>1000)r.Error("Радиус кольца: от 5 до 1000 мм.","Radius");
    if(p.Roller<.5||p.Roller>50)r.Error("Диаметр ролика: от 0,5 до 50 мм.","Roller");
    if(p.Eccentric<.01||p.Eccentric>20)r.Error("Эксцентриситет: от 0,01 до 20 мм.","Eccentric");
    if(p.Clearance<0||p.Clearance>2)r.Error("Зазор: от 0 до 2 мм.","Clearance");
    if(p.Bore<1||p.Bore>150)r.Error("Посадка подшипника: от 1 до 150 мм.","Bore");
    if(p.OutputRadius<1||p.OutputRadius>500)r.Error("Радиус пальцев: от 1 до 500 мм.","OutputRadius");
    if(p.OutputPin<1||p.OutputPin>30)r.Error("Диаметр пальца: от 1 до 30 мм.","OutputPin");
    if(p.Web<.1||p.Web>20)r.Error("Перемычка: от 0,1 до 20 мм.","Web");
    if(p.Segments<360||p.Segments>20000)r.Error("Число сегментов: от 360 до 20000.","Segments");
   }else{
    if(p.Teeth<8||p.Teeth>200)r.Error("Ведущие зубья: от 8 до 200.","Teeth");
    if(p.Kind!=2&&(p.Module<.1||p.Module>10))r.Error("Модуль: от 0,1 до 10 мм.","Module");
    if(p.Kind==2&&(p.Pitch<.5||p.Pitch>20||p.Center<5||p.Center>2000))r.Error("Проверьте шаг ремня (0,5–20 мм) и расстояние (5–2000 мм).","Pitch","Center");
    if(p.Kind==3&&(p.Planets<2||p.Planets>8))r.Error("Число сателлитов: от 2 до 8.","Planets");
   }
   if(!r.Valid)return r;
   if(p.Kind==0)Cycloidal(r);else if(p.Kind==3)Planetary(r);else Pair(r);
   return r;
  }
  static bool Finite(double value){return !double.IsNaN(value)&&!double.IsInfinity(value);}
  static void Cycloidal(Result r){
   var p=r.P;
   int minimum=p.RingOutput?3:2;
   if(!Integer(p.Ratio)||p.Ratio<minimum||p.Ratio>120){r.Errors.Add("Выбранная циклоидальная схема: целое i от "+minimum+" до 120. Для другого отношения нужна другая схема/каскад.");return;}
   if(p.Radius<=0||p.Roller<=0||p.Eccentric<=0||p.Clearance<0||p.Web<0||p.OutputRadius<=0||p.OutputPin<=0||p.OutputPins<3||p.OutputPins>16||p.Discs<1||p.Discs>2){r.Errors.Add("Проверьте положительные размеры, 3–16 выходных пальцев и 1–2 диска.");return;}
   r.Pins=PinCount(p);r.Lobes=r.Pins-1;r.Actual=p.Ratio;r.Spacing=2*p.Radius*Math.Sin(Math.PI/r.Pins);
   r.Hole=p.OutputPin+2*p.Eccentric+2*p.Clearance;r.Envelope=2*(p.Radius+p.Roller/2);r.OutRadius=p.OutputRadius;
   double k=p.Eccentric*r.Pins/p.Radius,rr=p.Roller/2+p.Clearance;
   if(r.Spacing<=p.Roller)r.Error("Ролики пересекаются: шаг "+F(r.Spacing)+" мм при Ø"+F(p.Roller)+" мм. Нужно R > "+F(p.Roller/(2*Math.Sin(Math.PI/r.Pins)))+" мм или Ø ролика < "+F(r.Spacing)+" мм.","Radius","Roller","Ratio");
   if(k>=1)r.Error("Эксцентриситет слишком велик: e·N/R="+F(k)+". Нужно e < "+F(p.Radius/r.Pins)+" мм или R > "+F(p.Eccentric*r.Pins)+" мм.","Eccentric","Radius","Ratio");
   if(k>=1)return;
   // Offset of the clockwise pin-centre curve along its inward (right) normal.
   // A nonpositive offset speed indicates a local cusp / undercut.
   double minSpeed=double.MaxValue;
   for(int j=0;j<Math.Max(1440,r.Pins*50);j++){
    double t=Tau*j/Math.Max(1440,r.Pins*50),n=r.Pins;
    double dx=-p.Radius*Math.Sin(t)+p.Eccentric*n*Math.Sin(n*t),dy=-p.Radius*Math.Cos(t)+p.Eccentric*n*Math.Cos(n*t);
    double ddx=-p.Radius*Math.Cos(t)+p.Eccentric*n*n*Math.Cos(n*t),ddy=p.Radius*Math.Sin(t)-p.Eccentric*n*n*Math.Sin(n*t);
    double v=Math.Sqrt(dx*dx+dy*dy),curvature=(dx*ddy-dy*ddx)/(v*v*v);
    minSpeed=Math.Min(minSpeed,1+rr*curvature);
   }
   if(minSpeed<=0)r.Error("Подрезание профиля: уменьшите Ø ролика/e или увеличьте R.","Roller","Eccentric","Radius");
   r.Profile=Profile(p);
   double minRad=r.Profile.Min(a=>Math.Sqrt(a.X*a.X+a.Y*a.Y));
   double centralWeb=minRad-p.Bore/2;
   if(centralWeb<p.Web)r.Error("Посадка подшипника выходит за профиль или не оставляет перемычку.","Bore","Radius","Web");
   double inner=p.OutputRadius-r.Hole/2-p.Bore/2,outer=minRad-p.OutputRadius-r.Hole/2;
   if(inner<p.Web||outer<p.Web)r.Error("Отверстия под пальцы не помещаются с перемычкой "+F(p.Web)+" мм. Измените радиус пальцев, отверстия или R.","OutputRadius","OutputPin","Bore","Radius");
   if(2*p.OutputRadius*Math.Sin(Math.PI/p.OutputPins)-r.Hole<p.Web)r.Error("Соседние отверстия под пальцы слишком близки. Увеличьте радиус пальцев или уменьшите их число/диаметр.","OutputPins","OutputRadius","OutputPin");
   if(p.Shaft/2+p.Eccentric>=p.Bore/2)r.Error("Вал с эксцентриситетом не помещается в посадке подшипника.","Shaft","Bore","Eccentric");
   r.Metric("Фактическое отношение",F(r.Actual)+":1");r.Metric("Лепестков на диске",r.Lobes.ToString());r.Metric("Роликов кольца",r.Pins.ToString());
   r.Metric("Шаг центров роликов",F(r.Spacing)+" мм");r.Metric("Ø отверстий под пальцы",F(r.Hole)+" мм");r.Metric("Габарит по роликам",F(r.Envelope)+" мм");
   r.Metric("e·N/R",F(k));r.Metric("Вращение выхода",p.RingOutput?"По направлению входа":"Против входа");r.Metric("Схема",Scheme(p));
   r.Add("Циклоидальный диск",r.Lobes+" лепестков; t="+F(p.Thickness)+" мм; посадка Ø"+F(p.Bore),p.Discs.ToString(),"По геометрии");
   r.Add(p.RingOutput?"Пальцы вращающегося кольца":"Пальцы неподвижного кольца",p.IntegratedRing?r.Pins+" встроенных зацепов на R="+F(p.Radius)+" мм":r.Pins+" позиций на R="+F(p.Radius)+" мм; оси/длины выбрать",p.IntegratedRing?"0 отдельно":r.Pins.ToString(),p.IntegratedRing?"В составе корпуса, без осей":"Число по геометрии");
   r.Add("Ролики / втулки кольца","Ø"+F(p.Roller)+" мм; один длинный ролик на палец, на весь пакет",p.IntegratedRing?"0 отдельно":r.Pins.ToString(),p.IntegratedRing?"Печатные зацепы объединены с корпусом":"Шаблон сборки");
   r.Add(p.RingOutput?"Неподвижные пальцы фланца":"Выходные пальцы","Ø"+F(p.OutputPin)+" мм, R="+F(p.OutputRadius)+" мм; длина по CAD",p.OutputPins.ToString(),"Число задано");
   r.Add("Отверстия в дисках","Ø"+F(r.Hole)+" мм, "+p.OutputPins+" на каждом диске",(p.OutputPins*p.Discs).ToString(),"Обработка, не покупка");
   r.Add("Эксцентриковый вал","e="+F(p.Eccentric)+" мм; вход Ø"+F(p.Shaft)+" мм","1","Шаблон сборки");
   r.Add("Подшипник эксцентрика","Наружный Ø"+F(p.Bore)+" мм; внутренний Ø и ширина не выбраны",p.Discs.ToString(),"1 на диск, допущение");
   r.Add("Опоры входного вала","Тип, размеры и нагрузки по CAD","2","Шаблон, не готовый BOM");
   r.Add(p.RingOutput?"Опоры вращающегося корпуса":"Опоры выходного фланца","Тип, размеры и нагрузки по CAD","2","Шаблон, не готовый BOM");
   r.Add(p.RingOutput?"Неподвижный центральный фланец":"Выходной фланец","Отверстия под "+p.OutputPins+" пальцев","1","Шаблон сборки");
   r.Add(p.RingOutput?"Вращающийся корпус с кольцом":"Корпус и крышки",p.RingOutput?"Выходной узел; опирается на неподвижную раму; крепление нагрузки по CAD":"Структура/толщина/крепёж ещё не выбраны",p.RingOutput?"1":"—","Нужен CAD");
   r.Add("Крепёж, шайбы, проставки","Диаметры и длины после компоновки","—","Нужен CAD");
   r.Notes.Add(p.RingOutput?"Центральный фланец и его пальцы закреплены на раме. Выход — наружное кольцо с роликами. i=N, диск имеет N−1 лепестков. Кольцо вращается в сторону входа.":"Кольцо закреплено; вход — эксцентрик; выход — пальцы. Разница: один зуб. i = N − 1.");
   if(p.RingOutput){r.Notes.Add("Диск не вращается вокруг собственной оси относительно рамы, но его центр движется по окружности радиуса e. Полностью неподвижный диск не работает с этой схемой.");r.Notes.Add("Мотор и центральная рама закреплены. Вращающийся наружный узел нужен на собственных опорах; детали рамы не должны быть жёстко соединены с вращающимся корпусом.");}
   r.Notes.Add("Ø отверстия = Ø выходного пальца + 2e + 2c. c — радиальный зазор; в профиле использован радиус ролика r+c.");
   r.Notes.Add(p.Discs==2?"Два диска: эксцентрики разнесены на 180°, профиль B повёрнут на −180°/(N−1) относительно A при общем направлении отверстий. Это одна ступень; отношение не перемножается.":"Один циклоидальный диск. Второй можно включить параметром «Количество дисков».");
   r.Notes.Add("Ролики считаются общими для 1–2 дисков. Если ролики раздельные по дискам, их количество нужно умножить на число дисков.");
   r.Notes.Add("Проверки перемычек консервативные: используются минимальный радиус профиля и заданная толщина перемычки.");
  }
  static void Pair(Result r){
   var p=r.P;if(p.Module<=0||p.Pitch<=0||p.Center<=0){r.Errors.Add("Модуль, шаг и межосевое расстояние должны быть положительными.");return;}
   r.Z1=p.Teeth;r.Z2=(int)Math.Round(p.Ratio*p.Teeth,MidpointRounding.AwayFromZero);
   if(r.Z2<8||r.Z2>2000){r.Errors.Add("Получилось недопустимое число зубьев ведомого колеса (8–2000). Измените i или число ведущих зубьев.");return;}
   r.Actual=(double)r.Z2/r.Z1;
   double d1=p.Kind==1?p.Module*r.Z1:p.Pitch*r.Z1/Math.PI,d2=p.Kind==1?p.Module*r.Z2:p.Pitch*r.Z2/Math.PI;
   if(p.Shaft>=(p.Kind==1?p.Module*(r.Z1-2.5):d1))r.Error("Входной вал не помещается в ведущем колесе. Уменьшите вал или увеличьте колесо.","Shaft",p.Kind==1?"Module":"Pitch","Teeth");
   r.Envelope=d1+d2;r.Spacing=(d1+d2)/2;
   r.Metric("Фактическое отношение",F(r.Actual)+":1");r.Metric("Отклонение от запроса",F((r.Actual/p.Ratio-1)*100)+" %");
   r.Metric("Ведущих / ведомых зубьев",r.Z1+" / "+r.Z2);r.Metric("Делительные Ø",F(d1)+" / "+F(d2)+" мм");
   if(p.Kind==1){
    r.Metric("Межосевое расстояние",F(r.Spacing)+" мм");r.Metric("Наружные Ø",F(d1+2*p.Module)+" / "+F(d2+2*p.Module)+" мм");
    r.Add("Ведущая шестерня","z="+r.Z1+", m="+F(p.Module)+", угол 20°","1","Геометрия, без сдвига");
    r.Add("Ведомая шестерня","z="+r.Z2+", m="+F(p.Module)+", угол 20°","1","Геометрия, без сдвига");
    if(r.Z1<17)r.Notes.Add("Малое число зубьев: проверьте подрезание стандартного профиля 20° и необходимость коррекции.");
    r.Notes.Add("i = z2/z1; d=mz; a=m(z1+z2)/2. Внешняя пара вращает выход в противоположную сторону.");
   }else{
    double c=p.Center,delta=(d2-d1)/2;
    if(c<=(d1+d2)/2)r.Errors.Add("Шкивы пересекаются по делительным окружностям. Увеличьте межосевое расстояние.");
    if(c<=Math.Abs(delta)){r.Errors.Add("Невозможна внешняя касательная ремня.");return;}
    double beta=Math.Asin(delta/c),length=2*Math.Sqrt(c*c-delta*delta)+Math.PI*(d1+d2)/2+2*delta*beta;
    double beltTeeth=Math.Round(length/p.Pitch);
    r.Metric("Длина по делительной линии",F(length)+" мм");r.Metric("Расчётные зубья ремня",F(length/p.Pitch));r.Metric("Ближайший целый ремень",F(beltTeeth)+" зуб.; проверить каталог");
    r.Add("Ведущий зубчатый шкив","z="+r.Z1+", шаг "+F(p.Pitch)+" мм; профиль и ширину выбрать","1","Кандидат");
    r.Add("Ведомый зубчатый шкив","z="+r.Z2+", шаг "+F(p.Pitch)+" мм; профиль и ширину выбрать","1","Кандидат");
    r.Add("Зубчатый ремень","Расчёт L="+F(length)+" мм; около "+F(beltTeeth)+" зубьев","1","Длина не подтверждена каталогом");
    r.Notes.Add("Расчёт открытого ремня по делительным окружностям. Округлённая длина требует нового межосевого расстояния/натяжителя и проверки каталога.");
   }
   r.Add("Валы","Посадки, диаметры и длины по CAD","2","Шаблон сборки");r.Add("Подшипники валов","Условно по 2 опоры на вал; тип и нагрузка неизвестны","4","Шаблон, не готовый BOM");
   r.Add("Корпус, крепёж, натяжитель","Состав и количество по CAD","—","Не рассчитано");
   r.Notes.Add("Зубья ведомого округлены до целого: ориентируйтесь на фактическое отношение. Чертёж — схема, не производственный профиль зубьев.");
  }
  static void PlanetarySingle(Result r){
   var p=r.P;if(p.Ratio<=2||p.Module<=0){r.Errors.Add("Для этой планетарной схемы i>2, модуль>0. Вход — солнце, кольцо неподвижно, выход — водило.");return;}
   r.Z1=p.Teeth;int approx=(int)Math.Round((p.Ratio-1)*r.Z1);
   int best=0;double err=double.MaxValue;
   for(int zr=Math.Max(r.Z1+16,approx-120);zr<=Math.Min(2000,approx+120);zr++){
    if((zr-r.Z1)%2!=0||(zr+r.Z1)%p.Planets!=0)continue;
    int zp=(zr-r.Z1)/2;
    if(zp<8||(r.Z1+zp)*Math.Sin(Math.PI/p.Planets)<=zp+2)continue;
    double x=Math.Abs(1+(double)zr/r.Z1-p.Ratio);if(x<err){err=x;best=zr;}
   }
   if(best==0){r.Errors.Add("В выбранном диапазоне не найдено сочетание зубьев с равномерными сателлитами. Измените i, число зубьев солнца или число сателлитов.");return;}
   r.Z2=best;r.Zp=(r.Z2-r.Z1)/2;r.Actual=1+(double)r.Z2/r.Z1;r.Envelope=p.Module*r.Z2;
   if(p.Shaft>=p.Module*(r.Z1-2.5))r.Error("Входной вал не помещается в солнечной шестерне.","Shaft","Module","Teeth");
   r.Metric("Фактическое отношение",F(r.Actual)+":1");r.Metric("Отклонение от запроса",F((r.Actual/p.Ratio-1)*100)+" %");
   r.Metric("Солнце / сателлит / кольцо",r.Z1+" / "+r.Zp+" / "+r.Z2);r.Metric("Сателлитов",p.Planets.ToString());
   r.Metric("Делительный Ø кольца",F(r.Envelope)+" мм");r.Metric("Радиус осей сателлитов",F(p.Module*(r.Z1+r.Zp)/2)+" мм");
   r.Add("Солнечная шестерня","z="+r.Z1+", m="+F(p.Module),"1","По геометрии");r.Add("Сателлит","z="+r.Zp+", m="+F(p.Module),p.Planets.ToString(),"По геометрии");
   r.Add("Неподвижное внутреннее колесо","z="+r.Z2+", m="+F(p.Module),"1","По геометрии");r.Add("Оси сателлитов","Диаметры и длины по нагрузкам",p.Planets.ToString(),"Шаблон сборки");
   r.Add("Подшипники сателлитов","Условно один на сателлит; конкретная опора не выбрана",p.Planets.ToString(),"Шаблон сборки");
   r.Add("Водило","Компоновка/посадки по CAD","1","Шаблон сборки");r.Add("Опоры входа и выхода","Условно по 2 на каждый вал","4","Шаблон сборки");
   r.Add("Корпус и крепёж","Размеры и количество неизвестны","—","Нужен CAD");
   r.Notes.Add("Проверены Zr=Zs+2Zp, целое (Zs+Zr)/число сателлитов и отсутствие пересечения соседних сателлитов по окружностям вершин.");
   r.Notes.Add("i=1+Zr/Zs. Кольцо неподвижно, солнце — вход, водило — выход. Подбор ближайшего отношения в ±120 зубьях от запроса.");
   r.Notes.Add("Не проверены интерференция внутреннего зацепления и прочность. Показаны эвольвентные боковые поверхности 20° с упрощённым корнем без галтели; это компоновочный эскиз.");
   if(r.Z1<17||r.Zp<17)r.Notes.Add("Число зубьев солнца/сателлита меньше 17: необходима проверка подрезания и коррекции профиля 20°.");
  }
 }
 public static class Files {
  static string Q(string s){return "\""+s.Replace("\"","\"\"")+"\"";}
  public static void Csv(Result r,string file){var b=new StringBuilder();b.AppendLine("Деталь;Размер / описание;Количество;Основание");
   b.AppendLine(Q("Запрос i="+Engine.F(r.P.Ratio))+";"+Q("Фактическое i="+Engine.F(r.Actual))+";;"+Q(r.Valid?"Предварительная геометрия":"Есть ошибки геометрии"));
   foreach(var x in r.Items)b.AppendLine(string.Join(";",new[]{Q(x.Name),Q(x.Size),Q(x.Qty),Q(x.Status)}));
   foreach(var x in r.Metrics)b.AppendLine(Q("Параметр: "+x[0])+";"+Q(x[1])+";;");
   foreach(var x in r.Errors)b.AppendLine(Q("ОШИБКА")+";"+Q(x)+";;");
   foreach(var x in r.Notes)b.AppendLine(Q("Допущение")+";"+Q(x)+";;");
   b.AppendLine(Q("Перед заказом")+";"+Q("Прочность, нагрузки, материалы, маркировки подшипников, крепёж и длины по CAD не подтверждены.")+";;");
   ReleaseSupport.WriteText(file,b.ToString(),new UTF8Encoding(true));
  }
  public static void Svg(Result r,string file){DrawingExport.Svg(r,file);}
  public static void Dxf(Result r,string file){DrawingExport.Dxf(r,file);}

 }
 static class Program {
  [STAThread] static int Main(string[] args){
   if(args.Length>0&&args[0]=="--placement-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return PlacementTests.Run(args.Length>1?args[1]:Path.GetTempPath());}
   if(args.Length>0&&args[0]=="--preset-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return PresetTransitionTests.Run(args.Length>1?args[1]:Path.GetTempPath());}
   if(args.Length>0&&args[0]=="--micro-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return MicroTests.Run(args.Length>1?args[1]:Path.GetTempPath());}
   if(args.Length>0&&args[0]=="--visibility-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return VisibilityTests.Run(args.Length>1?args[1]:Path.GetTempPath());}
   if(args.Length>0&&args[0]=="--ui-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return PrintTests.Ui(args.Length>1?args[1]:Path.GetTempPath());}
   if(args.Length>0&&args[0]=="--print-test")return PrintTests.Run(args.Length>1?args[1]:Path.GetTempPath());
   if(args.Length>0&&args[0]=="--viewer-test")return ViewerTests.Run(args.Length>1?args[1]:Path.GetTempPath());
   if(args.Length>0&&args[0]=="--cad-test")return CadTests.Run(args.Length>1?args[1]:Path.GetTempPath());
   if(args.Length>0&&args[0]=="--self-test"){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);return Tests.Run(args.Length>1?args[1]:Path.GetTempPath());}
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(sender,e)=>ReleaseSupport.Report(e.Exception);AppDomain.CurrentDomain.UnhandledException+=(sender,e)=>ReleaseSupport.Report(e.ExceptionObject as Exception);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);try{var form=new MainForm();if(args.Length>0)form.LoadProjectFile(args[0]);if(args.Length>1&&args[1]=="--3d")form.ShowFullModel();Application.Run(form);return 0;}catch(Exception ex){ReleaseSupport.Report(ex);return 1;}
  }
 }
 static class Tests {
  static void Assert(bool x,string s){if(!x)throw new Exception(s);}
  public static int Run(string folder){try{
   Directory.CreateDirectory(folder);var report=new StringBuilder();
   foreach(int i in new[]{8,9,20,30,60,120}){
    var p=new Parameters{Ratio=i};Engine.AutoSize(p);var r=Engine.Calculate(p);Assert(r.Valid,"Auto size "+i+": "+string.Join(";",r.Errors));Assert(r.Pins==i+1&&r.Lobes==i,"Ratio identity");
    Assert(Math.Abs(r.Hole-(p.OutputPin+2*p.Eccentric+2*p.Clearance))<1e-10,"Output hole");Assert(r.Profile.Length>=r.Pins*40,"Sampling");
    // Verify every sampled profile point remains outside every fixed ring roller in the assembled pose.
    foreach(double phase in new[]{0.0,.5,1.7,3.2})foreach(var v in r.Profile)for(int k=0;k<r.Pins;k++){double a=Engine.Tau*k/r.Pins,rot=-phase/r.Lobes,dx=p.Eccentric*Math.Cos(phase)+v.X*Math.Cos(rot)-v.Y*Math.Sin(rot)-p.Radius*Math.Cos(a),dy=p.Eccentric*Math.Sin(phase)+v.X*Math.Sin(rot)+v.Y*Math.Cos(rot)-p.Radius*Math.Sin(a);Assert(Math.Sqrt(dx*dx+dy*dy)>=p.Roller/2-0.002,"Ring collision "+i);}
    File.WriteAllText(Path.Combine(folder,"Пример_"+i+"к1.gear.json"),new JavaScriptSerializer().Serialize(p),Encoding.UTF8);
    if(i==20){Files.Dxf(r,Path.Combine(folder,"test-profile.dxf"));Files.Svg(r,Path.Combine(folder,"test-profile.svg"));Files.Csv(r,Path.Combine(folder,"test-bom.csv"));}
    report.AppendLine(i+":1: "+r.Pins+" pins; valid; R="+Engine.N(p.Radius));
   }
   foreach(int i in new[]{8,20,30,60,120}){
    var p=new Parameters{Ratio=i,RingOutput=true};Engine.AutoSize(p);var r=Engine.Calculate(p);
    Assert(r.Valid,"Ring-output auto-size "+i+": "+string.Join(";",r.Errors));Assert(r.Pins==i&&r.Lobes==i-1,"Ring-output counts");
    foreach(double phase in new[]{0.0,.5,1.7,3.2}){
     double ra=Engine.RingAngle(p,phase),da=Engine.DiscAngle(p,phase);
     Assert(da==0,"Fixed disc orientation");Assert(Math.Abs(i*ra-phase)<1e-10,"Ring output speed");
     Assert(Math.Abs((r.Lobes)*(da-phase)-r.Pins*(ra-phase))<1e-9,"Willis kinematic constraint");
     foreach(var v in r.Profile)for(int k=0;k<r.Pins;k++){
      double a=Engine.Tau*k/r.Pins+ra,dx=p.Eccentric*Math.Cos(phase)+v.X-p.Radius*Math.Cos(a),dy=p.Eccentric*Math.Sin(phase)+v.Y-p.Radius*Math.Sin(a);
      Assert(Math.Sqrt(dx*dx+dy*dy)>=p.Roller/2-.002,"Moving ring / disc collision "+i);
     }
     for(int h=0;h<p.OutputPins;h++){
      double t=Engine.Tau*h/p.OutputPins,x=p.OutputRadius*Math.Cos(t),y=p.OutputRadius*Math.Sin(t);
      double holeX=x+p.Eccentric*Math.Cos(phase),holeY=y+p.Eccentric*Math.Sin(phase),offset=Math.Sqrt((holeX-x)*(holeX-x)+(holeY-y)*(holeY-y));
      Assert(offset+p.OutputPin/2<=r.Hole/2+1e-9,"Fixed carrier pin / disc hole collision");
     }
    }
    Assert(r.Items[1].Name.Contains("вращающегося")&&r.Items[3].Name.Contains("Неподвижные"),"Ring-output BOM roles");
    var json=new JavaScriptSerializer();Assert(json.Deserialize<Parameters>(json.Serialize(p)).RingOutput,"Ring-output project roundtrip");
    File.WriteAllText(Path.Combine(folder,"Наружное_кольцо_"+i+"к1.gear.json"),json.Serialize(p),Encoding.UTF8);
    if(i==20){Files.Csv(r,Path.Combine(folder,"ring-output-bom.csv"));Files.Svg(r,Path.Combine(folder,"ring-output-profile.svg"));Files.Dxf(r,Path.Combine(folder,"ring-output-profile.dxf"));}
    report.AppendLine("Ring output "+i+":1: "+r.Pins+" rollers, "+r.Lobes+" lobes; no disc spin; moving-ring collision check passed.");
   }
   Assert(!new JavaScriptSerializer().Deserialize<Parameters>("{\"Ratio\":20}").RingOutput,"Legacy project compatibility");
   Assert(!Engine.Calculate(new Parameters{Ratio=2,RingOutput=true}).Valid,"Two-pin ring accepted");
   var bad=new Parameters{Ratio=20,Radius=10,Roller=6,Eccentric=2};Assert(!Engine.Calculate(bad).Valid,"Impossible geometry accepted");
   Assert(!Engine.Calculate(new Parameters{Ratio=20.5}).Valid,"Fractional cycloid accepted");
   var d2=new Parameters{Discs=2};Engine.AutoSize(d2);var r2=Engine.Calculate(d2);Assert(r2.Items[0].Qty=="2"&&r2.Items[1].Qty=="21"&&r2.Items[4].Qty=="12","Two-disc BOM");
   var pair=Engine.Calculate(new Parameters{Kind=1,Ratio=3.14,Teeth=20});Assert(pair.Z2==63&&Math.Abs(pair.Actual-3.15)<1e-8,"Pair rounding");
   var belt=Engine.Calculate(new Parameters{Kind=2,Ratio=2,Teeth=20,Center=80});Assert(belt.Valid,"Belt setup");
   var planet=Engine.Calculate(new Parameters{Kind=3,Ratio=4,Teeth=24,Planets=3});Assert(planet.Valid&&planet.Z2==72&&planet.Zp==24,"Planetary selection");
   Assert(!Engine.Calculate(new Parameters{Kind=3,Ratio=2}).Valid,"Planetary boundary");
   var js=new JavaScriptSerializer();var saved=js.Deserialize<Parameters>(js.Serialize(d2));Assert(saved.Discs==2&&saved.Ratio==20,"JSON round trip");
   // Regression cases found while auditing the user's screenshot and editing workflow.
   var screenshot=CalculateCase(new Parameters{Ratio=60,RingOutput=true,Radius=34,Roller=7,Eccentric=1.92});Assert(!screenshot.Valid&&screenshot.Errors.Count>=2&&screenshot.BadFields.Contains("Roller")&&screenshot.BadFields.Contains("Eccentric"),"Screenshot rejection");
   Assert(!Engine.Calculate(new Parameters{Radius=double.NaN}).Valid&&!Engine.Calculate(new Parameters{Eccentric=double.PositiveInfinity}).Valid,"Nonfinite input accepted");
   Assert(!Engine.Calculate(new Parameters{Kind=1,Ratio=2,Module=.1,Shaft=100}).Valid,"Shaft wider than gear accepted");
   Assert(!Engine.Calculate(new Parameters{Kind=3,Ratio=4,Module=.1,Shaft=100}).Valid,"Shaft wider than sun accepted");
   int autos=0,unavailable=0;
   foreach(bool ring in new[]{false,true})foreach(int ratio in new[]{3,8,20,60,120})foreach(int pinCount in new[]{3,6,12,16})foreach(double roller in new[]{1.0,4.0,12.0,50.0}){
    var c=new Parameters{Ratio=ratio,RingOutput=ring,OutputPins=pinCount,Roller=roller};var original=Engine.Clone(c);bool found=Engine.AutoSize(c);
    if(found){Assert(Engine.Calculate(c).Valid&&c.Radius<=1000&&c.OutputRadius<=500,"Autosize invalid / outside UI limits");Assert(c.OutputPins==pinCount&&c.Roller==roller&&c.Ratio==ratio,"Autosize overwrote independent input");autos++;}
    else{Assert(c.Radius==original.Radius&&c.Eccentric==original.Eccentric&&c.OutputRadius==original.OutputRadius,"Failed autosize mutated model");unavailable++;}
   }
   var crowded=new Parameters{RingOutput=true,OutputPins=16};Assert(Engine.AutoSize(crowded)&&Engine.Calculate(crowded).Valid,"Sixteen-pin autosize");
   bool blocked=false;try{Files.Dxf(screenshot,Path.Combine(folder,"must-not-exist.dxf"));}catch{blocked=true;}Assert(blocked&&!File.Exists(Path.Combine(folder,"must-not-exist.dxf")),"Invalid DXF written");
   var invalidBore=new Parameters{Bore=4,Shaft=5};Assert(!Engine.AutoSize(invalidBore),"Autosize impossible central bearing");
   V4Tests.Run(folder,report);ReleaseTests.Run(folder,report);
   using(var form=new MainForm())form.Regression();
   report.AppendLine("PASS bug regressions: screenshot, NaN/Infinity, shaft/gear fit, invalid DXF gate, UI parsing/commit/limits/legacy projects.");
   report.AppendLine("Autosize grid: "+autos+" valid, "+unavailable+" unavailable; failed attempts preserve inputs.");
   report.AppendLine("PASS: geometry/collision, fractional ratios, 2-disc counts, pair rounding, belt, planetary constraints, project roundtrip, CSV/SVG/DXF.");
   File.WriteAllText(Path.Combine(folder,"test-results.txt"),report.ToString(),Encoding.UTF8);return 0;
  }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"test-error.txt"),ex.ToString(),Encoding.UTF8);return 1;}}
  static Result CalculateCase(Parameters p){return Engine.Calculate(p);}
 }
}
