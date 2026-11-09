using TatumWelcome2.BLL;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class DepositMenu
{
    public static bool Run(Account current)
    {
        Console.WriteLine("(Enter 0 to cancel)");

        while (true)
        {
            Console.Write("Deposit amount: ");
            string input = Console.ReadLine() ?? "";

            if (input == "0")
            {
                Console.WriteLine("Deposit cancelled.");
                return false;
            }

            if (!decimal.TryParse(input, out decimal amount))
            {
                Console.WriteLine("Enter a valid number.");
                continue;
            }

            bool success = DepositService.Deposit(current, amount);

            if (success)
            {
                Console.WriteLine(
                    $"Deposited. Balance: {current.Balance:N2}");

                return true;
            }

            Console.WriteLine(
                "Deposit failed. Check the amount and account status.");
        }
    }
}