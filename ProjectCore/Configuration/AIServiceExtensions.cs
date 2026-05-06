using ProjectCore.Models.AI;

namespace ProjectCore.Configuration
{
    public static class AIServiceExtensions
    {
            public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
            {
                services.Configure<AISettings>(configuration.GetSection(AISettings.SectionName));
                services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
    
                return services;
            }
    }
}
