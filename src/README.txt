Gear Lab 1.5.0
Build: powershell -ExecutionPolicy Bypass -File .\build.ps1
.NET Framework 4.8; WinForms/WPF; vendored LibTessDotNet with license.
Tests: --self-test, --preset-test, --micro-test, --print-test, --visibility-test, --viewer-test, --cad-test, --ui-test <folder>.
1.5.0: normal presets reset all CAD dimensions after Micro; preserve the selected 3D view.
ResetCadSettings and the "Сбросить 3D" button restore case/support parameters without refitting or changing working profiles, ratio, or AutoFit mode. Invalid custom geometry remains blocked.
Micro: CycloidLayout=1, MotorPreset=5; defaults and independent assumptions in MICRO_REFERENCE.md. Legacy projects keep CycloidLayout=0.
CadTransitions supports a large recess terminating in several smaller holes; shared by STEP and STL triangulation.
