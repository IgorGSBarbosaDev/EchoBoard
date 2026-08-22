# Product

<!-- impeccable:product-schema 1 -->

## Platform

windows-desktop

## Users

Os usuários principais são jogadores que usam Discord, streamers, criadores de conteúdo e participantes de comunidades de voz. Eles precisam disparar efeitos sonoros rapidamente durante chamadas, jogos e transmissões, normalmente sem tirar o foco da atividade principal.

## Product Purpose

O EchoBoard é um painel de áudio pessoal para Windows que permite importar, organizar e reproduzir sons, capturar o microfone físico, misturar voz e efeitos e encaminhar o resultado para uma saída virtual.

O sucesso do produto é permitir uma operação rápida, estável e compreensível durante o uso real em Discord, OBS e aplicativos semelhantes, sem depender de vários aplicativos abertos para biblioteca, hotkeys, mixagem e roteamento.

## Positioning

O EchoBoard centraliza biblioteca de sons, hotkeys globais, reprodução, captura de microfone, mixagem e roteamento em um único aplicativo local para Windows, mantendo o monitoramento local utilizável mesmo quando uma saída virtual externa não está disponível.

## Operating Context

- Uso recorrente durante jogos, chamadas de voz, lives e gravações.
- O usuário pode operar o aplicativo em paralelo com Discord, OBS ou outros aplicativos de comunicação e transmissão.
- Sons são importados de arquivos locais, organizados por categorias e favoritos e reproduzidos por clique ou hotkey global.
- O microfone físico e os efeitos são misturados em tempo real; o resultado pode ser monitorado localmente e enviado para um endpoint virtual externo, como VB-CABLE ou VoiceMeeter.
- A troca, desconexão ou indisponibilidade de dispositivos de áudio faz parte do uso normal e deve ser comunicada sem encerrar o aplicativo.

## Capabilities and Constraints

- Importar e organizar arquivos MP3, WAV, OGG, FLAC, M4A e AAC.
- Reproduzir sons por clique e hotkey global, com controle de pausa, retomada, parada e parada de todos.
- Capturar microfone, misturar voz e efeitos em áudio PCM float a 48 kHz quando possível e oferecer monitoramento e saída virtual independentes.
- Persistir biblioteca, categorias, favoritos, recentes, hotkeys e configurações localmente em SQLite.
- Usar Windows 10/11, 64 bits, com .NET 10, WinUI 3, MVVM, NAudio/WASAPI e arquitetura modular separando App, Application, Domain, Audio e Infrastructure.
- Permanecer local-first: não adicionar contas, backend, nuvem, telemetria, sincronização ou driver de áudio próprio.
- Não incluir no MVP macOS, Linux, mobile, download de áudio, compartilhamento público, gravação/edição de áudio, TTS, equalizador, efeitos de voz, hotkeys globais de mouse, Stream Deck ou captura de aplicativos específicos.
- Manter temas claro e escuro, operação por teclado e comportamento estável quando dispositivos físicos ou virtuais não estiverem disponíveis.
- O produto não instala nem implementa um driver virtual; o roteamento para Discord e OBS depende de endpoint externo configurado pelo usuário.

## Brand Commitments

- Nome: EchoBoard.
- Interface bonita, clara e discreta, orientada à operação rápida durante uso real.
- Identidade própria; não copiar nome, interface, ícones, código, assets ou identidade visual de produtos de terceiros.

## Evidence on Hand

- `docs/PRD.md`: fonte de verdade do produto, visão, público, escopo, requisitos e critérios de aceitação.
- `README.md`: visão geral, pré-requisitos, comandos, arquitetura resumida e instruções de roteamento virtual.
- `docs/architecture.md`: responsabilidades dos projetos e regras de dependência.
- `docs/audio-routing.md`: fluxo de sinal e checklist de validação de roteamento.
- `docs/design-system.md` e `src/EchoBoard.App/Themes/`: tokens, estilos e recursos visuais existentes.
- `src/` e `tests/`: implementação WinUI/MVVM, domínio, áudio, persistência e testes automatizados.
- `docs/design-reference/`: referências visuais existentes para a interface do aplicativo; não são prova de usuários, métricas ou depoimentos.

## Product Principles

- Priorizar operação rápida durante o uso real.
- Manter baixa latência, estabilidade e recuperação clara diante de falhas de dispositivos.
- Centralizar biblioteca, hotkeys, mixagem e roteamento sem esconder o estado do áudio.
- Preservar funcionamento local, privacidade e ausência de dependências de conta ou nuvem.
- Evoluir a interface sem quebrar fluxos, bindings, comandos, temas ou arquitetura modular existentes.

## Accessibility & Inclusion

- Operações importantes devem permanecer acessíveis por teclado, incluindo hotkeys globais e comandos equivalentes na interface.
- Informações de estado de reprodução, níveis e disponibilidade de dispositivos devem ter representação textual além de sinais visuais.
- Os temas claro e escuro devem manter legibilidade e estados de foco identificáveis.
