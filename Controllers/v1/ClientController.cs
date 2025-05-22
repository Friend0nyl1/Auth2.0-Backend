using System.Security.Claims;
using System.Threading.Tasks;
using auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/v1/[controller]")]
public class ClientController(AuthorizationContext context,ILogger<ClientController> logger) : ControllerBase
{
    private readonly AuthorizationContext _context = context;

    private readonly ILogger<ClientController> _logger = logger;
    [Authorize(AuthenticationSchemes = "Bearer V2")]
    [HttpDelete("{clientId}")]
    public async Task<IActionResult> DeleteClient(string clientId)
    {
        try
        {
            _logger.LogInformation(clientId);
            // var userName = HttpContext.User.Identity?.Name;
            // string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var client = await _context.Clients.Where(c => c.Id == clientId).FirstOrDefaultAsync();
            if (client == null)
            {
                return BadRequest("Client not found");
            }
            _context.Clients.Remove(client);
            await  _context.SaveChangesAsync();
            return Ok("Client deleted successfully");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
        
     }
}