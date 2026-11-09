
using System.Security.Cryptography;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public static class AccountCreationService
{
    private static readonly object CreationLock = new();

    public static Account? CreateAccount(
        List<Account> accounts,
        string name,
        string phone,
        string pin,
        string city,
        decimal openingDeposit)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(pin) ||
            string.IsNullOrWhiteSpace(city) ||
            openingDeposit < 1000)
        {
            return null;
        }

        lock (CreationLock)
        {
            Account account = new Account
            {
                Name = name.Trim(),
                Phone = phone.Trim(),
                Branch = city.Trim(),
                Pin = pin,
                AccountNumber = GenerateAccountNumber(accounts)
            };


            bool deposited = account.Deposit(
                openingDeposit,
                "Opening deposit");

            if (!deposited)
            {
                return null;
            }

            accounts.Add(account);

            return account;
        }
    }

    private static string GenerateAccountNumber(
        List<Account> accounts)
    {
        string number;

        do
        {
            number = $"00{RandomNumberGenerator.GetInt32(100_000_000):D8}";
        }
        while (AccountService.FindAccount(accounts, number) != null);

        return number;
    }
}
