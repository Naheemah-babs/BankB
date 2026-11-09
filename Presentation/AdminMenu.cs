
using TatumWelcome2.BLL;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class AdminMenu
{
    public static void Run(AdminService adminService)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("===== ADMIN MENU =====");
            Console.WriteLine("1. Freeze account");
            Console.WriteLine("2. Unfreeze account");
            Console.WriteLine("3. Check account status");
            Console.WriteLine("0. Logout");
            Console.Write("Choose an option: ");

            string choice = (Console.ReadLine() ?? "").Trim();

            switch (choice)
            {
                case "1":
                    ChangeAccountStatus(adminService, true);
                    break;

                case "2":
                    ChangeAccountStatus(adminService, false);
                    break;

                case "3":
                    CheckAccountStatus(adminService);
                    break;

                case "0":
                    Console.WriteLine("Admin logged out.");
                    return;

                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private static void ChangeAccountStatus(
        AdminService adminService,
        bool freeze)
    {
        Console.Write("Enter customer account number (0 to cancel): ");
        string accountNumber = (Console.ReadLine() ?? "").Trim();

        if (accountNumber == "0")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        Account? account = adminService.FindAccount(accountNumber);

        if (account == null)
        {
            Console.WriteLine("Account not found.");
            return;
        }

        Console.WriteLine($"Account holder: {account.Name}");
        Console.WriteLine($"Account number: {account.AccountNumber}");
        Console.WriteLine(
            $"Current status: {(account.IsFrozen ? "Frozen" : "Active")}");

        if (freeze && account.IsFrozen)
        {
            Console.WriteLine("This account is already frozen.");
            return;
        }

        if (!freeze && !account.IsFrozen)
        {
            Console.WriteLine("This account is already active.");
            return;
        }

        string action = freeze ? "freeze" : "unfreeze";

        Console.Write($"Are you sure you want to {action} this account? (yes/no): ");
        string confirmation =
            (Console.ReadLine() ?? "").Trim().ToLower();

        if (confirmation != "yes")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        bool success = freeze
            ? adminService.FreezeAccount(accountNumber)
            : adminService.UnfreezeAccount(accountNumber);

        if (success)
        {
            Console.WriteLine(
                freeze
                    ? "Account frozen successfully."
                    : "Account unfrozen successfully.");
        }
        else
        {
            Console.WriteLine("Unable to update account status.");
        }
    }

    private static void CheckAccountStatus(AdminService adminService)
    {
        Console.Write("Enter customer account number (0 to cancel): ");
        string accountNumber = (Console.ReadLine() ?? "").Trim();

        if (accountNumber == "0")
        {
            Console.WriteLine("Operation cancelled.");
            return;
        }

        Account? account = adminService.FindAccount(accountNumber);

        if (account == null)
        {
            Console.WriteLine("Account not found.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Account holder: {account.Name}");
        Console.WriteLine($"Account number: {account.AccountNumber}");
        Console.WriteLine(
            $"Account status: {(account.IsFrozen ? "Frozen" : "Active")}");
    }
}
