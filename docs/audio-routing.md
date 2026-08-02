# Audio routing

EchoBoard uses Windows WASAPI endpoints through NAudio. It does not install a virtual driver.

```text
Physical microphone ───────────────┐
                                  ├─> Virtual bus ─> external cable ─> Discord / OBS
Imported sounds and hotkeys ───────┤
                                  └─> Monitor bus ─> headphones / speakers
```

The virtual bus contains microphone + effects. The local monitor contains effects only, which avoids sending the physical microphone back to the user's speakers. Both buses use stereo IEEE float PCM at 48 kHz and independent renderers, volumes, mute controls, buffers, and limiting. Microphone capture and WASAPI rendering use event-driven 20 ms targets. The microphone ring buffer is bounded to the same target duration and drops the oldest samples on overflow, so temporary scheduling or device-format differences cannot turn into an ever-growing voice delay.

Settings and the global player use one observable routing profile. Changing a volume or mute in either surface updates the mixer immediately and persists the same complete profile. Monitor mute silences the monitor bus without disabling or recreating its endpoint; **Local monitor** remains the separate route enable switch.

The shared routing snapshot also exposes real peak meters for effects, the local monitor, and the virtual output. Effects are measured after the per-sound and global effects gain; monitor and virtual output meters are measured after their independent route gains. A disconnected route reports zero until its renderer is active again.

Each WASAPI route adapts the internal mix to the endpoint mix format while keeping the application mixer at 48 kHz stereo float. A monitor failure does not recreate or stop the virtual route, and a virtual-route failure does not interrupt local playback or microphone capture. Saved endpoints remain identified by their Windows MMDevice ID while disconnected and are retried automatically.

## Configure

1. Open **Settings → Mixer and routing**.
2. Select the physical microphone. On first configuration, EchoBoard prefers NVIDIA Broadcast when it is available, otherwise the previous/default microphone.
3. Select the Windows output used as the local monitor.
4. Install an external cable such as VB-CABLE or VoiceMeeter if Discord/OBS routing is required.
5. Select the cable's render endpoint as **Virtual output**. Known cable endpoints are placed first and marked **Virtual cable**; additional virtual cable/router families are also recognized when their Windows endpoint names identify them.
6. In Discord or OBS, select the corresponding cable capture endpoint as the microphone/input:
   - VB-CABLE: EchoBoard uses `CABLE Input`; Discord/OBS uses `CABLE Output`.
   - VoiceMeeter: EchoBoard uses the desired VoiceMeeter input bus; Discord/OBS uses the matching output bus.

Without a virtual output, EchoBoard stays in degraded mode: local playback, the library, hotkeys, and the monitor continue to work. A disconnected saved endpoint is retried by its Windows device ID without restarting the mixer or microphone.

EchoBoard blocks known input/output pairs from the same endpoint family when that combination would feed the mixed signal back into its own microphone path. Do not select a virtual cable capture endpoint as the EchoBoard microphone; use the physical microphone or NVIDIA Broadcast output instead.

## Route states

- **Active**: the renderer accepted the stream and is transmitting.
- **Unconfigured**: no virtual endpoint was selected; local playback remains available.
- **Unavailable**: the saved endpoint is disconnected and reconnection is pending.
- **Failed**: initialization, format negotiation, or feedback protection prevented the route from starting. The technical error is available in Audio Diagnostics and the log.

## Manual validation

- Play an imported sound from Dashboard, Library, Favorites, and Recent.
- Confirm the local monitor contains effects and does not loop the microphone.
- Confirm the virtual cable receives microphone + effects.
- Disconnect and reconnect each endpoint independently.
- Restart EchoBoard and confirm the same MMDevice IDs are restored.
- Trigger a sound through a global hotkey while EchoBoard is unfocused.
- Change all four volumes and mutes while voice and effects are active.
- Verify Discord/OBS input levels and disable aggressive noise suppression if it removes sound effects.

The hardware-gated smoke tests can be enabled with:

- `ECHOBOARD_SMOKE_AUDIO_FILE`: absolute path decoded and played directly through the configured mixer.
- `ECHOBOARD_SMOKE_AUDIO`: absolute path of an already imported audio file, used by the history/play-count restart test.
- `ECHOBOARD_SMOKE_VIRTUAL_OUTPUT_ID`: exact Windows MMDevice ID of the cable render endpoint.

The virtual-endpoint smoke test restores the previous routing profile in `finally`. These variables are intentionally unset in normal unit-test and CI runs.
