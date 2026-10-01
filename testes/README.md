# Testes do instalador Patch 1

`testar.ps1` executa 22 testes usando o mesmo Catalog.cs e Installer.cs do instalador. A tradução aprovada precisa estar no caminho privado documentado no README principal.

Obtenha separadamente estas fontes originais e coloque-as **apenas localmente**, sem commit/upload:

| Caminho relativo à raiz | SHA-256 |
|---|---|
| testes/fontes/catalog.original.json | 547fac4bc778376129ac0855e405ffd7845aadcc9922471f23cc725222779217 |
| testes/fontes/bundle.original.bundle | 337a77c68bb7429677e4eeb4d6e1f9b8f23398c219456fa2d1d501a9a743c667 |
| testes/fontes/pre-patch1/catalog.original.json | d3840b0dcbafa591c087a39cafb27a64d4cff55c93543b83eca58706de95ec84 |
| testes/fontes/pre-patch1/bundle.original.bundle | 6fe36d8aa2c79f356e7634a95ca79f6e38883069174deeddc69f8343111a2b87 |

As duas primeiras são do Patch 1; as demais são da versão anterior, usadas para testar a recusa dessa versão e a preservação de backups antigos. Não substitua as fontes ausentes por arquivos de outra versão. Sem essas fontes privadas, a suíte completa não pode ser executada; o script informa o arquivo ausente.

A única adaptação no programa de testes desta cópia é aceitar qualquer nome de checkout, validando os dois arquivos de projeto. Todos os cenários e gravações em uma subpasta exclusiva de testes/execucoes foram mantidos.
