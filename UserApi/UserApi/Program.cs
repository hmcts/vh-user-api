using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.FileProviders;
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
            const string vhInfraCore = "/mnt/secrets/vh-infra-core";
            const string vhUserApi = "/mnt/secrets/vh-user-api";
            const string vhAdminWeb = "/mnt/secrets/vh-admin-web";
            var keyVaults = new[] { vhInfraCore, vhUserApi, vhAdminWeb };

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    foreach (var keyVault in keyVaults.Where(Directory.Exists))
                    {
                        configBuilder.Add(GetKeyPerFileSource(keyVault));
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseIISIntegration();
                    webBuilder.UseStartup<Startup>();
                });
        }
        
        private static KeyPerFileConfigurationSource GetKeyPerFileSource(string filePath)
        {
            return new KeyPerFileConfigurationSource
            {
                FileProvider = new PhysicalFileProvider(filePath),
                Optional = true,
                ReloadOnChange = true,
                SectionDelimiter = "--" // Set your custom delimiter here
            };
        }
    }
}