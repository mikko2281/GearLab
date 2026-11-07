from pathlib import Path
import os
os.chdir(Path(__file__).resolve().parents[1])
import json,re
pairs={}
for line in Path('tools/ui64_catalog.txt').read_text(encoding='utf-8-sig').splitlines():
 if not line or '|' not in line: continue
 if line.startswith('Передача |i|'): pairs['Передача |i|']='Transmission |i|';continue
 k,v=line.split('|',1)
 pairs[k.replace('\\n','\n')]=v.replace('\\n','\n')
pairs.update({'Gear Lab 6.7.0 · Калькулятор редукторов':'GearLab 6.7.0 · Reducer calculator','GearLab 6.7.0 · Робототехника':'GearLab 6.7.0 · Robotics','Шаг 0,01–100 мм; чувствительность 1–30; анимация 0,01–10; автосохранение 15–600 с.':'Step 0.01–100 mm; sensitivity 1–30; animation 0.01–10; autosave 15–600 s.', 'ЛКМ — вращение · СКМ / Shift+ЛКМ — сдвиг · колесо — масштаб · F — вписать':'Left-drag: orbit · Middle / Shift+left: pan · Wheel: zoom · F: frame all'})
# Fragments are used for generated status text; identifiers and editable data never pass through this catalog.
pairs.update({' мм/с':' mm/s',' мм':' mm',' об/мин':' rpm',' Н·м':' N·m',' Н':' N',' кг':' kg','Вход ':'Input ','Выход ':'Output ','Габарит ':'Size ','Пакет ≈ ':'Stack ≈ ','Шарики ':'Balls ',' · впадины ':' · pockets ',' · выход ':' · output ','Ступень ':'Stage ',' из ':' of ','3D-сцена · миллиметры · сетка ':'3D scene · millimeters · grid ','Подготовка интерактивной сцены Three.js…':'Preparing interactive Three.js scene…','Ошибка сцены: ':'Scene error: ','Сообщение сцены: ':'Scene message: ','Параметр ':'Parameter ','Сдвиг детали ':'Part offset ','Открыт проект: ':'Project opened: ','Проект сохранён: ':'Project saved: ','Экспорт сохранён: ':'Export saved: ','Геометрия':'Geometry','Предупреждения':'Warnings','не число':'not a number','Запас':'Margin','Нагрузка':'Load','Длина':'Length','Ширина':'Width','Толщина':'Thickness','Радиус':'Radius','Диаметр':'Diameter','Угол':'Angle','Имя':'Name','Цвет':'Color','Материал':'Material','Масса':'Mass','Плотность':'Density','Положение':'Position','Вращение':'Rotation','Ось':'Axis','Скорость':'Speed','Момент':'Torque','Привод':'Drive','Звено':'Link','Трубчатый':'Tubular','Балка':'Beam','Плита':'Plate','Ступени':'Stages','Зазор':'Clearance','Профиль':'Profile','Ограничения':'Limits','Объём':'Volume','Площадь':'Area','Трение':'Friction','Единицы':'Units','Прочность':'Strength','Рабочие профили':'Working profiles','Схема':'Diagram','Общий':'Total','Редуктор':'Reducer','Редукторы · ':'Reducers · '})
q=lambda s:json.dumps(s,ensure_ascii=False)
items=',\n'.join('{'+q(k)+','+q(v)+'}' for k,v in pairs.items())
code='''using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace GearLab {public static partial class I18n {
 static readonly Dictionary<string,string> Catalog=new Dictionary<string,string>(StringComparer.Ordinal){
'''+items+'''};
 static readonly Lazy<Regex> Parts=new Lazy<Regex>(()=>new Regex(string.Join("|",Catalog.Keys.OrderByDescending(k=>k.Length).Select(k=>(char.IsLetter(k[0])?@"(?<![\\p{L}\\p{N}_])":"")+Regex.Escape(k)+(char.IsLetter(k[k.Length-1])?@"(?![\\p{L}\\p{N}_])":""))),RegexOptions.Compiled));
 static string TranslateParts(string text){return Parts.Value.Replace(text,m=>Catalog.ContainsKey(m.Value)?Catalog[m.Value]:m.Value);}
}}
'''
Path('src/UiTextCatalog.cs').write_text(code,encoding='utf-8-sig')
print(len(pairs),'translations')
