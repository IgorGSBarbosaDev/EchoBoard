---
name: EchoBoard
description: Mesa de mixagem operacional para sons, voz e roteamento local no Windows.
colors:
  primary: "#2F80FF"
  primary-light: "#146EF5"
  primary-hover: "#5A9BFF"
  primary-pressed: "#0D4EB5"
  neutral-bg: "#080B12"
  neutral-surface: "#111827"
  neutral-elevated: "#0E1420"
  neutral-card: "#151C2B"
  neutral-selected: "#1C2D4D"
  neutral-hover: "#1B2435"
  neutral-border: "#263147"
  neutral-border-soft: "#1D2738"
  text-primary: "#F4F7FB"
  text-secondary: "#A9B3C6"
  text-disabled: "#707A8F"
  on-primary: "#FFFFFF"
  success: "#25C58A"
  warning: "#F0B429"
  error: "#F05252"
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
  accent-cyan: "#1597A8"
  accent-violet: "#8B6FFF"
  accent-emerald: "#25A978"
  accent-rose: "#E0527C"
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

A atmosfera é precisa, limpa, discreta e orientada à operação rápida. O visual atual parte de uma base escura azul-marinho/grafite, com uma versão clara equivalente, bordas finas, ícones vetoriais nativos e um único acento semântico por vez. A combinação cromática atual é a autoridade documentada deste snapshot, mas permanece uma candidata explícita para uma futura revisão de cor; não deve ser tratada como uma decisão irreversível.

**Key Characteristics:**

- Controle operacional e leitura imediata de estado.
- Superfícies tonais planas com bordas discretas.
- Azul de ação reservado para comandos, seleção e progresso.
- Densidade compacta, tipografia Segoe UI e hierarquia curta.
- Tema escuro inicial com paridade clara e paletas de acento trocáveis.

## Colors

A paleta é temática e semântica: superfícies frias formam a base, o acento selecionado orienta a ação e verde, âmbar e vermelho comunicam estado. A implementação possui famílias de acento Blue, Cyan, Violet, Emerald e Rose; os tokens de frontmatter registram o azul padrão e os principais representantes alternativos.

### Primary

- **Azul elétrico de ação** (`{colors.primary}`): comandos primários, seleção ativa, foco visual, progresso e indicadores de reprodução.
- **Azul de ação para tema claro** (`{colors.primary-light}`): variante de contraste usada quando o tema claro está ativo.
- **Azul de resposta** (`{colors.primary-hover}`): estado de passagem do ponteiro sobre ações.
- **Azul pressionado** (`{colors.primary-pressed}`): confirmação tátil de comandos acionados.

### Secondary

- **Ciano de sinal** (`{colors.accent-cyan}`): alternativa de acento disponível na paleta do aplicativo.
- **Violeta de sinal** (`{colors.accent-violet}`): alternativa de acento disponível na paleta do aplicativo.
- **Esmeralda de sinal** (`{colors.accent-emerald}`): alternativa de acento disponível na paleta do aplicativo.
- **Rosa de sinal** (`{colors.accent-rose}`): alternativa de acento disponível na paleta do aplicativo.

### Neutral

- **Noite de operação** (`{colors.neutral-bg}`): canvas principal do tema escuro.
- **Superfície de trabalho** (`{colors.neutral-surface}`): shell, áreas de conteúdo e painéis principais.
- **Superfície elevada** (`{colors.neutral-elevated}`): topbar, player inferior, busca e camadas que precisam se separar sem sombra.
- **Cartão de controle** (`{colors.neutral-card}`): cards, campos, botões de ícone e áreas interativas.
- **Seleção azulada** (`{colors.neutral-selected}`): seleção, estados ativos e botões secundários.
- **Hover de superfície** (`{colors.neutral-hover}`): resposta discreta de navegação e controles.
- **Borda estrutural** (`{colors.neutral-border}`): contornos de 1px que organizam a interface.
- **Borda suave** (`{colors.neutral-border-soft}`): divisores e separações de menor ênfase.
- **Texto primário** (`{colors.text-primary}`): títulos, valores e informação operacional principal.
- **Texto secundário** (`{colors.text-secondary}`): captions, metadados, rótulos e orientação auxiliar.
- **Texto desabilitado** (`{colors.text-disabled}`): estados indisponíveis e informações sem ação.
- **Base clara** (`{colors.light-bg}`), **superfície clara** (`{colors.light-surface}`) e **elevação clara** (`{colors.light-elevated}`): equivalentes do tema claro.

### Named Rules

**The Signal Over Decoration Rule.** Cor deve indicar ação, seleção ou estado do áudio; não deve virar textura ou preenchimento ornamental.

**The One Accent Rule.** A paleta de acento selecionada é a voz de ação da tela; estados de sucesso, atenção e erro mantêm seus significados semânticos.

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

O ritmo base é a escala 2/4/8/12/16/20/24/32px, com 14px como gap recorrente entre blocos de dashboard e 20px como distância entre regiões do shell. Painéis usam padding interno de 20px; controles usam 14px horizontal por 8px vertical.

O layout é responsivo dentro do desktop Windows. O Dashboard usa estados amplo a partir de 1100px, médio a partir de 720px e compacto abaixo disso. O player usa 1180px para a composição completa, 720px para reorganizar a mixagem em duas linhas e um modo compacto que empilha agora tocando, transporte e mixer.

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
