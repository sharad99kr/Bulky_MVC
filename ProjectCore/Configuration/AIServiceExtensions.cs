using Microsoft.SemanticKernel;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;

namespace ProjectCore.Configuration
{
    public static class AIServiceExtensions
    {
            public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
            {
                //Bind strongly typed options
                services.Configure<AISettings>(configuration.GetSection(AISettings.SectionName));
                services.Configure<AzureOpenAISettings>(configuration.GetSection(AzureOpenAISettings.SectionName));
                
                //Bind Semantic Kernel with Azure OpenAI
                var azureConfig = configuration.GetSection(AzureOpenAISettings.SectionName)
                                    .Get<AzureOpenAISettings>();
                
                services.AddKernel().AddAzureOpenAIChatCompletion(
                                    deploymentName: azureConfig.DeploymentName,
                                    endpoint: azureConfig.Endpoint,
                                    apiKey: azureConfig.ApiKey);

                //Register AI services(Scoped - one instance per HTTP request)
                //AzureOpenAIService is the provider - wired to IAIService
                services.AddScoped<AzureOpenAIService>();
                services.AddScoped<IAIService>(sp => 
                                        sp.GetRequiredService<AzureOpenAIService>());

                //BookAIService is the domain layer - wired to IProductAIService
                //It receives IAIService via constructor injection(gets AzureOpenAIService) 
                services.AddScoped<IProductAIService, BookAIService>();

                //Register memory cache for caching AI responses caching
                services.AddMemoryCache();

                return services;
            }
    }
}
