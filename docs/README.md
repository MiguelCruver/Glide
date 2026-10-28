# Glide — site de divulgação

Página em português para apresentar o Glide, com a identidade fornecida por Miguel Cruver. Fundo preto puro, logo original, cristais em CSS 3D, perspectiva pelo cursor, superfícies de vidro, vitrine com capturas reais e crédito ao criador.

## Abrir e editar

Não há dependências de instalação nem etapa de compilação. Abra `index.html` no navegador ou sirva esta pasta com um servidor estático. Exemplo com Python:

```sh
python -m http.server 58961 --bind 127.0.0.1
```

Acesse `http://127.0.0.1:58961`. Use outra porta se ela estiver ocupada.

Arquivos principais:

- `index.html`: conteúdo e estrutura.
- `styles.css`: identidade visual, vidro, cristais 3D e layouts responsivos.
- `app.js`: interação de perspectiva, abas acessíveis e aviso de download.
- `config.js`: endereço do instalador, quando disponível.
- `assets/`: logo, capturas e fonte local.

## Nitidez e interação 3D

A logo principal usa `assets/glide-crisp.png`, uma restauração visual produzida com a ferramenta integrada de imagem, preservando a referência fornecida. O original `glide.jpeg` permanece disponível. Ambos têm 1254 × 1254 pixels: houve restauração de nitidez, não aumento comprovado de resolução ou recuperação exata de detalhes perdidos.

Brief utilizado: restaurar a logo fielmente, preservar silhueta do G, cinco barras, símbolo de play, cores e reflexos; reduzir artefatos de JPEG; manter preto puro; não adicionar elementos nem redesenhar a marca.

As capturas do app permanecem intactas. Clique em “Ampliar imagem” e depois “Tamanho original” para ver cada arquivo na resolução nativa, sem ampliar artificialmente textos.

O efeito de perspectiva move logo, painéis e reflexos em camadas CSS 3D. Não é um modelo volumétrico da logo. Funciona pelo cursor e pelas setas com a cena em foco. O controle “Efeito 3D interativo” permite pausar ou ativar o movimento. Por escolha explícita do criador, a interação 3D inicia ligada e pode ser pausada pelo controle. A preferência de reduzir movimentos continua desativando a animação automática de entrada. Não há movimento contínuo.

## Download do app

Configurado para `Glide-Setup-v2.0.0.exe`, disponível na release pública `v2.0.0` (pré-lançamento). O site verifica esse arquivo pela API pública do GitHub e libera o botão quando a verificação tem sucesso. Em caso de indisponibilidade, mantém o aviso de preparação.

O endereço é fixo nessa versão, pois `/releases/latest/` não seleciona pré-lançamentos. Para outra versão, atualize `downloadUrl`, `releaseApiUrl` e `assetName` em `config.js`.

## Publicação no GitHub Pages

Site público: https://miguelcruver.github.io/Glide/

Código do site em `docs/` no repositório `MiguelCruver/Glide`, branch `master`. O workflow `.github/workflows/pages.yml` publica essa pasta pelo GitHub Actions. A configuração Source do GitHub Pages deve permanecer em GitHub Actions. Mudanças nos arquivos do site nessa branch acionam nova publicação.

Todos os caminhos de assets são relativos, permitindo hospedagem em `/Glide/`. A pasta inclui `.nojekyll`. O código do aplicativo foi preservado.

## Conteúdo e privacidade

As descrições se baseiam nas telas fornecidas. Não são prometidos números de desempenho, certificações, criptografia validada ou integrações verificadas em execução. A vitrine identifica as imagens como capturas do app, não como uma execução do programa no navegador.

O site não inclui analytics, formulários, cookies próprios ou cadastro. Consulta a API pública do GitHub uma vez por carregamento para verificar o instalador configurado. Fonte e imagens são servidas localmente. A hospedagem escolhida pode manter seus próprios registros de acesso.

## Assets e licença

- Logo e capturas: fornecidas pelo usuário para este projeto; direitos e permissões de terceiros continuam sob responsabilidade dos respectivos titulares. Não há concessão de licença para redistribuição geral desses materiais.
- Space Grotesk: fonte local, Google Fonts, SIL Open Font License 1.1. Licença em `assets/OFL.txt`.
- Ícones: desenhos SVG simples incluídos no próprio HTML; nenhuma biblioteca externa.

## Verificação

Consulte `VERIFICACAO.md`. O site não substitui a validação do instalador ou dos recursos do app.
