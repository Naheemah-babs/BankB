
using System.Net;
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public class InterbankOutgoingTransferService
{
    private readonly IBankApiClient _bankApiClient;
    private readonly object _lock = new();

    public InterbankOutgoingTransferService(
        IBankApiClient bankApiClient)
    {
        _bankApiClient = bankApiClient;
    }

    public string SendTransfer(
        Account sender,
        string bankUrl,
        string recipientAccountNumber,
        decimal amount,
        string pin)
    {
        if (!AccountService.VerifyPin(sender, pin))
        {
            return "Invalid PIN. Transfer cancelled.";
        }

        if (string.IsNullOrWhiteSpace(bankUrl) ||
            string.IsNullOrWhiteSpace(recipientAccountNumber))
        {
            return "Invalid bank or recipient.";
        }

        lock (_lock)
        {
            TransactionResult check = sender.CheckWithdraw(amount);

            if (check != TransactionResult.Success)
            {
                return $"Transfer failed: {check}";
            }

            string reference = Guid.NewGuid().ToString("N");

            var request = new InterbankTransferRequestDto
            {
                Reference = reference,
                SenderAccountNumber = sender.AccountNumber,
                RecipientAccountNumber = recipientAccountNumber,
                Amount = amount
            };

            TransactionResult debitResult = sender.TransferOut(
                amount,
                $"Interbank transfer to {recipientAccountNumber}",
                reference);

            if (debitResult != TransactionResult.Success)
            {
                return $"Transfer failed: {debitResult}";
            }

            try
            {
                using HttpResponseMessage response =
                    _bankApiClient.SendTransferAsync(bankUrl, request)
                        .GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    return $"Transfer successful! Reference: {reference}";
                }

               
                if (response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    bool reversed = sender.TransferIn(
                        amount,
                        $"Reversal of rejected interbank transfer {reference}",
                        reference);

                    if (reversed)
                    {
                        return $"Transfer failed. Amount reversed. Reference: {reference}";
                    }

                    return $"Transfer failed, but reversal could not be completed. Reference: {reference}";
                }

                return $"Transfer could not be confirmed. Reference: {reference}.";
            }
            catch (HttpRequestException)
            {
                return $"Transfer could not be confirmed due to a network error. Reference: {reference}.";
            }
            catch (TaskCanceledException)
            {
                return $"Transfer could not be confirmed because the request timed out. Reference: {reference}.";
            }
        }
    }
}
