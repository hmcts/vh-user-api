using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UserApi
{
    public class Program
    {
        protected Program()
        {
        }
        
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // ReSharper disable once MemberCanBePrivate.Global Needed for client generation on build with nswag
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseIISIntegration();
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureAppConfiguration((_, configuration) => AddAllKeysPerFileForKubernetes(configuration));
                })
                .ConfigureAppConfiguration((_, configuration) => AddAllKeysPerFileForKubernetes(configuration));
        }

        private static void AddAllKeysPerFileForKubernetes(IConfigurationBuilder configuration)
        {
            const string rootPath = "/mnt/secret/vh-infra-core/vh-user-api";

            if (Directory.Exists(rootPath))
            {
                configuration.AddKeyPerFile(directoryPath: rootPath, optional: true);
            }
        }
    }
}