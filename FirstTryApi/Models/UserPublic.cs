namespace FirstTryApi.Models
{

    public class UserPublic
    {
        public int Id { get; set; }
        public string Pseudo { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
    }
}