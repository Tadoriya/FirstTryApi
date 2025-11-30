namespace FirstTryApi.Models;


public class UserUpdate : UserInfo
{
    public UserRole Role { get; set; } = UserRole.User;
}