---
name: EchoBoard
description: Mesa de mixagem operacional para sons, voz e roteamento local no Windows.
colors:
  primary: "#D3D3D3"
  primary-light: "#146EF5"
  primary-hover: "#F0F0F0"
  primary-pressed: "#AFAFAF"
  neutral-bg: "#111111"
  neutral-surface: "#171717"
  neutral-elevated: "#202020"
  neutral-card: "#191919"
  neutral-selected: "#2D2D2D"
  neutral-hover: "#252525"
  neutral-border: "#303030"
  neutral-border-soft: "#252525"
  text-primary: "#F5F5F5"
  text-secondary: "#B8B8B8"
  text-disabled: "#8A8A8A"
  on-primary: "#121212"
  success: "#D0D0D0"
  warning: "#A6A6A6"
  error: "#EAEAEA"
  light-bg: "#F5F7FB"
  light-surface: "#FFFFFF"
  light-elevated: "#F8FAFD"
  light-border: "#D8E0ED"
  light-border-soft: "#E7ECF3"
  light-text-primary: "#111827"
  light-text-secondary: "#5B6475"
  light-success: "#168A60"
  light-warning: "#B77900"
  light-error: "#C53030"
  light-accent-cyan: "#087E8B"
  light-accent-violet: "#6D4DE3"
  light-accent-emerald: "#168A60"
  light-accent-rose: "#C93663"
typography:
  display:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "28px"
    fontWeight: 600
  headline:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "18px"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "14px"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI, sans-serif"
    fontSize: "12px"
    fontWeight: 600
    letterSpacing: "0.06em"
rounded:
  sm: "4px"
  md: "6px"
  lg: "8px"
  pill: "999px"
  player: "19px"
spacing:
  2: "2px"
  4: "4px"
  8: "8px"
  12: "12px"
  16: "16px"
  20: "20px"
  24: "24px"
  32: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "36px"
  button-secondary:
    backgroundColor: "{colors.neutral-selected}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "36px"
  button-icon:
    backgroundColor: "{colors.neutral-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "8px"
    size: "40px"
  input-search:
    backgroundColor: "{colors.neutral-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "8px 14px"
    height: "40px"
  card-panel:
    backgroundColor: "{colors.neutral-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.lg}"
    padding: "20px"
  status-card:
    backgroundColor: "{colors.neutral-card}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "12px 14px"
  hotkey-badge:
    backgroundColor: "{colors.neutral-selected}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.pill}"
    padding: "4px 10px"
  player-play:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.player}"
    size: "38px"
---

# Design System: EchoBoard

## Overview

**Creative North Star: "Mesa de mixagem operacional"**

EchoBoard é uma interface de controle de áudio, não um painel administrativo genérico. O sistema visual organiza entradas, efeitos, níveis, dispositivos e reprodução como uma mesa de mixagem compacta: cada superfície deve ajudar o usuário a localizar estado e agir rapidamente, sem competir com o áudio ou com a chamada em andamento.

A atmosfera é precisa, limpa, discreta e orientada à operação rápida. O tema escuro usa preto e grafite em camadas tonais, bordas finas e texto claro; ações, seleção e estados também permanecem em tons neutros. O tema claro preserva a paleta azul e os acentos selecionáveis existentes. Ícones e rótulos mantêm a leitura dos estados de áudio nos dois temas.

**Key Characteristics:**

- Controle operacional e leitura imediata de estado.
- Superfícies tonais planas com bordas discretas.
- Tons claros de cinza reservados para comandos, seleção e progresso no tema escuro.
- Densidade compacta, tipografia Segoe UI e hierarquia curta.
- Tema escuro inicial monocromático; paletas de acento selecionáveis no tema claro.

## Colors

A paleta é temática. No tema escuro, superfícies, ações, foco e estados usam uma escala de preto e cinza. O tema claro conserva a paleta azul e as alternativas de acento Blue, Cyan, Violet, Emerald e Rose.

### Primary

- **Cinza de ação** (`{colors.primary}`): comandos primários, seleção ativa, foco visual, progresso e indicadores de reprodução no tema escuro.
- **Azul de ação para tema claro** (`{colors.primary-light}`): variante preservada quando o tema claro está ativo.
- **Cinza de resposta** (`{colors.primary-hover}`): estado de passagem do ponteiro sobre ações escuras.
- **Cinza pressionado** (`{colors.primary-pressed}`): confirmação tátil de comandos acionados no tema escuro.

### Secondary

- **Ciano de sinal** (`{colors.light-accent-cyan}`): alternativa de acento disponível no tema claro.
- **Violeta de sinal** (`{colors.light-accent-violet}`): alternativa de acento disponível no tema claro.
- **Esmeralda de sinal** (`{colors.light-accent-emerald}`): alternativa de acento disponível no tema claro.
- **Rosa de sinal** (`{colors.light-accent-rose}`): alternativa de acento disponível no tema claro.

### Neutral

- **Noite de operação** (`{colors.neutral-bg}`): canvas principal do tema escuro.
- **Superfície de trabalho** (`{colors.neutral-surface}`): shell, áreas de conteúdo e painéis principais.
- **Superfície elevada** (`{colors.neutral-elevated}`): topbar, player inferior, busca e camadas que precisam se separar sem sombra.
- **Cartão de controle** (`{colors.neutral-card}`): cards, campos, botões de ícone e áreas interativas.
- **Seleção grafite** (`{colors.neutral-selected}`): seleção, estados ativos e botões secundários.
- **Hover de superfície** (`{colors.neutral-hover}`): resposta discreta de navegação e controles.
- **Borda estrutural** (`{colors.neutral-border}`): contornos de 1px que organizam a interface.
- **Borda suave** (`{colors.neutral-border-soft}`): divisores e separações de menor ênfase.
- **Texto primário** (`{colors.text-primary}`): títulos, valores e informação operacional principal.
- **Texto secundário** (`{colors.text-secondary}`): captions, metadados, rótulos e orientação auxiliar.
- **Texto desabilitado** (`{colors.text-disabled}`): estados indisponíveis e informações sem ação.
- **Base clara** (`{colors.light-bg}`), **superfície clara** (`{colors.light-surface}`) e **elevação clara** (`{colors.light-elevated}`): equivalentes do tema claro.

### Named Rules

**The Signal Over Decoration Rule.** Cor deve indicar ação, seleção ou estado do áudio; não deve virar textura ou preenchimento ornamental.

**The Neutral Dark State Rule.** O tema escuro distingue sucesso, atenção e erro por rótulo, ícone e intensidade de cinza; o tema claro preserva as cores semânticas atuais.

## Typography

**Display Font:** Segoe UI (padrão nativo do Windows)

**Body Font:** Segoe UI (padrão nativo do Windows)

**Label/Mono Font:** não há uma família separada; labels usam Segoe UI com peso e espaçamento de caracteres.

**Character:** A tipografia é familiar, funcional e silenciosa. Pesos SemiBold e Bold criam pontos de ancoragem; o tamanho reduzido de captions e labels mantém a interface densa sem transformar o produto em uma tela de dados.

### Hierarchy

- **Display** (SemiBold, 28px): títulos de páginas e superfícies de maior contexto.
- **Headline** (SemiBold, 18px): títulos de seção, títulos de cards e cabeçalhos do player.
- **Title** (SemiBold, 14px): nomes de controles, itens e informação operacional curta.
- **Body** (Regular, 14px): descrições, instruções e texto legível de apoio.
- **Label** (SemiBold, 12px, character spacing aproximado de 0.06em): metadados, badges e rótulos curtos; labels de seção podem reduzir para 10px quando funcionam como eyebrow.

### Named Rules

**The Short Hierarchy Rule.** Toda tela deve declarar poucas camadas tipográficas claras; não criar títulos gigantes ou níveis decorativos dentro de painéis compactos.

## Layout

O shell usa três faixas: topbar de 80px, conteúdo flexível e player inferior persistente. A navegação fica à esquerda em modo compacto de 56px ou expandido de 224px; o conteúdo usa padding de página de 32px e ocupa o espaço restante com alinhamento stretch.

O ritmo base é a escala 2/4/8/12/16/20/24/32px, com 14px como gap recorrente entre blocos da Biblioteca e 20px como distância entre regiões do shell. Painéis usam padding interno de 20px; controles usam 14px horizontal por 8px vertical.

O layout é responsivo dentro do desktop Windows. O shell expõe Biblioteca e Configurações; filtros de favoritos permanecem na Biblioteca, e diagnósticos de áudio ficam na página única de Configurações. O player usa 1180px para a composição completa, 720px para reorganizar a mixagem em duas linhas e um modo compacto que empilha agora tocando, transporte e mixer.

## Elevation & Depth

O sistema é flat-by-default. Não há vocabulário de sombras ou blur nos tokens centrais; profundidade vem de mudanças tonais entre canvas, superfície, elevação e cartão, sempre acompanhadas por bordas de 1px quando uma região precisa ser delimitada. O drawer de detalhes e notificações usam overlay e z-order para estabelecer foco temporário, não sombras decorativas.

### Named Rules

**The Tonal Layer Rule.** Criar profundidade com superfícies tonais e bordas existentes antes de inventar sombra, gradiente ou efeito de vidro.

**The Quiet Surface Rule.** Superfícies em repouso devem permanecer silenciosas; destaque aparece apenas quando existe uma ação, seleção ou estado de áudio que o justifique.

## Shapes

As formas são retângulos suavemente arredondados: 4px para pequenos elementos e estados, 6px para controles, 8px para cards e painéis. O raio pill de 999px é reservado para hotkeys e badges compactos; cards de dispositivo e painéis de status permanecem retangulares com cantos moderados. O player usa um botão circular de reprodução de 38px com raio 19px como exceção funcional.

Contornos usam 1px e o mesmo sistema de bordas sem contornos duplos ou molduras pesadas. A forma deve reforçar agrupamento e hit target, não adicionar personalidade ornamental.

## Components

### Buttons

- **Shape:** controles com raio médio de 6px, altura mínima de 36px e peso SemiBold.
- **Primary:** preenchimento no acento de ação, texto sobre ação, padding de 14px horizontal por 8px vertical e borda no mesmo acento.
- **Hover / Focus:** hover usa a variante de ação mais clara; focus usa o token de foco visível; pressed usa o tom de ação pressionada.
- **Secondary:** superfície selecionada com texto primário e borda estrutural.
- **Ghost / Icon:** fundo transparente ou cartão neutro, usados para ações auxiliares e comandos de shell; botões de ícone padrão têm 40px quadrados.

### Cards / Containers

- **Corner Style:** raio de 8px para cards e painéis.
- **Background:** painel usa superfície de trabalho; card usa cartão de controle; camadas elevadas usam superfície elevada.
- **Shadow Strategy:** nenhuma sombra em repouso; profundidade por tonalidade e borda de 1px.
- **Border:** borda estrutural ou suave, sempre fina.
- **Internal Padding:** painel usa 20px; card de status usa 14px horizontal por 12px vertical; cards de som usam padding local de 13px quando precisam de densidade.

### Inputs / Fields

- **Style:** campos de busca têm altura mínima de 40px, fundo de cartão, padding horizontal de 14px, raio de 6px e borda estrutural.
- **Focus:** usar o brush de foco/acento do tema, preservando o contorno legível e sem glow decorativo.
- **Disabled:** reduzir texto e borda para os tokens desabilitados, mantendo a estrutura e o rótulo compreensíveis.

### Navigation

- **Style:** NavigationView nativa em modo LeftCompact, ícones vetoriais do sistema, item de 40px de altura e labels visíveis apenas quando a pane está aberta.
- **Default:** texto secundário e fundo do canvas.
- **Hover:** superfície hover sem deslocamento ou animação chamativa.
- **Active:** acento de ação no ícone/indicador e superfície selecionada no item.

### Status Cards

Cards de dispositivo têm mínimo de 84px de altura, ícone em um bloco de 36px e três camadas textuais: label primário, nome do dispositivo e estado semântico. Verde, âmbar, vermelho e neutro sempre aparecem acompanhados por texto ou forma; cor sozinha não comunica disponibilidade.

### Sound Card

O SoundCard é o componente assinatura: uma faixa de categoria no topo, título, metadados, waveform real ou estado indisponível, hotkey e ação de reprodução. O corpo é a ação de playback; favorito, opções e detalhes permanecem controles separados para não disparar reprodução acidental.

### Audio Level Meter

O medidor usa rótulo, valor textual e uma barra horizontal de 12px. O preenchimento é semântico e o trilho permanece visível em estado vazio; a leitura numérica acompanha o sinal para não depender apenas de cor ou comprimento.

## Do's and Don'ts

### Do:

- **Do** usar os recursos `ThemeResource` e estilos compartilhados antes de criar valores locais.
- **Do** manter temas claro e escuro semanticamente equivalentes e validar os dois.
- **Do** usar ícones vetoriais nativos e AutomationProperties para comandos de áudio.
- **Do** manter níveis, dispositivos, transporte, hotkeys e avisos visíveis sem ruído decorativo.
- **Do** usar bordas de 1px, superfícies tonais e a escala de espaçamento existente.
- **Do** tratar estados de áudio com texto, ícone e cor semântica combinados.

### Don't:

- **Don't** transformar a interface em um dashboard administrativo cheio de métricas ou hero text.
- **Don't** introduzir gradientes complexos, blur pesado, sombras decorativas ou animação ornamental.
- **Don't** usar pill em cards de dispositivo, painéis ou controles que não sejam badges compactos.
- **Don't** hardcodar cores de tema em páginas ou controles reutilizáveis.
- **Don't** usar emoji, letras improvisadas ou ícones não vetoriais como substitutos dos símbolos nativos.
- **Don't** mudar o significado de verde, âmbar, vermelho e neutro para apenas criar variedade cromática.
