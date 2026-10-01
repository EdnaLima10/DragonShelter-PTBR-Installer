# Substituição manual do repositório — Patch 1

Destino: https://github.com/EdnaLima10/DragonShelter-PTBR-Installer

Copie o **conteúdo** desta pasta para a raiz do repositório, preservando os subdiretórios. Nada foi publicado, nenhum commit ou push foi executado.

## Atualizar os arquivos existentes na raiz

- README.md
- PROJETO.md
- compilar.ps1

## Mover/substituir os quatro arquivos antigos da raiz

Remova do futuro estado do repositório os arquivos V1 `Catalog.cs`, `Installer.cs`, `Program.cs` e `DragonShelterPTBR.csproj` da raiz, substituindo-os pelas versões Patch 1 em **codigo/** com os mesmos nomes. Não mantenha ambas as cópias. A estrutura com codigo é a usada pelo projeto testado e pelo script de compilação.

## Adicionar

- codigo/ (quatro arquivos do instalador)
- testes/Program.cs, testes/Testes.csproj, testes/README.md
- testar.ps1
- .gitignore
- global.json
- licencas/ (três avisos originais, sem alteração)
- MANIFESTO_FONTES_SHA256.json
- VALIDACAO_FONTE_PATCH1.md
- este documento

## Manter fora do GitHub

Payload/bundles, catálogos e outros dados do jogo, traduções extraídas, fontes privadas de testes, resultados de testes, EXEs/DLLs/ZIPs, distribuição, caches de compilação e backups. Somente os arquivos desta pasta limpa devem ser selecionados no upload manual. Não copie a pasta de validação privada nem a pasta inteira do projeto de trabalho.

Não atribua uma licença nova ao código nesta atualização. Preserve os créditos e os avisos de terceiros incluídos.
