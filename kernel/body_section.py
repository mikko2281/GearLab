"""Nominal closed circular section sizing, N/mm/MPa; not joint FEA."""
import math

def equivalent_stress(radius,wall,moment,torque,axial):
    if not all(math.isfinite(v) for v in (radius,wall,moment,torque,axial)) or wall<=0 or radius<=wall:
        raise ValueError('Некорректное замкнутое сечение трубы.')
    inner=radius-wall
    area=math.pi*(radius**2-inner**2)
    inertia=math.pi*(radius**4-inner**4)/4
    polar=2*inertia
    normal=abs(moment)*radius/inertia+abs(axial)/area
    shear=abs(torque)*radius/polar
    return math.sqrt(normal**2+3*shear**2)

def size_section(minimum,wall,moment,torque,axial,allowable,maximum=70):
    if not math.isfinite(allowable) or allowable<=0:
        raise ValueError('Допускаемое напряжение должно быть положительным.')
    radius=max(minimum,wall+.5)
    while radius<=maximum+1e-9:
        stress=equivalent_stress(radius,wall,moment,torque,axial)
        if stress<=allowable:return radius,stress
        radius+=.5
    raise ValueError('Изгиб, кручение и осевая сила требуют радиуса каркаса больше 70 мм. Измените нагрузку, материал или компоновку.')
