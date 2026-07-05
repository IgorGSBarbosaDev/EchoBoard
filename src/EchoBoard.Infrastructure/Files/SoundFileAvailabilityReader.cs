using EchoBoard.Application.Library;

namespace EchoBoard.Infrastructure.Files;

public sealed class SoundFileAvailabilityReader : ISoundFileAvailabilityReader
{
    public Task<bool> ExistsAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(File.Exists(PathNormalizer.NormalizeFilePath(filePath)));
    }
}
