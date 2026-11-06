using System;using System.Linq;using System.Collections.Generic;
namespace GearLab {
 public sealed class TransmissionType {public int Id;public string Name,Category,Limit;public TransmissionType(int id,string n,string c,string l){Id=id;Name=n;Category=c;Limit=l;}public override string ToString(){return Name;}}
 public static class TransmissionCatalog {
  public static readonly TransmissionType[] All={
   new TransmissionType(0,"Циклоидальный","Соосные",""),new TransmissionType(1,"Зубчатая пара","Параллельные валы",""),new TransmissionType(2,"Зубчатый ремень","Параллельные валы",""),new TransmissionType(3,"Планетарный","Соосные",""),new TransmissionType(4,"Волновой ПТК","Соосные","Профильная одноволновая передача с шариками; ресурс и контактные напряжения не рассчитаны"),
   new TransmissionType(5,"Червячный","Перекрещивающиеся валы","Компоновочный профиль колеса; сопряжение и ресурс не подтверждены"),
   new TransmissionType(6,"Конический","Пересекающиеся валы","Прямые зубья: приближение плоскими сечениями, без сферической эвольвенты"),
   new TransmissionType(7,"RV","Соосные","Двухступенчатая кинематика; контакт и распределение по кривошипам не рассчитаны"),
   new TransmissionType(8,"Дифференциальный планетарный","Соосные","Уиллис; КПД задан пользователем, циркулирующая мощность требует отдельного расчёта"),
   new TransmissionType(9,"Гипоидный · исследование","Исследовательские","Компоновка со смещением осей, не сопряжённый гипоидный профиль"),
   new TransmissionType(10,"Глобоидный · исследование","Исследовательские","Огибающая форма червяка, не проверенная станочная производящая поверхность"),
   new TransmissionType(11,"Магнитный · исследование","Исследовательские","Полюсная кинематика; предельный момент неизвестен без электромагнитного FEA"),
   new TransmissionType(12,"Реечная передача","Линейные","Эвольвентная шестерня и трапецеидальная рейка; прочность не рассчитана")};
  public static string[] Names{get{return All.Select(x=>x.Name).ToArray();}}
  public static Parameters Starter(int kind){var p=new Parameters{UseUniversalBase=true,Kind=kind,AutoFit=false,Overall=0,Clearance=.15,Ratio=kind==5||kind==10?20:2,Teeth=20,Module=1,Thickness=8,Wall=4,MotorPreset=2};Engine.SetPreset(p,2);p.AutoFit=false;p.Overall=0;p.Thickness=8;p.Wall=4;p.Teeth=20;p.BearingSimulation=false;if(kind==7){p.Teeth=18;p.Ratio=3;p.RvPins=21;p.Radius=42;p.Roller=4;p.Eccentric=.7;p.Discs=2;p.Module=.6;}if(kind==6||kind==9)p.Thickness=6;if(kind==8){p.Teeth=24;p.PlanetTeeth=18;p.SecondPlanetTeeth=18;p.Ratio=3.5;}if(kind==11){p.InnerPolePairs=4;p.OuterPolePairs=20;p.Radius=40;p.Thickness=12;p.Ratio=5;}if(kind==12){p.Ratio=1;p.RackLength=160;p.RackTravel=60;}return p;}
 }
 public sealed class TransmissionEngineering {
  public double SpeedGain,OutputRpm,OutputTorque,LinearSpeed,OutputForce,InputPower,OutputPower,Efficiency,Tangential,Radial,Axial,OutputRadial,OutputAxial,LeadAngle,Cone1,Cone2,ConeDistance,WormDiameter,WheelDiameter,Centre;
  public bool Linear,MultiInput,SelfLockEstimate;public string Qualification="Приближённая инженерная оценка";
  public List<string> Warnings=new List<string>();
 }
}
