# Задание для Codex: выпустить GearLab 7.0.0 на GitHub

Скопируй это задание в Codex, открытый в папке `C:\Users\aselk\OneDrive\Desktop\GearLab\02_Исходный_код`. Соблюдай `AGENTS.md` и `CLAUDE.md`.

## Что сейчас лежит в папке

- Git: ветка `codex/desktop-handoff-691`, HEAD `79920016`. Кодовая база 6.9.1 (`715f4c5`).
- В рабочем дереве **не закоммичены** три обновления, сделанные Claude (Cowork):
  - **6.10.0** — современные инженерные вкладки, вид материала на 3D-модели, 3D-стрелки нагрузки, наглядная перегрузка. Запись: `docs/updates/6.10.0.md`, патч `docs/updates/patches/6.10.0.patch`.
  - **6.11.0** — свои редукторы в роботе («＋ Добавить в робота», «⚙ Редукторы», значки на суставах, «Мои редукторы»). `docs/updates/6.11.0.md`, `patches/6.11.0.patch`.
  - **7.0.0** — режим «◎ Просмотр» (звено под курсором разлетается на детали), оптимизация расчёта робота и обмена со сценой, версия 7.0.0. `docs/updates/7.0.0.md`, `patches/7.0.0.patch`.
- Номер версии 7.0.0 уже стоит в `src/AssemblyInfo.cs`, `ReleaseSupport.Version` и заголовках окон.
- Готовое описание релиза: `docs/RELEASE_7.0.0.md`.
- Готовый EXE от Claude: `..\01_Готовая_программа\GearLab_7.0.0.exe`. Его собрали не официальным `build.ps1`, поэтому в релиз его **не класть**: релизный EXE собери сам.
- `origin` указывает на локальный репозиторий на этом компьютере. GitHub: `https://github.com/mikko2281/GearLab`.

## Шаги

1. **Ничего не потерять.** Выполни `git status` и `git stash list`. Не делай reset, checkout файлов или clean, которые могут стереть рабочее дерево. Сначала сделай резервную копию: `git stash create`, а потом `git update-ref refs/backup/pre-7.0.0 <hash>`. Ещё лучше — скопируй рабочие файлы в архив.

2. **Ветка и коммиты.** Создай ветку `release/7.0.0` от текущего HEAD.
   - Лучше сделать три коммита по версиям. В отдельном `git worktree` от `79920016` по очереди примени `git apply` для `docs/updates/patches/6.10.0.patch`, `6.11.0.patch` и `7.0.0.patch`. Каждый коммит включает свои файлы из `docs/updates/files/<версия>.txt`, свою запись `docs/updates/<версия>.md` и изменения `index.json` и `ДЕРЕВО_ОБНОВЛЕНИЙ.md`.
   - Сообщения коммитов:
     - `6.10.0: modern engineering tabs, material look in 3D, load arrows, overload visuals`
     - `6.11.0: user reducers in robots, reducer badges and settings`
     - `7.0.0: inspect mode (exploded link on hover), robotics performance, version 7.0.0`
   - Если патч не применяется чисто (рабочее дерево 79920016 могло отличаться от коммита), сделай **один** коммит всего рабочего дерева: `GearLab 7.0.0 (includes 6.10.0 and 6.11.0)`.
   - После этого `git diff release/7.0.0 -- src web docs ДЕРЕВО_ОБНОВЛЕНИЙ.md` с рабочим деревом должен быть пустым.

3. **Сборка и тесты на Windows** (из корня):

   ```powershell
   .\src\build.ps1 -OutputFolder Windows-7.0.0
   $env:GEARLAB_PROFILE_DIR = Join-Path $PWD 'test-results\profile-700'
   .\test.ps1 -NoBuild -OutputFolder Windows-7.0.0 -ResultsFolder test-results\7.0.0 -Suites ui700,ui611,ui610,overload,overload-ui,compound,compound-ui,shared-ptk,train60,robot-transmission60,workspace64,upgrade62,viewer,robot,add-link673
   node .\tools\test_scene_intro.mjs
   node .\tools\test_render_scheduler.mjs
   node --experimental-vm-modules .\tools\test_joint_controller.mjs
   node --experimental-vm-modules .\tools\test_web_preferences672.mjs
   ```

   `web/scene.js` изменён, поэтому JS-проверки обязательны. Claude проверял только в Wine и Chromium: там `ui700` (14), `ui611` (31), `ui610` (28), `overload`, `compound`, `train60`, `robot-transmission60` и `workspace64` прошли. `robot` и `add-link673` упирались в отсутствие `runtime\python.exe`, а `overload-ui` в Wine не завершается — у 6.9.1 так же.
   - Если что-то падает, найди причину. Маленькую ошибку исправь отдельным коммитом и опиши её в `docs/updates/7.0.0.md`, в раздел «Проверки».
   - **Не выпускай релиз с падающими тестами** — сначала сообщи пользователю.

4. **Ручная проверка** (окно программы из `Windows-7.0.0`). Снимки сохрани в `docs/verification/7.0.0/`.
   - «Робототехника» → «◎ Просмотр»: наведение на звено разносит детали, уход курсора собирает звено. Посмотри и на роботе с процедурным корпусом.
   - «⚙ Редукторы»: значки на суставах, щелчок открывает карточку.
   - «Редукторы» → «＋ Добавить в робота» → новый робот → второе звено.
   - «Ступени» → Материалы / Нагрузки / Перегрузка: вид материала на модели, стрелки, тепловая окраска на 120 % предела.
   - Если что-то не так, это не повод молча выпускать: опиши проблему.

5. **Архив.** Собери пакет так же, как прошлые выпуски. Смотри `.github/workflows/release.yml`, `tools/ci_release.ps1`, `tools/Package-Windows.ps1`, `tools/package_local.py`, `tools/verify_package.py`.
   - Если упаковщик привязан к номеру версии, параметризуй его, а не копируй ещё один `package_7x.py` / `publish-7.0.yml`.
   - Имя архива: `GearLab-7.0.0-windows.zip`. Внутри `GearLab.exe` версии 7.0.0.0, обновлённая папка `web` (новые `scene.js` и `index.html`), `examples`, `kernel`, `runtime` и лицензии, как раньше.
   - Посчитай SHA-256 и проверь архив `tools/verify_package.py`.
   - **Опубликованные версии и архивы не заменять.**

6. **GitHub.**
   - Если нужно, добавь remote: `git remote add github https://github.com/mikko2281/GearLab.git`.
   - Запушь `release/7.0.0` и открой PR в основную ветку репозитория. Заголовок: `GearLab 7.0.0`, описание — из `docs/RELEASE_7.0.0.md`.
   - После проверок слей PR обычным merge, без force-push.
   - Поставь тег `v7.0.0` на коммит слияния и запушь тег.
   - Создай релиз:

     ```powershell
     gh release create v7.0.0 GearLab-7.0.0-windows.zip --title "GearLab 7.0.0" --notes-file docs/RELEASE_7.0.0.md
     ```

     Добавь в описание релиза SHA-256 архива и результаты тестов из шага 3. Это обычный релиз, не pre-release, но в тексте должно остаться предупреждение: конструкция предварительная, `ReadyForManufacture=false`.
   - Если нет прав на push, `gh` не авторизован или основная ветка защищена — остановись и скажи пользователю, что нужно сделать. Не обходи защиту.

7. **Дерево обновлений после релиза.**
   - Впиши настоящие SHA коммитов, тег `v7.0.0` и ссылку на релиз в `docs/updates/6.10.0.md`, `6.11.0.md`, `7.0.0.md`, `docs/updates/index.json` (`Commit`, `"Published": true` для 7.0.0) и `ДЕРЕВО_ОБНОВЛЕНИЙ.md`.
   - Убери пометки «не закоммичено».
   - Закоммить это как `docs: record 7.0.0 release` и запушь.

8. **Ответ пользователю:**
   - SHA коммитов;
   - ссылка на PR и релиз;
   - SHA-256 архива;
   - что прошло, что упало и что не проверено.

## Ограничения

- `ReadyForManufacture=false`. Не выдавать оценки за проверенную прочность. Где нет метода или данных — «не определено».
- Не менять код сверх исправления найденных ошибок. Функции 7.0.0 описаны в `docs/updates/7.0.0.md`.
- Не удалять старые `publish-*.yml` и `package_*.py` без отдельного согласия пользователя.
