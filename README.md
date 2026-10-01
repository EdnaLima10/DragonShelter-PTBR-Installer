# Dragon Shelter PT-BR Installer — Patch 1

Código-fonte do instalador Windows x64 da tradução PT-BR para **Dragon Shelter Patch 1**. Versão do instalador: **1.1.0**. Esta é a edição compilada e testada no jogo pelo responsável pelo projeto, com 22 testes internos aprovados.

O instalador verifica a compatibilidade pelos hashes dos arquivos, faz backup dos originais, instala a tradução e permite restaurar o original. Edições diferentes e combinações desconhecidas são recusadas. Backups do Patch 1 usam `.dragon-shelter-ptbr-patch1`, separados dos backups V1.

## Estrutura

- `codigo/Program.cs`: interface Windows Forms e entrada do programa.
- `codigo/Installer.cs`: instalação, backups, recuperação e restauração.
- `codigo/Catalog.cs`: hashes homologados e atualização do catálogo Addressables.
- `codigo/DragonShelterPTBR.csproj`: .NET 10, Windows x64, executável autocontido.
- `compilar.ps1`: confere o payload, prepara diretórios locais, compila e copia as licenças.
- `testar.ps1`, `testes/Program.cs`, `testes/Testes.csproj`: testes com instalações simuladas.
- `global.json`: SDK usado para esta reprodução, .NET **10.0.302**.
- `licencas/`: licenças e avisos de terceiros preservados.
- `PROJETO.md`: detalhes técnicos; `PUBLICACAO_MANUAL.md`: migração do repositório V1.

## O payload NÃO pertence ao repositório

O código sozinho não contém a tradução. Para compilar o instalador completo, é necessário obter separadamente o payload aprovado e disponibilizá-lo **somente na máquina local**, em:

```text
payload/localization-string-tables-english(en)_assets_all_PTBR_PATCH1.bundle
```

Tamanho: **136062 bytes**. SHA-256:

```text
141b5e97717f6708e4ab27f0b95423cae59a57394defe4805faecb8c2f2fdd3d
```

Ele é incorporado como recurso `PTBR.bundle` durante a compilação. Não publique esse bundle, arquivos originais do jogo, catálogos, tabelas traduzidas, EXEs ou ZIPs neste repositório. O EXE também contém o payload. `.gitignore` protege os caminhos usuais, mas não substitui a conferência dos arquivos selecionados em um upload manual.

## Como reproduzir o EXE Patch 1

Requisitos: Windows x64, PowerShell e SDK .NET **10.0.302**. A primeira compilação precisa acessar o NuGet para obter os pacotes/runtime, salvo quando já houver cache local.

1. Copie ou baixe a árvore completa do repositório, preservando a pasta `codigo`. Prefira um caminho curto (por exemplo, `C:/src/DragonShelter`), pois ferramentas do Windows podem falhar com caminhos muito longos.
2. Instale o SDK indicado em `global.json`.
3. Crie `payload` localmente e coloque nela o bundle aprovado com o nome e hash acima.
4. Abra PowerShell na raiz desta árvore e execute:

```powershell
.\compilar.ps1
```

Resultado: `distribuicao/DragonShelterPTBR.exe`, autocontido para Windows x64, com licenças em `distribuicao/licencas`. Não é necessário instalar .NET na máquina do jogador. O script não instala a tradução nem altera arquivos do jogo.

Os quatro arquivos em `codigo` são cópias exatas do projeto que gerou a edição Patch 1. Os scripts desta cópia pública acrescentam verificações de arquivos privados e inicialização de diretórios para funcionar em uma árvore limpa; a lógica do instalador não foi alterada.

Isso permite reproduzir um instalador com o mesmo código, payload e comportamento. **Não é uma garantia de SHA-256 idêntico entre compilações**: caminho de compilação, runtime/pacotes e ambiente podem afetar os bytes do EXE. O EXE já validado pelo projeto tinha 116226238 bytes e SHA-256 `9e2cf7a37e47ac4e4825148236c56c747e02b5144fde90dddecdfbf52cdd3fdc`.

## Testes

Consulte `testes/README.md` para as quatro fontes privadas necessárias. Depois execute:

```powershell
.\testar.ps1
```

Os 22 testes criam cópias em `testes/execucoes/<id>`. Não execute nem adapte os testes para gravar na instalação real. A pasta do checkout pode ter qualquer nome; a raiz é validada pelos arquivos de projeto.

## Segurança

A verificação de SHA-256 precede a instalação. Só o bundle inglês e os campos CRC/tamanho de sua entrada no catálogo são substituídos. Backups são verificados e preservados. A restauração recusa arquivos desconhecidos. Não há assinatura Authenticode; o runtime é incorporado ao executável.

## Créditos e licenças

Brazilian Portuguese translation, revision and installer project: LanaLym.

Dragon Shelter and its original game assets belong to their respective developers and rights holders.

As licenças .NET, Windows Desktop e avisos de terceiros foram preservados em `licencas`. Não foi criada nem atribuída uma nova licença ao código do instalador; os avisos de terceiros não concedem direitos sobre o jogo ou a tradução.
