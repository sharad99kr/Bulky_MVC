using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Bulky.Utility;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using ProjectCore.Filters;
using ProjectCore.Models.AI;
using ProjectCore.Plugins;
using ProjectCore.Services.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using KernelPluginFactory = ProjectCore.Plugins.KernelPluginFactory;

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

            services.AddSingleton(sp =>
            {
                var kernelBuilder = Kernel.CreateBuilder();
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: azureConfig.DeploymentName,
                    endpoint: azureConfig.Endpoint,
                    apiKey: azureConfig.ApiKey);

                
#pragma warning disable SKEXP0010
                kernelBuilder.AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: "text-embedding-3-small",
                    endpoint: azureConfig.Endpoint,
                    apiKey: azureConfig.ApiKey);
#pragma warning restore SKEXP0010

                var logging= sp.GetRequiredService<ILogger<AIFunctionInvocationFilter>>();
                kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter>(new AIFunctionInvocationFilter(logging));

                return kernelBuilder.Build();
            });

            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => 
            sp.GetRequiredService<Kernel>()
            .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());

            services.AddSingleton<IChatCompletionService>(sp =>
            sp.GetRequiredService<Kernel>()
            .GetRequiredService<IChatCompletionService>());


            services.AddScoped<IKernelPluginFactory, KernelPluginFactory>();


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

                services.AddScoped<IAzureSearchIndexService, AzureSearchIndexService>();

                //reading azure search AI API key and endpoint from configuration and registering SearchClient and SearchIndexClient
                var searchConfig = configuration.GetSection(AzureSearchSettings.SectionName)
                                        .Get<AzureSearchSettings>() ??
                                        throw new InvalidOperationException(
                                            "AzureSearch configuration section is missing.");

                services.Configure<AzureSearchSettings>(configuration.GetSection(AzureSearchSettings.SectionName));

                var credential = new AzureKeyCredential(searchConfig.ApiKey);
                services.AddSingleton(new SearchClient(new Uri(searchConfig.Endpoint), SD.AzureSearchIndexName, credential));
                services.AddSingleton(new SearchIndexClient(new Uri(searchConfig.Endpoint), credential));

                //Registering the Chat service 
                services.AddScoped<IChatService, ChatService>();

                return services;
            }
    }

    
}
