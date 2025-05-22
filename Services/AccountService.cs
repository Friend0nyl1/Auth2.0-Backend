
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using auth.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace auth.Services;
public class AccountService(ILogger<AccountService> logger,AuthorizationContext context ,IConfiguration configuration , TokenService tokenService, IHttpContextAccessor httpContextAccessor)
{
    private readonly AuthorizationContext _context = context;

    private readonly TokenService _tokenService = tokenService;
    private readonly ILogger<AccountService> _logger =   logger;
    private readonly string _secretKey = configuration["Jwt:SecretKey"];
    private readonly string _issuer = configuration["Jwt:Issuer"];
    private readonly string _audience = configuration["Jwt:Audience"];
    private readonly IConfiguration _configuration = configuration;
    private readonly string _expirationMinutes = configuration["Jwt:TokenExpirationMinutes"];
    
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;



    public async Task<ApiResponse<string>> Login(Login login)
    {
        _logger.LogInformation(login.Email);
        try
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(account => account.Email == login.Email);
            if (account != null && VerifyPassword(login.Password, account.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()), // ID ของผู้ใช้งาน
                    new Claim(ClaimTypes.Email, account.Email), // อีเมลของผู้ใช้งาน
                    new Claim(ClaimTypes.Name, account.DisplayName), // ชื่อผู้ใช้งาน
                    // เพิ่ม Claims อื่นๆ ที่จำเป็น เช่น Roles
                    // new Claim(ClaimTypes.Role, "Admin"),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, "Cookies");

                var authProperties = new AuthenticationProperties
                {
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // กำหนดเวลาหมดอายุของ Cookie (ถ้าไม่ได้ใช้ SlidingExpiration)
                };

                if (_httpContextAccessor.HttpContext != null)
                {
                    await _httpContextAccessor.HttpContext.SignInAsync(
                        "Cookies",
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation($"User {account.Email} logged in and session created.");
                }
                else
                {
                    _logger.LogError("HttpContext is null, cannot create session.");
                    return new ApiResponse<string>{ Success = false, Message = "Server error: Could not create session." };
                }
                 string token = GenerateJwtToken(account);
                 
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Logged in",
                    Data = token
                };
            }
            else
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Username or password is incorrect"
                };
            }
        }
        catch (System.Exception)
        {
            _logger.LogInformation("Error logging in");
            return new ApiResponse<string>
            {
                Success = false,
                Message = "Error logging in"
            };
        }

    }

private string GenerateJwtToken(Account account)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expireMinutes = jwtSettings.GetValue<int>("TokenExpirationMinutes");

        _logger.LogInformation(secretKey);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            // เพิ่ม Claims อื่นๆ ที่คุณต้องการใส่ใน Token เช่น Roles, Permissions
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<ApiResponse<string>> RegisterAccount(CreateAccountRequest account)
    {
        try
        {
            _logger.LogInformation(account.Email + " " + account.Password); 
            var existingUser = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == account.Email);

            if (existingUser != null)
            {
                return new ApiResponse<string> { Success = false, Message = "Email already exists" };
            }
            if (account.Password != account.ConfirmPassword)
            {
                return new ApiResponse<string> { Success = false, Message = "Passwords do not match" };
            }
            Account newAccount = new Account
            {
                DisplayName = account.DisplayName,
                Email = account.Email,
                Password = HashPassword(account.Password),
                UserName = account.UserName
            };
            _context.Accounts.Add(newAccount);
            await _context.SaveChangesAsync();
            Account createdAccount = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == account.Email);
            if (createdAccount == null)
            {
                return new ApiResponse<string> { Success = false, Message = "Error creating account" };
            }_logger.LogInformation(createdAccount.Id + " " + createdAccount.DisplayName + " " + createdAccount.Email + " " + createdAccount.Password + " " + createdAccount.UserName);
            string token = GenerateJwtToken(createdAccount);

            return new ApiResponse<string> { Success = true, Data = token };
        }
        catch (System.Exception)
        {
            _logger.LogInformation("Error registering account");
            return new ApiResponse<string> { Success = false, Message = "Error registering account" };
        }
    }
    
    public async Task<ApiResponse<Account>> GetAccount(string id)
    {
        // var verificationResult = _tokenService.ValidateToken(accessToken);
        // if (!verificationResult.IsValid)
        // {
        //     // return Ok(new
        //     // {
        //     //     isValid = true,
        //     //     userId = verificationResult.UserId,
        //     //     scopes = verificationResult.Scopes
        //     //     // ข้อมูลอื่น ๆ ที่ต้องการส่งกลับ
        //     // });
        //     return new ApiResponse<Account> { Success = false, Message = verificationResult.ErrorMessage };
        // }
        try
        {
            
            // _logger.LogInformation(accessToken);
            // var handler = new JwtSecurityTokenHandler();

            // string Id = GetClaimValue(accessToken, "sub");
            // Console.WriteLine(Id);

            // var token = handler.ReadJwtToken(accessToken);
            
            // if (token == null)
            // {
            //     return null;
            // }
            var account = await _context.Accounts.Select(a => new Account{
                Id = a.Id,
                DisplayName = a.DisplayName,
                Email = a.Email,
                UserName = a.UserName
            }).FirstOrDefaultAsync(a => a.Id == long.Parse(id));

            if (account == null)
            {
                return new ApiResponse<Account> { Success = false, Message = "Account not found" };
            }
            return new ApiResponse<Account> { Success = true, Data = account };
        }
        catch (System.Exception)
        {

            return new ApiResponse<Account> { Success = false, Message = "Error getting account" };
        }
    }

    public string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return HashPassword(password) == hashedPassword;
    }

        public  string GetClaimValue(string jwtToken, string claimType)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(jwtToken))
        {
            Console.WriteLine("Invalid JWT format.");
            return null;
        }

        var token = handler.ReadJwtToken(jwtToken);
        return token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }


}