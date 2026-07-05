using EchoBoard.Application.Audio;
using NAudio.CoreAudioApi;

namespace EchoBoard.Audio.Microphone;

public sealed class WasapiAudioInputDeviceEnumerator : IAudioInputDeviceEnumerator
{
    public Task<IReadOnlyList<AudioInputDeviceDto>> ListAsync(CancellationToken cancellationToken)
    {
        using var enumerator = new MMDeviceEnumerator();
        string? defaultId = null;
        try
        {
            defaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications).ID;
        }
        catch (NAudio.MmException)
        {
            defaultId = null;
        }

        var devices = enumerator
            .EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(device => new AudioInputDeviceDto(
                device.ID,
                device.FriendlyName,
                string.Equals(device.ID, defaultId, StringComparison.Ordinal),
                IsAvailable: true))
            .ToArray();

        return Task.FromResult<IReadOnlyList<AudioInputDeviceDto>>(devices);
    }
}
