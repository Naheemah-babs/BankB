
using TatumWelcome2.BLL;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.Presentation;

public static class DataMenu
{
    public static async Task<bool> RunAsync(
        Account current,
        IBankApiClient bankApiClient)
    {
        Console.WriteLine("(Enter 0 at any step to cancel)");

        string network;

        while (true)
        {
            Console.WriteLine(
                "Network: 1. MTN  2. Airtel  3. Glo  4. 9mobile");

            Console.Write("Choose network: ");
            string input = Console.ReadLine() ?? "";

            if (input == "0")
            {
                Console.WriteLine("Data purchase cancelled.");
                return false;
            }

            if (!int.TryParse(input, out int choice) ||
                choice < 1 ||
                choice > DataPlans.Networks.Length)
            {
                Console.WriteLine(
                    "Invalid network. Choose a number from the list.");
                continue;
            }

            network = DataPlans.Networks[choice - 1];
            break;
        }

        DataPlan selectedPlan;

        while (true)
        {
            Console.WriteLine("Plans:");

            for (int i = 0; i < DataPlans.DataPlanItems.Count; i++)
            {
                DataPlan plan = DataPlans.DataPlanItems[i];

                Console.WriteLine(
                    $"{i + 1}. {plan.Name} - {plan.Price:N2}");
            }

            Console.Write("Choose plan: ");
            string input = Console.ReadLine() ?? "";

            if (input == "0")
            {
                Console.WriteLine("Data purchase cancelled.");
                return false;
            }

            if (!int.TryParse(input, out int choice) ||
                choice < 1 ||
                choice > DataPlans.DataPlanItems.Count)
            {
                Console.WriteLine(
                    "Invalid plan. Choose a number from the list.");
                continue;
            }

            selectedPlan = DataPlans.DataPlanItems[choice - 1];
            break;
        }

        string phone;

        while (true)
        {
            Console.Write("Phone number, 11 digits: ");
            phone = Console.ReadLine() ?? "";

            if (phone == "0")
            {
                Console.WriteLine("Data purchase cancelled.");
                return false;
            }

            if (!PhoneNumberValidator.IsValid(phone))
            {
                Console.WriteLine(
                    "Enter a valid Nigerian mobile number.");
                continue;
            }

            break;
        }

        BillPurchaseResult purchase =
            await DataService.PurchaseAsync(
                current,
                network,
                phone,
                selectedPlan,
                bankApiClient);

        if (purchase.IsPending)
        {
            Console.WriteLine(
                "\nYour data purchase is pending confirmation.");

            Console.WriteLine(
                $"Amount: {selectedPlan.Price:N2}");

            Console.WriteLine(
                $"Reference: {purchase.Reference}");

            return true;
        }

        if (purchase.Result == TransactionResult.Success)
        {
            Console.WriteLine(
                "\nYour data purchase was successful.");

            Console.WriteLine(
                $"Balance: {current.Balance:N2}");

            return true;
        }

        if (purchase.Result == TransactionResult.InsufficientFunds)
        {
            Console.WriteLine(
                "\nInsufficient funds. " +
                $"Available balance: {current.AvailableBalance:N2}.");

            return false;
        }

        Console.WriteLine(
    "\nYour data purchase was unsuccessful.");

        Console.WriteLine(
            "The payment provider could not process your request.");

        Console.WriteLine(
            "No money was deducted from your account.");

        Console.WriteLine(
            "Please try again later.");

        return false;
    }
}
