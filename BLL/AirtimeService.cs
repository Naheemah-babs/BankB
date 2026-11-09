
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public static class AirtimeService
{
    public static async Task<BillPurchaseResult> PurchaseAsync(
        Account account,
        string network,
        string phone,
        decimal amount,
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

        if (amount < 50)
        {
            return new BillPurchaseResult(
                TransactionResult.InvalidAmount, false, "");
        }

        string reference = Guid.NewGuid().ToString("N");

        BillPaymentRequestDto request = new()
        {
            Reference = reference,
            BankId = "BankB",
            AccountNumber = account.AccountNumber,
            PaymentType = "Airtime",
            Network = network,
            PhoneNumber = phone,
            Amount = amount
        };

        return await BillPurchaseService.PurchaseAsync(
            account,
            request,
            bankApiClient,
            $"Airtime {network} {phone} Ref: {reference}");
    }
}
