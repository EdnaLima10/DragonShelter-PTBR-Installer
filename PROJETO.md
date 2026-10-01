# Dragon Shelter PT-BR — Patch 1 (instalador 1.1.0)

## Pastas

- `codigo/`: C# WinForms, leitor de catálogo e operações transacionais.
- `payload/`: pasta local privada, ausente do GitHub; o bundle é incorporado ao EXE.
- `distribuicao/`: somente os arquivos que devem ser enviados ao Nexus.
- `testes/`: programa de testes separado, fontes copiadas e instalações simuladas.
- `compilacao/`: caches e temporários do SDK; não distribuir.
- `pacote-nexus/`: saída privada de distribuição, ausente do GitHub.

## Compilar e testar

SDK .NET 10.0.302 necessário apenas no computador de desenvolvimento.
`compilar.ps1` publica Windows x64 autocontido em `distribuicao/`.
`testar.ps1` executa somente o programa interno de testes, nunca a interface.
Todas as gravações de teste ficam em uma nova pasta `testes/execucoes/<id>/`.
Não executar os testes apontando para a instalação real: a raiz é fixada pelo script.

As fontes de teste atuais são o bundle inglês do Patch 1 e o catálogo original da instalação atual. As fontes anteriores estão preservadas em testes/fontes/pre-patch1. Nenhum arquivo da instalação foi alterado.

## Identidades incorporadas

| Arquivo | SHA-256 |
|---|---|
| Catálogo original | `547fac4bc778376129ac0855e405ffd7845aadcc9922471f23cc725222779217` |
| Bundle original | `337a77c68bb7429677e4eeb4d6e1f9b8f23398c219456fa2d1d501a9a743c667` |
| Bundle PT-BR preservado | `141b5e97717f6708e4ab27f0b95423cae59a57394defe4805faecb8c2f2fdd3d` |
| Catálogo corrigido | `a345f06d8bb224a727a752a469e364aeb1ffdc07102e8e40aca39be71f482d24` |

CRC original 2920548108; novo 3469154707. Tamanho original 133123; novo 136062.
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
o runtime Windows Desktop. O teste do instalador no jogo foi concluído com sucesso pelo responsável pelo projeto.

## Validação realizada

22 testes internos aprovados, com os mesmos arquivos Catalog.cs e Installer.cs
compilados em um executável de teste separado. Relatório em `testes/execucoes/`.
Incluem hashes de origem, igualdade do catálogo corrigido, campos preservados,
verificação sem escrita, instalação/restauração, reinstalação idempotente,
arquivos desconhecidos/ausentes, payload corrompido, seis pontos de falha,
recuperação de par misto, backup corrompido, atualização externa, instalação
manual e bloqueio real de arquivo com recuperação posterior.

O responsável pelo projeto confirmou o teste de instalação e tradução no jogo.


## Cópia pública

Fontes privadas e resultados são excluídos desta árvore. Consulte README.md e testes/README.md para preparar o ambiente. Os scripts públicos verificam o payload e criam diretórios ausentes. Os fontes do instalador são idênticos aos usados na compilação Patch 1. Backups usam .dragon-shelter-ptbr-patch1; os backups V1 são preservados.
