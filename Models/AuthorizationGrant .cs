using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace auth.Models;

public class AuthorizationGrant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    [Required]
    public required string Code { get; set; }

    [Required]
    public required string RedirectUri { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required]
    public required string Scope { get; set; }

    [Required]
    public required DateTime ExpirationTime { get; set; }
    
    
}