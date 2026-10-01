# Validação da cópia pública — Patch 1

- Os quatro arquivos em codigo são idênticos byte a byte aos do projeto que produziu o instalador Patch 1. Hashes em MANIFESTO_FONTES_SHA256.json.
- Compilação com o compilar.ps1 desta cópia concluída em uma árvore privada separada, com o payload aprovado fornecido localmente.
- 22 testes aprovados com testar.ps1 desta cópia, usando fontes privadas fora da pasta pública.
- Confirmada a incorporação exata do payload aprovado no EXE reconstruído.
- A limitação de caminhos longos do Windows encontrada na validação foi resolvida usando uma raiz curta; orientação incluída no README.
- Caches e fontes privadas não fazem parte da pasta para upload.
- EXE distribuído, bundles, código do projeto original e pacote Nexus preservados por conferência de SHA-256.
- Licenças copiadas sem alterações. Nenhuma licença nova atribuída ao código.
- Nenhuma publicação, commit ou push realizados.

SHA-256 do EXE reconstruído na validação: 9e2cf7a37e47ac4e4825148236c56c747e02b5144fde90dddecdfbf52cdd3fdc.
A reprodução funcional foi verificada; diferenças binárias entre ambientes não são tratadas como divergências de tradução.
