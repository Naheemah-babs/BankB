
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public static class DataService
{
    public static async Task<BillPurchaseResult> PurchaseAsync(
        Account account,
        string network,
        string phone,
        DataPlan plan,
        IBankApiClient bankApiClient)
    {
        if (!DataPlans.Networks.Contains(network))
        {
            return new BillPurchaseResult(
                TransactionResult.InvalidNetwork, false, "");
        }

        if (phone.Length != 11 || !phone.All(char.IsDigit))
        {
            return new BillPurchaseResult(
                TransactionResult.InvalidPhone, false, "");
        }

        if (!DataPlans.DataPlanItems.Contains(plan))
        {
            return new BillPurchaseResult(
                TransactionResult.InvalidPlan, false, "");
        }

        string reference = Guid.NewGuid().ToString("N");

        BillPaymentRequestDto request = new()
        {
            Reference = reference,
            BankId = "BankB",
            AccountNumber = account.AccountNumber,
            PaymentType = "Data",
            Network = network,
            PhoneNumber = phone,
            Amount = plan.Price,
            DataPlanName = plan.Name
        };

        return await BillPurchaseService.PurchaseAsync(
            account,
            request,
            bankApiClient,
            $"Data {network} {plan.Name} {phone} Ref: {reference}");
    }
}
