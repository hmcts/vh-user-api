namespace UserApi
{
    public class Settings
    {
        public string DefaultPassword { get; set; }
        /// <summary>
        ///     Flag to determine if we are running in production environment,
        ///     display the judges list based on the environment
        /// </summary>
        public bool IsLive { get; set; }
        public string ReformEmail { get; set; }
    }
}