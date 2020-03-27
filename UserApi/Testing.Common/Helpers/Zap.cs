using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OWASPZAPDotNetAPI;
using System; 
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Testing.Common.Configuration;

namespace Testing.Common.Helper
{
    public static class Zap
    {
        private const string Configuration = "Release";
        
        private const string Tag = "test";

        private const string DockerCompose = "docker-compose";

        private static readonly string DockerArguments = $"-f docker-compose.yml -f docker-compose.test.yml -p {ZapConfiguration.ServiceName}";

        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

        private static HttpClientHandler httpClientHandler => new HttpClientHandler { Proxy = WebProxy };

        private static ZapConfiguration ZapConfiguration => new ConfigurationBuilder()
                                                            .AddJsonFile("appsettings.json")
                                                            .AddUserSecrets("CF55F1BB-0EE3-456A-A566-70E56AC24C95")
                                                            .Build()
                                                            .GetSection("ZapConfiguration")
                                                            .Get<ZapConfiguration>();
         

        private static readonly ClientApi Api = new ClientApi(ZapConfiguration.ApiAddress, ZapConfiguration.ApiPort, ZapConfiguration.ApiKey);

        public static IWebProxy WebProxy => ZapConfiguration.ZapScan ? new WebProxy($"http://{ZapConfiguration.ApiAddress}:{ZapConfiguration.ApiPort}", false) : null;

        public static bool SetupProxy => ZapConfiguration.ZapScan;

        private static string WorkingDirectory => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))));


        public static void Start()
        {
            if (!ZapConfiguration.ZapScan) return;

            Build();
            StartContainers();

            var zapStarted = WaitForZap().Result;

            if (!zapStarted)
            {
                throw new Exception($"Zap startup failed after trying for '{TestTimeout}' ");
            }

            var started = WaitForService().Result;

            if (!started)
            {
                throw new Exception($"Application service startup failed after trying for '{TestTimeout}'");
            }
        }

        public static void ReportAndShutDown(string reportFileName)
        {
            if (!ZapConfiguration.ZapScan) return;

            try
            {
                reportFileName = $"{reportFileName}-Tests-Security-{DateTime.Now.ToString("dd-MMM-yyyy-hh-mm-ss")}";
                WriteHtmlReport(reportFileName);
                WriteXmlReport(reportFileName);
            }
            finally
            {
                StopContainers();
            }
        }

        private static void Build()
        {
            var processStartInfo = CreateProcess("dotnet", $"publish --configuration {Configuration}", $"{WorkingDirectory}\\{ZapConfiguration.SolutionFolderName}");
           
            RunProcess(processStartInfo);
        }

        private static void StartContainers()
        { 
            var processStartInfo = CreateProcess(DockerCompose, $"{DockerArguments} up -d --build");

            RunProcess(processStartInfo, true);
        }
        private static ProcessStartInfo CreateProcess(string fileName, string arguments, string workingDirectory = null)
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = !string.IsNullOrEmpty(workingDirectory) ? workingDirectory: WorkingDirectory
            };
        }

        private static void StopContainers()
        {
            var processStartInfo = CreateProcess(DockerCompose, $"{DockerArguments} down --rmi local");            

            RunProcess(processStartInfo, true);
        }

        private static void RunProcess(ProcessStartInfo processStartInfo, bool addEnvironmentVariables = false)
        {
            if(addEnvironmentVariables)
            {
                processStartInfo.Environment["TAG"] = Tag;
                processStartInfo.Environment["CONFIGURATION"] = Configuration;
                processStartInfo.Environment["COMPUTERNAME"] = Environment.MachineName;
            }

            var process = Process.Start(processStartInfo);

            process.WaitForExit();
            process.ExitCode.Should().Be(0);
        }

        private static async Task<bool> WaitForService()
        {
            var testServiceUrl = $"https://{ZapConfiguration.ServiceName}/swagger/index.html";

            using (var client = new HttpClient(httpClientHandler))
            {
                return await PollApi(client, testServiceUrl);
            }
        }

        private static async Task<bool> WaitForZap()
        {
            var zapUrl = $"http://{ZapConfiguration.ApiAddress}:{ZapConfiguration.ApiPort}";
            
            using (var client = new HttpClient())
            {
               return await PollApi(client, zapUrl);
            }
        }

        private static async Task<bool> PollApi(HttpClient client, string url)
        {
            client.Timeout = TimeSpan.FromSeconds(1);

            var startTime = DateTime.Now;
            while (DateTime.Now - startTime < TestTimeout)
            {
                try
                {
                    var response = await client.GetAsync(new Uri(url)).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch
                { 

                }

                await Task.Delay(1000).ConfigureAwait(false);
            }

            return false;
        }

        private static void WriteHtmlReport(string reportFileName)
        {
            File.WriteAllBytes(reportFileName + ".html", Api.core.htmlreport());
        }

        private static void WriteXmlReport(string reportFileName)
        {
            File.WriteAllBytes(reportFileName + ".xml", Api.core.xmlreport());
        }


    }
}
