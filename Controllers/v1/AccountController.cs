using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using auth.Models;
using auth.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace auth.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AccountController(AccountService accountService) : ControllerBase
    {
        private readonly AccountService _accountService = accountService;

        [HttpPost("register")]
        public async Task<ActionResult<Account>> RegisterAccount(CreateAccountRequest account)
        {
            var result = await _accountService.RegisterAccount(account);

            if (result.Success)
            {
                return Ok(result); // Return 200 OK with the ApiResponse
            }
            else
            {
                return BadRequest(result); // Return 400 Bad Request with the ApiResponse
            }
        }

        [Authorize(AuthenticationSchemes = "Bearer V2")]
        [HttpGet("account")]
        public async Task<ActionResult<Account>> GetAccount()
        {
           var userName = HttpContext.User.Identity?.Name;
            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _accountService.GetAccount(userId);

            if (result.Success)
            {
                return Ok(result); // Return 200 OK with the ApiResponse
            }
            else
            {
                return BadRequest(result); // Return 400 Bad Request with the ApiResponse
            }
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<Account>>> Login(Login login)
        {
            var result = await _accountService.Login(login);



            if (result.Success)
            {
                return Ok(result); // Return 200 OK with the ApiResponse
            }
            else
            {
                return Unauthorized(); // Return 400 Bad Request with the ApiResponse
            }   
        }   



    }
}
