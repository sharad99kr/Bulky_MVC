using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Bulky.DataAccess;
using Bulky.DataAccess.AI.CQRS.Commands;
using Bulky.DataAccess.AI.Inventory.Interfaces;
using Bulky.DataAccess.AI.Inventory.Services;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectCore;
using System.Security.Authentication;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) => {
        var cfg = ctx.Configuration;

        // EF Core — same connection string as BulkyWeb.
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(cfg.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // Unit of Work + Repositories.
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Inventory services.
        services.AddScoped<IInventoryReader, InventoryReader>();
        services.AddScoped<IInventoryOrchestrator, InventoryOrchestrationService>();

        // IChatClient for MAF agents (reuse same Azure OpenAI deployment).
        services.AddSingleton<IChatClient>(sp => {
            var endpoint = cfg["AzureOpenAI:Endpoint"]!;
            var deployment = cfg["AzureOpenAI:DeploymentName"]!;
            var apiKey = cfg["AzureOpenAI:ApiKey"];
            AzureOpenAIClient azureClient = string.IsNullOrEmpty(apiKey)
                ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
                : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            return azureClient.GetChatClient(deployment)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        });

        // IInventoryAgentFactory (after Day 2 agents are built).
        services.AddScoped<IInventoryAgentFactory, InventoryAgentFactory>();

        // MediatR — scans the assembly for handlers.
        services.AddMediatR(mt => {
            mt.RegisterServicesFromAssemblyContaining<ProjectCoreAssemblyMarker>();
            mt.RegisterServicesFromAssemblyContaining<DataAccessAssemblyMarker>();
        });

        // MassTransit — same CloudAMQP config as BulkyWeb.
        var rabbitHost = cfg["RabbitMQ:Host"];
        var rabbitVHost = cfg["RabbitMQ:VHost"];
        var rabbitUser = cfg["RabbitMQ:Username"];
        var rabbitPassword = cfg["RabbitMQ:Password"];

        services.AddMassTransit(x => {
            // The Function is a publisher only — no consumers here.
            // BulkyWeb hosts the consumers (NotificationConsumer,
            // DiscrepancyConsumer, DeadLetterConsumer).
            if(string.IsNullOrWhiteSpace(rabbitHost)) {
                x.UsingInMemory();
            } else {
                x.UsingRabbitMq((_, cfg) => {
                    cfg.Host(rabbitHost, rabbitVHost, h => {
                        h.Username(rabbitUser);
                        h.Password(rabbitPassword);
                        h.UseSsl(s => s.Protocol = SslProtocols.Tls12);
                    });
                });
            }
        });
    })
    .Build();

await host.RunAsync();
