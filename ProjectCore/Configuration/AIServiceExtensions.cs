using Microsoft.SemanticKernel;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

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
                                    .Get<AzureOpenAISettings>() ?? 
                                    throw new InvalidOperationException(
                                        "AzureOpenAI configuration section is missing. " +
                                        "Check appsettings.json and User Secrets.");

                var kernelBuilder = services.AddKernel();
                kernelBuilder.AddAzureOpenAIChatCompletion(
                                    deploymentName: azureConfig.DeploymentName,
                                    endpoint: azureConfig.Endpoint,
                                    apiKey: azureConfig.ApiKey);
            
                #pragma warning disable SKEXP0010
                kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
                        deploymentName: "text-embedding-3-small",
                                        endpoint: azureConfig.Endpoint,
                                        apiKey: azureConfig.ApiKey
                                        );
                #pragma warning restore SKEXP0010

                //Resilience Patterns : Exponential Backoff with Jitter for transient fault handling
                services.AddHttpClient("AzureOpenAI")
                            .AddStandardResilienceHandler(options => {
                                options.Retry.MaxRetryAttempts = 3;
                                options.Retry.Delay = TimeSpan.FromSeconds(2);
                                options.Retry.UseJitter = true;  // prevents retry storms
                                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                            });


                services.AddScoped<IRagEvaluationService, RagEvaluationService>();
            
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

                //Register EmbeddingService for generating and caching embeddings
                services.AddScoped<IEmbeddingService, AzureEmbeddingService>();

                services.AddScoped<ISearchService, ProductSearchService>();

                

                return services;
            }
    }
}
