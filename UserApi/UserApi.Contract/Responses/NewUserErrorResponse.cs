namespace UserApi.Contract.Responses
{
    public class NewUserErrorResponse
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }
}