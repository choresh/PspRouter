using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OpenAI;
using PspRouter.Lib;
using DotNetEnv;

namespace PspRouter.Trainer;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load .env file if it exists
        try
        {
            Env.Load();
        }
        catch (FileNotFoundException)
        {
            // .env file not found, that's okay - use other sources
        }
        
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("PspRouter.Trainer starting...");
        
        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Application terminated unexpectedly");
            throw;
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Register OpenAI client
                var openAiApiKey = configuration["OPENAI_API_KEY"] ?? 
                                 throw new InvalidOperationException("OpenAI API key not found in configuration");
                
                services.AddSingleton<OpenAIClient>(provider => new OpenAIClient(openAiApiKey));
                
                // Register training services
                services.AddSingleton<ITrainingService, TrainingService>();
                services.AddSingleton<ITrainingDataProvider, TrainingDataProvider>();
                
                // Register hosted service for training
                services.AddHostedService<TrainingManager>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                }
            });
}
