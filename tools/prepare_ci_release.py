"""Record CI verification honestly alongside the local interactive GUI evidence."""

import sys,json,hashlib

from pathlib import Path

root=Path(__file__).resolve().parents[1];request=Path(sys.argv[1]);commit=sys.argv[2]

release=json.loads(request.read_text(encoding='utf-8-sig'))

assert release['draft'] is True and release['tag_name']=='v5.6.0','Only the selected draft may be updated'

digest=hashlib.sha256((root/'Windows/GearLab.exe').read_bytes()).hexdigest().upper()

report=root/'docs/verification/5.4/README.md'

report.write_text('> The interactive GUI results below describe the locally tested desktop build. This release package is rebuilt from the same application source by GitHub Actions. Its exact binary and native runtime are independently verified without opening desktop windows.\n\n'+report.read_text(encoding='utf-8'),encoding='utf-8')

(root/'docs/CI_RELEASE_VERIFICATION.md').write_text(f'''# Server package verification



Source commit: `{commit}`.

Packaged executable SHA-256: `{digest}`.

Runtime: Python 3.12.10 x64, cadquery-ocp 8.0.1.1.0, VTK 9.6.2.



The server verifies both six-axis cached projects in the executable, native CAD operations, motion bounds, combined section stresses, degenerate sketch handling, an actual hole cut in the exported analytic robot STEP, the flat wave reducer solids and STEP roundtrip, independent fast preview without a native worker, and shipped controller math. The complete portable archive is checked by CRC, its file hashes and an isolated native CAD operation.



Interactive Windows GUI and full fresh robot generation results are recorded separately in docs/verification/5.4 (historical) and docs/verification/5.6 (current); docs/verification/5.5 is historical. Hosted CI does not claim to rerun those desktop GUI suites. The robot remains a preliminary layout, ReadyForManufacture=false.

''',encoding='utf-8')

body=release['body'].replace('на итоговом EXE повторно пройдены','на локальной проверенной сборке повторно пройдены')

body+='\n\nПакет этого релиза собран GitHub Actions из того же кода приложения. Его EXE, сохранённые CAD-проекты, нативное ядро, изменение STEP и переносной архив дополнительно проверены без открытия интерфейсных окон. GUI-проверки выше относятся к локальной Windows-сборке; подробности — docs/CI_RELEASE_VERIFICATION.md. Серверный комплект использует Python 3.12.10 x64.\n'

request.write_text(json.dumps(dict(tag_name='v5.6.0',target_commitish=commit,name=release['name'],body=body,draft=True,prerelease=False),ensure_ascii=True),encoding='ascii')

print('CI package metadata ready',digest)

