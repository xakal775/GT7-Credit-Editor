# GT7 Credit Editor

Editor de créditos **simples, gratuito e offline** para o save local do port **RyGT7** no Windows.

> **Projeto independente da comunidade.** Não é afiliado, desenvolvido ou endossado pela Polyphony Digital, Sony Interactive Entertainment ou pelos responsáveis pelo port RyGT7. **Não é um editor para contas online/PSN.**

## ⬇️ Download

Baixe o arquivo `GT7CreditEditor.exe` na seção **Releases** deste repositório (versão `v1.0.0`).

- Executável portátil, sem instalador.
- Windows x64 com .NET Framework 4.x (normalmente disponível no Windows 10/11).
- Não exige Cheat Engine nem outras bibliotecas de terceiros.
- Código-fonte completo disponível em [`src/GT7CreditEditor.cs`](src/GT7CreditEditor.cs).

## 🎮 Como usar

1. **Feche totalmente o jogo**, incluindo `GT7.exe` e `RyGT7.exe`.
2. Execute `GT7CreditEditor.exe`.
3. O editor procura automaticamente o arquivo `_sd\gt7offline\gt7save.bin` quando executado dentro de uma subpasta da instalação. Se necessário, clique em `...` e selecione manualmente seu `gt7save.bin`.
4. Confira **SALDO ATUAL**.
5. Informe o valor desejado em **NOVO SALDO** (de `0` até `100.000.000` créditos).
6. Clique em **SALVAR CRÉDITOS** e confirme.
7. Abra o jogo para conferir o novo saldo.

**Exemplo:** se você possui `5.350 Cr` e digita `100.000 Cr`, o editor define o saldo final para `100.000 Cr` (não soma `100.000` ao valor anterior).

## 🛡️ Segurança e backups

O programa foi criado para reduzir o risco de perder o progresso:

- Detecta se `GT7.exe` ou `RyGT7.exe` está aberto e **bloqueia a gravação**.
- Confere a estrutura esperada do save antes de editar.
- Compara o hash SHA-256 do arquivo com o estado carregado; se ele mudou, exige recarregar.
- Faz **backup integral da pasta `gt7offline`** antes de qualquer alteração.
- Altera somente o campo de **4 bytes** correspondente aos créditos, preservando os outros bytes.
- Grava primeiro um arquivo temporário e realiza substituição atômica com backup adicional.
- Confere o resultado após salvar.

### Onde ficam os backups?

Para a instalação exemplificada abaixo, os backups são criados em:

```text
C:\GAMES\GT7\_SAVE_BACKUPS\GT7CreditEditor_AAAAMMDD_HHMMSS_mmm\
```

Se você escolher outro `gt7save.bin`, a pasta `_SAVE_BACKUPS` será criada **dois níveis acima da pasta do save** selecionado.

### Como restaurar um backup?

1. Feche totalmente `GT7.exe` e `RyGT7.exe`.
2. Localize a pasta de backup criada pelo editor.
3. Copie de volta os arquivos da pasta de backup para a pasta `gt7offline` original, **sem apagar o backup**.
4. Abra o jogo e confira seu progresso.

## ✅ Compatibilidade

| Item | Situação |
| --- | --- |
| RyGT7 para Windows (build usada nos testes de 16/11/2026) | Leitura do save real validada |
| Saldo entre 0 e 100 milhões | Suportado |
| Editor aberto junto com o jogo | Permitido para consulta; **gravação bloqueada** |
| Outros ports, versões de save ou plataformas | Não validados |
| Conta online/PSN | Não suportado |

O offset de créditos foi identificado por comparação de saves locais antes/depois de uma compra no jogo. O teste automatizado confirma que a alteração é restrita ao campo dos créditos. O teste de leitura no save real foi realizado **sem modificá-lo durante o desenvolvimento do editor**.

Se surgir **“Versão do save não reconhecida”**, o aplicativo interrompe a operação. Não force a edição: abra uma issue com a **versão do RyGT7 e a mensagem de erro**, mas **não envie seu save pessoal** publicamente.

## 🧰 Compilar a partir do código

Requer Windows e o compilador C# `csc.exe` do .NET Framework:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe /optimize+ /platform:x64 `
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
  /out:GT7CreditEditor.exe .\src\GT7CreditEditor.cs
```

Teste da lógica de créditos (gera apenas dados sintéticos):

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:exe /platform:x64 `
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll `
  /out:GT7CreditEditor.Test.exe .\src\GT7CreditEditor.cs
& .\GT7CreditEditor.Test.exe --self-test
```

Resultado esperado: `SELFTEST_OK 5350 -> 100000; only credit bytes changed`.

## ❓ Perguntas frequentes

**Preciso instalar o programa?** Não. Ele é um `.exe` portátil.

**Preciso colocar o `.exe` na pasta do RyGT7?** Não é obrigatório: ele permite selecionar o save. Para a busca automática, coloque-o na subpasta `Tools` da instalação.

**O saldo aumenta ou é substituído?** É **substituído** pelo número informado.

**O que acontece se o jogo estiver aberto?** O editor permite visualizar o saldo gravado, mas não salva mudanças até fechar o jogo.

**Meu save não foi reconhecido.** Pode ser outra estrutura/versão. Não utilize patches de versões incompatíveis.

**Por que não funciona com minha conta PlayStation?** O projeto edita apenas saves locais do port offline RyGT7.

## 🗒️ Versões

Consulte o [CHANGELOG](CHANGELOG.md). A primeira versão é `v1.0.0`.

## 🌎 English summary

**GT7 Credit Editor** is a free, portable Windows tool for editing the **local offline RyGT7** save balance (`gt7save.bin`). Close the game, run the tool, select your save, enter the desired balance, and click **SALVAR CREDITOS**. It creates a complete backup before editing and blocks writes while the game is running. **No PSN/online support.** See the Portuguese instructions above for full details.

## 📄 Licença

Código disponibilizado sob a licença MIT. Veja [`LICENSE`](LICENSE).
