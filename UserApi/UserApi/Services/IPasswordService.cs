namespace UserApi.Services
{
    public interface IPasswordService
    {
        string GenerateRandomPasswordWithDefaultComplexity();
    }
}