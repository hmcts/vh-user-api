using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OWASPZAPDotNetAPI;
using System; 
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Testing.Common.Configuration;

namespace Testing.Common.Helper
{
    public static class Zap
    {
        private const string Configuration = "Release";

        private const string DockerCompose = "docker-compose";

        private static readonly string DockerArguments = $"-f docker-compose.yml -f docker-compose.test.yml -p {ZapConfiguration.ServiceName}";

        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

        private static HttpClientHandler httpClientHandler => new HttpClientHandler { Proxy = WebProxy };

        private static ZapConfiguration ZapConfiguration => new ConfigurationBuilder()
                                                            .AddJsonFile("appsettings.json")
                                                            .Build()
                                                            .GetSection("ZapConfiguration")
                                                            .Get<ZapConfiguration>();
                 

        private static readonly ClientApi Api = new ClientApi(ZapConfiguration.ApiAddress, ZapConfiguration.ApiPort, GetApiKey(ZapConfiguration.ApiConfigPath));

        public static IWebProxy WebProxy => (ZapConfiguration.ZapScan || ZapConfiguration.SetUpProxy) ? new WebProxy($"http://{ZapConfiguration.ApiAddress}:{ZapConfiguration.ApiPort}", false) : null;

        public static bool SetupProxy => (ZapConfiguration.ZapScan || ZapConfiguration.SetUpProxy);

        private static string WorkingDirectory => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))));

        private static bool setToken = false;

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
            else
            {
                Api.pscan.setEnabled("true");
                Api.pscan.enableAllScanners();
                Api.core.setMode("attack");
                Api.ascan.enableAllScanners("");
            }

            var started = WaitForService().Result;

            if (!started)
            {
                throw new Exception($"Application service startup failed after trying for '{TestTimeout}'");
            }
        }

        public static void SetAuthToken(string bearerToken)
        {
            if (!ZapConfiguration.ZapScan) return;

            if (!setToken)
            {
                var ruleDescription = "Auth";
                var replacers = (ApiResponseList)Api.replacer.rules();
                if(replacers != null && replacers.List.Count > 0)
                {
                    foreach(var replacer in replacers.List)
                    {
                        if(((ApiResponseSet)replacer).Dictionary["description"] == ruleDescription)
                        {
                            Api.replacer.removeRule(ruleDescription);
                            break;
                        }
                    }
                }
                Api.replacer.addRule(ruleDescription, "true", "REQ_HEADER", "false", "Authorization", $"Bearer {bearerToken}", "");

                setToken = true;
            }
        }

        public static void ReportAndShutDown(string reportFileName, string scanUrl)
        {
            if (!ZapConfiguration.ZapScan) return;

            try
            {
                PollPassiveScanCompletion();

                if(!string.IsNullOrEmpty(scanUrl) && ZapConfiguration.ActiveScan)
                {
                    Scan(scanUrl);
                }

                if (!string.IsNullOrEmpty(reportFileName))
                {
                    reportFileName = $"{reportFileName}-Tests-Security-{DateTime.Now.ToString("dd-MMM-yyyy-hh-mm-ss")}";
                    WriteHtmlReport(reportFileName);
                    WriteXmlReport(reportFileName);
                }
            }
            finally
            {
                StopContainers();
            }
        }

        public static void Scan(string target)
        {
            StartSpidering(target);
            ActiveScan(target);
        }

        private static void StartSpidering(string target)
        {
            try
            { 
                PollScanStatus(Api.spider.scan(target, "", "true", "", ""), "spider");
            }
            catch (Exception e)
            {
                throw new Exception($"Error on running spider scan at '{target}' : {e.Message}");
            }
        }

        private static void ActiveScan(string url)
        {
            try
            {
                PollScanStatus(Api.ascan.scan(url, "true", "", "", "", "", ""), "active");               
            }
            catch (Exception e)
            {
                throw new Exception($"Error on running active scan at '{url}' : {e.Message}");
            }
        }

        private static void PollScanStatus(IApiResponse response,string type)
        {
            var scanid = ((ApiResponseElement)response).Value;

            int progress;
            while (true)
            {
                Thread.Sleep(2000);
                var resp = (ApiResponseElement)(type == "spider" ? Api.spider.status(scanid) : Api.ascan.status(scanid));
                progress = Convert.ToInt32(resp.Value);
                if (progress >= 100)
                {
                    break;
                }
            }
        }
        
        private static void PollPassiveScanCompletion()
        {
            while (true)
            {
                Thread.Sleep(1000);
                var response = (ApiResponseElement) Api.pscan.recordsToScan();
                if (response != null && response.Value == "0")
                    break;
            }
        }

        private static string GetApiKey(string configFile)
        {
            var doc = new XmlDocument();
            doc.Load(configFile);

            var node = doc.GetElementsByTagName("key");

            if (node.Count > 0 && node[0] != null )
                return node[0].InnerText;

            throw new Exception($"Unable to resolve api key from {configFile}");
        }
        private static void Build()
        {
            var processStartInfo = CreateProcess("dotnet", $"publish --configuration {Configuration}", $"{WorkingDirectory}\\{ZapConfiguration.SolutionFolderName}");
           
            RunProcess(processStartInfo);
        }

        private static void StartContainers()
        { 
            var processStartInfo = CreateProcess(DockerCompose, $"{DockerArguments} up -d --build");

            RunProcess(processStartInfo);
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

            RunProcess(processStartInfo);
        }

        private static void RunProcess(ProcessStartInfo processStartInfo)
        {           
            processStartInfo.Environment["CONFIGURATION"] = Configuration;

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
