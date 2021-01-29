using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
<<<<<<< HEAD
using VH.Core.Configuration;
=======
using UserApi.AksKeyVaultFileProvider;
>>>>>>> aks key vault load secrets

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

            return Host.CreateDefaultBuilder(args)
<<<<<<< HEAD
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    configBuilder.AddAksKeyVaultSecretProvider(vhInfraCore);
                    configBuilder.AddAksKeyVaultSecretProvider(vhUserApi);
=======
                .ConfigureAppConfiguration((_, configuration) =>
                {
                    var path = "/mnt/secrets/vh-infra-core/";
                    configuration.AddKeyPerFile(k =>
                    {
                        k.FileProvider = new AksKeyVaultSecretFileProvider(path);
                        k.Optional = true;
                    });
>>>>>>> aks key vault load secrets
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    webBuilder.UseIISIntegration();
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}