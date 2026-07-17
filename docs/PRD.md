# EchoBoard — Product Requirements Document (PRD)

**Documento:** Fonte da Verdade do Produto  
**Versão:** 1.0  
**Status:** Aprovado para início de desenvolvimento  
**Plataforma:** Windows 10/11, 64 bits  
**Tipo:** Aplicativo desktop local  
**Última atualização:** 04/07/2026  

---

## 1. Propósito deste documento

Este PRD é a referência central para decisões de produto, design, arquitetura e desenvolvimento do **EchoBoard**.

Qualquer mudança de escopo, comportamento, requisito técnico ou prioridade deve atualizar este arquivo antes ou junto da implementação. Em caso de conflito entre este documento, issues, prompts, protótipos ou conversas, **este PRD prevalece** até ser formalmente revisado.

### 1.1 Regras de manutenção

- Alterações relevantes devem ser registradas no histórico de decisões.
- Funcionalidades novas não devem ser implementadas sem requisito documentado.
- Itens marcados como **fora do escopo** não podem entrar no MVP.
- Toda funcionalidade do MVP deve possuir critério de aceite e roteiro de teste.
- O projeto não deve copiar nome, interface, ícones, código, assets ou identidade visual de produtos de terceiros.

---

## 2. Visão do produto

O EchoBoard é um aplicativo desktop para Windows que permite importar, organizar e disparar áudios durante chamadas, jogos e transmissões. Ele deve misturar o microfone físico do usuário com efeitos sonoros e encaminhar o áudio final para um dispositivo virtual, permitindo uso em Discord, OBS e aplicativos semelhantes.

O produto deve priorizar:

- operação rápida durante uso real;
- baixa latência e estabilidade;
- interface bonita, clara e discreta;
- hotkeys globais;
- organização eficiente da biblioteca de sons;
- configuração simples de dispositivos de áudio;
- funcionamento totalmente local.

### 2.1 Proposta de valor

> Um painel de áudio pessoal para Windows: rápido para usar, simples de configurar e capaz de enviar voz e efeitos sonoros para chamadas e streams sem depender de múltiplos aplicativos abertos.

---

## 3. Problema que o produto resolve

Para usar efeitos sonoros em Discord, OBS ou jogos, o usuário normalmente precisa combinar um player de áudio, software de hotkeys, mixer, cabo virtual e configurações manuais. Isso gera dificuldade de uso, risco de eco, confusão entre dispositivos e interface pouco prática durante chamadas ou jogos.

O EchoBoard centraliza a biblioteca, a reprodução, os atalhos, a mixagem de voz e o roteamento de saída em um único aplicativo Windows.

---

## 4. Público-alvo e cenários de uso

### 4.1 Público principal

- Jogadores que usam Discord.
- Streamers que usam OBS.
- Criadores de conteúdo.
- Usuários que participam de comunidades de voz.
- Usuários que desejam efeitos sonoros e comandos rápidos durante chamadas.

### 4.2 Cenários principais

1. O usuário joga em tela cheia, pressiona uma hotkey e reproduz um efeito sonoro no Discord.
2. O usuário faz uma live e dispara sons pelo EchoBoard; o OBS recebe a voz e os efeitos pela mesma entrada virtual.
3. O usuário organiza memes, alertas e sons recorrentes por categorias e favoritos.
4. O usuário quer escutar localmente o que está enviando para os outros, sem criar eco ou loop de áudio.
5. O usuário troca de headset e o aplicativo identifica que o dispositivo anterior não está disponível.

---

## 5. Objetivos do produto

### 5.1 Objetivos do MVP

- Importar e organizar arquivos MP3, WAV, OGG, FLAC, M4A e AAC.
- Reproduzir sons por clique e hotkey global.
- Capturar áudio do microfone físico.
- Misturar voz e efeitos sonoros.
- Enviar o áudio resultante a uma saída virtual configurada pelo usuário.
- Permitir monitoramento local em headset ou caixas de som.
- Persistir biblioteca, hotkeys, perfis e configurações.
- Ser estável para uso diário em Discord e OBS.

### 5.2 Objetivos de qualidade

- Interface responsiva enquanto há reprodução e captura de áudio.
- Configuração inicial compreensível para alguém que não conhece roteamento de áudio.
- Consumo controlado de CPU e memória.
- Design consistente em tema escuro e claro.
- Código modular, testável e documentado.

---

## 6. Escopo

### 6.1 Escopo do MVP

O MVP é composto por seis áreas: biblioteca, reprodução, hotkeys, dispositivos/mixagem, experiência de uso e persistência.

### 6.2 Fora do escopo do MVP

Os itens abaixo não podem atrasar a primeira versão utilizável:

- criação de driver virtual próprio;
- suporte a macOS, Linux ou mobile;
- login, conta, nuvem ou sincronização;
- download de áudio da internet;
- marketplace ou compartilhamento público de bibliotecas;
- gravação, edição de áudio, TTS, equalizador e efeitos de voz;
- hotkeys globais de mouse;
- integração com Stream Deck;
- playlists longas e automações remotas;
- captura de áudio de aplicativos específicos.

---

## 7. Funcionalidades e comportamentos

### 7.1 Biblioteca de sons

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| LIB-01 | Importar arquivos | Permitir selecionar vários arquivos MP3, WAV, OGG, FLAC, M4A e AAC por seletor de arquivos. | MVP |
| LIB-02 | Arrastar e soltar | Permitir arrastar arquivos compatíveis para a janela do aplicativo. | MVP |
| LIB-03 | Validar importação | Recusar extensões não suportadas, arquivos ilegíveis e duplicidades por caminho. | MVP |
| LIB-04 | Metadados do som | Salvar nome, caminho, formato, duração, tamanho, data de criação e data de alteração. | MVP |
| LIB-05 | Categorias | Permitir criar, renomear, reordenar e excluir categorias. Ao excluir categoria, solicitar destino para os sons vinculados. | MVP |
| LIB-06 | Organização | Permitir mover sons entre categorias e definir ordem manual. | MVP |
| LIB-07 | Busca | Filtrar sons por nome enquanto o usuário digita. | MVP |
| LIB-08 | Favoritos | Marcar/desmarcar som como favorito e exibir coleção dedicada. | MVP |
| LIB-09 | Recentes | Exibir sons reproduzidos recentemente. | MVP |
| LIB-10 | Renomear | Alterar apenas o nome exibido no EchoBoard, sem renomear o arquivo original. | MVP |
| LIB-11 | Remover da biblioteca | Remover referência do EchoBoard sem apagar o arquivo do disco. | MVP |
| LIB-12 | Informações visuais | Exibir nome, duração, hotkey, favorito e estado de reprodução no card. | MVP |
| LIB-13 | Cor/ícone do som | Permitir cor de destaque por som; ícones personalizados entram depois do MVP. | MVP |
| LIB-14 | Tags | Adicionar tags reutilizáveis para filtros avançados. | Fase 2 |
| LIB-15 | Playlists | Criar listas ordenadas de sons. | Fase 2 |

### 7.2 Reprodução de áudio

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| PLAY-01 | Reproduzir por clique | Um clique no card ou botão de play inicia a reprodução. | MVP |
| PLAY-02 | Reproduzir por hotkey | A hotkey associada dispara o mesmo comportamento do clique. | MVP |
| PLAY-03 | Pausar e retomar | Pausar o áudio atual e retomá-lo do mesmo ponto, quando aplicável. | MVP |
| PLAY-04 | Parar | Parar o áudio atual e resetar a posição de reprodução. | MVP |
| PLAY-05 | Parar todos | Interromper todos os sons ativos com ação de interface ou hotkey global. | MVP |
| PLAY-06 | Reprodução simultânea | Permitir mais de um som ativo ao mesmo tempo quando a regra do som permitir. | MVP |
| PLAY-07 | Interromper anterior | Cada som pode ser configurado para interromper o som anterior antes de iniciar. | MVP |
| PLAY-08 | Repetição | Permitir loop por som. | MVP |
| PLAY-09 | Volume individual | Cada som possui ganho próprio persistido. | MVP |
| PLAY-10 | Volume global de efeitos | Um controle ajusta o ganho de todos os efeitos sem alterar volumes individuais salvos. | MVP |
| PLAY-11 | Progresso | Exibir posição atual e duração durante a reprodução. | MVP |
| PLAY-12 | Estado visual | Destacar claramente cards em reprodução, pausa ou fila. | MVP |
| PLAY-13 | Waveform | Exibir waveform simplificada ou barra de progresso visual gerada no momento da importação. | MVP |
| PLAY-14 | Reprodução aleatória | Tocar som aleatório da categoria selecionada. | Fase 2 |
| PLAY-15 | Fila | Enfileirar sons para reprodução sequencial. | Fase 2 |

### 7.3 Hotkeys globais

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| HOT-01 | Registro global | Hotkeys devem funcionar quando EchoBoard não estiver em foco. | MVP |
| HOT-02 | Hotkey por som | Associar uma combinação de teclado a cada som. | MVP |
| HOT-03 | Ações globais | Permitir hotkeys para parar todos, pausar, mutar efeitos, mutar microfone e abrir/ocultar janela. | MVP |
| HOT-04 | Conflitos | Impedir associação duplicada e informar conflito de forma clara. | MVP |
| HOT-05 | Persistência | Salvar hotkeys entre sessões. | MVP |
| HOT-06 | Segurança de registro | Ao fechar/minimizar definitivamente o aplicativo, remover registros de hotkey. | MVP |
| HOT-07 | Captura de mouse | Suporte a botões do mouse e combinações avançadas. | Fase 2 |

**Decisão do MVP:** hotkeys globais de teclado serão implementadas com a API nativa do Windows. Suporte de mouse não é parte da primeira entrega.

### 7.4 Dispositivos de áudio e roteamento

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| AUD-01 | Descobrir dispositivos | Listar entradas e saídas de áudio disponíveis no Windows. | MVP |
| AUD-02 | Microfone físico | Permitir selecionar microfone físico para captura de voz. | MVP |
| AUD-03 | Monitor local | Permitir selecionar fones, headset ou caixas para monitorar o áudio final. | MVP |
| AUD-04 | Saída virtual | Permitir selecionar saída virtual instalada pelo usuário. | MVP |
| AUD-05 | Perfil de áudio | Salvar combinações de microfone, monitor e saída virtual. | MVP |
| AUD-06 | Teste de áudio | Disponibilizar teste de dispositivos e exibir feedback de sinal. | MVP |
| AUD-07 | Dispositivo indisponível | Detectar desconexão e informar ao usuário, sem encerrar o app. | MVP |
| AUD-08 | Troca de dispositivo | Permitir selecionar novo dispositivo sem reiniciar o aplicativo. | MVP |
| AUD-09 | Assistente de roteamento | Explicar visualmente o fluxo microfone → EchoBoard → saída virtual → Discord/OBS. | MVP |

**Nota importante:** o EchoBoard não criará um driver de microfone virtual no MVP. O usuário deverá instalar e selecionar um dispositivo virtual compatível, como VB-CABLE, para que Discord ou OBS recebam o áudio processado.

### 7.5 Captura, mixagem e saída

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| MIX-01 | Capturar microfone | Capturar o sinal do microfone selecionado em tempo real. | MVP |
| MIX-02 | Converter formato | Converter fontes para o formato interno antes da mixagem. | MVP |
| MIX-03 | Misturar voz e efeitos | Combinar microfone e sons em um único barramento de áudio. | MVP |
| MIX-04 | Saída virtual | Renderizar o áudio mixado no dispositivo virtual selecionado. | MVP |
| MIX-05 | Monitoramento local | Renderizar o áudio final também no dispositivo local quando habilitado. | MVP |
| MIX-06 | Volumes independentes | Controlar separadamente microfone, efeitos, monitor local e saída virtual. | MVP |
| MIX-07 | Mute | Permitir mutar microfone, efeitos ou toda a saída virtual. | MVP |
| MIX-08 | Limiter | Limitar picos para reduzir clipping e distorção perceptível. | MVP |
| MIX-09 | Medidores | Exibir nível do microfone, do barramento mixado e da saída virtual. | MVP |
| MIX-10 | Processamento não bloqueante | O processamento de áudio não pode ocorrer na thread da interface. | MVP |
| MIX-11 | Normalização | Normalizar volume percebido entre sons. | Fase 2 |
| MIX-12 | Delay manual | Permitir compensação manual de atraso. | Fase 2 |
| MIX-13 | Efeitos de áudio | Equalizador, pitch e efeitos de voz. | Futuro |

### 7.6 Configurações e uso diário

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| SET-01 | Tema escuro | Usar tema escuro por padrão. | MVP |
| SET-02 | Tema claro | Permitir alternar para tema claro. | MVP |
| SET-03 | Persistência do tema | Manter o tema escolhido após reiniciar. | MVP |
| SET-04 | Bandeja do sistema | Minimizar para a bandeja do Windows. | MVP |
| SET-05 | Fechar para bandeja | Ao fechar a janela, permitir manter processo ativo na bandeja conforme preferência. | MVP |
| SET-06 | Inicialização | Permitir abrir junto com Windows e/ou iniciar minimizado. | MVP |
| SET-07 | Modo compacto | Exibir mini-player com controles essenciais. | MVP |
| SET-08 | Restaurar sessão | Restaurar última categoria, dimensões da janela e configurações relevantes. | MVP |
| SET-09 | Reset | Permitir restaurar configurações padrão com confirmação. | MVP |
| SET-10 | Diagnóstico | Exibir dispositivos, perfil ativo, frequência de áudio, estado do motor e últimos erros relevantes. | MVP |
| SET-11 | Perfis de uso | Criar perfis como Discord, OBS e Jogos. | Fase 2 |

### 7.7 Feedback, erros e acessibilidade

| ID | Requisito | Comportamento esperado | Prioridade |
|---|---|---|---|
| UX-01 | Toasts | Mostrar feedback curto após importação, erro, alteração de dispositivo ou registro de hotkey. | MVP |
| UX-02 | Erros explicativos | Erros devem explicar causa provável e ação recomendada. | MVP |
| UX-03 | Estados vazios | Biblioteca, categoria e busca vazias devem orientar a próxima ação. | MVP |
| UX-04 | Teclado | Permitir navegação básica por teclado. | MVP |
| UX-05 | Escala do Windows | Suportar escalonamento e resoluções usuais sem sobreposição. | MVP |
| UX-06 | Contraste | Manter contraste adequado em ambos os temas. | MVP |
| UX-07 | Tooltips | Exibir dicas em ícones e controles menos óbvios. | MVP |
| UX-08 | Não depender só de cor | Estados críticos devem usar texto, ícone ou forma além de cor. | MVP |

---

## 8. Regras de negócio

1. Um som não pode possuir mais de uma hotkey ativa com a mesma combinação de teclas.
2. Um som removido da biblioteca não deve apagar o arquivo original do usuário.
3. Arquivo duplicado pelo mesmo caminho não pode ser importado novamente; o sistema deve oferecer abrir o item existente.
4. Quando um dispositivo configurado deixa de existir, o motor de áudio deve parar apenas a rota afetada e manter o app funcional.
5. O usuário não pode iniciar o motor de áudio sem microfone e saída virtual válidos quando a função de transmissão estiver habilitada.
6. O monitoramento local pode ser desligado sem desligar a saída virtual.
7. O mudo de microfone não deve mutar os efeitos, salvo quando o usuário escolher “mutar saída final”.
8. O aplicativo deve preservar as configurações do usuário mesmo quando a biblioteca estiver vazia.
9. Importações e análises de waveform devem ocorrer em segundo plano.
10. Nenhuma leitura/escrita de SQLite pode ocorrer na thread de áudio.
11. Um erro em um arquivo específico não deve impedir a reprodução dos demais sons.
12. Ao trocar de perfil de áudio, o motor deve ser reiniciado de forma controlada para evitar rota parcial ou eco.

---

## 9. Experiência visual e design system

### 9.1 Direção de design

O EchoBoard deve parecer um painel de controle de áudio moderno: técnico, rápido e organizado. A experiência deve ser visualmente marcante, mas sem efeitos que aumentem consumo de GPU ou distraiam durante uso real.

Princípios:

- prioridade para leitura e ação rápida;
- cards grandes e claramente clicáveis;
- azul usado para ação, seleção e sinal ativo;
- animações curtas e discretas;
- superfícies com profundidade leve, sem excesso de blur;
- informações de áudio sempre visíveis, mas não invasivas.

### 9.2 Paleta de cores

#### Tema escuro — padrão

| Token | Cor |
|---|---|
| Background Primary | `#080B12` |
| Background Secondary | `#101522` |
| Surface / Card | `#151C2B` |
| Surface Active | `#1B2740` |
| Blue Primary | `#2F80FF` |
| Blue Hover | `#5A9BFF` |
| Blue Deep | `#0D4EB5` |
| Text Primary | `#F4F7FB` |
| Text Secondary | `#A9B3C6` |
| Border | `#263147` |
| Success | `#25C58A` |
| Warning | `#F0B429` |
| Error | `#F05252` |

#### Tema claro

| Token | Cor |
|---|---|
| Background Primary | `#F5F7FB` |
| Background Secondary | `#FFFFFF` |
| Surface / Card | `#FFFFFF` |
| Surface Active | `#E7F0FF` |
| Blue Primary | `#146EF5` |
| Blue Hover | `#0E5CD1` |
| Text Primary | `#111827` |
| Text Secondary | `#5B6475` |
| Border | `#D8E0ED` |
| Success | `#168A60` |
| Warning | `#B77900` |
| Error | `#C53030` |

### 9.3 Layout principal

```text
┌───────────────────────────────────────────────────────────────────┐
│ EchoBoard | Busca | Mic ativo | Saída virtual | Tema | Settings   │
├───────────────┬───────────────────────────────────────┬───────────┤
│ Categorias    │ Grid de sons                          │ Detalhes  │
│               │                                       │ / Fila    │
│ Favoritos     │ [Som 1] [Som 2] [Som 3]               │           │
│ Recentes      │ [Som 4] [Som 5] [Som 6]               │           │
│ Memes         │ [Som 7] [Som 8] [Som 9]               │           │
│ Jogos         │                                       │           │
├───────────────┴───────────────────────────────────────┴───────────┤
│ Player | Progresso | Mic | Efeitos | Monitor | Saída virtual | Stop │
└───────────────────────────────────────────────────────────────────┘
```

### 9.4 Componentes mínimos

- `SoundCard`
- `CategoryItem`
- `AudioLevelMeter`
- `DeviceStatusBadge`
- `HotkeyBadge`
- `VolumeSlider`
- `IconButton`
- `EmptyState`
- `ToastNotification`
- `ConfirmDialog`
- `CompactPlayer`

### 9.5 Animações permitidas

- hover e pressão de botões/cards;
- transição curta de tema;
- expansão/retração de painel;
- pulso suave no item em reprodução;
- atualização discreta de medidores.

Não usar animações contínuas decorativas, partículas, fundos pesados, 3D ou blur excessivo.

---

## 10. Stack e decisões técnicas

### 10.1 Stack principal

| Camada | Tecnologia | Papel |
|---|---|---|
| Linguagem | C# | Linguagem principal do aplicativo. |
| Runtime | .NET 10 | Base de execução e ferramentas. |
| UI | WinUI 3 + XAML | Interface Windows moderna e nativa. |
| Arquitetura UI | MVVM + CommunityToolkit.Mvvm | Separação entre views, estado e comandos. |
| Áudio | NAudio | Reprodução, captura, mixagem, conversão e integração com WASAPI. |
| API de áudio | WASAPI | Captura e renderização de áudio no Windows. |
| Dados | SQLite | Banco local sem servidor. |
| ORM | Entity Framework Core | Migrations e persistência. |
| Hotkeys | Win32 `RegisterHotKey` | Atalhos globais de teclado no Windows. |
| Logs | Serilog | Logs estruturados e diagnóstico. |
| Testes | xUnit + FluentAssertions + NSubstitute | Testes unitários e mocks. |
| CI | GitHub Actions | Build e testes automáticos. |
| Distribuição | MSIX ou instalador `.exe` | Empacotamento futuro. |

### 10.2 Decisões técnicas obrigatórias

- O MVP será feito inteiramente em C#; C++ não faz parte da primeira versão.
- WinUI 3 será usado para evitar uma aplicação web empacotada e manter aparência Windows nativa.
- O motor de áudio será isolado em projeto próprio para possível substituição futura por DLL nativa.
- O aplicativo funcionará localmente, sem API ou backend.
- O driver/cabo virtual será dependência externa do usuário; não será desenvolvido pelo projeto.
- A operação deve usar WASAPI em modo compartilhado no MVP, priorizando compatibilidade com Discord, OBS e outros programas.

---

## 11. Arquitetura do sistema

### 11.1 Estrutura de projetos

```text
EchoBoard.App
├── Views
├── ViewModels
├── Controls
├── Themes
└── Composition Root / DI

EchoBoard.Application
├── Use Cases
├── DTOs
├── Interfaces
├── Validators
└── Application Services

EchoBoard.Domain
├── Entities
├── Enums
├── Value Objects
├── Domain Rules
└── Exceptions

EchoBoard.Audio
├── Device Discovery
├── Microphone Capture
├── Playback
├── Decoding
├── Resampling
├── Mixing
├── Rendering
└── Waveform Analysis

EchoBoard.Infrastructure
├── SQLite / EF Core
├── File System
├── Hotkeys
├── Settings
├── Logging
└── Windows Integration
```

### 11.2 Regras de dependência

```text
EchoBoard.App → Application, Audio, Infrastructure
EchoBoard.Application → Domain
EchoBoard.Audio → Application, Domain
EchoBoard.Infrastructure → Application, Domain
EchoBoard.Domain → nenhuma outra camada
```

### 11.3 Princípios arquiteturais

- A interface não acessa banco de dados diretamente.
- A interface não controla dispositivos de áudio diretamente.
- O motor de áudio não depende de controles visuais.
- O domínio não depende de UI, banco, NAudio ou WinUI.
- Processamento de áudio, importação e waveform não podem bloquear a interface.
- Logs devem permitir diagnosticar falhas sem registrar conteúdo de áudio.

---

## 12. Arquitetura e fluxo de áudio

### 12.1 Formato interno

Todas as fontes devem ser convertidas para um formato comum antes da mixagem:

```text
PCM Float 32 bits
48.000 Hz
Estéreo quando aplicável
```

### 12.2 Fluxo principal

```text
Microfone físico
       │
       ▼
Captura WASAPI
       │
       ▼
Conversão / Resampling
       │
       ├───────────────────────────┐
       │                           │
       ▼                           ▼
          Sons MP3/WAV/OGG/FLAC/M4A/AAC → Decoder → Resampler
                                                   │
                                                   ▼
                                              Mixer / Limiter
                                                   │
                       ┌───────────────────────────┴──────────────────────────┐
                       ▼                                                      ▼
            Saída virtual selecionada                                  Monitor local
            (ex.: CABLE Input)                                         Headset / caixas
                       │
                       ▼
                 Discord / OBS
```

### 12.3 Regras do motor de áudio

- O áudio deve ser processado fora da thread da interface.
- A thread de áudio não pode consultar SQLite ou acessar arquivos de UI.
- Sons curtos podem ser pré-carregados; sons longos devem utilizar streaming.
- Medidores visuais devem atualizar no máximo 30 vezes por segundo.
- Monitor local e saída virtual devem usar renderizadores/buffers independentes.
- A falha de uma saída deve ser isolada, sem derrubar a biblioteca ou a interface.
- O mixer deve prevenir clipping perceptível por meio de limiter simples.
- O motor deve reiniciar de forma segura quando o perfil de áudio for alterado.

---

## 13. Modelo de dados

### 13.1 Entidades principais

```text
Sound
- Id
- Name
- FilePath
- Extension
- Duration
- FileSize
- Volume
- AccentColor
- IsFavorite
- IsLoopEnabled
- StopPreviousSound
- CategoryId
- SortOrder
- CreatedAt
- UpdatedAt

Category
- Id
- Name
- AccentColor
- SortOrder
- CreatedAt

HotkeyBinding
- Id
- SoundId (nullable para ação global)
- ActionType
- KeyCombination
- IsEnabled
- CreatedAt

AudioDeviceProfile
- Id
- Name
- InputDeviceId
- MonitorOutputDeviceId
- VirtualOutputDeviceId
- MicrophoneVolume
- EffectsVolume
- MonitorVolume
- VirtualOutputVolume
- IsDefault

AppSetting
- Id
- Key
- Value
- UpdatedAt

RecentlyPlayed
- Id
- SoundId
- PlayedAt
```

### 13.2 Persistência

- Banco local: SQLite.
- Migrations: Entity Framework Core.
- Configurações não sensíveis podem ser armazenadas em arquivo de configuração local; dados estruturados e relacionais ficam no SQLite.
- Áudios não serão copiados por padrão; o banco armazenará referência ao caminho de origem.
- Caso arquivo seja movido ou apagado, o EchoBoard deve identificar o item como indisponível e oferecer localizar novo caminho ou remover da biblioteca.

---

## 14. Requisitos não funcionais

### 14.1 Performance

Metas de referência em computador Windows moderno:

| Métrica | Meta |
|---|---|
| Inicialização do aplicativo | Até 3 segundos |
| CPU em repouso | Menor que 3% |
| CPU em uso comum | Menor que 10% |
| Atualização de medidores | 20 a 30 FPS |
| Latência adicional esperada | Menor que 80 ms em cenário normal |
| Reprodução simultânea | Pelo menos 8 sons curtos |
| Biblioteca | Pelo menos 1.000 sons sem travar a interface |

Essas metas dependem de hardware, driver, headset e dispositivo virtual do usuário.

### 14.2 Estabilidade

- Não encerrar se headset, microfone ou saída virtual for desconectado.
- Preservar dados após reinicialização inesperada.
- Impedir corrupção do banco em operações comuns.
- Ter logs rotacionados por tamanho/data.
- Oferecer diagnóstico de dispositivos e últimas falhas.

### 14.3 Privacidade

- Não enviar áudio, nomes de arquivos, configurações ou telemetria para servidores.
- Não exigir login.
- Não gravar conteúdo do microfone em logs.
- Informar claramente quando a captura de microfone estiver ativa.

### 14.4 Acessibilidade

- Navegação básica por teclado.
- Contraste adequado em ambos os temas.
- Escala respeitando configurações do Windows.
- Tooltips e labels para ícones.
- Estados comunicados por texto/ícone além de cor.

---

## 15. Onboarding e configuração inicial

### 15.1 Fluxo de primeiro uso

1. Usuário abre o EchoBoard.
2. Aplicativo apresenta um resumo de como o roteamento funciona.
3. Usuário seleciona microfone físico.
4. Usuário seleciona monitor local.
5. Usuário seleciona saída virtual.
6. Aplicativo executa teste de sinal.
7. Usuário importa os primeiros áudios.
8. Usuário pode configurar hotkeys.
9. Aplicativo exibe instruções curtas para Discord e OBS.

### 15.2 Roteamento esperado

```text
EchoBoard → saída virtual (ex.: CABLE Input)
Dispositivo virtual → entrada do Discord/OBS (ex.: CABLE Output)
```

### 15.3 Diagnóstico obrigatório

A tela de diagnóstico deve mostrar:

- microfone selecionado;
- monitor selecionado;
- saída virtual selecionada;
- se cada dispositivo está disponível;
- formato de áudio ativo;
- estado do motor;
- medidor de entrada;
- medidor de saída;
- alerta de loop/rota inválida quando detectável;
- últimos erros relevantes.

---

## 16. Testes e critérios de aceite

### 16.1 Estratégia de testes

| Tipo | Cobertura esperada |
|---|---|
| Unitários | Regras de biblioteca, validação, configurações, hotkeys e mixagem isolada. |
| Integração | SQLite, importação, persistência e carregamento de configurações. |
| Manual de áudio | Dispositivos reais, VB-CABLE, Discord, OBS, headset USB e desconexão de dispositivo. |
| Regressão | Checklist repetível antes de cada release. |

### 16.2 Critérios de aceite do MVP

- [ ] Importa MP3, WAV, OGG, FLAC, M4A e AAC válidos.
- [ ] Rejeita arquivo inválido sem travar a interface.
- [ ] Mantém biblioteca, categorias, favoritos e hotkeys após reiniciar.
- [ ] Reproduz áudio localmente.
- [ ] Executa hotkeys globais de teclado fora do app.
- [ ] Captura microfone físico.
- [ ] Mostra medidor de sinal do microfone.
- [ ] Mistura voz e efeitos.
- [ ] Envia áudio mixado à saída virtual.
- [ ] Discord recebe o áudio pela entrada virtual.
- [ ] OBS recebe o áudio pela entrada virtual.
- [ ] Monitoramento local pode ser ligado/desligado.
- [ ] Alterna tema claro/escuro.
- [ ] Funciona na bandeja do sistema.
- [ ] Não trava com reprodução repetida de sons.
- [ ] Detecta ausência de dispositivo configurado e informa ação recomendada.
- [ ] README permite que outra pessoa configure o projeto do zero.

---

## 17. Segurança, riscos e mitigação

| Risco | Impacto | Mitigação |
|---|---|---|
| Eco ou loop de áudio | Alto | Separar monitor da saída virtual; onboarding e diagnóstico claros. |
| Latência elevada | Alto | WASAPI, formato interno comum, buffers controlados e testes reais. |
| Discord filtrar os efeitos | Médio | Documentar ajuste de supressão de ruído/cancelamento de eco no guia. |
| Dispositivo virtual ausente | Alto | Detectar ausência e guiar instalação/configuração. |
| Dispositivo desconectado | Médio | Escutar alterações e solicitar nova seleção. |
| Clipping/distorção | Médio | Limiter e controles de ganho independentes. |
| Uso alto de CPU | Médio | Limitar updates visuais, pré-carregar sons curtos e usar streaming para longos. |
| Hotkey em conflito | Médio | Validar antes de registrar e informar conflito. |
| Crescimento excessivo de escopo | Alto | Bloquear itens de Fase 2/Futuro até aprovação do MVP. |
| Arquivo movido ou excluído | Médio | Marcar como indisponível e oferecer relocalizar/remover. |

---

## 18. Roadmap de desenvolvimento

### Fase 0 — Fundação

- Solução .NET e estrutura de projetos.
- WinUI 3, DI, logs, testes e GitHub Actions.
- Shell inicial do aplicativo.

### Fase 1 — Design e navegação

- Temas claro/escuro.
- Layout principal.
- Componentes reutilizáveis.
- Estados vazios e feedbacks.

### Fase 2 — Biblioteca e persistência

- SQLite, migrations e entidades.
- Importação de MP3, WAV, OGG, FLAC, M4A e AAC.
- Categorias, busca, favoritos e ordenação.

### Fase 3 — Reprodução local e hotkeys

- Player local.
- Volumes, pausa, stop e reprodução simultânea.
- Hotkeys globais de teclado.

### Fase 4 — Entrada, mixagem e saída virtual

- Descoberta de dispositivos.
- Captura de microfone.
- Mixer, limiter, monitor local e saída virtual.

### Fase 5 — Uso diário e release

- Bandeja, modo compacto, onboarding, diagnóstico.
- Testes em Discord e OBS.
- Documentação, screenshots, vídeo e release.

---

## 19. Estrutura de repositório esperada

```text
echoboard/
├── src/
│   ├── EchoBoard.App/
│   ├── EchoBoard.Application/
│   ├── EchoBoard.Domain/
│   ├── EchoBoard.Audio/
│   └── EchoBoard.Infrastructure/
├── tests/
│   ├── EchoBoard.Application.Tests/
│   ├── EchoBoard.Audio.Tests/
│   └── EchoBoard.Infrastructure.Tests/
├── docs/
│   ├── PRD.md
│   ├── architecture.md
│   ├── audio-routing.md
│   ├── design-system.md
│   ├── setup-discord.md
│   ├── setup-obs.md
│   └── troubleshooting.md
├── assets/
├── .github/workflows/
├── README.md
├── CONTRIBUTING.md
├── LICENSE
└── EchoBoard.sln
```

---

## 20. Documentação obrigatória no GitHub

O repositório deverá conter:

- README com visão geral e instalação;
- este PRD em `docs/PRD.md`;
- diagrama de arquitetura;
- diagrama de fluxo de áudio;
- guia de configuração para Discord;
- guia de configuração para OBS;
- guia de uso de dispositivo virtual;
- troubleshooting para eco, ausência de dispositivo, silêncio e latência;
- screenshots dos temas claro/escuro;
- GIF ou vídeo curto demonstrando o fluxo principal;
- roadmap;
- licença;
- créditos e licenças das bibliotecas utilizadas.

---

## 21. Histórico de decisões

| Data | Decisão | Motivo |
|---|---|---|
| 04/07/2026 | Desenvolver em C#/.NET/WinUI 3 | Melhor equilíbrio entre produtividade, visual Windows nativo e integração com APIs de áudio. |
| 04/07/2026 | Usar NAudio + WASAPI | Cobrir reprodução, captura, mixagem e renderização no Windows. |
| 04/07/2026 | Não desenvolver driver virtual no MVP | Alto risco técnico, instalação privilegiada e escopo incompatível com a primeira entrega. |
| 04/07/2026 | Usar dispositivo virtual externo | Permite integração real com Discord/OBS sem criar driver próprio. |
| 04/07/2026 | Hotkeys apenas de teclado no MVP | Reduz complexidade e conflitos com jogos, antivírus e acessibilidade. |
| 04/07/2026 | Tema escuro por padrão; claro opcional | Uso frequente durante chamadas/jogos e preferência visual definida. |

---

## 22. Decisão final de produto

O EchoBoard será desenvolvido inicialmente como um **soundboard e mixer de voz para Windows**, com foco em uma experiência diária de uso: biblioteca organizada, hotkeys rápidas, voz + efeitos no mesmo fluxo de áudio, monitoramento local e integração prática com Discord e OBS por um dispositivo virtual externo.

O MVP será considerado bem-sucedido quando permitir que o usuário abra o aplicativo, pressione uma hotkey durante uma chamada e faça os participantes ouvirem o efeito junto com sua voz, com configuração estável e interface clara.
