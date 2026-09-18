using CustomAgent.Server.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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

        var kernelBuilder = builder.Services.AddKernel();

        kernelBuilder.Plugins.AddFromType<GetDateTime>();

        builder.Services.AddSingleton<IChatCompletionService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var httpClient = httpClientFactory.CreateClient("AzureOpenAI");

            return new AzureOpenAIChatCompletionService(
                deploymentName: builder.Configuration.GetValue<string>("AzureOpenAI:DeploymentName")!,
                endpoint: builder.Configuration.GetValue<string>("AzureOpenAI:Endpoint")!,
                apiKey: builder.Configuration.GetValue<string>("AzureOpenAI:Key")!,
                httpClient: httpClient);
        });

        FunctionChoiceBehaviorOptions options = new() { AllowConcurrentInvocation = true };


        builder.Services.AddTransient<PromptExecutionSettings>(_ => new OpenAIPromptExecutionSettings
        {
            //Temperature = 0.75,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: options)
        });


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
