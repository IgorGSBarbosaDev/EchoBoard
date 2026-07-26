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
- Medidores reais de efeitos, monitor e saída virtual no Dashboard e Diagnóstico.
- Proteção contra famílias conhecidas de endpoints que causariam feedback.
- Classificação expandida para famílias adicionais de cabos e roteadores virtuais.
- Build sem avisos/erros e 205/205 testes automatizados aprovados.

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

- [x] Exibir medidores reais dos barramentos de efeitos, monitor e saída virtual; os níveis são capturados no mixer e publicados pelo snapshot compartilhado.
- [x] Expandir a classificação de famílias de dispositivos para cabos e roteadores virtuais adicionais, preservando a proteção contra feedback por família.
- [ ] Validar em hardware adicional os formatos MP3, WAV, Ogg/Vorbis, FLAC, M4A e AAC; a suíte automatizada cobre decodificação e mixagem, mas a validação física depende do reinício e dos dispositivos externos.
- [x] Avaliar telemetria local de falhas de rota somente em logs; não há coleta externa, backend ou persistência de áudio.

## Dependência externa

O EchoBoard não cria nem instala driver de áudio virtual. A transmissão para OBS, Discord, jogos e chamadas depende de VB-CABLE, VoiceMeeter ou outro endpoint virtual compatível instalado pelo usuário.
