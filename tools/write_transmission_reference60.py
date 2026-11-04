"""Describe actual algorithms; these formulas are not executable configuration."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
formulas=[
 ('worm','i=z2/starts; d1=q*m; d2=z2*m; a=(d1+d2)/2; gamma=atan(starts/q)','Геометрически рассчитано'),
 ('worm.losses','rho=atan(mu/cos(alpha)); eta=tan(gamma)/tan(gamma+rho); self_lock_estimate=(cos(alpha)*tan(gamma)<=mu)','Приближённая оценка контакта; без подшипников/смазки, не гарантирует удержание'),
 ('bevel','delta1=atan2(sin(Sigma),z2/z1+cos(Sigma)); delta2=Sigma-delta1; R=m*z1/(2*sin(delta1)); b<=R/3','Пересекающиеся оси, оба конуса острые'),
 ('bevel.forces','dm1=m*z1-b*sin(delta1); Ft=2000*T1/dm1; Fr1=Ft*tan(alpha)*cos(delta1); Fa1=Ft*tan(alpha)*sin(delta1)','Приближённая статическая оценка; гипоидный случай только эквивалентная коническая оценка'),
 ('rv','i=1+(z2/z1)*N; nout=nin/i; ncrank=ncarrier-(nin-ncarrier)*z1/z2','Корпус закреплён, выход — водило; НЕ независимое произведение ступеней'),
 ('compound.willis','zr=zs+za+zb; k=zr*za/(zs*zb); (ws-wc)+k*(wr-wc)=0; wc=(ws+k*wr)/(1+k)','Составные соосные сателлиты A/B, одинаковый модуль'),
 ('compound.phase','(zs*zb+zr*za) mod (gcd(za,zb)*planet_count)=0','Условие равномерной установки; не контактный анализ'),
 ('magnetic','segments=p1+p2; nout=-nin*p1/p2','Модулятор неподвижен; предельный момент неизвестен без FEA'),
 ('rack','r=m*z/2; v_mm_s=n*pi*m*z/60; F_N=1000*T*eta/r','Момент в Н·м, радиус в мм; усилие в Н'),
 ('power','Pin=T*abs(n)*pi/30; Pout=Pin*eta; Tout=T*abs(i)*eta','Теоретический момент не является допустимым рабочим'),
 ('train','n_child=n_parent*gain; T_child=T_parent*power_share*abs(i)*eta; sum(outgoing_power_share)<=1','Граф без циклов. После рейки вращательная ступень отклоняется'),
 ('train.two_input','Tr=k*Ts; wc=(ws+k*wr)/(1+k); Pout=(P1+P2)*eta','Требуется совместимый баланс входных моментов; циркулирующая мощность отклоняется'),
 ('bearings','R2=F*(load_z-bearing1_z)/(bearing2_z-bearing1_z); R1=F-R2','Балочная статическая оценка. Не расчёт ресурса/преднатяга'),
 ('lewis','Y=pi*(0.154-0.912/z); sigma_MPa=Ft*K/(b*m*Y); safety=allowable_MPa/sigma_MPa','Только стандартная внешняя прямозубая 20°, z>=17; allowable=0 означает неизвестно'),
 ('mass','mass_kg=volume_mm3*1e-9*density_kg_m3; COM=sum(volume_i*COM_i)/sum(volume_i)','Геометрическая оценка, габаритные моторы имеют заданную пользователем массу')]
files=['TransmissionMath.cs','TransmissionGeometry.cs','TransmissionLoads.cs','TransmissionTrain.cs','TransmissionHousing.cs','TransmissionAudit.cs','TransmissionSizing.cs','RobotTransmission60.cs','RobotTrainIntegration.cs']
data={'Version':'6.0-preview.1','LoadedByApplication':False,'Units':{'length':'mm','torque':'N*m','force':'N','power':'W','stress':'MPa','speed':'rpm; rack mm/s'},'Formulas':[dict(Id=i,Expression=e,Qualification=q) for i,e,q in formulas], 'Sources':{f:'src/'+f for f in files},'SHA256':{f:hashlib.sha256((root/'src'/f).read_bytes()).hexdigest() for f in files},'SpecializedValidation':False,'RatedTorqueKnown':False}
(root/'docs/ENGINEERING_6.0.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print('6.0 formula reference:',len(formulas),'formulas')
