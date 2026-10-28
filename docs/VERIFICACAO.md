# Verificação — 28/10/2026

## Estado

Interface de divulgação implementada localmente. Download real pendente do instalador e de um endereço público aprovado. Site ainda não publicado.

## Conferências realizadas

- Sintaxe de `app.js` e `config.js` verificada com `node --check`, sem erros.
- Página aberta no navegador integrado com as imagens e a fonte local carregadas.
- Revisão visual em computador (viewport padrão de 1280 px), celular de 390 px, celular compacto de 320 px e tablet de 768 px.
- Corrigidos vazamento horizontal dos elementos decorativos, quebra excessiva do título no celular e sobreposição entre texto e logo no tablet.
- Abas de música, Connect e mensagens acionadas no navegador. Setas do teclado alternaram aba ativa, imagem e associação do painel.
- Botão de download abriu o aviso de preparação. Fechamento por Escape devolveu o foco ao botão; fechamento pelo botão “Entendi” também verificado.
- Links para recursos e sobre o projeto acionados e seções inspecionadas visualmente.
- Nenhum erro ou aviso retornado pelo registro do navegador consultado durante a revisão.
- Revisão independente por outro agente, somente por leitura de HTML/CSS/JS: nenhum achado acionável.

## Limitações

- O ambiente estava com `prefers-reduced-motion: reduce`. Foi observado que a perspectiva pelo cursor e a animação inicial respeitam essa preferência. A execução das animações com a preferência desativada não foi verificada visualmente.
- Layouts conferidos em larguras simuladas no navegador integrado; sem testes em aparelhos físicos, Safari ou Firefox.
- Revisão de teclado e estrutura semântica realizada, mas sem auditoria integral de WCAG, leitor de tela ou medição de desempenho.
- Não foi acessado o repositório privado do app, criado ou executado instalador, nem validada a implementação dos recursos do programa.
- Não há build, servidor de aplicação, dependências npm, contas, analytics ou banco de dados. A publicação em GitHub Pages não foi executada.

## Próximo passo

Receber o endereço do repositório do app, verificar a existência de instalador e definir um canal público de distribuição. Depois preencher `downloadUrl` em `config.js` e testar o download real antes de publicar.

## Revisão de nitidez e 3D — 28/10/2026

- Logo restaurada pelo gerador integrado de imagens e conferida visualmente contra a referência. PNG de 1254 × 1254 px; mesma dimensão nativa do JPEG original, com bordas e superfícies mais limpas. Capturas originais do aplicativo preservadas.
- Adicionados painel de ampliação e modo de tamanho nativo. No navegador, a captura do Connect foi exibida com largura renderizada de 1178 px, igual à largura natural do arquivo.
- Controle explícito de movimento verificado com a preferência de redução ativa. Cena responde ao cursor e às setas quando ativada; a matriz de transformação 3D e deslocamentos dos painéis foram observados no navegador.
- Verificado no navegador que Escape fecha a captura ampliada e devolve o foco ao botão de ampliação.
- Sete cenários de teste de interação passaram, cobrindo redução por padrão, ativação explícita, limites de rotação, desativação, ativação padrão quando permitida, saída do cursor e alteração da preferência do sistema.
- O modelo visual continua usando a imagem da logo em camadas CSS 3D. Não foi criado um arquivo de modelagem 3D volumétrico.

A limitação anterior sobre falta de verificação da perspectiva foi resolvida por ativação explícita do controle local. Permanecem as limitações de aparelhos físicos, outros navegadores e auditoria completa de acessibilidade.

## Revisão de responsividade e ativação padrão — 28/10/2026

- Conforme pedido posterior do criador, o efeito 3D agora inicia ativo mesmo com a preferência de redução de movimentos. O controle de pausa permanece disponível; a animação automática de entrada continua desativada por essa preferência.
- Cabeçalho sem limite central de largura: marca à esquerda, navegação ao centro e download à direita. Em 1920 px, a marca ficou a 72 px da borda esquerda, em vez de acompanhar um contêiner estreito centralizado.
- Conteúdo principal ampliado para até 1440 px. Composição em duas colunas desde 768 px; abaixo disso, conteúdo empilhado com texto alinhado à esquerda.
- Layouts conferidos em 1920, 1366, 1280, 1024, 768, 390 e 320 px. Nas medições de 1920, 1366, 1024, 768, 390 e 320 px, a largura rolável foi igual à largura útil da página: sem vazamento horizontal.
- Em 768 px, texto e cena ocupam colunas de 334,5 px, separadas por 20 px, sem sobreposição dos blocos.
- Oito cenários de teste de ativação padrão, interação, pausa e centralização passaram. Sintaxe do JavaScript conferida. No navegador, o estado ativo foi confirmado logo após recarregar, e as setas produziram rotação 3D sem ativação manual.
- Removido o desfoque de fundo dos pequenos painéis na cena 3D, preservando transparências, gradientes e reflexos para evitar artefatos de composição em tamanhos diferentes.

## Publicação verificada — 28/10/2026

Site publicado em https://miguelcruver.github.io/Glide/. Workflow 40790376612, tentativa 2: sucesso. Página respondeu HTTP 200. Configuração e conteúdo enviados nos commits 896704d e 5f1727b.

Verificação no navegador público: efeito 3D ativo por padrão, consulta da release concluída e mensagem de download disponível. Clique no botão baixou Glide-Setup-v2.0.0.exe para Downloads. Arquivo disponível na release v2.0.0, marcada como pré-lançamento. O executável não foi aberto nem instalado.

Esta publicação resolve as pendências anteriores de hospedagem e conexão do download. Permanecem as limitações anteriormente registradas sobre validação do app e ambientes não testados.
