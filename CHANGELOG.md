# Changelog

## v1.0.0 — 2026-11-16

- Primeira versão pública do editor portátil RyGT7.
- Interface Windows escura com saldo atual e entrada de novo saldo.
- Detecção automática do save e seleção manual de arquivo.
- Validação da seção `PROGRESS` do save e créditos big-endian de 32 bits.
- Limite configurado para 0–100.000.000 créditos.
- Bloqueio de gravação enquanto `GT7.exe` ou `RyGT7.exe` estiver aberto.
- Backup completo da pasta do save, mais backup atômico.
- Verificação SHA-256 do arquivo antes da alteração.
- Teste automatizado com arquivo sintético e confirmação dos bytes editados.
