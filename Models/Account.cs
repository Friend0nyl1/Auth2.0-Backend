using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace auth.Models;

[Index(nameof(UserName), nameof(Email), IsUnique = true, Name = "IX_UniqueUsernameEmail")]
public class Account {

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get ; set ;}

    [Required]
    public required string UserName { get ; set ;}

    [Required]
    public  string Password {get ; set ;}
    
    [Required]
    [EmailAddress]
    public required string Email { get ; set; }

    [Required]
    public required string DisplayName { get ; set;}
}

public class CreateAccountRequest  {

    [Required]
    public required string Password {get ; set ;}

    [Required]
    public required string ConfirmPassword {get ; set ;}
    
    [Required]
    [EmailAddress]
    public required string Email { get ; set; }

    [Required]
    public required string DisplayName { get ; set;}    

    [Required]
    public required string UserName { get ; set ;}

}

public class Login {

    [Required]
    [EmailAddress]
    public required string Email { get ; set; }

    [Required]
    public required string Password {get ; set ;}
}