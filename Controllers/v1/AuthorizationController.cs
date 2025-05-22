using auth.Models;
using auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthorizationController(AuthorizationService authorizationService,AuthorizationContext context,ILogger<AuthorizationController> logger) : ControllerBase
{
    private static readonly Random _random = new Random();
    private readonly AuthorizationService _authorizationService = authorizationService;

    private readonly AuthorizationContext _context = context;

    private readonly ILogger<AuthorizationController> _logger = logger;
    [Authorize (AuthenticationSchemes = "Cookies")]
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(
        [FromQuery] string response_type,
        [FromQuery] string client_id,
        [FromQuery] string redirect_uri,
        [FromQuery] string scope,
        [FromQuery] string state)
    {
        // 1. ตรวจสอบ response_type
       try{
         if (response_type != "code")
        {
            return BadRequest("Invalid response_type");
        }

        // 2. ตรวจสอบ client_id
        var client = await _authorizationService.ValidateClient(client_id);
        if (client == null)
        {
            return BadRequest("Invalid client_id");
        }

        // 3. ตรวจสอบ redirect_uri
        if (!client.RedirectUris.Split(';').Contains(redirect_uri))
        {
            return BadRequest("Invalid redirect_uri");
        }



        // 4. ตรวจสอบ scope (ในตัวอย่างนี้ข้ามไปก่อน)

        // 5. ตรวจสอบสถานะการ Login ของผู้ใช้ (ในตัวอย่างนี้สมมติว่า Login แล้ว และมี User ID)
        //    ในระบบจริง คุณจะต้องมีกระบวนการ Authentication ก่อนหน้านี้

        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return Unauthorized("Account not authenticated");
            }


        // 6. สร้าง Authorization Code
        string authorizationCode = GenerateAuthorizationCode();
        DateTime expirationTime = DateTime.UtcNow.AddMinutes(30); // กำหนดอายุของ Code

        // 7. บันทึก Authorization Grant ในฐานข้อมูล
        var grant = new AuthorizationGrant
        {
            Code = authorizationCode,
            ClientId = client.ClientId,
            UserId = accountId,
            Scope = scope,
            RedirectUri = redirect_uri,
            ExpirationTime = expirationTime
        };
        _context.AuthorizationGrants.Add(grant);
        await _context.SaveChangesAsync();

        // 8. Redirect ผู้ใช้กลับไปยัง Client Application พร้อม Authorization Code
        var redirectUrl = $"{redirect_uri}?code={authorizationCode}&state={state}";
        return Ok(redirectUrl );
       }
       catch (Exception ex){
        Console.WriteLine("Error: " + ex.Message);
        _logger.LogInformation(ex.Message);
           return BadRequest(ex.Message);
       }
    }

    private static string GenerateAuthorizationCode(int length = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}