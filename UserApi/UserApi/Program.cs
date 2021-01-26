using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UserApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateWebHostBuilder(string[] args)
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

        public static void AddAllKeysPerFileForKubernetes(this IConfigurationBuilder configuration)
        {
            const string rootPath = "/mnt/secret/vh-infra-core/vh-user-api";

            if (Directory.Exists(rootPath))
            {
                configuration.AddKeyPerFile(directoryPath: rootPath, optional: true);
            }
        }
    }
}