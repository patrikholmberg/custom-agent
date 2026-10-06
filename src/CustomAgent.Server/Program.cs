using CustomAgent.Server.Tools;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.ClientModel.Primitives;

namespace CustomAgent.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        IList<AITool> tools =
        [
            AIFunctionFactory.Create(new GetDateTime().GetCurrentDateTime, "get_current_date_time"),
            AIFunctionFactory.Create(new WriteToDisk().WriteContentToFile, "write_content_to_file"),
        ];

        builder.Services.AddKeyedSingleton<IChatClient>("azure", (serviceProvider, _) =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var httpClient = httpClientFactory.CreateClient("AzureOpenAI");

            var client = new AzureOpenAIClient(
                new Uri(builder.Configuration.GetValue<string>("AzureOpenAI:Endpoint")!),
                new AzureKeyCredential(builder.Configuration.GetValue<string>("AzureOpenAI:Key")!),
                new AzureOpenAIClientOptions { Transport = new HttpClientPipelineTransport(httpClient) });

            return client
                .GetChatClient(builder.Configuration.GetValue<string>("AzureOpenAI:DeploymentName")!)
                .AsIChatClient();
        });

        builder.Services.AddKeyedSingleton<IChatClient>("ollama", (serviceProvider, _) =>
            new OllamaApiClient(
                new Uri(builder.Configuration.GetValue<string>("ollama:Endpoint")!),
                builder.Configuration.GetValue<string>("ollama:Model")!));

        foreach (var key in new[] { "azure", "ollama" })
        {
            builder.Services.AddKeyedSingleton<AIAgent>(key, (serviceProvider, _) =>
                serviceProvider.GetRequiredKeyedService<IChatClient>(key).AsAIAgent(
                    new ChatClientAgentOptions
                    {
                        Name = "CustomAgent",
                        AllowConcurrentInvocation = true,
                        ChatOptions = new ChatOptions
                        {
                            //Temperature = 0.75f,
                            Tools = tools
                        }
                    },
                    serviceProvider.GetRequiredService<ILoggerFactory>(),
                    serviceProvider));
        }


        builder.Services.AddHttpClient("AzureOpenAI", (servicesProvider, client) =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseHttpContentTracing();

        app.UseDefaultFiles();
        app.MapStaticAssets();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
