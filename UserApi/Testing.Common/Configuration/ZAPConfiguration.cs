namespace Testing.Common.Configuration
{
    public class ZapConfiguration
    {
        public string ApiAddress { get; set; }
        public int ApiPort { get; set; }
        public string ApiConfigPath { get; set; }
        public bool ZapScan { get; set; }
        public string ServiceName { get; set; }
        public string SolutionFolderName { get; set; }
    }
}
