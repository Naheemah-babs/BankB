
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public class AdminService
{
    private readonly List<Account> _accounts;

    public AdminService(List<Account> accounts)
    {
        _accounts = accounts;
    }

    public bool FreezeAccount(string accountNumber)
    {
        Account? account = AccountService.FindAccount(
            _accounts, accountNumber);

        if (account == null)
        {
            return false;
        }

        if (account.IsFrozen)
        {
            return false;
        }

        account.Freeze();
        return true;
    }

    public bool UnfreezeAccount(string accountNumber)
    {
        Account? account = AccountService.FindAccount(
            _accounts, accountNumber);

        if (account == null)
        {
            return false;
        }

        if (!account.IsFrozen)
        {
            return false;
        }

        account.Unfreeze();
        return true;
    }

    public Account? FindAccount(string accountNumber)
    {
        return AccountService.FindAccount(
            _accounts, accountNumber);
    }
}
