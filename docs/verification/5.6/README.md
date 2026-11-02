# GearLab 5.6 verification

Local interactive EXE SHA256: 0F76CD70417C28CD59592C5CF7FEBF495050EEFADAFC6ECA01174F3B28218203.
reducer56-results.json: actual ready GPU scene for 50:1, 80:1, 100:1, common MainForm/menus/buttons, sliders, family switching, geometry cache, animation and project migration. No native worker starts during the entire preview session.
fast-preview-results.json: independent headless meshing of all three ratios.
baseline-5.5.json: the old OCCT construction alone, without extra mesh/UI work.
wave-export-results.json and step-validation-results.json: exact full STEP export (19 valid solids, including motor and input shaft), cached print subset (3 valid solids). This export is intentionally separate from interactive preview.
Regression reports describe the same production implementation before adding the headless test entry and workspace-close guard. The final executable's GUI performance was independently rechecked above.
Server package EXE may have another hash. Its independent native checks are documented in docs/CI_RELEASE_VERIFICATION.md. Reports in 5.4 and 5.5 folders are historical, not tests of this release.
