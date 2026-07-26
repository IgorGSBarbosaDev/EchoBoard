# Audio routing

EchoBoard uses Windows WASAPI endpoints through NAudio. It does not install a virtual driver.

```text
Physical microphone ───────────────┐
                                  ├─> Virtual bus ─> external cable ─> Discord / OBS
Imported sounds and hotkeys ───────┤
                                  └─> Monitor bus ─> headphones / speakers
```

The virtual bus contains microphone + effects. The local monitor contains effects only, which avoids sending the physical microphone back to the user's speakers. Both buses use stereo IEEE float PCM at 48 kHz and independent renderers, volumes, mute controls, buffers, and limiting.

Settings and the global player use one observable routing profile. Changing a volume or mute in either surface updates the mixer immediately and persists the same complete profile. Monitor mute silences the monitor bus without disabling or recreating its endpoint; **Local monitor** remains the separate route enable switch.

## Configure

1. Open **Settings → Mixer and routing**.
2. Select the physical microphone. On first configuration, EchoBoard prefers NVIDIA Broadcast when it is available, otherwise the previous/default microphone.
3. Select the Windows output used as the local monitor.
4. Install an external cable such as VB-CABLE or VoiceMeeter if Discord/OBS routing is required.
5. Select the cable's render endpoint as **Virtual output**.
6. In Discord or OBS, select the corresponding cable capture endpoint as the microphone/input.

Without a virtual output, EchoBoard stays in degraded mode: local playback, the library, hotkeys, and the monitor continue to work. A disconnected saved endpoint is retried by its Windows device ID without restarting the mixer or microphone.

## Manual validation

- Play an imported sound from Dashboard, Library, Favorites, and Recent.
- Confirm the local monitor contains effects and does not loop the microphone.
- Confirm the virtual cable receives microphone + effects.
- Disconnect and reconnect each endpoint independently.
- Verify Discord/OBS input levels and disable aggressive noise suppression if it removes sound effects.
