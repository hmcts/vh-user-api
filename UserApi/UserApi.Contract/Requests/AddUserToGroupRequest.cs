namespace UserApi.Contract.Requests
{
    public class AddUserToGroupRequest
    {
        public string UserId { get; set; }
        public string GroupName { get; set; }
    }
}