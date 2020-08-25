namespace UserApi.Contract.Requests
{
    public class CreateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RecoveryEmail { get; set; }
        public bool IsTestUser { get; set; }
    }
}