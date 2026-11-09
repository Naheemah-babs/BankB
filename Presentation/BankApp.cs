
using Microsoft.Extensions.Configuration;
using TatumWelcome2.BLL;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class BankApp
{
    public static async Task RunAsync(
        List<Account> accounts,
        IBankApiClient bankApiClient,
        InterbankOutgoingTransferService outgoingTransferService,
        AdminService adminService,
        IConfiguration configuration)
    {
        while (true)
        {
            AsciiArt.ShowWelcome();

            Console.WriteLine("\n===== TATUM BANK =====");
            Console.WriteLine("1. Create account");
            Console.WriteLine("2. Customer login");
            Console.WriteLine("3. Admin login");
            Console.WriteLine("0. Exit");
            Console.Write("Choose: ");

            string choice = (Console.ReadLine() ?? "").Trim();

            switch (choice)
            {
                case "1":
                    AccountCreationMenu.Run(accounts);
                    break;

                case "2":
                    await RunCustomerAsync(
                        accounts,
                        bankApiClient,
                        outgoingTransferService);
                    break;

                case "3":
                    if (AuthenticateAdmin(configuration))
                    {
                        Console.WriteLine("\nAdmin login successful.");
                        AdminMenu.Run(adminService);
                    }
                    break;

                case "0":
                    AsciiArt.ShowGoodbye();
                    return;

                default:
                    Console.WriteLine(
                        "Invalid choice. Please try again.");
                    break;
            }
        }
    }

    private static bool AuthenticateAdmin(
        IConfiguration configuration)
    {
        string? adminUsername =
            configuration["TatumAdmin:Username"];

        string? adminPin =
            configuration["TatumAdmin:Pin"];

        if (string.IsNullOrWhiteSpace(adminUsername) ||
            string.IsNullOrWhiteSpace(adminPin))
        {
            Console.WriteLine(
                "Admin login is not configured. Contact the system administrator.");
            return false;
        }

        Console.WriteLine("\n===== ADMIN LOGIN =====");
        Console.WriteLine("(Enter 0 to cancel)");

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Console.Write("Admin username: ");
            string username = (Console.ReadLine() ?? "").Trim();

            if (username == "0")
                return false;

            Console.Write("Admin PIN: ");
            string pin = PinInput.ReadHiddenPin();

            if (pin == "0")
                return false;

            if (username == adminUsername && pin == adminPin)
                return true;

            Console.WriteLine(
                $"Invalid admin credentials. Attempts remaining: {3 - attempt}");
        }

        Console.WriteLine("Too many failed admin login attempts.");
        return false;
    }

    private static async Task RunCustomerAsync(
        List<Account> accounts,
        IBankApiClient bankApiClient,
        InterbankOutgoingTransferService outgoingTransferService)
    {
        Account? current = LoginMenu.Run(accounts);

        if (current == null)
            return;

        Console.WriteLine($"\nWelcome, {current.Name}.");

        bool running = true;

        while (running)
        {
            bool operationCompleted = false;

            Console.WriteLine("\n===== CUSTOMER MENU =====");
            Console.WriteLine("1. Deposit");
            Console.WriteLine("2. Withdraw");
            Console.WriteLine("3. Transfer");
            Console.WriteLine("4. Airtime");
            Console.WriteLine("5. Data");
            Console.WriteLine("6. Transaction history");
            Console.WriteLine("7. Check balance");
            Console.WriteLine("8. Logout");
            Console.Write("Choose: ");

            string choice = (Console.ReadLine() ?? "").Trim();

            if (current.IsFrozen &&
                (choice is "1" or "2" or "3" or "4" or "5"))
            {
                Console.WriteLine(
                    "\nYour account is frozen. You cannot perform this transaction. Please contact the bank administrator.");

                continue;
            }

            switch (choice)
            {
                case "1":
                    operationCompleted = DepositMenu.Run(current);
                    break;

                case "2":
                    operationCompleted = WithdrawalMenu.Run(current);
                    break;

                case "3":
                    operationCompleted = TransferMenu.Run(
                        current,
                        accounts,
                        bankApiClient,
                        outgoingTransferService);
                    break;

                case "4":
                    operationCompleted =
                        await AirtimeMenu.RunAsync(
                            current, bankApiClient);
                    break;

                case "5":
                    operationCompleted =
                        await DataMenu.RunAsync(
                            current, bankApiClient);
                    break;

                case "6":
                    AccountHistory.PrintHistory(current);
                    operationCompleted = true;
                    break;

                case "7":
                    Console.WriteLine(
                        $"Balance: {current.Balance:N2}");

                    Console.WriteLine(
                        $"Available balance: {current.AvailableBalance:N2}");

                    operationCompleted = true;
                    break;

                case "8":
                    running = false;
                    AsciiArt.ShowGoodbye();
                    break;

                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

            if (operationCompleted && !AskToContinue())
            {
                running = false;
                AsciiArt.ShowGoodbye();
            }
        }
    }

    private static bool AskToContinue()
    {
        while (true)
        {
            Console.Write(
                "\nWould you like to perform another operation? (yes/no): ");

            string answer =
                (Console.ReadLine() ?? "").Trim().ToLower();

            if (answer == "yes" || answer == "y")
                return true;

            if (answer == "no" || answer == "n")
                return false;

            Console.WriteLine("Please type yes or no.");
        }
    }
}
