using Microsoft.Extensions.DependencyInjection;

namespace EchoBoard.Audio;

public static class DependencyInjection
{
    public static IServiceCollection AddAudio(this IServiceCollection services)
    {
        return services;
    }
}
