# Сторонние компоненты

GearLab остаётся бесплатной программой без общей открытой лицензии. FREEWARE относится к собственному коду GearLab и не ограничивает права по лицензиям библиотек.

| Компонент | Версия / условия |
|---|---|
| LibTessDotNet | SGI Free Software License B 2.0, встроен в EXE; licenses/LibTessDotNet.txt |
| Python | 3.12.14 x64, PSF и включённые уведомления; runtime/LICENSE.txt, licenses/Python.txt |
| OCP | cadquery-ocp 8.0.1.1.0, Apache-2.0; licenses/OCP-Apache-2.0.txt |
| Open CASCADE | 8.0.1, LGPL-2.1 с исключением; licenses/OCCT-LGPL-2.1.txt, OCCT-exception-notice.txt |
| VTK | 9.6.2, BSD; связанные с OCP библиотеки, без Python-приложения VTK; licenses/VTK.txt |
| Нативные зависимости | FreeImage (FIPL), FreeType (FTL), OpenEXR/Imath, JPEG, LittleCMS, Lerc, XZ/liblzma, PNG, WebP, OpenJPEG/OpenJPH, LibRaw (LGPL), TIFF, zlib/zstd/libdeflate, Viskores и вспомогательные библиотеки; уведомления в licenses/ |
| Microsoft VC Runtime | DLL из официальных колёс/Python; [условия Microsoft](https://visualstudio.microsoft.com/license-terms/vs2022-cruntime/), не FREEWARE GearLab |

Оригинальные уведомления сохранены в метаданных колёс. licenses/ORIGINS.json содержит происхождение и SHA256 текстов, runtime/NATIVE_IMPORTS.json — фактические нативные зависимости. licenses/sources/ содержит исходники OCCT 8.0.1, LibRaw 0.22.2 и систему сборки/патчи OCP 8.0.1.1.0. Библиотеки не модифицировались. Выбор необходимых DLL при комплектовании не меняет их машинный код.

Python и DLL доступны отдельными файлами и загружаются динамически. Их можно заменить совместимыми версиями или собственной сборкой, в том числе для отладки таких изменений. Условия собственного кода GearLab не запрещают изменение библиотек и необходимое для отладки изменений обратное исследование в пределах лицензий библиотек.

Исходники и сборка: [OCCT](https://github.com/Open-Cascade-SAS/OCCT/tree/V8_0_1), [OCP](https://github.com/CadQuery/OCP), [колёса/патчи](https://github.com/CadQuery/ocp-build-system/tree/v8.0.1.1.0), [LibRaw](https://github.com/LibRaw/LibRaw/tree/0.22.2), [VTK](https://gitlab.kitware.com/vtk/vtk), [Python](https://github.com/python/cpython/tree/v3.12.14). Геометрия Micro: src/MICRO_REFERENCE.md исходного комплекта.


5.0: Three.js 0.180.0 (MIT), локальные модули/OrbitControls/TransformControls; лицензия web/vendor/LICENSE-three.txt. Microsoft WebView2 SDK 1.0.4314.35; LICENSE.txt и NOTICE.txt из upstream NuGet включаются в licenses/WebView2 готового комплекта. Хэши и официальные источники: web/vendor/ORIGINS.json. Собственный код GearLab по MIT не лицензируется.
