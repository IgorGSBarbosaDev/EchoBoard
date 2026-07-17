namespace EchoBoard.Application.Library;

public sealed class GenerateSoundWaveformUseCase
{
    private readonly ISoundLibraryRepository sounds;
    private readonly IAudioFileMetadataReader metadataReader;

    public GenerateSoundWaveformUseCase(ISoundLibraryRepository sounds, IAudioFileMetadataReader metadataReader)
    {
        this.sounds = sounds;
        this.metadataReader = metadataReader;
    }

    public async Task<byte[]> ExecuteAsync(Guid soundId, CancellationToken cancellationToken)
    {
        var sound = await sounds.GetSoundAsync(soundId, cancellationToken)
            ?? throw new SoundNotFoundException(soundId);
        if (sound.WaveformPeaks.Length == 32)
        {
            return [.. sound.WaveformPeaks];
        }

        var metadata = await metadataReader.ReadAsync(sound.FilePath, cancellationToken);
        if (metadata.WaveformPeaks is not { Length: 32 } peaks)
        {
            return [];
        }

        sound.SetWaveformPeaks(peaks, DateTimeOffset.UtcNow);
        await sounds.UpdateSoundAsync(sound, cancellationToken);
        return [.. peaks];
    }
}
