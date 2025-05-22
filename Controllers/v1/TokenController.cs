using auth.Models;
using auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/v1/[controller]")]
public class TokenController(AuthorizationContext context, TokenService tokenService ,ILogger<TokenController> logger) : ControllerBase
{
    private readonly AuthorizationContext _context = context;
    private readonly TokenService _tokenService = tokenService;

    private readonly ILogger<TokenController> _logger = logger;

    
    [HttpPost("token")]
    public async Task<IActionResult> Token(
        [FromForm] string grant_type,
        [FromForm] string code,
        [FromForm] string redirect_uri,
        [FromForm] string client_id,
        [FromForm] string client_secret,
        [FromForm] string? refresh_token)
    {
        _logger.LogInformation(grant_type + " " + code + " " + redirect_uri + " " + client_id + " " + client_secret + " " + refresh_token);
        // 1. ตรวจสอบ grant_type
        if (grant_type == "authorization_code")
        {
            return await HandleAuthorizationCodeGrant(code, redirect_uri, client_id, client_secret);
        }
        else if (grant_type == "refresh_token")
        {
            return await HandleRefreshTokenGrant(refresh_token, client_id, client_secret);
        }
        else if (grant_type == "client_credentials")
        {
            return await HandleClientCredentialsGrant(client_id, client_secret);
        }
        else
        {
            return BadRequest("Invalid grant_type");
        }
    }

    private async Task<IActionResult> HandleAuthorizationCodeGrant(string code, string redirect_uri, string client_id, string client_secret)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
       try{
         // 2. ตรวจสอบ Authorization Code
        var grant = await _context.AuthorizationGrants
            .FirstOrDefaultAsync(g => g.Code == code && g.ClientId == client_id && g.RedirectUri == redirect_uri && g.ExpirationTime > DateTime.UtcNow);
        if (grant == null)
        {
            return BadRequest("Invalid or expired authorization code");
        }

        // 3. ตรวจสอบ Client ID และ Client Secret
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        if (client == null)
        {
            return Unauthorized("Invalid client credentials");
        }

        // 4. สร้าง Access Token และ Refresh Token
        var accessToken = _tokenService.GenerateAccessToken(client.ClientId, grant.UserId, grant.Scope?.Split(' '));
        var refreshToken = _tokenService.GenerateRefreshToken();
    _logger.LogInformation(accessToken + " " + refreshToken);
        // 5. บันทึก Refresh Token (Associate กับ User และ Client)
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            ClientId = client.ClientId,
            UserId = grant.UserId,
            ExpirationTime = DateTime.UtcNow.AddDays(30) // กำหนดอายุ Refresh Token
        };
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        // 6. ลบ Authorization Code ที่ใช้แล้ว
        _context.AuthorizationGrants.Remove(grant);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        // 7. ส่งคืน Access Token และ Refresh Token
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _tokenService.AccessTokenExpirationSeconds, // กำหนดจาก Service
            refresh_token = refreshToken,
            scope = grant.Scope
        });
       }
       catch(Exception ex){
        Console.WriteLine("ex.Message");
        _logger.LogInformation(ex.Message);
        return BadRequest(ex.Message);
       }
    }

    private async Task<IActionResult> HandleRefreshTokenGrant(string refresh_token, string client_id, string client_secret)
    {
       try{
         // 8. ตรวจสอบ Refresh Token
        var refreshTokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refresh_token && rt.ClientId == client_id && rt.ExpirationTime > DateTime.UtcNow);

        if (refreshTokenEntity == null)
        {
            return BadRequest("Invalid or expired refresh token");
        }

        // 9. ตรวจสอบ Client ID และ Client Secret
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        if (client == null)
        {
            return Unauthorized("Invalid client credentials");
        }

        // 10. สร้าง Access Token ใหม่
        var accessToken = _tokenService.GenerateAccessToken(client.ClientId, refreshTokenEntity.UserId, null); // อาจดึง Scope จาก Client หรือ Refresh Token

        // 11. สร้าง Refresh Token ใหม่ (ทางเลือก - สามารถ Reuse หรือสร้างใหม่)
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        refreshTokenEntity.Token = newRefreshToken;
        refreshTokenEntity.ExpirationTime = DateTime.UtcNow.AddDays(30);
        await _context.SaveChangesAsync();

        // 12. ส่งคืน Access Token และ Refresh Token ใหม่
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _tokenService.AccessTokenExpirationSeconds,
            refresh_token = newRefreshToken
        });
       }
       catch(Exception ex){
           return BadRequest(ex.Message);
       }
    }

    private async Task<IActionResult> HandleClientCredentialsGrant(string client_id, string client_secret)
    {
       try{
          // 13. ตรวจสอบ Client ID และ Client Secret
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == client_id && c.ClientSecret == client_secret);
        if (client == null)
        {
            return Unauthorized("Invalid client credentials");
        }

        // 14. สร้าง Access Token (ไม่มี User Context ใน Client Credentials)
        var accessToken = _tokenService.GenerateAccessToken(client.ClientId, null, client.AllowedGrantTypes?.Split(';')); // อาจใช้ AllowedGrantTypes เป็น Scope ชั่วคราว

        // 15. ส่งคืน Access Token
        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _tokenService.AccessTokenExpirationSeconds,
            // ไม่มี refresh_token ใน Client Credentials Grant โดยทั่วไป
        });
    }catch (Exception ex){
        return BadRequest(ex.Message);
    }
    }

    [HttpPost("verify-token")]
    public IActionResult VerifyAccessToken([FromBody] VerifyTokenRequest request){
        if (string.IsNullOrEmpty(request?.Token))
        {
            return BadRequest("Token is required.");
        }
        var verificationResult = _tokenService.ValidateToken(request.Token);
        if (verificationResult.IsValid)
        {
            return Ok(new
            {
                isValid = true,
                userId = verificationResult.UserId,
                scopes = verificationResult.Scopes
                // ข้อมูลอื่น ๆ ที่ต้องการส่งกลับ
            });
        }
        else
        {
            return Unauthorized(new { isValid = false, error = verificationResult.ErrorMessage });
        }
    }
}