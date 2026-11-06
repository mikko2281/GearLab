# Настройки GearLab 6.3 / Preferences

Откройте «Настройки» на верхней панели. Выберите раздел слева, измените значения и нажмите «Применить». Цвета и язык действуют в открытых и новых рабочих пространствах и сохраняются между запусками.

- **Оформление:** темы Blender — тёмная / светлая. Выбор темы заменяет индивидуальную палитру после применения.
- **Цвета:** фон окна, панели, заголовки, поля, основной и вторичный текст, границы, акцент, выделение, фон сцены/чертежа, сетка, три оси, успех, предупреждение и ошибка. Нажмите образец или введите `#RRGGBB`. Геометрия и цвета деталей в STEP/STL сохраняются.
- **3D и анимация:** качество перестраивает текущую сетку предпросмотра; чувствительность меняет вращение камеры; скорость действует во всех трёх редакторах; привязка и шаг регулируют перемещение деталей.
- **Язык:** русский или English. Названия и идентификаторы в файлах проекта не переписываются, числовой формат ввода сохраняется.
- **Сохранение:** восстановительные копии проектов робота, включение и интервал. Основной проект сохраняйте обычной командой «Сохранить».

«Применить» сохраняет независимую копию. Последующие правки остаются черновиком. «Отмена» возвращает последнее применённое состояние; в отдельном диалоговом окне также закрывает его. «Стандартные» подготавливает исходные значения до применения. Ошибочный HEX или число не сохраняются. Повреждённый файл настроек заменяется стандартными значениями с сообщением; перед следующим сохранением создаётся `.bak`.

Настройки прежних версий читаются с сохранением камеры, качества, анимации и автосохранения; оформление переводится в новую тёмную тему. Версия 6.2 и пользовательские проекты не удаляются.

## English

Open **Preferences** in the top bar. Choose a category on the left, edit values, then click **Apply**. Appearance offers dark/light Blender-inspired presets; Colors provides 17 editable interface and viewport roles using swatches or `#RRGGBB`. 3D and animation controls preview detail, orbit sensitivity, playback speed and snapping. Language switches Russian/English without restarting. Saving configures robot recovery copies; use Save for your main project file.

Apply saves an independent copy. Further edits remain a draft; Cancel discards them. Restore defaults prepares defaults until Apply. Invalid values are rejected. Corrupt preferences recover to defaults with a diagnostic, and the previous file is backed up before saving. Project data, identifiers, geometry and numeric parsing are preserved.

Windows common dialogs and their standard buttons follow the OS language. Older engineering diagnostics and source/example names can retain Russian text; controls, property names, viewport commands and the built-in English help use the selected language.
