
using TatumWelcome2.BLL;
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class TransferMenu
{
    public static bool Run(
        Account current,
        List<Account> accounts,
        IBankApiClient bankApiClient,
        InterbankOutgoingTransferService outgoingTransferService)
    {
        Console.WriteLine("(Enter 0 at any step to cancel)");

        while (true)
        {
            Console.WriteLine("1. Transfer to Tatum accounts");
            Console.WriteLine("2. Transfer to other banks");
            Console.Write("Choose transfer type: ");

            string choice = Console.ReadLine() ?? "";

            switch (choice)
            {
                case "1":
                    return IntraBank(current, accounts);

                case "2":
                    return InterBank(
                        current,
                        bankApiClient,
                        outgoingTransferService);

                case "0":
                    Console.WriteLine("Transfer cancelled.");
                    return false;

                default:
                    Console.WriteLine(
                        "Invalid transfer type. Please choose 1 or 2.");
                    break;
            }
        }
    }

    private static bool IntraBank(
        Account current,
        List<Account> accounts)
    {
        Account? recipient;

        while (true)
        {
            Console.Write("Recipient account number: ");
            string number = Console.ReadLine() ?? "";

            if (number == "0")
            {
                Console.WriteLine("Transfer cancelled.");
                return false;
            }

            recipient = TransferService.FindRecipient(
                accounts, number);

            if (recipient == null)
            {
                Console.WriteLine("Account not found.");
                continue;
            }

            if (recipient.AccountNumber == current.AccountNumber)
            {
                Console.WriteLine(
                    "You can't transfer to your own account.");
                continue;
            }

            break;
        }

        Console.WriteLine($"Recipient: {recipient.Name}");

        while (true)
        {
            Console.Write("Transfer amount: ");
            string input = Console.ReadLine() ?? "";

            if (input == "0")
            {
                Console.WriteLine("Transfer cancelled.");
                return false;
            }

            if (!decimal.TryParse(input, out decimal amount))
            {
                Console.WriteLine("Enter a valid number.");
                continue;
            }

            TransactionResult check =
                TransferService.Check(current, amount);

            if (check == TransactionResult.InvalidAmount)
            {
                Console.WriteLine(
                    "Amount must be greater than zero.");
                continue;
            }

            if (check == TransactionResult.InsufficientFunds)
            {
                Console.WriteLine(
    $"Insufficient available funds. Available balance: {current.AvailableBalance:N2}");
                continue;
            }

            if (check != TransactionResult.Success)
            {
                Console.WriteLine(
                    $"Transfer unavailable: {check}");
                return false;
            }

            Console.WriteLine(
                $"You are sending {amount:N2} to " +
                $"{recipient.Name} ({recipient.AccountNumber}).");

            int pinAttempts = 0;

            while (pinAttempts < 3)
            {
                Console.Write("Enter your PIN to confirm: ");

                string pin = PinInput.ReadHiddenPin();

                if (pin == "0")
                {
                    Console.WriteLine("Transfer cancelled.");
                    return false;
                }

                TransactionResult result =
                    TransferService.Transfer(
                        current, recipient, amount, pin);

                if (result == TransactionResult.InvalidPin)
                {
                    pinAttempts++;
                    Console.WriteLine("Incorrect PIN.");
                    continue;
                }

                if (result == TransactionResult.Success)
                {
                    Console.WriteLine(
                        $"{amount:N2} sent to {recipient.Name}. " +
                        $"Balance: {current.Balance:N2}");
                    return true;
                }

                Console.WriteLine($"Transfer failed: {result}");
                return false;
            }

            Console.WriteLine(
                "Too many wrong PIN attempts. Transaction cancelled.");
            return false;
        }
    }

    private static bool InterBank(
        Account current,
        IBankApiClient bankApiClient,
        InterbankOutgoingTransferService outgoingTransferService)
    {
        Console.Write("Recipient account number: ");
        string recipientNumber = Console.ReadLine() ?? "";

        if (recipientNumber == "0")
        {
            Console.WriteLine("Transfer cancelled.");
            return false;
        }

        Console.WriteLine("1. Bank A");
        Console.Write("Choose receiving bank: ");

        string bankChoice = Console.ReadLine() ?? "";

        if (bankChoice == "0")
        {
            Console.WriteLine("Transfer cancelled.");
            return false;
        }

        if (bankChoice != "1")
        {
            Console.WriteLine("Invalid bank selection.");
            return false;
        }

        const string bankUrl = "http://localhost:5000";

        try
        {
            AccountLookupDto? recipient =
                bankApiClient.FindAccountAsync(
                    bankUrl, recipientNumber)
                .GetAwaiter().GetResult();

            if (recipient == null)
            {
                Console.WriteLine(
                    "Recipient account not found.");
                return false;
            }

            Console.WriteLine(
                $"Recipient: {recipient.Name}");

            Console.WriteLine(
                $"Account number: {recipient.AccountNumber}");

            if (!ConfirmYesNo(
                "Is this the correct recipient? (yes/no): "))
            {
                Console.WriteLine("Transfer cancelled.");
                return false;
            }

            decimal amount;

            while (true)
            {
                Console.Write("Transfer amount: ");
                string input = Console.ReadLine() ?? "";

                if (input == "0")
                {
                    Console.WriteLine("Transfer cancelled.");
                    return false;
                }

                if (!decimal.TryParse(input, out amount))
                {
                    Console.WriteLine("Enter a valid number.");
                    continue;
                }

                TransactionResult check =
                    TransferService.Check(current, amount);

                if (check == TransactionResult.InvalidAmount)
                {
                    Console.WriteLine(
                        "Amount must be greater than zero.");
                    continue;
                }

                if (check == TransactionResult.InsufficientFunds)
                {
                    Console.WriteLine(
    $"Insufficient available funds. Available balance: {current.AvailableBalance:N2}");
                    continue;
                }

                if (check != TransactionResult.Success)
                {
                    Console.WriteLine(
                        $"Transfer unavailable: {check}");
                    return false;
                }

                break;
            }

            Console.WriteLine(
                $"You are sending {amount:N2} to " +
                $"{recipient.Name} ({recipient.AccountNumber}).");

            int pinAttempts = 0;
            string? verifiedPin = null;

            while (pinAttempts < 3)
            {
                Console.Write("Enter your PIN to confirm: ");

                string pin = PinInput.ReadHiddenPin();

                if (pin == "0")
                {
                    Console.WriteLine("Transfer cancelled.");
                    return false;
                }

                if (AccountService.VerifyPin(current, pin))
                {
                    verifiedPin = pin;
                    break;
                }

                pinAttempts++;
                Console.WriteLine("Incorrect PIN.");
            }

            if (verifiedPin == null)
            {
                Console.WriteLine(
                    "Too many wrong PIN attempts. Transaction cancelled.");
                return false;
            }

            if (!ConfirmYesNo(
                "Proceed with this transfer? (yes/no): "))
            {
                Console.WriteLine("Transfer cancelled.");
                return false;
            }

            string result =
                outgoingTransferService.SendTransfer(
                    current,
                    bankUrl,
                    recipient.AccountNumber,
                    amount,
                    verifiedPin);

            Console.WriteLine(result);

            if (result.StartsWith("Transfer successful!"))
            {
                Console.WriteLine(
                    $"Remaining balance: {current.Balance:N2}");
                return true;
            }

            return false;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine(
                "Unable to contact the receiving bank.");
            return false;
        }
    }

    private static bool ConfirmYesNo(string message)
    {
        while (true)
        {
            Console.Write(message);

            string answer =
                (Console.ReadLine() ?? "")
                .Trim()
                .ToLower();

            if (answer == "yes")
                return true;

            if (answer == "no" || answer == "0")
                return false;

            Console.WriteLine("Please enter yes or no.");
        }
    }
}
