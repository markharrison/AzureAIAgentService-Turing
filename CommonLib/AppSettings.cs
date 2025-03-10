using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace MarkAgentService.CommonLib
{
    public class AppSettings
    {
        public string aiProjectConnectionString { get; set; }
        public string vectorStoreId { get; set; }

        public ConfigurationManager configuration;

        public AppSettings()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "development";
            var hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddEnvironmentVariables();

            configuration = hostBuilder.Configuration;

            aiProjectConnectionString = configuration["AIProjectConnectionString"] ?? "";

            vectorStoreId = configuration["VectorStoreId"] ?? "";

        }

    }
}

