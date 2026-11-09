using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly List<Account> _accounts;

        public AccountRepository(List<Account> accounts)
        {
            _accounts = accounts;
        }

        public Account? FindByAccountNumber(string accountNumber)
        {
            foreach (Account account in _accounts)
            {
                if (account.AccountNumber == accountNumber)
                {
                    return account;
                }
            }

            return null;
        }
    }
}