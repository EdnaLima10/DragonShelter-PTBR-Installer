# Substituição manual do repositório — Patch 3

Destino: https://github.com/EdnaLima10/DragonShelter-PTBR-Installer

Copie o conteúdo da pasta de preparação do Patch 3 para o repositório, preservando os subdiretórios.

## Atualizar os arquivos existentes

- README.md
- PROJETO.md
- compilar.ps1
- MANIFESTO_FONTES_SHA256.json

## Atualizar as pastas existentes

- codigo/ — substituir os quatro arquivos-fonte pelo código atualizado do Patch 3:
  - Catalog.cs
  - Installer.cs
  - Program.cs
  - DragonShelterPTBR.csproj

- testes/ — atualizar:
  - Program.cs
  - Testes.csproj
  - README.md

- licencas/ — atualizar os arquivos de licença quando houver nova versão correspondente ao Patch 3.

## Adicionar ou manter

- testar.ps1
- .gitignore
- global.json
- VALIDACAO_FONTE_PATCH3.md
- este documento

## Manter fora do GitHub

Payload/bundles, catálogos e outros dados do jogo, traduções extraídas, fontes privadas de testes, resultados de testes, EXEs/DLLs/ZIPs de distribuição, caches de compilação e backups.

O bundle PT-BR do Patch 3 não faz parte do repositório público. Ele é incorporado ao executável do instalador durante a compilação.

Somente os arquivos da estrutura pública do projeto devem ser publicados no GitHub.

Não copiar a pasta inteira do projeto de trabalho nem arquivos privados de validação ou distribuição.

## Integridade

Os hashes SHA-256 dos arquivos-fonte utilizados na compilação do Patch 3 estão registrados em MANIFESTO_FONTES_SHA256.json.

O código publicado deve corresponder à versão utilizada para gerar e testar o instalador do Patch 3.

## Licenças e créditos

Não atribuir uma licença nova ao código nesta atualização.

Preservar os créditos e os avisos de terceiros incluídos no projeto.
