using System.ComponentModel.DataAnnotations;

namespace FirstTryApi.Models;


public class UserPass
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Username must be alphanumeric and between 3 and 20 characters")]
    public string Username { get; set; } = null!; 

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9&^!@#]{4,20}$", ErrorMessage = "Username must be alphanumeric and between 3 and 20 characters")]
    public string Password { get; set;  }= null!;
}