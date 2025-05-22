using System.Threading.Tasks;
using auth.Models;
using auth.Services;
using Microsoft.EntityFrameworkCore;

public class AuthorizationService(AuthorizationContext context,AccountService accountService)
{
    private readonly AuthorizationContext _context = context;
    private static readonly Random _random = new Random();
    private readonly AccountService _accountService = accountService;

    public async Task<Client> ValidateClient(string clientId)
    {
        var client =  await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
        return client;
    }



}