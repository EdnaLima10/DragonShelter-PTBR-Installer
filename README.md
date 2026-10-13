# Dragon Shelter PT-BR Installer — Patch 3

Instalador da tradução brasileira (PT-BR) para Dragon Shelter.

## Patch 3

Esta versão atualiza o instalador e a tradução para o Patch 3 do jogo.

- Instalador: `DragonShelterPTBR_Patch3.exe`
- Plataforma: Windows x64
- Release: 1.3.0
- Bundle PT-BR: `localization-string-tables-english(en)_assets_all_PTBR_PATCH3.bundle`
- 17 tabelas
- 4.114 entradas no total
- 90 entradas novas traduzidas
- 12 entradas revisadas
- 4.012 entradas mantidas do Patch 2
- 0 entradas removidas

O Patch 3 foi construído a partir do PT-BR Patch 2, preservando IDs, Path IDs, nomes internos, referências e demais metadados necessários.

## Instalação

O instalador verifica a compatibilidade da instalação antes de realizar qualquer alteração.

A identificação do Patch 3 utiliza os hashes SHA-256 do `catalog.json` e do bundle inglês correspondente. O payload PT-BR também é validado antes da instalação.

O instalador cria backups dos arquivos originais em:

`.dragon-shelter-ptbr-patch3`

O recurso **Restaurar original** utiliza esses backups para restaurar o bundle e o catálogo originais.

## Testes

Foram aprovados **26 testes** em cópias de teste, incluindo:

- instalação e restauração;
- criação e validação de backups;
- validação independente do catálogo;
- falhas durante diferentes etapas;
- bloqueio de arquivos;
- recuperação de instalação interrompida;
- recusa de arquivos incompatíveis;
- preservação dos backups das versões anteriores.

## Limite da validação

A validação estrutural e dos mecanismos de instalação foi concluída em cópias de teste.

O instalador Patch 3 ainda não foi executado na pasta real do jogo, e não houve teste visual da tradução em uma partida durante esta validação.

## Estrutura

- `codigo/` — código-fonte do instalador
- `licencas/` — licenças utilizadas
- `testes/` — código e evidências dos testes

## Integridade

SHA-256 do bundle PT-BR Patch 3:

`8f2f33ebf439752ab3c41d80951a5ef6c422a9264e1c0c53025575104afd2e22`

SHA-256 do instalador Patch 3:

`d4e7021adcd03224dc2e7069138ba6751325c4fb1a6153ae38d869e821cd1fbc`

Fontes, testes, licenças e evidências completas estão disponíveis neste repositório.
