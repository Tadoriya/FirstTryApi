namespace FirstTryApi.Models;


public class UserUpdate : UserPass
{
    public UserRole Role { get; set; } = UserRole.User;
}