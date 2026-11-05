"""Write a release-specific closure report only from completed test evidence."""
import csv,hashlib,json,shutil,html,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
results=root/'test-results-fixes'
docs=root/'docs/verification/6.0-preview3'
docs.mkdir(parents=True,exist_ok=True)
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
audit=read(results/'audit/audit-fixes-results.json')
reg=read(results/'regression/status.json')
robot=read(results/'robot-full/status.json')
native=read(results/'regression/transmission60/native-results.json')
params=read(results/'parameters/summary.json')
param_rows=read(results/'parameters/sensitivity.json')
modes=read(results/'parameters/modes.json')
steps=read(results/'step-reimport.json')
cache=read(results/'cache-checks/status.json')
assert audit['Passed'] and not params['Errors']
assert all(r['ExitCode']==0 for r in reg+robot),(reg,robot)
assert len(native)==8 and all(r['Ok'] for r in native)
assert len(steps)==6 and all(r['Valid'] for r in steps)
assert len(cache)==2 and all(r['ExitCode']==0 for r in cache)
assert len(modes)==13 and all(r['Success'] and r['Valid'] for r in modes)
assert not any(r.get('Exception') or r.get('InvalidMeshes') for r in param_rows)
assert (results/'regression/complete.txt').exists() and (results/'robot-full/complete.txt').exists()
for name in ['audit','parameters']:
 for p in (results/name).glob('*.json'):shutil.copy2(p,docs/(name+'-'+p.name))
for label,p in [('regression-status.json',results/'regression/status.json'),('robot-status.json',results/'robot-full/status.json'),('native-results.json',results/'regression/transmission60/native-results.json')]:shutil.copy2(p,docs/label)
shutil.copy2(results/'step-reimport.json',docs/'step-reimport.json')
shutil.copy2(results/'cache-checks/status.json',docs/'cache-status.json')
for suite in ['workspace','self','controller','robot','print']:
 for p in (results/'regression'/suite).glob('*results*.txt'):shutil.copy2(p,docs/(suite+'-'+p.name))
for suite in ['connected','blender']:
 for p in (results/'robot-full'/suite).glob('*results.txt'):shutil.copy2(p,docs/(suite+'-'+p.name))
 for name in ['contacts.json','six-axis.png','joint-base.png','joint-wrist.png']:
  p=results/'robot-full'/suite/name
  if p.exists():shutil.copy2(p,docs/(suite+'-'+name))
for name in ['wave-3d.png','wave-ui.png']:
 p=results/'regression/reducer56'/name
 if p.exists():shutil.copy2(p,docs/name)
for name in ['type-5-scene.png','type-8-scene.png','type-12-scene.png']:
 p=results/'regression/ui60'/name
 if p.exists():shutil.copy2(p,docs/name)
with (docs/'parameter-cases.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=['Kind','Key','Old','Requested','Effective','Valid','GeometryChanged','CalculationChanged','MotionChanged','Errors'],extrasaction='ignore',delimiter=';');w.writeheader();w.writerows(param_rows)
files=list((root/'src').glob('*.cs'))+list((root/'kernel').glob('*.py'))
verification={'Version':'6.0.0-preview.3','ExeSHA256':hashlib.sha256((root/'Windows-fix/GearLab.exe').read_bytes()).hexdigest().upper(),'SourceSHA256':{p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files},'AuditAssertions':audit['Checks'],'ParameterCases':params['Cases'],'VisibleFamilyFieldPairs':params['Fields'],'BoundaryCases':params['BoundaryCases'],'NativePreviewWorkers':params['NativeWorkers'],'RegressionSuites':len(reg),'RobotSuites':len(robot),'AllPassed':True,'ClosedFindings':['B01','B02','B03','B04','B05','B06','B07','B08','B09','Q01','Q02']}
(docs/'build-verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2),encoding='utf-8')
fixes=[
('B01','Подбор больше не отбрасывает допустимое решение; ищет допустимый интервал модуля с нижними/верхними ограничениями. В RV подбирается и радиус, в магнитном — радиус. Отказ сохраняет параметры.'),
('B02','Оба экспортных пути цепи сначала захватывают текущие параметры и пересчитывают общую сборку. Проверен настоящий STL зубчатой детали толщиной 10 мм вместо прежних 7; неверный ввод сохраняет старый файл.'),
('B03','Единый выбор состава применяется к SVG/DXF/CSV/STEP новых типов. Проверены фактические файлы всех восьми новых семейств для печатного состава и рабочих профилей.'),
('B04','Автоматически определяемый модуль/межосевое/радиус блокируется в интерфейсе. Ручное управление возвращается после выключения автоподбора.'),
('B05','Момент и КПД доступны в прежних четырёх типах и влияют на расчёт; при изменении нагрузки геометрия переиспользуется.'),
('B06','Явная применимость полей корпуса, Lewis, второго входа, магнитного/реечного и волнового режимов. Неприменимые поля скрыты. Минимальная толщина крышки названа минимумом; размеры ниже требований опор не обязаны менять результат. У рейки исправлено применение универсального основания.'),
('B07','Engine.Calculate проверяет конечность всех double-полей до выбора семейства. Все 13 типов и NaN/±Infinity проверены; строгий файловый парсер сохранён.'),
('B08','Сустав принимает только конечный выход последовательной цепи. Промежуточный выход отклоняется до изменения сустава; конечный пример даёт i=40 и КПД=0,49. Габарит цепи также проверяется до изменения данных.'),
('B09','F1 заменена справкой Preview 3: 13 типов, параметры/автоподбор, форматы/состав, ступени, робот, настройки и реальные ограничения.'),
('Q01','Workspace-тест проверяет пять действующих разделов, единственное окно, сохранение параметров и ручную позу/пределы/статику робота; удалённый CAD-раздел не требуется.'),
('Q02','Connected-тест проверяет текущую общую MainForm и готовность настоящей 3D-сцены вместо удалённого WaveView. Полные connected и blender сценарии завершены, включая пересборку, STEP/STL, контакты и UI.'),
]
lines=['# GearLab 6.0 Preview 3 — исправления по аудиту 05.11.2026','',
'Исправлены все девять пунктов продукта и обе проблемы устаревших тестов из отчёта. Исходная Preview 2 и исходный аудит сохранены. Пользовательские проекты не переписывались.','',
f'Итоговый EXE: `{verification["ExeSHA256"]}`. Локальная проверка этой сборки; это не публикация на GitHub.','',
'| Пункт | Что исправлено |','|---|---|']
lines.extend('| '+a+' | '+b+' |' for a,b in fixes)
lines+=['','## Проверки','',f'- {audit["Checks"]} целевых утверждений по исправлениям: PASS.',f'- {len(reg)} общих наборов и {len(robot)} полных сценария корпуса/робота: PASS; тайм-аутов нет.',f'- {params["Cases"]} изменений параметров для {params["Fields"]} видимых пар «семейство–поле»: завершены; корректные варианты проверены на конечные координаты и положительные объёмы сеток. Отклонённые несовместимые размеры не считаются ошибками.',f'- {params["BoundaryCases"]} граничных случаев и автоподбор всех 13 типов: завершены. Все 13 автоподборов при контрольном лимите допустимы.',f'- Нативные STEP/STL восьми новых типов: PASS, повторное чтение OCCT, валидность и положительные объёмы, CRC и парность рёбер STL.',
'- Полные connected и blender: точные проверки текущей позы в указанной области, сохранение, перестроение изменённого звена, печатный ZIP, STEP, 3D и переключение общего волнового редактора.','',
'Подробные результаты: [docs/verification/6.0-preview3](verification/6.0-preview3). Старые отчёты в verification/6.0 относятся к Preview 2.','',
'## Как пользоваться изменёнными полями','',
'Для изменения автоматически подобранного размера выключите автоподбор. У червячного/глобоидного КПД вычисляется по трению и геометрии: отдельное неиспользуемое поле КПД скрыто. Второй вход доступен при соответствующей схеме дифференциала. Поля расчёта Lewis доступны лишь при поддержанном профиле 20° и z≥17. Число и размер шариков продолжают менять отдельный 2D-вид; 3D-опоры — габаритные кольца. Минимальная толщина крышки ограничена размещением опор.','',
'## Сохраняющиеся инженерные границы','',
'Это компоновочный предварительный выпуск. Исправление багов не добавляет отсутствующие контактные/упругие/ресурсные/тепловые/FEA-расчёты и не подтверждает изготовимость. У магнитной передачи предельный момент неизвестен. Пересечения проверены в текущей позе и заявленной области; полный диапазон движения не сертифицирован. Точный STEP может занимать минуты. Физические моторы и печатные прототипы не испытывались.','']
lines.extend(['## Дополнительная проверка экспорта и примеров','', '- STEP прежних четырёх типов, полный волновой (19 тел) и печатный поднабор (3 тела) повторно прочитаны OCCT: валидные тела и положительные объёмы.', '- Два встроенных примера робота обновлены из прошедших сценариев; все шесть ключей каждого кэша проверены.', ''])
(root/'docs/AUDIT_FIXES_PREVIEW_3.md').write_text('\n'.join(lines),encoding='utf-8')
def inline(s):
 s=html.escape(s);s=re.sub(r'`([^`]+)`',r'<code>\1</code>',s);return re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'<a href="\2">\1</a>',s)
body=[];table=False;ul=False
for line in lines:
 if line.startswith('|'):
  if not table:body.append('<table>');table=True
  if line.startswith('|---'):continue
  cells=line.strip('|').split('|');body.append('<tr>'+''.join('<td>'+inline(c.strip())+'</td>' for c in cells)+'</tr>');continue
 if table:body.append('</table>');table=False
 if line.startswith('- '):
  if not ul:body.append('<ul>');ul=True
  body.append('<li>'+inline(line[2:])+'</li>');continue
 if ul:body.append('</ul>');ul=False
 if line.startswith('# '):body.append('<h1>'+inline(line[2:])+'</h1>')
 elif line.startswith('## '):body.append('<h2>'+inline(line[3:])+'</h2>')
 elif line:body.append('<p>'+inline(line)+'</p>')
if ul:body.append('</ul>')
page='<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>GearLab Preview 3 — исправления аудита</title><style>body{font:16px/1.6 Segoe UI,Arial,sans-serif;color:#173149;background:#edf2f7;margin:0}main{max-width:1200px;padding:36px 44px;background:white;margin:28px auto;border-radius:10px}h1{font-size:30px;border-bottom:5px solid #e6ab4b;padding-bottom:20px}h2{margin-top:32px}table{border-collapse:collapse;width:100%;font-size:14px}td{border:1px solid #cfdae5;padding:12px;vertical-align:top}tr:first-child{background:#dcebf5;font-weight:bold}tr:nth-child(even){background:#f4f7fa}td:first-child{white-space:nowrap}code{overflow-wrap:anywhere;font-size:13px;background:#edf3f8}a{color:#126b99}li{margin:8px 0}@media(max-width:700px){main{padding:20px 16px;margin:0}h1{font-size:24px}}@media print{body{background:white}main{margin:0;padding:0}tr{break-inside:avoid}}</style><main>'+''.join(body)+'</main></html>'
(root/'docs/AUDIT_FIXES_PREVIEW_3.html').write_text(page,encoding='utf-8')
(docs/'README.md').write_text('# Проверка Preview 3\n\nВсе результаты этой папки относятся к EXE из build-verification.json.\n\n'+ '\n'.join(f'- {r["Suite"]}: PASS ({r.get("Seconds",0):.2f} с)' for r in reg+robot)+'\n\nНативный и GUI scope не подтверждает физическую прочность или сопряжение зубьев.\n',encoding='utf-8')
print(json.dumps(verification|{'SourceSHA256':str(len(files))+' files'},ensure_ascii=False))
