using TatumWelcome2.Models;

namespace TatumWelcome2.Interfaces
{
    public interface IAccountRepository
    {
        Account? FindByAccountNumber(string accountNumber);
    }
}