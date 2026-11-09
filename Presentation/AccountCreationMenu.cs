
using TatumWelcome2.BLL;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class AccountCreationMenu
{
    public static void Run(List<Account> accounts)
    {
        Console.WriteLine("\n===== CREATE ACCOUNT =====");
        Console.WriteLine("Enter 0 at any step to cancel.");

        string? name = ReadNameOrCity(
            "Full name: ",
            "Name");

        if (name == null)
            return;

        string? phone = ReadPhone();

        if (phone == null)
            return;

        string? pin = ReadPin();

        if (pin == null)
            return;

        string? city = ReadNameOrCity(
            "City: ",
            "City");

        if (city == null)
            return;

        decimal? openingDeposit = ReadOpeningDeposit();

        if (openingDeposit == null)
            return;

        Account? created = AccountCreationService.CreateAccount(
            accounts,
            name,
            phone,
            pin,
            city,
            openingDeposit.Value);

        if (created == null)
        {
            Console.WriteLine(
                "\nAccount creation failed. Please try again.");
            return;
        }

        Console.WriteLine(
            $"\nAccount created successfully! Welcome to Tatum, {created.Name}.");

        Console.WriteLine(
            $"Your account number is: {created.AccountNumber}");

        Console.WriteLine(
            "Keep your account number safe. You will need it to log in.");

        Console.WriteLine(
            $"Starting balance: ₦{created.Balance:N2}");

        Console.Write(
            "\nPress Enter to return to the main menu...");

        Console.ReadLine();
    }

    private static string? ReadNameOrCity(
        string prompt,
        string field)
    {
        while (true)
        {
            Console.Write(prompt);

            string input = (Console.ReadLine() ?? "").Trim();

            if (input == "0")
                return null;

            bool valid = input.Length >= 2 &&
                         input.Length <= 50;

            foreach (char character in input)
            {
                if (!char.IsLetter(character) &&
                    character != ' ' &&
                    character != '-')
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                return input;

            Console.WriteLine(
                $"{field} must contain 2–50 letters. Spaces and hyphens are allowed.");
        }
    }

    private static string? ReadPhone()
    {
        while (true)
        {
            Console.Write("Phone number (11 digits): ");

            string input = (Console.ReadLine() ?? "").Trim();

            if (input == "0")
                return null;

            if (input.Length == 11 &&
                input.StartsWith("0") &&
                input.All(char.IsDigit))
            {
                return input;
            }

            Console.WriteLine(
                "Enter a valid 11-digit phone number starting with 0.");
        }
    }

    private static string? ReadPin()
    {
        while (true)
        {
            Console.Write("Create a 4-digit PIN: ");

            string input = PinInput.ReadHiddenPin();

            if (input == "0")
                return null;

            if (input.Length == 4)
            {
                Console.Write("Confirm PIN: ");

                string confirmation = PinInput.ReadHiddenPin();

                if (confirmation == "0")
                    return null;

                if (confirmation == input)
                    return input;

                Console.WriteLine(
                    "PINs do not match. Please try again.");
                continue;
            }

            Console.WriteLine(
                "PIN must contain exactly 4 digits.");
        }
    }

    private static decimal? ReadOpeningDeposit()
    {
        while (true)
        {
            Console.Write(
                "Opening deposit (minimum ₦1,000): ");

            string input = (Console.ReadLine() ?? "").Trim();

            if (input == "0")
                return null;

            if (decimal.TryParse(input, out decimal amount) &&
                amount >= 1000)
            {
                return amount;
            }

            Console.WriteLine(
                "Opening deposit must be at least ₦1,000.");
        }
    }
}
