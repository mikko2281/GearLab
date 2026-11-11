# Карта проекта

| Путь | Назначение |
| --- | --- |
| `src/GearCalculator.cs` | Точка входа приложения и команды тестов, общие расчёты |
| `src/RobotDesignerForm.cs` | Документ робота, последовательное добавление звеньев, история изменений, сборка сцены |
| `src/RobotWorkspaceUX.cs` | Быстрый инспектор и независимая параллельная цепь |
| `src/RobotModel.cs` | Параметры звеньев, примеры, сохранение и прямая кинематика |
| `src/RobotCad.cs`, `src/RobotProceduralCad.cs` | Сборка механических и процедурных деталей в сцену |
| `src/ProceduralBodyModel.cs` | Спецификация CAD-генератора, fingerprint и результаты |
| `src/RobotConnections.cs` | Проверка сопряжений и совпадения отверстий |
| `kernel/` | Python/OCCT: реальная геометрия корпусов, STEP и проверки |
| `src/SettingsForm.cs`, `src/AppSettings.cs`, `src/UiAppearance.cs` | Настройки, миграция и оформление |
| `src/WpfSceneAnimation.cs`, `src/Appearance680Tests.cs` | Показ сборки и регрессии активной анимации |
| `.github/workflows/publish.yml`, `tools/publish_current.py` | Общая публикация текущей версии после CI |
| `web/` | Three.js/WebView2: показ деталей и управление сценой |
| `examples/` | Примеры проектов и сжатые кэши CAD |
| `src/*Tests.cs`, `tools/test_*` | Регрессии приложения и независимые проверки геометрии |
| `src/build.ps1` | Компиляция EXE .NET Framework |
| `test.ps1`, `tools/test_robot.ps1` | Общий набор проверок и robot-наборы |
| `tools/ci_build.ps1`, `tools/ci_verify.ps1` | Подготовка зависимостей и CI-проверки |
| `tools/package_local.py`, `tools/verify_package.py` | Локальный комплект с версией из AssemblyInfo и проверка архива |
| `.github/workflows/windows.yml` | Действующий общий CI; оставить |
| `runtime/`, `src/vendor/`, `web/vendor/`, `licenses/` | Python/OCCT и внешние зависимости с лицензиями |
| `docs/` | Документация и ограниченные отчёты проверок |

# Кандидаты на удаление после вывода исторических публикаций из эксплуатации

Ничего не удалено. Версионные workflow всё ещё ссылаются на одноимённые инструменты: удалять их следует одним изменением. `attest-release.yml` и `attest_release.py` обслуживают опубликованную 6.7.2; их сохранять до переноса аттестации на общий процесс. Старый номер в названии регрессионного теста сам по себе не делает тест ненужным.

- `.github/workflows/preview-6.0.yml`
- `.github/workflows/publish-5.6.yml`
- `.github/workflows/publish-6.1.yml`
- `.github/workflows/publish-6.2.yml`
- `.github/workflows/publish-6.3.yml`
- `.github/workflows/publish-6.4.yml`
- `.github/workflows/publish-6.4.1.yml`
- `.github/workflows/publish-6.5.yml`
- `.github/workflows/publish-6.6.yml`
- `.github/workflows/publish-6.7.yml`
- `.github/workflows/publish-6.7.1.yml`
- `.github/workflows/publish-6.7.2.yml`
- `.github/workflows/repair-6.2-notes.yml`
- `tools/package_preview60.py`
- `tools/package_63.py`
- `tools/package_64.py`
- `tools/package_641.py`
- `tools/package_65.py`
- `tools/package_66.py`
- `tools/package_67.py`
- `tools/package_671.py`
- `tools/package_672.py` — после переноса зависимости `attest_release.py`
- `tools/publish_preview60.py`
- `tools/publish_56.py`
- `tools/publish_61.py`
- `tools/publish_62.py`
- `tools/publish_63.py`
- `tools/publish_64.py`
- `tools/publish_641.py`
- `tools/publish_65.py`
- `tools/publish_66.py`
- `tools/publish_67.py`
- `tools/publish_671.py`
- `tools/publish_672.py`
- `tools/repair_62_notes.py`
- `tools/verify66.ps1`, `tools/verify67.ps1` — дублирующие обёртки; сами вызываемые ими регрессии сохранить
- `tools/test_audit_preview3.ps1`, `tools/test_robot_audit_preview3.ps1`, `tools/check_preview3_step.py` — исторические проверки конкретной preview-сборки
- `src/Reducer56Tests.cs` — тест старой волновой модели; актуальный ПТК проверяют `Wave56FastTests` и `Upgrade62Tests`

`tools/Package-Windows.ps1`, `tools/Package-Source.py`, `tools/prepare_ci_release.py`, `tools/ci_release.ps1`, `.github/workflows/release.yml` образуют старый общий процесс 5.6: кандидатами на удаление станут вместе, после окончательного выбора общего процесса упаковки и публикации. `verify_package.py`, актуальные `test_*`, исходники тестов и совместимость старых проектов сохранять.

# Ветки и версии

Одна задача — одна короткая ветка от актуального main. Для этой задачи используется явно заданная `fix/add-link-direction`. После локальной проверки пользователь подтверждает результат; затем PR и CI, review, слияние в main. Тег `v6.7.3` и релиз — отдельный разрешённый шаг после подтверждения. Опубликованные теги и архивы неизменяемы. Исправления повышают PATCH, функции — MINOR со сбросом PATCH, крупная смена — MAJOR. Версию хранить в одном месте и подставлять в интерфейс, EXE, документацию и упаковку. Версионные publish/package-скрипты заменить одним параметризованным процессом, не наращивать копии для каждого выпуска.
