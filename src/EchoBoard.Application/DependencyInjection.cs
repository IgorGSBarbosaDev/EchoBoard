using Microsoft.Extensions.DependencyInjection;
using EchoBoard.Application.Library;

namespace EchoBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<CreateSoundUseCase>();
        services.AddTransient<GetSoundUseCase>();
        services.AddTransient<ListSoundsUseCase>();
        services.AddTransient<UpdateSoundUseCase>();
        services.AddTransient<DeleteSoundUseCase>();
        services.AddTransient<CreateCategoryUseCase>();
        services.AddTransient<GetCategoryUseCase>();
        services.AddTransient<ListCategoriesUseCase>();
        services.AddTransient<UpdateCategoryUseCase>();
        services.AddTransient<DeleteCategoryUseCase>();

        return services;
    }
}
