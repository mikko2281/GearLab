"""Create a Russian, code-linked reference; never change live user settings."""
import hashlib,json,math,shutil
from pathlib import Path

root=Path(__file__).resolve().parents[1]

data={
 'format_version':1,
 'program_version':'5.3.0',
 'language':'ru-RU',
 'purpose':'Справочный конфиг: устройство программы, реальные параметры, формулы и границы расчётов.',
 'loaded_by_application':False,
 'editing_rules':[
  'Этот файл описывает реализацию 5.3. Программа не загружает из него формулы и коэффициенты. Правка этого файла сама по себе не меняет расчёты.',
  'Настройки интерфейса меняются в разделе Настройки и сохраняются в %LOCALAPPDATA%/GearLab/settings.json. Пример settings.example.json лежит рядом; существующие настройки пользователя не заменены.',
  'Размеры, нагрузки и моторы меняются в свойствах проекта; сохраняйте свой проект через интерфейс. Изменение геометрии требует перестроения корпуса перед экспортом.',
  'Для изменения алгоритма редактируют указанные исходники и пересобирают программу. Поле source относится к каталогу исходного выпуска, а не к установленным системным библиотекам.',
  'Формулы ниже — математическая запись, а не готовые выражения для вставки в CAD. В них знак ^ означает степень; CAD использует **. Не выполнять строки формул как произвольный код.'
 ],
 'units':{
  'length':'мм в CAD и каркасе; плечо для Н·м переводится в метры делением на 1000',
  'volume':'мм³; 1 мм³ = 1e-9 м³',
  'mass':'кг','force':'Н','torque':'Н·м в расчёте моторов; Н·мм в подборе сечения корпуса',
  'stress':'МПа = Н/мм²','density':'кг/м³','speed':'об/мин для моторов; °/с для вращательного шарнира, мм/с для линейного',
  'angles':'параметры интерфейса — градусы; sin/cos/tan в математических формулах — радианы, если явно не сказано иначе',
  'efficiency':'безразмерный коэффициент 0 < eta <= 1; 0.7 означает 70%'
 },
 'runtime_settings':{
  'source':['src/AppSettings.cs'],
  'path':'%LOCALAPPDATA%/GearLab/settings.json',
  'example_file':'settings.example.json',
  'load_behavior':'При отсутствии, ошибке, недопустимых значениях или размере файла >= 65536 байт применяются значения конструктора AppSettings.',
  'parameters':[
   {'key':'Theme','default':0,'allowed':{'0':'Светлая','1':'Тёмная'},'description':'Цветовая тема.'},
   {'key':'Quality','default':1,'allowed':{'0':'Быстрый','1':'Обычный','2':'Подробный'},'description':'Настройка качества предпросмотра; не допуск изготовления и не точность инженерного расчёта.'},
   {'key':'OrbitSensitivity','default':9,'range':[1,30],'description':'Чувствительность вращения камеры.'},
   {'key':'SnapEnabled','default':True,'description':'Включить привязку перемещения.'},
   {'key':'SnapStep','default':1,'range':[0.01,100],'unit':'мм','description':'Шаг привязки.'},
   {'key':'AnimationScale','default':0.1,'range':[0.01,10],'description':'Множитель скорости показа по умолчанию. Отдельные редакторы могут иметь собственную скорость.'},
   {'key':'AutoSave','default':True,'description':'Создавать восстановительные копии рабочих проектов.'},
   {'key':'AutoSaveSeconds','default':60,'range':[15,600],'unit':'с','description':'Интервал автосохранения.'}
  ],
  'recovery':'%LOCALAPPDATA%/GearLab/recovery-{robot|mechanism|cad}-{идентификатор}.json; отдельные копии для рабочих проектов.'
 },
 'modules':[],
 'formulas':[],
 'worked_examples':[],
 'source_snapshot':{}
}

def module(id,title,source,flow,parameters,limitations):
 data['modules'].append(dict(id=id,title=title,source=source,workflow=flow,parameters=parameters,limitations=limitations))
def formula(id,module,title,equations,symbols,source,notes=''):
 data['formulas'].append(dict(id=id,module=module,title=title,equations=equations,symbols=symbols,source=source,notes=notes))

module('cad','Параметрический CAD',
 ['src/CADModel.cs','src/CADExpression.cs','src/CADKernel.cs','kernel/cad_kernel.py','kernel/sketch_solver.py'],
 ['Проект содержит параметры, эскизы и упорядоченную историю операций.',
  'Формулы вычисляются с проверкой имён, конечности чисел и циклических зависимостей.',
  'Операции строят точные B-rep тела в Open CASCADE: примитивы, выдавливание, вращение, лофт, протяжка, булевы операции и модификаторы.',
  'Проверяются валидность твёрдого тела и ненулевой объём; отдельно создаётся треугольный предпросмотр.',
  'STEP хранит CAD-поверхности; STL хранит треугольники и не переносит историю операций.'],
 {'expression_operators':['+','-','*','/','**','скобки'],
  'expression_functions':['sqrt','sin','cos','tan','abs'],
  'pi_constant':'pi',
  'trigonometry_in_cad':'sin/cos/tan в редакторе выражений принимают градусы.',
  'expression_example':'diameter / 2 + wall',
  'kernel_expression_limits':'|результат| <= 1e9; для степени в Python-ядре |показатель| <= 8; список функций ограничен.',
  'sketch_max_scalar_variables':180,'sketch_max_iterations':50},
 ['Точные поверхности не означают наличие заводской истории у импортированного STEP.',
  'После сильного изменения топологии иногда требуется повторный выбор граней и рёбер.',
  'Не все операции Fusion реализованы; CAM, PCB и полноценный FEA отсутствуют.'])

module('mechanism','Свободный механизм: валы, шестерни, шкивы',
 ['src/MechanismModel.cs','src/MechanismSolver.cs','src/MechanismCad.cs'],
 ['Добавить входной вал, назначить момент и обороты.',
  'Посадить шестерни/шкивы на валы и создать связи gear, belt или coupling.',
  'Проверить геометрию зацепления; обойти граф от входа и распространить скорости и КПД.',
  'Просуммировать нагрузки от выходов к входу; сравнить требуемый момент с заданным.'],
 {'new_project_defaults':{'InputRpm':300,'InputTorque':2},'new_link_default_efficiency':0.97,
  'input_rpm_abs_max':60000,'input_torque_range_Nm':[0,1000000],
  'axes':'Параллельные оси Z.',
  'gear_constraints':'Одинаковые Module и PressureAngle; противоположные HelixAngle; одинаковая принадлежность к herringbone.',
  'centre_distance_tolerance_mm':0.02,'minimum_axial_overlap_mm':0.1},
 ['Расчёт допускает разветвлённое дерево без замкнутых контуров и нескольких путей передачи момента.',
  'AvailableTorque на каждой ветви предполагает весь входной момент для этой ветви. Эти предельные моменты нельзя одновременно получить на всех выходах.',
  'КПД задаётся пользователем; нет динамики, деформаций и проверки прочности зубьев.',
  'Ремень считается без проскальзывания; Ft — разность натяжений, а не полная нагрузка на опоры.',
  'Внутренние венцы и рейки доступны как CAD-геометрия; отдельный решатель их зацепления в графе не реализован.'])

module('reducers','Редукторы: циклоидальный, зубчатая пара, ремень, планетарный',
 ['src/GearCalculator.cs','src/PlanetaryTrain.cs','src/CadBuilder.cs','src/SceneMotion.cs'],
 ['Выбрать тип во вкладке Редукторы.',
  'Проверить диапазоны размеров и подобрать целые числа зубьев.',
  'Рассчитать профиль, габариты и геометрические ограничения; построить сборку и движение.',
  'Экспортировать выбранные детали; перечень шаблонных покупных компонентов требует проверки по каталогу.'],
 {'general_ratio_range':[1,200],'cycloid_ratio':'Целое 2–120 для неподвижного кольца; 3–120 для вращающегося кольца.',
  'cycloid_discs':[1,2],'cycloid_output_pins':[3,16],'planet_stages':'0 = автоматический подбор 1–3, либо явно 1–3.',
  'planet_ratio_tolerance_percent':[0,10],'default_pressure_angle_deg':20},
 ['Геометрический подбор не подтверждает подрезание всех зубьев, контактную прочность, ресурс и реальные нагрузки подшипников.',
  'Два циклоидальных диска относятся к одной ступени; передаточное отношение не перемножается.',
  'Ближайшая целая длина ремня — кандидат: нужны каталог, натяжитель или новое межосевое расстояние.'])

module('gear_library','Библиотека зубчатых CAD-деталей',
 ['src/CADGearLibrary.cs','kernel/cad_kernel.py'],
 ['Выбрать пресет прямозубой, косозубой, шевронной шестерни, внутреннего венца или рейки.',
  'Построить эвольвентные боковые поверхности и тело; для косого зуба создать сечения с поворотом по высоте.',
  'Вставить тело в CAD/механизм; геометрия и расчёт передачи являются отдельными этапами.'],
 {'preset_count':32,'ordinary_gear_teeth':[8,160],'module_mm':[0.2,10],'thickness_mm':[0.5,200],
  'pressure_angle_deg':[14,35],'helical_angle_abs_deg':[1,40],
  'module_convention':'В этой реализации d = m*z, в том числе для косого зуба. Не подменять m нормальным модулем без преобразования.',
  'profile_approximation':'Боковая эвольвента интерполируется по 13 радиусам; ниже базовой окружности используется прямой переход. Это не точная трохоида инструмента.'},
 ['При малом числе зубьев требуется проверка подрезания и коррекции.',
  'Геометрия шеврона идеализирована; компенсация осевой силы в решателе не учитывает ошибки изготовления.'])

module('wave','Волновой редуктор',
 ['src/WaveReducer.cs','kernel/wave_kernel.py'],
 ['Открыть Редукторы → Волновой; жёсткий венец закреплён, генератор волн — вход, гибкая чашка — выход.',
  'По передаточному числу выбрать зубья и построить заданную эллиптическую деформацию.',
  'Показать кинематическую анимацию и расчёт выхода при заданном КПД.'],
 {'defaults':{'Ratio':50,'Module':0.6,'CupDepth':25,'Thickness':8,'Wall':0.8,'RingWall':4,'Bore':5,'OutputBore':8,'Clearance':0.15,'BearingWall':1.5,'InputTorque':0.35,'InputRpm':60,'Efficiency':0.7,'LoadTorque':5},
  'ratio_range_integer':[20,120],'presets':[50,80,100],'module_mm':[0.15,2],'cup_depth_mm':[10,100],'cup_wall_mm':[0.3,3]},
 ['Эллиптическая форма задана геометрически. Упругость, контакт зубьев, допустимый момент, усталость и ресурс чашки не рассчитаны.',
  'OutputTorque — пересчёт заданного входного момента по КПД, а не подтверждённая несущая способность.'])

module('robot','Робот: каркас, кинематика и статические нагрузки',
 ['src/RobotModel.cs','src/RobotCad.cs','src/RobotSolver.cs'],
 ['Задать дерево звеньев, оси вращения/линейного перемещения, длины, смещения и пределы.',
  'Указать массу, монтажные размеры и рабочий момент двигателя, передаточное число, КПД, полезную нагрузку и внешние силы.',
  'Преобразовать каждое звено из локальной системы в мировую от корня к инструменту.',
  'Для каждого шарнира собрать силы и моменты от относящихся к нему движущихся деталей и всех потомков.',
  'Сравнить требуемые момент и обороты с введёнными возможностями мотора.'],
 {'document_defaults':{'Gravity':9.81,'SafetyFactor':1.5},'joint_motor_defaults':{'MotorTorque':0.35,'MotorMaxRpm':400,'MotorMass':0.28},
  'max_joints':20,'safety_factor_range':[1,10],'gravity_range_m_s2':[0,30],
  'six_axis_example':{'axes':['Z','Y','Y','Z','Y','X'],'lengths_mm':[180,220,170,160,100,85],'payload_kg':0.5,'root_offset_z_mm':130},
  'supplied_STEP_motor_dimensions_mm':{'Frame':41.9,'Length':37.5,'BoltPitch':31.3,'PilotDiameter':22,'PilotHeight':2,'ShaftDiameter':4.65,'ShaftLength':23.2},
  'motor_torque_source':'Вводите паспортный момент при рабочих оборотах. STEP и обозначение NEMA 17 не определяют момент.',
  'range_load_check':'Обычно 48 выборочных поз; внутри решателя число ограничено 8–128, генератор Random(17).'},
 ['Статика: нет инерционных моментов разгона/торможения, упругости, усталости, нагрева и расчёта крепежа.',
  'Если требуемые обороты выше MotorMaxRpm, результат не подтверждается. Ниже этого предела момент всё равно должен быть введён по реальной характеристике.',
  'Синусоидальная анимация показывает связи и пределы; она не является динамической симуляцией или контроллером реального робота.',
  'Максимум, найденный в выборочных позах, не является доказанным глобальным максимумом.'])

module('body','Процедурный корпус и соединения 5.3',
 ['src/ProceduralBodyModel.cs','src/RobotProceduralWorkspace.cs','src/RobotConnections.cs','kernel/connected_body.py','kernel/joint_geometry.py','kernel/procedural_body.py'],
 ['Собрать спецификацию из моторов, редуктора, подшипников, дерева каркаса, материала и нагрузки.',
  'Перестраивать звенья от конца цепи к основанию, учитывая массу уже созданных дочерних корпусов.',
  'При Connected=true создать корпуса статоров, выходные фланцы, полые несущие переходы, скруглённые седловые крепления и сервисные панели.',
  'Подобрать осевую траекторию из прямых и дуг; сначала проверить кандидаты выборками, затем проверить реальные CAD-тела.',
  'Проверить связность, перекрытие каркаса оболочкой, реальные отверстия, сопряжения, пересечения и STEP после записи/чтения.',
  'Сохранить точные тела и предпросмотр в проекте; при изменении параметров сделать геометрию устаревшей до перестроения.'],
 {'engine':'gearlab-aligned-saddles-2','native_revision':'aligned-saddles-2',
  'defaults':{'Connected':True,'Enabled':True,'AutoRegenerate':True,'Variant':'Reference','MinWall':2.4,'FrameWall':3.2,'SkinWall':2,'Clearance':0.35,'AllowableStressMPa':12,'Density':1270,'CableBendRadius':16,'InsertDiameter':4.6,'InsertLength':5.3,'ShaftFit':0.1,'PrintMargin':6},
  'validation':{'MinWall_mm':[1.2,20],'FrameWall_mm':'от MinWall до 30','SkinWall_mm':[1,8],'Clearance_mm':[0.05,2],'AllowableStressMPa':[0.1,250],'Density_kg_m3':[100,20000],'CableBendRadius_mm':[3,100],'InsertDiameter_mm':[3,15],'InsertLength_mm':[3,20],'ShaftFit_mm':[0.02,1],'PrintMargin_mm':[0,25]},
  'styles':{'Reference':'Базовый овальный профиль.','Ultra Slim':'Более компактный стиль.','Lightweight':'Облегчённый стиль.','High Strength':'Strength=1.4, rounded_rectangle.','Easy Print':'rounded_rectangle, меньше сглаживание.'},
  'style_note':'Стиль передаётся генератору целиком, но Connected использует лишь часть полей. Название варианта само по себе не подтверждает прочность или массу.',
  'connected_used_style_fields':['Strength','Slimness','Roundness','Profile','AccentColorCoverage'],
  'printer_mm':[256,256,256],'connected_print_limit_mm':244,'ready_for_manufacture':False,
  'parameter_scope_note':'ShaftFit и CableBendRadius хранятся в общих настройках, но не являются полным управлением посадками/проводкой Connected. Некоторые посадки зафиксированы в коде, см. формулы и ограничения. В Connected проверка печатного габарита использует фиксированные 244 мм (256−2×6), а не текущее PrintMargin; старая ветка использует Printer и PrintMargin.'},
 ['Connected использует номинальную оценку круглой трубы на изгиб. Она не рассчитывает концентрации напряжений в седле, крепёж, скруглённое прямоугольное сечение или анизотропию печати.',
  'Генератор Connected не создаёт развилку с несколькими дочерними звеньями и не поддерживает процедурное крепление линейного привода.',
  'Гибкие провода, реальные разъёмы и внутренняя передача требуют отдельного проектирования. Зарезервированный объём не означает готовую разводку.',
  'Старая ветка Connected=false использует другую оценку сечения; её формулы нельзя приписывать новой ветке.',
  'Константы вроде радиального зазора подшипника 0.075 мм являются геометрическими допущениями, не выбранным стандартным полем допуска.',
  'Результат всегда ReadyForManufacture=false; изменять этот флаг не заменяет инженерную проверку.'])

module('checks_export','Проверки, сохранение и экспорт',
 ['src/RobotMotionCheck.cs','src/PrintExport.cs','src/MeshDocument.cs','kernel/body_motion.py','kernel/assembly_contacts.py','kernel/cad_kernel.py'],
 ['Сохранить редактируемый проект с параметрами и историей.',
  'Для точной проверки пересечь реальные CAD-тела; общий контакт по поверхности допускается.',
  'Отдельно запустить проверку диапазона: интервальные границы, деление областей, точные столкновения в найденных позах.',
  'STEP сохраняет размещение в сборке; отдельные печатные STL ориентируются и переносятся на Z=0.'],
 {'motion_check_budget_regions':256,'motion_check_time_budget_seconds':30,
  'motion_statuses':{'clear':'Разделение подтверждено для включённых в проверку деталей во всём заданном диапазоне.','collision':'Найдено конкретное точное пересечение с позой.','unknown':'Бюджет исчерпан или остаётся неопределённость; отсутствие столкновений не подтверждено.'},
  'motion_scope':'Крепёж, проводка и внутренние движения пользовательских передач исключены; внутренние движения препятствуют выдаче полного clear.',
  'native_STL_meshing':'OCCT линейное отклонение 0.03 мм и угловое 0.12 рад в ветке export_stl; другие экспортеры имеют собственную дискретизацию.',
  'history_formats':['.cad.json','.robot.json','JSON проекта механизма','JSON полигональной модели'],
  'exchange_formats':['STEP','STL','OBJ для полигональной модели']},
 ['Точность сетки предпросмотра, допуск булевой операции и производственный допуск — разные параметры.',
  'Конечный набор поз и отсутствие грубых сближений не доказывают отсутствие всех столкновений.',
  'STL не восстанавливает параметрическую историю. Геометрическая валидность не подтверждает прочность.'])

formula('ui.snap','cad','Привязка перемещения',
 ['x_snap = Round(x / h) * h'],{'x':'координата, мм','h':'SnapStep, мм','Round':'System.Math.Round; середины округляются к ближайшему чётному'},['src/AppSettings.cs'],
 'Применяется при SnapEnabled=true; при false координата не округляется.')
formula('cad.volume_mass','cad','Объём, масса и центр масс',
 ['V = integral_V dV','m = rho * V_mm3 * 1e-9','c = integral_V r dV / V'],
 {'rho':'плотность, кг/м³','V_mm3':'объём тела в мм³','c':'локальный центр масс'},['kernel/cad_kernel.py','src/RobotCad.cs'],
 'Native STEP использует объём и центр масс OCCT. Для мотора применяется явно заданная MassOverride; служебные направляющие имеют нулевую массу.')
formula('mesh.volume','checks_export','Объём замкнутой треугольной сетки',
 ['v_i = dot(a_i, cross(b_i, c_i)) / 6','V_signed = sum(v_i)','C = sum(v_i * (a_i+b_i+c_i) / 4) / V_signed'],
 {'a_i,b_i,c_i':'вершины ориентированного треугольника в мм'},['src/RobotCad.cs','src/MeshDocument.cs'],
 'Нужна замкнутая согласованно ориентированная сетка. Объём и центр масс открытой сетки недостоверны.')
formula('cad.constraint_solver','cad','Численное решение ограничений эскиза',
 ['J_ij ~= (r_i(x+h_j*e_j) - r_i(x)) / h_j','h_j = 1e-5 * max(1, abs(x_j))','(transpose(J)*J + lambda*I) * dx = -transpose(J)*r','x_next = x + dx','DOF = number_of_variables - rank(J)'],
 {'r':'вектор невязок ограничений','J':'численный якобиан','lambda':'демпфирование, начально 1e-5','DOF':'оценка оставшихся степеней свободы'},['kernel/sketch_solver.py'],
 'До 50 итераций. Ранний выход при max|r|<1e-6; невязка >1e-4 вызывает ошибку. Невязки разных связей нормированы по-разному, в том числе угол, поэтому число не является единым производственным допуском.')
formula('cad.constraint_residuals','cad','Примеры невязок ограничений',
 ['horizontal: u_y = 0','vertical: u_x = 0','coincident: p_a - p_b = 0','length: norm(u) - L = 0','parallel: cross2(u,v) / max(norm(u),norm(v)) = 0','perpendicular: dot(u,v) / max(norm(u),norm(v)) = 0'],
 {'u,v':'векторы отрезков, мм','cross2(u,v)':'u_x*v_y-u_y*v_x','L':'заданный размер, мм'},['kernel/sketch_solver.py'])

formula('transmission.power','mechanism','Скорость и механическая мощность',
 ['omega = 2*pi*n/60 = pi*n/30','P = abs(omega) * T'],
 {'n':'об/мин','omega':'рад/с','T':'неотрицательная величина момента, Н·м','P':'Вт'},['src/MechanismSolver.cs','src/WaveReducer.cs'])
formula('mechanism.speed','mechanism','Передача скорости по связи',
 ['gear: k = n_driven/n_driver = -z_driver/z_driven','belt: k = D_driver/D_driven','coupling: k = 1','s_out = s_parent*k','n_out = n_input*s_out'],
 {'s':'отношение скорости данного вала к входному','D':'делительный диаметр, мм','z':'число зубьев'},['src/MechanismSolver.cs'],
 'Знак минус означает обратное вращение внешней зубчатой пары. Открытый ремень сохраняет направление.')
formula('mechanism.torque','mechanism','КПД, доступный момент и приведение нагрузки',
 ['eta_path = product(eta_link)','T_available = T_input*eta_path/abs(s)','T_parent_required += T_child_required*abs(s_child/s_parent)/eta_link','overloaded = T_root_required > T_input + 1e-8'],
 {'eta':'КПД связи, 0<eta<=1','s':'отношение скорости к входному валу','T':'Н·м'},['src/MechanismSolver.cs'],
 'Требуемый момент каждого вала начинается с его LoadTorque. Приведение выполняется от листьев дерева к входу.')
formula('mechanism.forces','mechanism','Усилия в зацеплении',
 ['Ft = 2000*T_driver/d_driver_mm','Fr = Ft*tan(alpha)','Fa = Ft*abs(tan(beta))'],
 {'Ft':'окружная сила, Н','Fr':'радиальная сила для gear, Н','Fa':'осевая сила для косозубого gear, Н','T_driver':'момент, необходимый для данной ветви, Н·м','alpha':'PressureAngle в радианах','beta':'HelixAngle в радианах'},['src/MechanismSolver.cs'],
 'Для herringbone Fa принимается равной нулю; для ремня Fr и Fa равны нулю в этой модели. Это упрощение, не расчёт реакций опор.')

formula('gears.dimensions','gear_library','Размеры внешней шестерни и пары',
 ['d = m*z','r_pitch = d/2','r_base = r_pitch*cos(alpha)','r_tip = r_pitch+m','r_root = r_pitch-1.25*m','a = m*(z1+z2)/2','i_abs = z2/z1'],
 {'m':'модуль данной реализации, мм','z':'число зубьев','alpha':'угол зацепления, рад','a':'межосевое расстояние, мм'},['kernel/cad_kernel.py','src/GearCalculator.cs','src/MechanismModel.cs'])
formula('gears.involute','gear_library','Эвольвента и толщина зуба',
 ['t = sqrt(max(0,(r/r_base)^2-1))','inv(r) = t-atan(t)','half = pi/(2*z)-backlash/(2*r_pitch)','offset = half+inv(r_pitch)','external_flank_angle(r) = offset-inv(r)','internal_cutout_angle(r) = pi/z-(offset-inv(r))'],
 {'r':'радиус точки, мм','backlash':'линейный боковой зазор, мм','half':'половина угловой толщины зуба на делительной окружности, рад'},['kernel/cad_kernel.py'],
 'Для контура выреза внутреннего венца: r_root=r_pitch-m, r_tip=r_pitch+1.25*m. Отрицательная толщина зуба запрещена.')
formula('gears.helix_rack','gear_library','Косой зуб, шеврон и рейка',
 ['twist = thickness*tan(beta)/r_pitch','helical: rotation(z_height)=twist*z_height/thickness','herringbone: rotation идёт 0 -> twist/2 -> 0','rack_pitch = pi*m'],
 {'twist':'полный угол закручивания, рад','thickness':'ширина венца, мм','beta':'угол наклона, рад','rack_pitch':'линейный шаг рейки, мм'},['kernel/cad_kernel.py'],
 'При |twist|>pi построение отклоняется. Шеврон строится двумя соединёнными половинами.')
formula('reducers.pair_rounding','reducers','Подбор зубьев ведомого колеса',
 ['z2 = RoundAwayFromZero(i_requested*z1)','i_actual = z2/z1','error_percent = 100*(i_actual/i_requested-1)'],
 {'i_requested':'запрошенное передаточное число','z1,z2':'целые числа зубьев'},['src/GearCalculator.cs'])
formula('belt.geometry','reducers','Длина открытого ремня',
 ['d1 = pitch*z1/pi','d2 = pitch*z2/pi','delta = (d2-d1)/2','beta = asin(delta/C)','L = 2*sqrt(C^2-delta^2)+pi*(d1+d2)/2+2*delta*beta','belt_teeth_candidate = Round(L/pitch)'],
 {'pitch':'шаг ремня, мм','C':'межосевое расстояние, мм','L':'длина по делительной линии, мм'},['src/GearCalculator.cs'],
 'Требуется C>(d1+d2)/2 и C>|delta|. Округление зубьев не подстраивает автоматически C под каталог.')
formula('planet.geometry','reducers','Планетарная ступень с закреплённым кольцом',
 ['zr = zs+2*zp','i = 1+zr/zs','(zs+zr) mod planets = 0','(zs+zp)*sin(pi/planets) > zp+2','i_total = product(i_stage)'],
 {'zs':'зубья солнца, вход','zr':'зубья неподвижного кольца','zp':'зубья сателлита','planets':'число сателлитов; выход — водило'},['src/GearCalculator.cs','src/PlanetaryTrain.cs'],
 'Последнее неравенство проверяет непересечение соседних сателлитов по вершинам при стандартной высоте зуба. Подбор минимизирует число ступеней в допуске, затем отклонение; при равенстве выбирает меньший максимум zr.')
formula('cycloid.counts','reducers','Циклоидальная схема и посадки',
 ['fixed_ring: N=i+1; lobes=N-1; n_out=-n_in/i','rotating_ring: N=i; lobes=N-1; n_out=+n_in/i','roller_spacing = 2*R*sin(pi/N)','output_hole_diameter = output_pin_diameter+2*e+2*c','outer_envelope = 2*(R+roller_diameter/2)','k = e*N/R < 1'],
 {'N':'число пальцев/роликов кольца','R':'радиус их центров, мм','e':'эксцентриситет, мм','c':'радиальный зазор, мм'},['src/GearCalculator.cs'],
 'Для вращающегося кольца диск не вращается относительно рамы, но его центр совершает орбитальное движение.')
formula('cycloid.profile','reducers','Профиль циклоидального диска',
 ['rr = roller_diameter/2+c','psi = atan2(sin((1-N)*t), R/(e*N)-cos((1-N)*t))','x(t) = R*cos(t)-rr*cos(t+psi)-e*cos(N*t)','y(t) = -R*sin(t)+rr*sin(t+psi)+e*sin(N*t)'],
 {'t':'параметр от 0 до 2*pi, рад','x,y':'мм','rr':'эффективный радиус ролика с зазором'},['src/GearCalculator.cs'],
 'Дискретизация: steps=ceil(max(Segments,N*40)/(N-1))*(N-1). Проверяются перемычки по минимальному радиусу профиля.')
formula('cycloid.undercut','reducers','Проверка локального подрезания смещённого профиля',
 ['curvature = (x_prime*y_second-y_prime*x_second)/(x_prime^2+y_prime^2)^(3/2)','offset_speed_factor = 1+rr*curvature','min(offset_speed_factor) > 0'],
 {'x,y':'исходная кривая центров: x=R*cos(t)-e*cos(N*t), y=-R*sin(t)+e*sin(N*t)','rr':'радиус ролика с зазором'},['src/GearCalculator.cs'],
 'Минимум ищется выборкой max(1440,N*50) точек; это реализованная локальная численная проверка, не полный контактный анализ.')

formula('wave.ratio','wave','Зубья, направление, момент и мощность',
 ['zf=2*i','zc=zf+2','i=zf/(zc-zf)','n_out=-n_in/i','T_out=T_in*i*eta','T_in_required=T_load/(i*eta)','P_in=T_in*n_in*pi/30'],
 {'zf':'зубья гибкой чашки','zc':'зубья жёсткого венца','i':'положительная величина отношения','T':'Н·м','n':'об/мин'},['src/WaveReducer.cs','kernel/wave_kernel.py'])
formula('wave.deformation','wave','Заданная деформация чашки',
 ['r_pitch=m*zf/2','deformation=m','kappa=deformation/r_pitch=1/i','scale_x=1+kappa','scale_y=1-kappa','theta_out=-theta_generator/i','floor_thickness=max(1.5,2*wall)'],
 {'m':'модуль, мм','wall':'стенка чашки, мм','theta':'рад'},['kernel/wave_kernel.py','src/WaveReducer.cs'],
 'В предпросмотре деформация плавно нарастает к венцу по высоте. Это заданное преобразование, не решение уравнений упругости.')

formula('robot.kinematics','robot','Прямая кинематика',
 ['B_j = Translate(offset_j)*End_parent','Output_j = Motion(axis_j,q_j)*B_j','End_j = Translate(end_vector_j)*Output_j','p_world = Transform(p_local, pose_matrix)'],
 {'B_j':'базовая система шарнира','Motion':'поворот на q в градусах либо сдвиг на q мм; единичная матрица для неподвижного','End_parent':'конечная система родителя; единичная для корня'},['src/RobotModel.cs','src/RobotCad.cs'],
 'Порядок записан как в WPF Matrix3D с векторами-строками. Для библиотек с векторами-столбцами порядок нужно транспонировать, а не копировать буквально.')
formula('robot.animation','robot','Демонстрационная анимация шарнира',
 ['A=(q_max-q_min)/2','q_c=(q_max+q_min)/2','start=asin(clamp((q_initial-q_c)/A,-1,1))','q(t)=q_c+A*sin(start+t*Speed/A)','q_final=clamp(q(t),q_min,q_max)'],
 {'t':'фаза времени анимации после множителя скорости показа','q':'° для вращения, мм для линейного перемещения','Speed':'°/с или мм/с'},['src/RobotModel.cs'],
 'При A≈0 сохраняется положение. Производная по времени ограничивается Speed, однако ускорения и требуемая для них инерционная нагрузка не рассчитываются.')
formula('robot.gravity','robot','Силы тяжести и внешняя нагрузка',
 ['F_part=(0,0,-m*g)','F_tool=(ForceX,ForceY,ForceZ-Payload*g)','r_m=(point_world-origin_joint)/1000'],
 {'m,Payload':'кг','g':'Gravity, м/с²','r_m':'плечо, м','ForceX/Y/Z':'внешняя сила в мировой системе, Н'},['src/RobotSolver.cs'],
 'Сила полезной нагрузки прикладывается в Tip соответствующего звена; сила тяжести детали — в её мировом центре масс.')
formula('robot.effort','robot','Момент или усилие шарнира',
 ['rotational: tau_signed = sum(dot(cross(r_m,F),axis_unit))','linear: force_signed = sum(dot(F,axis_unit))','required = abs(signed_effort)*SafetyFactor'],
 {'axis_unit':'единичный вектор оси в мировой системе','required':'Н·м для вращения или Н для линейного шарнира'},['src/RobotSolver.cs'],
 'Суммируются учтённые детали с OwnerId среди потомков и их нагрузки. Направляющие скелета исключены; неподвижному соединению моторный критерий не предъявляется.')
formula('robot.motor','robot','Вращательный мотор и редуктор',
 ['T_available=MotorTorque*Reduction*Efficiency','T_motor_required=T_required/(Reduction*Efficiency)','n_motor=Speed_deg_s/6*Reduction','Margin=T_available-T_required','Enough=(T_required<=T_available+1e-9) AND (n_motor<=MotorMaxRpm+1e-7)'],
 {'Reduction':'положительное отношение скорости мотора к выходу','Efficiency':'КПД','Margin':'разность в Н·м, не коэффициент запаса'},['src/RobotSolver.cs'],
 'Для CustomTransmission: Reduction=1/abs(output.SpeedRatio), Efficiency=output.PathEfficiency.')
formula('robot.screw','robot','Линейный привод через винт',
 ['F_available=MotorTorque*Reduction*Efficiency*2*pi/(ScrewLead/1000)','T_motor_required=F_required*(ScrewLead/1000)/(2*pi*Reduction*Efficiency)','n_motor=Speed_mm_s/ScrewLead*60*Reduction'],
 {'ScrewLead':'ход винта за оборот, мм/об','F':'Н','T':'Н·м'},['src/RobotSolver.cs'],
 'Потери объединены в заданном Efficiency. Отдельного расчёта трения, самоторможения и критической скорости винта нет.')

formula('body.load_envelope','body','Консервативная нагрузка для генератора',
 ['reach_m=sum(descendant.Length_mm)/1000','F_external=sum(norm(descendant.ExternalForce))','M_conservative_Nmm=(mass_envelope*g+F_external)*max(0.02,reach_m)*SafetyFactor*1000','DesignMomentNmm=max(current_required_Nm*1000,M_conservative_Nmm)','DesignTorqueNmm=max(current_required_Nm*1000,MotorTorque*Reduction*Efficiency*1000)','AxialForceN=(mass_envelope*g+F_external)*SafetyFactor'],
 {'mass_envelope':'сумма масс моторов, нагрузок и оценки корпусов по потомкам, кг','current_required':'0, если при сборке спецификации не передан рассчитанный mechanical build'},['src/ProceduralBodyModel.cs'],
 'Оценка корпуса берётся из готовой массы дочернего генератора либо из объёмной аппроксимации, с нижней границей 0.25 кг. Это запасная оценка, а не фактический момент в текущей позе.')
formula('body.mass_envelope','body','Оценка массы ещё не построенного корпуса',
 ['V_est=pi*((GearboxDiameter/2+6)^2-(GearboxDiameter/2)^2)*(MotorLength+GearboxLength)+pi*(400-256)*Length','m_body_est=max(0.25,V_est*Density*1e-9+0.18)','mass_envelope=sum(MotorMass+Payload+m_body_est_or_generated_descendant_mass)'],
 {'V_est':'приближённый объём, мм³','Density':'плотность звена, кг/м³'},['src/ProceduralBodyModel.cs'],
 'У потомка с готовым корпусом используется его Result.MassKg с той же нижней границей 0.25. Собственная масса текущего корпуса на этом этапе ещё оценивается.')
formula('body.connected_section','body','Подбор круглой трубы Connected=true',
 ['fw=max(wall,FrameWall)*Style.Strength','r0=max(18,fw+CableDiameter+4)*(1+0.12*(1-Style.Slimness))','I=pi*(r^4-(r-fw)^4)/4','sigma_nominal=M_design*r/I','r=r0; пока sigma_nominal>AllowableStressMPa и r<70: r=r+0.5'],
 {'wall':'max(2.4,MinWall), мм в dimensions()','fw':'стенка каркаса, мм','r':'наружный расчётный радиус, мм','M_design':'DesignMomentNmm, Н·мм','I':'момент инерции круглого сечения, мм⁴','sigma_nominal':'МПа'},['kernel/connected_body.py'],
 'В текущей ветке в sigma не включены DesignTorqueNmm и AxialForceN. Для rounded_rectangle эта же круглая оценка остаётся приближённой. Предел цикла 70 мм не является подтверждением прочности.')
formula('body.legacy_section','body','Другая оценка при Connected=false',
 ['r_inner=max(0.1,r-fw)','A=pi*(r^2-r_inner^2)','I=pi*(r^4-r_inner^4)/4','sigma=M*r/I+F_axial/A','A_median=pi*(r-fw/2)^2','tau=T/(2*A_median*fw)','sigma_equivalent=sqrt(sigma^2+3*tau^2)'],
 {'M,T':'Н·мм','F_axial':'Н','sigma,tau':'МПа'},['kernel/procedural_body.py'],
 'Номинальная балочная/Bredt оценка с подбором радиуса и 35 шагами двоичного поиска. Не относится к основному генератору Connected=true и не заменяет анализ печатного стыка.')
formula('body.envelope','body','Габариты корпуса привода и крепёжного круга',
 ['wall=max(2.4,MinWall)','boss=InsertDiameter/2+wall','r_inner=max(ReducerDiameter/2+gap,hypot(MotorFrame/2+abs(MotorOffsetX),MotorFrame/2+abs(MotorOffsetY))+gap)','r_bolt_circle=r_inner+boss','r_outer=r_bolt_circle+boss','frame_plate=max(10,2*wall+InsertDiameter)'],
 {'gap':'Clearance, мм','boss':'радиус прилива под вставку, мм','r_bolt_circle':'радиус, не диаметр; BodyMate.BoltCircle хранит удвоенное значение'},['kernel/connected_body.py'],
 'Радиусы учитывают квадратный габарит мотора и смещение его центра; фактический разъём требует отдельной проверки.')
formula('body.route','body','Осевой переход трубы',
 ['r_bound=r для oval','r_bound=r*(sqrt(2)*(1-roundness)+roundness) для rounded_rectangle','bend_radius=1.55*r_outer_bound','trim=bend_radius*tan(turn_angle/2)','route_score=sum(straight_lengths)-2*sum(trim)+sum(bend_radius*turn_angle)'],
 {'r_outer_bound':'габарит профиля с зазором и оболочкой','turn_angle':'изменение направления, рад','roundness':'значение стиля, при расчёте габарита ограничено 0.1–0.95'},['kernel/joint_geometry.py','kernel/connected_body.py'],
 'Проверяются оба осевых направления входа и конечный набор длин. Для простого углового перехода радиус может быть уменьшен с ограничением места >1.3*r_outer_bound+4. Кандидаты сортируются по длине. Это поиск среди шаблонов, не глобальная оптимизация всей конструкции.')
formula('body.saddle','body','Скруглённое седло вместо треугольной перемычки',
 ['n_x=(R_big-R_small)/distance','n_y=sqrt(1-n_x^2)','A=(R_big*n_x,R_big*n_y)','B=(distance+R_small*n_x,R_small*n_y)'],
 {'distance':'расстояние между центрами окружностей, мм','R_big,R_small':'радиусы двух сопрягаемых окружностей, мм'},['kernel/joint_geometry.py'],
 'Прямые общие касательные и дуги образуют точную выпуклую оболочку двух окружностей; контур выдавливается на заданную глубину. Требуется |n_x|<1.')
formula('body.bolts_fits','body','Отверстия и некоторые фиксированные посадки Connected',
 ['angle_k=pi/4+2*pi*k/count','bolt_point_k=origin+pc*cos(angle_k)*local_x+pc*sin(angle_k)*local_y','clearance_hole_diameter=bolt+0.4','pilot_hole_diameter=MotorPilot+2*gap','bearing_seat_radius=BearingOuter/2+0.075','output_journal_radius=BearingInner/2-0.02'],
 {'pc':'радиус крепёжного круга, мм','count':'обычно 4','bolt':'номинальный диаметр винта, мм'},['kernel/connected_body.py'],
 '0.075 и 0.02 мм — радиальные добавки. Посадочный диаметр подшипника увеличивается на 0.15 мм, шейка уменьшается на 0.04 мм. Это зафиксированные допущения, не стандартная посадка H7 и не значение ShaftFit.')
formula('body.screw_length','body','Выбор длины общего винта',
 ['stack+3 <= L <= stack+engagement-0.4','L = первый допустимый из [6,8,10,12,14,16,18,20,25,30,35,40]'],
 {'stack':'толщина пакета, мм','engagement':'доступная глубина зацепления, мм','L':'длина винта, мм'},['kernel/connected_body.py'],
 'Если допустимой длины нет, построение выдаёт ошибку. Этот подбор не проверяет срыв резьбы, натяг, затяжку или доступ инструмента.')
formula('checks.mates','checks_export','Совпадение посадок соседних звеньев',
 ['distance=norm(world_origin_a-world_origin_b) <= 1e-5 mm','abs(dot(normal_a,normal_b)) >= 1-1e-8','bolt_miss=max_over_a(min_over_b(norm(world_bolt_a-world_bolt_b))) <= 1e-5 mm'],
 {'normal':'единичная нормаль фланца','bolt_miss':'наибольшее расстояние до ближайшего ответного отверстия'},['src/RobotConnections.cs'],
 'Проверяются актуальные результаты генератора. Для устаревшей геометрии требуется перестроение; экспорт её деталей блокируется.')
formula('checks.connected_geometry','checks_export','Геометрические критерии основного генератора',
 ['intersection_error если Volume(Common(A,B)) > 0.05 mm3','cover_error если Volume(core outside housing_envelope) >= 0.05 mm3','root_contact_distance < 1e-5 mm','shell_before_split: solid_count=1 AND BRepCheck_Analyzer.IsValid()'],
 {'Common':'точная булева общая часть B-rep тел','housing_envelope':'полный внешний объём корпуса до вычитания полостей'},['kernel/connected_body.py'],
 '0.05 мм³ — порог классификации в этой ветке; другие проверки имеют свои критерии. Крепёж/вставки исключены из части попарных пересечений. Совпадение контактных поверхностей допустимо.')
formula('checks.print','checks_export','Ориентация STL и габарит печати',
 ['p_rotated=Rotate(p,PrintAngles)','p_print=p_rotated-((x_min+x_max)/2,(y_min+y_max)/2,z_min)','size_i=max_i-min_i'],
 {'PrintAngles':'сохранённая ориентация конкретной детали','p_print':'вершина отдельного STL с минимумом Z=0','size_i':'габарит после поворота, мм'},['src/PrintExport.cs','kernel/connected_body.py','kernel/procedural_body.py'],
 'STL центрируется по XY, нижняя точка Z=0. Проверка габарита не подтверждает технологичность, нужные поддержки и прочность по слоям. STEP не переводится в печатную раскладку. В Connected ориентация перебирает X/Y по 0 и 90°, Z по 0–165° с шагом 15°; выбирается минимум по критериям выхода за 244 мм, высоты Z и максимального размера.')

data['worked_examples']=[
 {'id':'rotary_static','description':'Одна точечная нагрузка 0.5 кг на горизонтальном плече 300 мм, ось перпендикулярна плечу и тяжести; массы звеньев и внешние силы здесь намеренно равны нулю.',
  'input':{'mass_kg':0.5,'lever_m':0.3,'g':9.81,'safety':1.5,'motor_torque_Nm':0.35,'reduction':50,'efficiency':0.7,'joint_speed_deg_s':5},
  'expected':{'required_Nm':0.5*9.81*0.3*1.5,'available_Nm':0.35*50*0.7,'motor_required_Nm':0.5*9.81*0.3*1.5/(50*0.7),'motor_rpm':5/6*50},
  'note':'Это учебная проверка формулы, не итоговая нагрузка шестиосевого примера.'},
 {'id':'gear_pair','description':'Внешняя пара 20/60, m=2 мм; 300 об/мин и 2 Н·м на входе, КПД0.97, нагрузка выхода3 Н·м.',
  'input':{'z1':20,'z2':60,'module_mm':2,'input_rpm':300,'input_torque_Nm':2,'efficiency':0.97,'load_Nm':3},
  'expected':{'centre_distance_mm':80,'output_rpm':-100,'available_output_Nm':5.82,'required_input_Nm':3/3/0.97,'input_power_W':2*300*math.pi/30}},
 {'id':'wave_50','description':'Волновой редуктор с закреплённым венцом, значения формы по умолчанию.',
  'input':{'ratio':50,'input_rpm':60,'input_torque_Nm':0.35,'efficiency':0.7,'load_Nm':5},
  'expected':{'flex_teeth':100,'ring_teeth':102,'output_rpm':-1.2,'output_torque_Nm':12.25,'input_power_W':0.35*60*math.pi/30,'required_input_Nm':5/(50*0.7)},
  'note':'Числа описывают идеализированную передачу мощности с заданным КПД; допустимый момент чашки не определён.'}
]

data['program_version']='5.3.0'
data['purpose']='Справочный конфиг GearLab 5.3: управление роботом, устройство программы, реальные параметры, исправленные нагрузки и формулы.'
modules={m['id']:m for m in data['modules']}
formulas={f['id']:f for f in data['formulas']}
body=modules['body']
body['source']+=['src/BodyLoadEnvelope.cs','kernel/body_section.py']
body['parameters']['engine']='gearlab-loads-controls-3';body['parameters']['native_revision']='loads-controls-3'
body['limitations'][0]='Connected использует совместную номинальную оценку круглой трубы: изгиб, кручение и осевую силу. Она не рассчитывает концентрации напряжений в седле, крепёж, скруглённое прямоугольное сечение или анизотропию печати.'
body['workflow'][1]='Перестраивать звенья от конца цепи к основанию. Силовая оценка использует массы и локальные центры масс учтённых деталей, монтажные смещения, точки внешних сил и пределы линейного хода потомков.'
body['limitations']+=['Геометрическая оценка плеча является верхней границей для учтённых масс текущей модели и заданных точечных сил. Масса ещё не построенного корпуса оценивается приближённо: это не доказанная верхняя граница его окончательной массы и не глобальный оптимизатор конструкции.']
f=formulas['body.load_envelope']
f['equations']=[
 'b_root=0',
 'b_child=b_parent+norm(End_parent)+travel_parent+norm(Offset_child)',
 'travel_j=max(abs(Minimum_j),abs(Maximum_j)) для линейного шарнира; иначе 0',
 'R_part=b_joint+norm(LocalTransform(COM_part))+(InputSide ? 0 : travel_joint)',
 'R_tool=b_joint+travel_joint+norm(End_joint)',
 'F_bound=SafetyFactor*sum(mass_part*g+Payload*g+norm(ExternalForce)+reserve_mass*g)',
 'M_bound_Nmm=SafetyFactor*sum(force_magnitude_N*point_radius_mm)',
 'DesignMomentNmm=max(current_rotational_required_Nm*1000,M_bound_Nmm)',
 'DesignTorqueNmm=max(M_bound_Nmm,current_rotational_required_Nm*1000,MotorTorque*Reduction*Efficiency*1000)',
 'AxialForceN=F_bound'
]
f['symbols']={'b_joint':'верхняя граница пути от оси подбираемого шарнира до начала данного звена, мм','R_part,R_tool':'оценки радиуса положения центра масс/точки силы, мм','Offset_child':'полный вектор монтажного смещения дочернего шарнира, мм','COM_part':'локальный центр масс фактической детали','reserve_mass':'оценка массы корпуса, если он включён, но ещё не построен'}
f['source']=['src/BodyLoadEnvelope.cs','src/ProceduralBodyModel.cs']
f['notes']='Корневое смещение самого подбираемого шарнира перемещает ось вместе с нагрузкой и не является плечом. Смещения дочерних суставов учитываются по каждому пути, а не суммой длин всех ветвей. Для известных внутренних вращений/орбит применяется радиальная граница ComponentRadius; неподдержанное вложенное движение отклоняется. current_required допускается только в Н·м: линейный корневой шарнир блокируется до расчёта. При current_required=0 оценка M_bound сохраняется. Оценка будущей массы остаётся приближённой.'
f=formulas['body.mass_envelope'];f['equations']=[
 'm_part=MassOverride, если задана; иначе Volume_mm3*Density*1e-9',
 'V_est=pi*((GearboxDiameter/2+6)^2-(GearboxDiameter/2)^2)*(MotorLength+GearboxLength)+pi*(400-256)*Length',
 'reserve_mass=max(0.25,V_est*Density*1e-9+0.18)',
 'reserve_radius=b_joint+travel_joint+max(Length,MotorLength+GearboxLength+GearboxDiameter+norm(MotorOffsetXY))',
 'MassKg=sum(учтённые фактические массы+Payload+резервы отсутствующих корпусов)']
f['source']=['src/BodyLoadEnvelope.cs','src/RobotCad.cs'];f['notes']='Резерв добавляется только при BodyGenerator.Enabled=true и GeneratedBody=null. При наличии готового корпуса учитываются массы и центры его отдельных деталей. Масса резерва является допущением, а не подтверждённой массой будущего тела.'
f=formulas['body.connected_section'];f['title']='Совместный подбор круглой трубы Connected=true'
f['equations']=[
 'fw=max(wall,FrameWall)*Style.Strength',
 'r0=max(18,fw+CableDiameter+4)*(1+0.12*(1-Style.Slimness))',
 'r_inner=r-fw',
 'A=pi*(r^2-r_inner^2)',
 'I=pi*(r^4-r_inner^4)/4',
 'J=2*I',
 'sigma=abs(DesignMomentNmm)*r/I+abs(AxialForceN)/A',
 'tau=abs(DesignTorqueNmm)*r/J',
 'sigma_equivalent=sqrt(sigma^2+3*tau^2)',
 'r=max(r0,fw+0.5); увеличивать r на 0.5 мм до sigma_equivalent<=AllowableStressMPa',
 'если допустимый радиус не найден до 70 мм — ошибка, геометрия не принимается'
]
f['symbols']={'wall':'max(2.4,MinWall), мм','fw':'стенка каркаса, мм','r':'наружный расчётный радиус, мм','A':'площадь поперечного сечения, мм²','I':'момент инерции круглого сечения, мм⁴','J':'полярный момент инерции круглой трубы, мм⁴','sigma,tau,sigma_equivalent':'напряжения, МПа'}
f['source']=['kernel/body_section.py','kernel/connected_body.py']
f['notes']='В 5.3 учтены изгиб, осевая сила и кручение. Для круглой трубы применяется упругая формула tau=T*r/J вместо тонкостенной аппроксимации Бредта старой ветки. Для rounded_rectangle круглая оценка остаётся приближённой. FEA стыков, вставок и печатной анизотропии отсутствует.'
f=formulas['cad.constraint_residuals'];f['equations']+=['directional_constraints: hypot(u_x,u_y)>=1e-7 мм для каждого участвующего отрезка']
f['notes']='Перед horizontal, vertical, parallel, perpendicular, angle и tangent длина каждого участвующего отрезка проверяется явно. Нулевая длина одного или обоих отрезков вызывает ошибку неопределённого направления. Дополнительный численный предохранитель length(v)=max(1e-10,hypot(v)) сохраняется; он сам по себе не заменяет эту проверку.'
f=formulas['cad.constraint_solver'];f['notes']+=' Начиная с 5.3 вырожденное направление отрезка отклоняется до вычисления направленных невязок.'
modules['robot']['source']+=['src/RobotJointControllers.cs']
module('controllers','Управление построенным роботом',
 ['src/RobotJointControllers.cs','src/RobotWorkspaceUX.cs','src/RobotProceduralWorkspace.cs','web/joint-controller.js','web/scene.js'],
 ['Нажать «Управление роботом», «Шарниры робота» в сцене или «4 · Движение и нагрузки». Всё открывается в текущем рабочем окне.',
  'Выбрать шарнир; изменить координату ползунком, точным вводом либо кнопками −/+. Общий шаг задаётся в градусах или миллиметрах.',
  'Потянуть кольцо вращательного шарнира или осевую стрелку линейного. Меняется координата шарнира, связанные звенья следуют по кинематической цепи.',
  'Во время движения обновляются поза и статические нагрузки. Размеры, STEP-тела и сетки заново не строятся.',
  'Отпускание завершает одно действие истории; Ctrl+Z отменяет всё перетаскивание. Esc отменяет текущий 3D-жест. «Все шарниры → 0» учитывает индивидуальные пределы.'],
 {'step_default':1,'step_range':[0.01,30],'step_units':'° для вращения, мм для линейной оси','numeric_decimal_places':2,'fixed_joints':'движение отключено','web_message_interval_ms':40,'home':'нуль с ограничением Minimum/Maximum','hardware_connection':False},
 ['Это управление моделью на экране; подключения к контроллерам реальных двигателей нет.',
  'Кольцо и стрелка требуют Three.js/WebView2. Ползунки и ввод доступны и при резервном WPF-просмотре.',
  'Пределы суставов ограничивают координату, но не предотвращают автоматически столкновения. Отдельно запустите CAD-проверку диапазона.'])
formula('controllers.rotation','controllers','Координата 3D-контроллера',
 ['q_delta=inverse(Q_start)*Q_proxy','angle=2*atan2(q_delta.z,q_delta.w)','increment=wrap_to_minus_pi_plus_pi(angle-previous_angle)','total_angle+=increment','value=clamp(start_value+RoundJS(rad_to_deg(total_angle)/step)*step,Minimum,Maximum)'],
 {'Q_start,Q_proxy':'кватернионы локальной системы контроллера; локальная Z направлена по мировой оси шарнира','RoundJS':'Math.round: середины округляются в сторону +∞','value':'угол шарнира, °'},['web/joint-controller.js'],
 'Развёртка угла сохраняет непрерывность при переходе через 180°. Применяются ограничения Minimum/Maximum. Конечная координата обязательно отправляется при отпускании, даже если последний промежуточный кадр пропущен ограничителем частоты.')
formula('controllers.linear','controllers','Линейный контроллер и шаговые кнопки',
 ['delta_mm=dot(proxy_world_position-start_world_position,axis_unit)','value=clamp(start_value+RoundJS(delta_mm/step)*step,Minimum,Maximum)','jog_value=clamp(current_visible_value+signed_step,Minimum,Maximum)'],
 {'axis_unit':'единичная мировая ось линейного шарнира','step':'шаг, мм для линейной оси; ° для вращательной','current_visible_value':'при остановке анимации сначала фиксируется видимая поза'},['web/joint-controller.js','src/RobotJointControllers.cs'])

data['worked_examples'].append({'id':'offset_payload_53','description':'Нагрузка 1 кг: от оси до конца родителя 100 мм, затем монтажное смещение дочернего шарнира ещё 500 мм по X. Вертикальный участок дочернего звена 20 мм не добавляет плечо тяжести вокруг Y. Другие массы в проверке обнулены.',
 'input':{'payload_kg':1,'parent_end_x_mm':100,'child_offset_x_mm':500,'child_end_z_mm':20,'g':9.81,'safety':1.5},
 'expected':{'actual_required_Nm':8.829,'path_radius_bound_mm':620,'payload_moment_bound_Nm':9.1233},
 'note':'M_bound >= фактического момента; собственное мировое смещение корневой оси результата не меняет.'})
data['verification_scope']={'controller_load_suite':'Проверки реальных C#-нагрузок, смещений/центров масс, 40 поз, раннего запрета линейного шаблона, истории управления и реального обмена WebView.',
 'gizmo_math':'22 проверки с поставляемым Three.js: локальные X/Y/Z оси, отрицательный угол, переход через 180°, ограничения, завершение/отмена и линейная проекция.',
 'section_sketch':'20 проверок: аналитические изгиб/кручение/осевая сила/совместное напряжение, рост сечения, ошибка невозможного сечения и вырожденные направления эскизов.',
 'manufacturing':'Проверки программной реализации не сертифицируют прочность или изготовленный робот.'}

data['program_version']='5.4.0'
data['purpose']='Справочный конфиг GearLab 5.4: корпус по референсу, управление, параметры, формулы и границы расчётов.'
modules={m['id']:m for m in data['modules']};formulas={f['id']:f for f in data['formulas']}
modules['body']['parameters']['engine']='gearlab-blender-elbow-2'
modules['body']['parameters']['native_revision']='blender-elbow-2'
modules['body']['parameters']['defaults']['ReferenceDesign']=False
modules['body']['parameters']['defaults']['InsertLength']=5.2
modules['body']['parameters']['reference_rule']='ReferenceDesign=true требует Connected=true. Старые документы без нового поля используют прежнюю геометрию.'
modules['body']['source']+=['kernel/reference_enclosure.py','src/ReferenceRobotDesign.cs','src/ProceduralBodyForm.cs']
modules['body']['limitations'][0]='Для прежнего трубчатого режима используется совместная номинальная оценка круглой трубы. В режиме ReferenceDesign она задаёт только исходный размер; напряжения коробчатого корпуса, вырезов, стыков, крепежа и печатных слоёв отдельно не решаются.'
modules['robot']['parameters']['six_axis_example'].update(lengths_mm=[110,220,170,100,100,65],poses_deg=[0,-45,90,0,60,0],reference_gripper=True)
modules['robot']['parameters']['document_defaults'].update(ReferenceGripper=False,GripperOpening=20)
modules['robot']['parameters']['gripper_opening_range_mm']=[0,40]
modules['robot']['source']+=['src/ReferenceRobotDesign.cs']
f=formulas['robot.gravity'];f['equations'][-1]='r_m=(load_point_world-origin_joint)/1000'
f['notes']='Вес детали приложен в её мировом центре масс. Полезная нагрузка и внешняя сила звена приложены в Tip, а у последнего звена с включённым референсным захватом — в точке TCP на конце пальцев. Это добавление выполнено вместе с геометрией захвата, чтобы масса груза не осталась ошибочно на запястье.'
f['source']+=['src/ReferenceRobotDesign.cs']
f=formulas['body.load_envelope'];f['equations']=[x.replace('R_tool=b_joint+travel_joint+norm(End_joint)','R_tool=b_joint+travel_joint+norm(End_joint)+ToolLoadExtension') for x in f['equations']]
f['symbols']['ToolLoadExtension']='для включённого захвата последнего звена: толщина инструментального фланца + 64 мм до конца пальцев; иначе 0'
formulas['body.connected_section']['notes']+=' Для ReferenceDesign=true этот расчёт используется как исходный размер, а не как решение напряжений произвольной кассеты; прочность остаётся pending.'
module('reference_design','Корпус по изображениям: скруглённая кассета и захват',
 ['kernel/reference_enclosure.py','kernel/connected_body.py','src/ReferenceRobotDesign.cs','src/ProceduralBodyModel.cs','src/ProceduralBodyForm.cs','web/scene.js','web/index.html'],
 ['Готовый пример открывает новый вариант; для своего проекта нажать «3 · Корпус → Корпус по референсу → всем». Размеры каркаса и моторы сохраняются.',
  'Между реальными фланцами строится непрерывный скруглённый корпус и отдельный замкнутый несущий короб. Занятые объёмы приводов вырезаются из геометрии.',
  'На широкой поверхности подбирается цельная крышка в углублении. Для крышки и остального кожуха проверяются как минимум два доступных крепления с опорами, реальными отверстиями и карманами вставок.',
  'Добавляются цилиндрические узлы, кольцевые накладки и боковые окна доступа. Осевой узел инструмента использует цилиндрический переход с открытым выходом к адаптеру захвата.',
  'Захват добавляется отдельной редактируемой сборкой; отверстия адаптера совмещаются с реальным инструментальным фланцем. Изменение раскрытия сохраняет нативные корпуса звеньев.',
  'Ракурсы «Референс» и просмотр «Студия» меняют только представление модели.'],
 {'mode':'ReferenceDesign','default_for_new_blank_settings':False,'new_example_mode':True,
  'reference_lengths_mm':[110,220,170,100,100,65],'reference_pose_deg':[0,-45,90,0,60,0],
  'effective_style_fields':{'Slimness':'глубина кассеты','Roundness':'радиус широких кромок с ограничением по габаритам','AccentColorCoverage':'ширина крышки','Strength':'стенка каркаса и стартовый размер'},
  'gripper':{'adapter_thickness_mm':4,'palm_mm':[60,28,30],'fingertip_z_from_adapter_mm':64,'opening_default_mm':20,'opening_range_mm':[0,40],'toggle_open_close_mm':[2,26]},
  'native_revision':'blender-elbow-2'},
 ['Форма построена по изображениям с известным монтажным габаритом NEMA 17. Заводские внутренние размеры и конструкция коммерческого робота из фотографий не восстановлены.',
  'Номинальное круглое сечение не подтверждает прочность произвольного коробчатого перехода после вырезов. ReadyForManufacture=false.',
  'Захват — компоновочная модель. Привод пальцев, усилие зажима, окончательные направляющие и крепёж не рассчитаны.',
  'На коротких узлах крышка автоматически уменьшается или смещается ради связности и доступа к винтам; неподходящие варианты явно отклоняются.',
  'Масса деталей захвата учитывается геометрически; полезная нагрузка приложена к TCP пальцев. Реальная масса ещё не добавленного привода захвата должна быть учтена отдельно.'])
formula('reference.cassette','reference_design','Размеры непрерывного корпуса и широких скруглений',
 ['U=(B-A)/norm(B-A)','N=normalize(e_z-U*dot(e_z,U)); при вырождении используется другая допустимая ось','V=normalize(cross(N,U))','R0=max(root_flange_radius+frame_wall,30)','R1=max(child_flange_radius+frame_wall,30)','H=max(24,min(R0,R1)*(1.05-0.30*Slimness))','edge_round=min(H*(0.4+0.38*Roundness),0.4*R0,0.4*R1)','структурный отступ = 0.45*edge_round','внешняя оболочка: R и H увеличены на gap+skin; радиус кромки увеличен на ту же величину'],
 {'A,B':'центры реальных ответных фланцев, мм','U,V,N':'ортонормированный базис корпуса','R0,R1':'радиусы концевых контуров кассеты, мм','H':'полуглубина, мм','Slimness,Roundness':'параметры стиля 0–1'},['kernel/reference_enclosure.py'],
 'Плоский контур — выпуклая оболочка двух окружностей, затем выдавливание и скругление широких кромок. Наружный и внутренний контуры кожуха используют согласованные отступы. Все булевы результаты и отдельные тела проверяются нативным CAD-ядром.')
formula('reference.panel','reference_design','Утопленная крышка и выбор креплений',
 ['panel_radius=max(8,min(R0,R1)*min(0.85,0.45+0.65*AccentColorCoverage),0.05*panel_length)','кандидаты длины: 1, 0.72, 0.5, 0.3 от исходной','кандидаты ширины: 1, 0.8, 0.6 от исходной','panel = Common(shell, rounded_panel_tool)','main = Cut(shell, expanded_recess_tool)','число связных тел panel=1; число связных тел main=1; доступных креплений каждого >=2'],
 {'Common,Cut':'булевы операции точных B-rep тел','expanded_recess_tool':'инструмент с монтажным зазором','доступное крепление':'луч попадает в поверхность, бобышка пересекает каркас и не пересекает защищённый привод'},['kernel/reference_enclosure.py'],
 'Для компактного запястья перебираются сторона, смещение и размер. Если нельзя закрепить оба элемента, корпус отклоняется. Винты выбираются из стандартных длин, головки утоплены в поверхность.')
formula('reference.tool_tcp','reference_design','Захват, раскрытие и точка нагрузки',
 ['jaw_center_x = +/- (Opening/2 + 2)','gap_between_inner_finger_faces = Opening','mount_thickness=max(10,2*max(2.4,MinWall)+InsertDiameter)','ToolLoadExtension=mount_thickness+64','TCP=Tip+OutputRotation*normalize(EndVector)*ToolLoadExtension'],
 {'Opening':'раскрытие 0–40 мм','Tip':'конец кости последнего звена','TCP':'центр конца пальцев, в мировой системе, мм','OutputRotation':'вращательная часть преобразования последнего звена'},['src/ReferenceRobotDesign.cs','src/RobotSolver.cs','src/BodyLoadEnvelope.cs'],
 'ToolLoadExtension действует только у последнего звена с включённым референсным захватом. Нагрузки в Н·м продолжают использовать плечо в метрах. Положение TCP совпадает с концом CAD-модели пальцев; привод зажима не моделируется.')
data['worked_examples'].append({'id':'gripper_tcp_54','description':'Нулевая поза шестиосевого примера; масса всех деталей в контрольной проверке обнулена, груз 0.5 кг. Горизонтальный вылет последней кости 65 мм, фланец 10 мм, пальцы до TCP 64 мм.',
 'input':{'payload_kg':0.5,'bone_x_mm':65,'mount_mm':10,'finger_tcp_mm':64,'g':9.81,'safety':1.5},
 'expected':{'lever_m':0.139,'shoulder_required_Nm':0.5*9.81*0.139*1.5},
 'note':'Проверка учитывает груз на пальцах, а не на более близком запястье. Моменты от собственной массы настоящей сборки добавляются отдельно.'})


module('author_robot','РОБОТ1.blend · размерный Г-образный корпус',
 ['src/RobotModel.cs','src/ProceduralBodyModel.cs','kernel/blender_enclosure.py','kernel/reference_enclosure.py','kernel/connected_body.py'],
 ['Исходная форма автора используется как размерный референс; треугольники не превращаются в STEP-грани.',
  'Масштаб задаётся по полной высоте исходной геометрии: 800 мм.',
  'Используются шесть реальных стыков с осями +Z, −Y, +Y, −Y, +Z, +Y; смещения костей сохраняются пространственно.',
  'Г-образное звено состоит из точного четверть-тора и цилиндра. Перпендикулярные узлы объединяют цилиндрическую ногу и округлый переход.',
  'Корпус охватывает следующий неподвижный статор и соединяется с его реальным задним монтажным фланцем.',
  'Моторные объёмы вырезаются из каркаса. Доступность вставок проверяется булевыми пересечениями; оболочка и крышка имеют собственный крепёж.',
  'На коротком запястье задние винты переносятся на доступную боковую сторону.',
  'Экспорт каждой детали проверяется повторным чтением STEP и сравнением объёма; внешний вид сверяется с исходным файлом.'],
 {'source':'РОБОТ1.blend','nominal_height_mm':800,'tube_diameter_mm':77.8111837325,'base_axis_raise_mm':10,'first_vertical_link_compensation_mm':-10,'wrist_side_clearance_mm':16.6,'FrameWall_mm':6.4,'SkinWall_mm':2,'InsertLength_mm':5.2,'GearboxLength_mm':[25,56,32,32,32,32],'AxisReversed_default':False,'native_revision':'blender-elbow-2'},
 ['Это реконструкция выбранного авторского корпуса, а не универсальный импорт произвольного BLEND.',
  'Для реальных NEMA 17 стык запястья расширен на 16.6 мм; ось основания поднята на 10 мм с компенсацией первого вертикального звена. Высоты остальных осей сохранены.',
  'Наружные цилиндры приводов увеличиваются ради толщины каркаса, оболочки и доступных вставок. Пиксельное совпадение с непечатной заготовкой не заявляется.',
  'Внутренняя передача, её выходной носитель, полная гибкая проводка и динамика робота не выводятся из внешней формы. ReadyForManufacture=false.',
  'Режим фиксирует круглый профиль; параметры тонкости и скругления кассетного варианта в нём не применяются. Доступны диаметр звена, стенки, усиление и доля цветной крышки.'])
formula('author.scale','author_robot','Масштаб и размещение осей автора',
 ['s=800/(2.1876449585-(-2.1889286041))=182.791398 мм/ед.',
  'P_i=s*(P_source_i-[0,0.2247911543,-2.1889286041])',
  'P_1.z += 10; End_1.z -= 10 (высоты P_2…P_6 не меняются)',
  'End_4.y -= 16.6 (дополнительное место для перпендикулярного NEMA 17)',
  'D_tube=0.425682962*s=77.8111837 мм',
  'Axis=AxisReversed ? -Axis_mode : Axis_mode'],
 {'s':'мм на единицу Blender','P_source_i':'центры измеренных швов, исходные координаты','P_i':'координаты осей, мм','AxisReversed':'обращает как вращение, так и ориентацию монтажной модели привода'},
 ['src/RobotModel.cs'], 'Точные измеренные координаты и SHA-256 исходного файла записаны в examples/reference/Robot1.measurements.json. Размерный исходник не является расчётом пригодности к печати.')
formula('author.elbow','author_robot','Круглое колено и силовая втулка следующего привода',
 ['r=max(r_section,D_tube/2-skin-gap,r_root_flange-frame_wall)',
  'R_bend=max(r+skin+gap+0.5,CableBendRadius)',
  'L=norm((B-A)-n_root*dot(B-A,n_root))',
  'L > R_bend+2*frame_wall',
  'колено: четверть-тора с большим радиусом R_bend и малым радиусом r',
  'прямой участок: цилиндр радиуса r, длина L-R_bend',
  'r_child_sleeve = r_child_case + gap + frame_wall',
  'каркас=Cut(наружный силовой объём,внутренний объём и защищённые приводы)',
  'оболочка: отступы gap и gap+skin; бобышки ограничены наружным силовым контуром'],
 {'A':'центр выходного фланца','B':'ось следующего привода','n_root':'ось выходного фланца','r_section':'радиус, выбранный по совместной изгибной, крутильной и осевой номинальной оценке'},
 ['kernel/blender_enclosure.py','kernel/reference_enclosure.py','kernel/connected_body.py'], 'Формула круглого сечения задаёт нижнюю границу размера. Она не подтверждает прочность всех локальных вырезов, колен и стыков. Корректность B-rep и отсутствие пересечений не заменяют расчёт прочности.')
formula('author.shaft_clearance','author_robot','Место для полного вала мотора и опоры основания',
 ['shaft_top = -GearboxLength - Plate + MotorShaftLength',
  'shaft_top + gap <= -BearingWidth',
  'ground_space = GroundDepth + rear_stator_plane',
  'ground_space >= mount_plate + floor_plate'],
 {'shaft_top':'конец монтажного габарита вала мотора, локальная координата мм','rear_stator_plane':'отрицательная координата нижней крышки','ground_space':'свободное место под крышкой до стола'},
 ['kernel/connected_body.py','src/ProceduralBodyModel.cs'], 'При недостатке места построение останавливается с указанием причины. Длина исходного вала мотора не укорачивается ради прохождения проверки.')

refs=sorted({s for group in ['modules','formulas'] for item in data[group] for s in item['source']}|{'src/AppSettings.cs'})
for ref in refs:
 p=root/ref
 assert p.is_file(),ref
 data['source_snapshot'][ref]=hashlib.sha256(p.read_bytes()).hexdigest().upper()

docs=root/'docs';docs.mkdir(exist_ok=True)
json_path=docs/'GearLab.config.json'
json_path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
settings={p['key']:p['default'] for p in data['runtime_settings']['parameters']}
(docs/'settings.example.json').write_text(json.dumps(settings,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

lines=['# GearLab 5.4 — конфиг, устройство и формулы','',
 'Основной структурированный файл: [GearLab.config.json](GearLab.config.json). Эта памятка создана из него.',
 '', '## Что можно менять','',*['- '+s for s in data['editing_rules']],
 '', '## Единицы','', '| Величина | Единицы |','|---|---|',
 *[f'| {k} | {v} |' for k,v in data['units'].items()],
 '', '## Реальные настройки программы','',
 '`%LOCALAPPDATA%/GearLab/settings.json` — действующий конфиг интерфейса. Безопаснее менять его через раздел «Настройки». '
 'Рядом лежит [settings.example.json](settings.example.json); это шаблон, существующий файл пользователя не изменён.',
 '', '| Поле | По умолчанию | Допустимо / описание |','|---|---|---|']
for p in data['runtime_settings']['parameters']:
 allowed=p.get('range',p.get('allowed',''))
 lines.append('| `'+p['key']+'` | `'+json.dumps(p['default'],ensure_ascii=False)+'` | '+(json.dumps(allowed,ensure_ascii=False)+'; ' if allowed!='' else '')+p['description']+' |')
lines+=['','В случае ошибки настроек программа использует значения по умолчанию. Восстановительные копии: `'+data['runtime_settings']['recovery']+'`.','']
for m in data['modules']:
 lines+=['## '+m['title'],'',*[(str(i+1)+'. '+s) for i,s in enumerate(m['workflow'])],
         '', '**Параметры и область действия:**','', '```json',json.dumps(m['parameters'],ensure_ascii=False,indent=2),'```',
         '', '**Реализованные формулы:**','']
 for f in data['formulas']:
  if f['module']!=m['id']:continue
  lines+=['### '+f['title']+' (`'+f['id']+'`)','','```text',*f['equations'],'```','',
          '; '.join(k+' — '+v for k,v in f['symbols'].items())+'.','']
  if f['notes']:lines+=[f['notes'],'']
  lines+=['Код: '+', '.join('`'+s+'`' for s in f['source'])+'.','']
 lines+=['**Ограничения:**','',*['- '+s for s in m['limitations']],'',
         'Исходники модуля: '+', '.join('`'+s+'`' for s in m['source'])+'.','']
lines+=['## Числовые примеры','']
for e in data['worked_examples']:
 lines+=['### '+e['id'],'',e['description'],'','```json',json.dumps({'input':e['input'],'expected':e['expected']},ensure_ascii=False,indent=2),'```','']
 if e.get('note'):lines+=[e['note'],'']
lines+=['## Актуальность описания','',
 'Сверено с исходниками выпуска 5.4.0, генератор `gearlab-blender-elbow-2` / `blender-elbow-2`. '
 'В `source_snapshot` основного JSON записаны SHA-256 использованных файлов: после изменения кода формулы и описание нужно проверить заново.',
 '', 'Справочник описывает существующую программу. Он не включает ещё не реализованные возможности из пожеланий и не является допуском к изготовлению робота.','']
(docs/'КОНФИГ_И_ФОРМУЛЫ.md').write_text('\n'.join(lines),encoding='utf-8')

print(json.dumps({'files':[str(docs/name) for name in ['GearLab.config.json','КОНФИГ_И_ФОРМУЛЫ.md','settings.example.json']], 'formulas':len(data['formulas']),'modules':len(data['modules']),'source_files':len(refs)},ensure_ascii=False,indent=2))
