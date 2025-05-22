using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace auth.Models;

public class Client
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientName { get; set; }

    [Required]
    public required string ClientSecret { get; set; }

    [Required]
    public required string RedirectUris { get; set; }

    [Required]
    public string AllowedGrantTypes { get; internal set; }

     [Required]
    public required string Description { get; set; }

    [Required]
    public required string Homepage { get; set; }

    [Required]
    public string[] Scopes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [ForeignKey("Account")]
    public long AccountId { get; set; }

    public Account Account { get; set; }

}

public class ClientRegistrationRequest
{
    public string clientName { get; set; }
    public List<string> allowedGrantTypes { get; set; } // เช่น ["authorization_code", "refresh_token"]

    public int accountId { get; set; }

    public string description { get; set; }
    public string[] scopes { get; set; }

    public string redirectUris { get; set; }

    public string homepage { get; set; }
    // คุณอาจเพิ่ม Fields อื่นๆ ที่ต้องการ เช่น Client Type (public, confidential)
}