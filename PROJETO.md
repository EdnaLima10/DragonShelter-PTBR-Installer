# Dragon Shelter PT-BR — Patch 3 (instalador 1.3.0)

## Pastas

- `codigo/`: C# WinForms, leitor de catálogo e operações transacionais.
- `payload/`: pasta local privada, ausente do GitHub; o bundle é incorporado ao EXE.
- `distribuicao/`: somente os arquivos que devem ser enviados ao Nexus.
- `testes/`: programa de testes separado, fontes copiadas e instalações simuladas.
- `compilacao/`: caches e temporários do SDK; não distribuir.
- `pacote-nexus/`: saída privada de distribuição, ausente do GitHub.

## Patch 3

Esta versão atualiza o instalador e a tradução PT-BR para o Patch 3 do jogo.

- Instalador: `DragonShelterPTBR_Patch3.exe`
- Plataforma: Windows x64
- Versão do instalador: 1.3.0
- Bundle PT-BR: `localization-string-tables-english(en)_assets_all_PTBR_PATCH3.bundle`
- 17 tabelas
- 4.114 entradas no total
- 90 entradas novas traduzidas
- 12 entradas revisadas
- 4.012 entradas mantidas do Patch 2
- 0 entradas removidas

O Patch 3 foi construído a partir da versão PT-BR anterior, preservando IDs, Path IDs, nomes internos, referências e demais metadados necessários.

## Compilar e testar

SDK .NET 10.0.302 necessário apenas no computador de desenvolvimento.

`compilar.ps1` publica o instalador Windows x64 autocontido em `distribuicao/`.

`testar.ps1` executa o programa interno de testes, utilizando uma instalação simulada.

As gravações de teste ficam em uma nova pasta dentro de `testes/execucoes/`.

As fontes utilizadas nos testes são mantidas dentro da estrutura de testes do projeto.

## Identidades incorporadas

Os arquivos-fonte utilizados pelo instalador possuem hashes SHA-256 registrados em `MANIFESTO_FONTES_SHA256.json`.

O manifesto permite verificar a integridade dos arquivos-fonte utilizados na compilação.

Os hashes atuais registrados no manifesto incluem:

| Arquivo | SHA-256 |
|---|---|
| `codigo/Catalog.cs` | `2B7B700C67885A1CF29934BE85A05362317EDFEA4CD09B6DCEDED831D40DE979` |
| `codigo/Installer.cs` | `36976976D8B693EEA70F722511E490875775792E6C411288A332A9132F9832BA` |
| `codigo/Program.cs` | `B0932A9C1D83743FC64D40CFDB2E1B71A8B65C290909E720B4774938C3444` |
| `codigo/DragonShelterPTBR.csproj` | `22CDAD4FC4687F2A37FAD47FC1DA2AA14AD3D2C30A08B1CC273E12CD428E5471` |

## Segurança e limites

O instalador verifica a compatibilidade da instalação antes de realizar alterações.

A identificação da versão utiliza hashes SHA-256 dos arquivos necessários para a instalação.

A entrada é localizada por InternalId exato normalizado, provider, tipo e unicidade.

As opções são localizadas pela serialização, sem dependência de índice ou offset fixo no código produtivo.

A substituição exige validação dos arquivos antes da alteração.

Backups dos arquivos originais são criados antes da instalação.

A troca dos arquivos utiliza operação temporária no mesmo volume e `File.Replace`.

Um mutex impede duas operações desta ferramenta na mesma sessão do Windows.

O processo Dragon Shelter aberto impede operações de instalação ou restauração.

A restauração recusa arquivos com bytes desconhecidos em vez de apagar alterações externas.

Não há assinatura Authenticode. O executável é autocontido e inclui o runtime necessário para Windows x64.

## Validação realizada

O instalador e seus componentes foram atualizados e testados para o Patch 3.

A validação inclui verificação de hashes, integridade dos arquivos, instalação, restauração e testes do instalador.

O responsável pelo projeto confirmou o funcionamento da instalação e da tradução no jogo.

## Cópia pública

As fontes públicas do instalador estão disponíveis neste repositório.

Arquivos privados, payloads e resultados de distribuição não fazem parte da árvore pública.

Consulte `README.md` e `testes/README.md` para informações adicionais sobre o projeto e os testes.

O código-fonte publicado corresponde à versão atualizada para o Patch 3.
