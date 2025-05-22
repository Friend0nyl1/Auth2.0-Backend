using auth.Models;
using auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/v1/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AuthorizationContext _context;

    private readonly TokenService _tokenService;
    
    private readonly AccountService _accountService;

    public RegistrationController(AuthorizationContext context, TokenService tokenService, AccountService accountService)
    {
        _context = context;
        _tokenService = tokenService;
        _accountService = accountService;

    }


    [Authorize(AuthenticationSchemes = "Bearer V2")]
    [HttpPost("client")]
    public async Task<IActionResult> RegisterClient([FromBody] ClientRegistrationRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.clientName) || string.IsNullOrWhiteSpace(request.redirectUris) || request.allowedGrantTypes == null || !request.allowedGrantTypes.Any())
            {
                return BadRequest("Request body is invalid.");
            }

            // สร้าง Client ID ที่ไม่ซ้ำ (คุณอาจใช้ Guid หรือ Logic อื่นๆ)
            string clientId = Guid.NewGuid().ToString();

            // สร้าง Client Secret ที่ปลอดภัย (ควรเก็บแบบ Hash ใน Production)
            string clientSecret = Guid.NewGuid().ToString().Replace("-", ""); // ตัวอย่างง่ายๆ

            var newClient = new Client
            {
                ClientId = clientId,
                ClientSecret = clientSecret, // ใน Production ควร Hash ก่อนบันทึก
                ClientName = request.clientName,
                RedirectUris = request.redirectUris,
                AllowedGrantTypes = string.Join(";", request.allowedGrantTypes),
                AccountId = request.accountId,
                Scopes = request.scopes,
                Homepage = request.homepage,
                Description = request.description



            };

            _context.Clients.Add(newClient);
            await _context.SaveChangesAsync();

            // ส่งคืนข้อมูล Client ที่ลงทะเบียนแล้ว (ควรระมัดระวังในการส่ง Client Secret)
            return Ok(new
            {
                client_id = newClient.ClientId,
                client_secret = newClient.ClientSecret,
                client_name = newClient.ClientName,
                redirect_uris = newClient.RedirectUris,
                allowed_grant_types = newClient.AllowedGrantTypes
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = "Bearer V2")]
    [HttpGet("client/{AccountId}")] // อ่านข้อมูล Client ด้วย Client ID
    public IActionResult GetClients(long AccountId)
    {

        var userName = HttpContext.User.Identity?.Name;
        string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (AccountId != long.Parse(userId))
        {
            return BadRequest("Invalid Bearer Token");
        }
        try
        {
            var clients = _context.Clients.Where(c => c.AccountId == AccountId).ToList();
            return Ok(clients);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
         }

    }
}