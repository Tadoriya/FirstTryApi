namespace FirstTryApi.Models
{
	public enum UserRole
	{
		User,
		Admin
	}
	public class User
	{
		public int Id { get; set; }
		public string Pseudo { get; set; } = string.Empty;
		public string MotdePasse { get; set; } = string.Empty;
		public UserRole Role { get; set; } = UserRole.User;
	}
}