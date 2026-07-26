# Trabalho restante do EchoBoard

Atualizado após o merge de `bugfix/audio-playback-mixer` na `main`.

## Já entregue

- Decoder por conteúdo, incluindo Ogg/Opus com extensão `.mp3`.
- Mixer singleton em 48 kHz, estéreo e float.
- Barramento virtual com voz + efeitos.
- Monitor local com efeitos somente.
- Renderizadores independentes, adaptação de formato e reconexão por MMDevice ID.
- Volumes e mutes compartilhados entre Player e Configurações.
- PlaybackCoordinator, progresso, pausa, stop e seek centralizados.
- Cards, favoritos, menus, exclusão e atualização entre as telas.
- Diagnóstico de microfone, monitor e saída virtual.
- Proteção contra famílias conhecidas de endpoints que causariam feedback.
- Build sem avisos/erros e 200/200 testes automatizados aprovados.

## Pendências de hardware e validação manual

1. Instalar e selecionar um cabo virtual externo, como VB-CABLE ou VoiceMeeter.
2. Validar fisicamente no OBS e Discord:
   - EchoBoard usando `CABLE Input` como saída virtual;
   - OBS/Discord usando `CABLE Output` como microfone;
   - somente voz, somente efeito e voz + efeito simultâneos;
   - voz continuando depois do término de um efeito.
3. Repetir a validação após reiniciar o aplicativo e após desconectar/reconectar o cabo.
4. Executar smoke test de hotkey com o EchoBoard sem foco.
5. Fazer revisão visual manual em tema claro/escuro e larguras compacta, média e ampla.

## Melhorias futuras

- Exibir medidores reais dos barramentos de efeitos e saída virtual; atualmente o medidor físico do microfone é o único nível contínuo exposto na interface.
- Expandir a classificação de famílias de dispositivos para cabos virtuais de outros fabricantes além dos padrões conhecidos.
- Adicionar onboarding guiado para instalação/configuração do cabo virtual, sem instalar driver automaticamente.
- Validar em hardware adicional os formatos MP3, WAV, Ogg/Vorbis, FLAC, M4A e AAC; a suíte automatizada cobre decodificação e o smoke físico atual usou Ogg/Opus.
- Avaliar telemetria local de falhas de rota somente em logs, mantendo o produto sem backend ou coleta externa.

## Dependência externa

O EchoBoard não cria nem instala driver de áudio virtual. A transmissão para OBS, Discord, jogos e chamadas depende de VB-CABLE, VoiceMeeter ou outro endpoint virtual compatível instalado pelo usuário.
