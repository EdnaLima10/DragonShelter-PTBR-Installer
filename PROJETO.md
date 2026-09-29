# Dragon Shelter PT-BR — V1

## Pastas

- `codigo/`: C# WinForms, leitor de catálogo e operações transacionais.
- `payload/`: cópia do bundle PT-BR aprovado, incorporada ao executável.
- `distribuicao/`: somente os arquivos que devem ser enviados ao Nexus.
- `testes/`: programa de testes separado, fontes copiadas e instalações simuladas.
- `compilacao/`: caches e temporários do SDK; não distribuir.
- `pacote-nexus/`: ZIP final montado a partir de `distribuicao/`.

## Compilar e testar

SDK .NET 10 necessário apenas no computador de desenvolvimento.
`compilar.ps1` publica Windows x64 autocontido em `distribuicao/`.
`testar.ps1` executa somente o programa interno de testes, nunca a interface.
Todas as gravações de teste ficam em uma nova pasta `testes/execucoes/<id>/`.
Não executar os testes apontando para a instalação real: a raiz é fixada pelo script.

As fontes de teste foram copiadas do bundle original de trabalho e do backup
original já existente. Os arquivos de origem não foram alterados.

## Identidades incorporadas

| Arquivo | SHA-256 |
|---|---|
| Catálogo original | `d3840b0dcbafa591c087a39cafb27a64d4cff55c93543b83eca58706de95ec84` |
| Bundle original | `6fe36d8aa2c79f356e7634a95ca79f6e38883069174deeddc69f8343111a2b87` |
| Bundle PT-BR preservado | `374656a92913e98939216cf813e81ef925a3e5eee7cf2813a9f083f4583f9ffd` |
| Catálogo corrigido | `e3baa58c3b271a8e587bc3401eac0eab4d961943e3b6e5b656c3672fdc46ddef` |

CRC original 2252795409; novo 1220118888. Tamanho original 129866; novo 132745.
O resultado do patch deve reproduzir o SHA-256 integral do catálogo homologado.

## Segurança e limites

Sem dependência do diagnóstico ou de caminhos pessoais no executável.
Entrada localizada por InternalId exato normalizado, provider, tipo e unicidade.
Opções localizadas pela serialização; sem índice/offset fixo no código produtivo.
Substituição com comprimento preservado e hash final obrigatório.
Backup verificado antes da troca, nunca sobrescrito por arquivos traduzidos.
Troca por temporário no mesmo volume e File.Replace; sem fallback de truncamento.
Mutex impede duas operações desta ferramenta na mesma sessão do Windows.
Processo Dragon Shelter aberto impede operações de instalação/restauração.
Não há bloqueio universal contra Steam, outro usuário ou programas externos.
Rollback em falhas tratáveis; falha persistente de armazenamento/permissão pode
deixar recuperação pendente. Não existe atomicidade conjunta dos dois arquivos.
Restauração recusa bytes desconhecidos em vez de apagar alterações externas.

Não há assinatura Authenticode. O executável é autocontido e grande por incluir
o runtime Windows Desktop. A GUI e o jogo não foram executados nesta entrega.

## Validação realizada

20 testes internos aprovados, com os mesmos arquivos Catalog.cs e Installer.cs
compilados em um executável de teste separado. Relatório em `testes/execucoes/`.
Incluem hashes de origem, igualdade do catálogo corrigido, campos preservados,
verificação sem escrita, instalação/restauração, reinstalação idempotente,
arquivos desconhecidos/ausentes, payload corrompido, seis pontos de falha,
recuperação de par misto, backup corrompido, atualização externa, instalação
manual e bloqueio real de arquivo com recuperação posterior.

Ainda pendentes por instrução: executar a GUI e validar a tradução dentro do jogo.
