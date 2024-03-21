namespace UserApi.Contract.Requests
{
    public class UpdateUserAccountRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        
        /// <summary>
        /// Contact email of the user (optional)
        /// </summary>
        public string ContactEmail { get; set; }
    }
}