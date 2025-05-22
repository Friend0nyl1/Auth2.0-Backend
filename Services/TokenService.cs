

// TokenService.cs
using auth.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class TokenService(IConfiguration configuration ,ILogger<TokenService> logger)
{
    private readonly ILogger<TokenService> _logger = logger;
    private readonly string _secretKey = configuration["Jwt:SecretKeyV2"];
    private readonly string _issuer = configuration["Jwt:Issuer"];

    private readonly string _audience = configuration["Jwt:Audience"];
    private readonly int _accessTokenExpirationMinutes = 60;

    public int AccessTokenExpirationSeconds => _accessTokenExpirationMinutes * 60;



    public string GenerateAccessToken(string clientId, string userId, string[] scopes)
    {
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId ?? clientId),
        // new Claim(JwtRegisteredClaimNames.Aud, _audience), // ใช้ค่า _audience ที่กำหนดค่าไว้
        // new Claim(JwtRegisteredClaimNames.Iss, _issuer),   // เพิ่ม Issuer Claim
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        if (scopes != null && scopes.Any())
        {
            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scope", scope));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            NotBefore = DateTime.UtcNow, // เพิ่ม Not Before Claim
            SigningCredentials = creds,
            Issuer = _issuer,
            Audience = _audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public JwtValidationResult ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return new JwtValidationResult { IsValid = false, ErrorMessage = "Token is missing." };
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken validatedToken;
                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                    // IssuerSigningKeys = _jwks?.GetSigningKeys(), // ใช้สำหรับ Algorithm แบบ Asymmetric
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero // ป้องกันปัญหาเรื่องเวลาไม่ตรงกันเล็กน้อย
                };

                tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null)
                {
                    return new JwtValidationResult { IsValid = false, ErrorMessage = "Invalid JWT format." };
                }

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier); // หรือ Claim อื่น ๆ ที่คุณใช้ระบุ User ID
                var scopeClaims = jwtToken.Claims.Where(c => c.Type == "scope").Select(c => c.Value).ToArray(); // หรือ Claim ที่คุณใช้เก็บ Scopes

                return new JwtValidationResult
                {
                    IsValid = true,
                    UserId = userIdClaim?.Value,
                    Scopes = scopeClaims
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new JwtValidationResult { IsValid = false, ErrorMessage = "Token has expired." };
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return new JwtValidationResult { IsValid = false, ErrorMessage = "Invalid token signature." };
            }
            catch (SecurityTokenValidationException ex)
            {
                return new JwtValidationResult { IsValid = false, ErrorMessage = $"Token validation failed: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "An unexpected error occurred.");
                return new JwtValidationResult { IsValid = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }

    public string GenerateRefreshToken()
    {
        // สร้าง Refresh Token ที่มีความเป็น Unique (เช่น Guid หรือ Random String)
        return Guid.NewGuid().ToString().Replace("-", "");
    }
}