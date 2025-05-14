namespace UserApi.Services.Interfaces;

public interface IPasswordService
{
    string GenerateRandomPasswordWithDefaultComplexity();
}