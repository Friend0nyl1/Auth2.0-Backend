// Models/RefreshToken.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    public string ClientId { get; set; }

    public string UserId { get; set; }

    public DateTime ExpirationTime { get; set; }
}