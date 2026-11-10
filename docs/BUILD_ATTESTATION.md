# Подтверждение происхождения сборки GearLab

Workflow [Build and attest GearLab 6.7.2](https://github.com/mikko2281/GearLab/actions/workflows/attest-release.yml) заново собирает программу, выполняет native CAD и программные проверки, проверяет переносимый архив, создаёт SLSA provenance через GitHub OIDC / Sigstore и проверяет подписи командой `gh attestation verify`.

Рабочий коммит содержит исходный код 6.7.2 плюс workflow, вспомогательный скрипт и эту документацию. Перед сборкой проверяется, что остальные файлы совпадают с исходным коммитом тега `v6.7.2` — `dbdaae4c2c8315b1d0729748608e73805bcd74ea`. Точный коммит новой сборки указан в provenance и build-info.

В [релиз](https://github.com/mikko2281/GearLab/releases/tag/v6.7.2) добавляются отдельные файлы `GearLab_6.7.2_attested_<run>_<attempt>_*`: Windows/source ZIP, сведения о сборке, контрольные суммы, Sigstore bundle, результаты проверки и инструкция VERIFY.txt. Исходные релизные файлы и тег сохраняются. Подтверждение относится к новым файлам и EXE из нового архива; старые сборки автоматически его не получают.

Проверка скачанного архива с помощью GitHub CLI:

```powershell
gh attestation verify <путь-к-новому-Windows.zip> -R mikko2281/GearLab --signer-workflow mikko2281/GearLab/.github/workflows/attest-release.yml --source-digest <SourceCommit-из-build-info>
```

После распаковки таким же способом можно проверить `GearLab_6.7.2/GearLab.exe`. Подписи отдельно подтверждают EXE, оба ZIP, build-info и контрольные суммы. VERIFY.txt содержит готовую команду с именем архива и коммитом.

Это внешняя подпись происхождения сборки, а не Windows Authenticode: издатель Windows не меняется. Подтверждение связывает файлы с исходным кодом и workflow; оно не является гарантией отсутствия ошибок или инженерной готовности робота.

Документация: [GitHub artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations), [GitHub CLI verification](https://cli.github.com/manual/gh_attestation_verify).
