
using TatumWelcome2.BLL;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class WithdrawalMenu
{
    public static bool Run(Account current)
    {
        Console.WriteLine("(Enter 0 to cancel)");

        while (true)
        {
            Console.Write("Withdraw amount: ");
            string input = Console.ReadLine() ?? "";

            if (input == "0")
            {
                Console.WriteLine("Withdrawal cancelled.");
                return false;
            }

            if (!decimal.TryParse(input, out decimal amount))
            {
                Console.WriteLine("Enter a valid amount.");
                continue;
            }

            TransactionResult check =
                WithdrawalService.Check(current, amount);

            if (check == TransactionResult.InvalidAmount)
            {
                Console.WriteLine("Amount must be greater than zero.");
                continue;
            }

            if (check == TransactionResult.InsufficientFunds)
            {
                Console.WriteLine($"Insufficient available funds. Available balance: {current.AvailableBalance:N2}.");
                continue;
            }

            if (check != TransactionResult.Success)
            {
                Console.WriteLine($"Withdrawal unavailable: {check}");
                return false;
            }

            int pinAttempts = 0;

            while (pinAttempts < 3)
            {
                Console.Write(
                    $"Enter your PIN to confirm withdrawal of {amount:N2}: ");

                string pin = Console.ReadLine() ?? "";

                if (pin == "0")
                {
                    Console.WriteLine("Withdrawal cancelled.");
                    return false;
                }

                TransactionResult result =
                    WithdrawalService.Withdraw(current, amount, pin);

                if (result == TransactionResult.InvalidPin)
                {
                    pinAttempts++;
                    Console.WriteLine("Incorrect PIN.");
                    continue;
                }

                if (result == TransactionResult.Success)
                {
                    Console.WriteLine(
                        $"Withdrawn. Balance: {current.Balance:N2}");

                    return true;
                }

                Console.WriteLine($"Withdrawal failed: {result}");
                return false;
            }

            Console.WriteLine(
                "Too many wrong PIN attempts. Transaction cancelled.");

            return false;
        }
    }
}
