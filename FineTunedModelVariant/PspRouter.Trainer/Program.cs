using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;
using PspRouter.Lib;
using DotNetEnv;

namespace PspRouter.Trainer;

public class Program
{
    public static async Task Main(string[] args)
    {
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
                // Load .env file first, before other configuration sources
                try
                {
                    // Try multiple locations for .env file
                    var envPaths = new[]
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", ".env")
                    };

                    bool envLoaded = false;
                    foreach (var envPath in envPaths)
                    {
                        if (File.Exists(envPath))
                        {
                            Env.Load(envPath);
                            Console.WriteLine($"✅ .env file loaded successfully from: {envPath}");
                            envLoaded = true;
                            break;
                        }
                    }

                    if (!envLoaded)
                    {
                        Console.WriteLine("⚠️ .env file not found in any of these locations:");
                        foreach (var path in envPaths)
                        {
                            Console.WriteLine($"   - {path}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading .env file: {ex.Message}");
                }
                
                /*
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddCommandLine(args);
                */
            })
            .ConfigureServices((context, services) =>
            {
                // Get .env file variables
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                // Register OpenAI client               
                services.AddSingleton<OpenAIClient>(provider => new OpenAIClient(apiKey));
                
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
