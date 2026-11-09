
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public class AccountService
{
    public static Account? FindAccount(
        List<Account> accounts,
        string accountNumber)
    {
        foreach (Account account in accounts)
        {
            if (account.AccountNumber == accountNumber)
            {
                return account;
            }
        }

        return null;
    }

    public static bool VerifyPin(Account account, string pin)
    {
        return account.Pin == pin;
    }

    public static Account? Login(
        List<Account> accounts,
        string accountNumber,
        string pin)
    {
        Account? account = FindAccount(
            accounts,
            accountNumber);

        if (account == null ||
            !VerifyPin(account, pin))
        {
            return null;
        }

        return account;
    }

    public static TransactionResult Transfer(
        Account from,
        Account to,
        decimal amount)
    {
        return Account.TransferBetween(
            from,
            to,
            amount);
    }
}


