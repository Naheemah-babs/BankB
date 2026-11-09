
using TatumWelcome2.BLL;
using TatumWelcome2.Models;
using TatumWelcome2.Services;

namespace TatumWelcome2.Presentation;

public static class LoginMenu
{
    public static Account? Run(List<Account> accounts)
    {
        int attempts = 0;

        while (attempts < 3)
        {
            Console.Write("Account number: ");
            string number = Console.ReadLine() ?? "";

            Console.Write("PIN: ");
            string pin = PinInput.ReadHiddenPin();

            Account? current =
                AccountService.Login(accounts, number, pin);

            if (current != null)
                return current;

            attempts++;
            Console.WriteLine("Invalid account number or PIN.");
        }

        Console.WriteLine(
            "Too many failed attempts. Returning to login...");

        return null;
    }
}
