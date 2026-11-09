using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.Controllers;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapGet("/accounts/{number}",
            (string number, IAccountRepository repository) =>
            {
                Account? account = repository.FindByAccountNumber(number);

                if (account == null)
                    return Results.NotFound("Account not found.");

                var response = new AccountLookupDto
                {
                    AccountNumber = account.AccountNumber,
                    Name = account.Name
                };

                return Results.Ok(response);
            });
    }
}