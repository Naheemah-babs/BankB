
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public class InterbankOutgoingTransferService
{
    private readonly IBankApiClient _bankApiClient;
    private readonly object _lock = new();

    private readonly ConcurrentDictionary<string, PendingInterbankTransfer>
        _pendingTransfers = new();

    private readonly SemaphoreSlim _reconciliationLock = new(1, 1);

    public InterbankOutgoingTransferService(IBankApiClient bankApiClient)
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
            return "Invalid PIN. Transfer cancelled.";

        if (string.IsNullOrWhiteSpace(bankUrl) ||
            string.IsNullOrWhiteSpace(recipientAccountNumber))
            return "Invalid bank or recipient.";

        lock (_lock)
        {
            TransactionResult check = sender.CheckWithdraw(amount);

            if (check != TransactionResult.Success)
                return $"Transfer failed: {check}";

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
                return $"Transfer failed: {debitResult}";

            try
            {
                using HttpResponseMessage response =
                    _bankApiClient.SendTransferAsync(bankUrl, request)
                        .GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                    return $"Transfer successful! Reference: {reference}";

                if (response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    bool reversed = sender.TransferIn(
                        amount,
                        $"Reversal of rejected interbank transfer {reference}",
                        reference);

                    return reversed
                        ? $"Transfer failed. Amount reversed. Reference: {reference}"
                        : $"Transfer failed, but reversal could not be completed. Reference: {reference}";
                }

                SavePending(sender, bankUrl, request);

                return $"Transfer pending confirmation. Reference: {reference}.";
            }
            catch (HttpRequestException)
            {
                SavePending(sender, bankUrl, request);

                return $"Network error. Transfer pending confirmation. Reference: {reference}.";
            }
            catch (TaskCanceledException)
            {
                SavePending(sender, bankUrl, request);

                return $"Transfer timed out. Pending confirmation. Reference: {reference}.";
            }
        }
    }

    private void SavePending(
        Account sender,
        string bankUrl,
        InterbankTransferRequestDto request)
    {
        _pendingTransfers.TryAdd(
            request.Reference,
            new PendingInterbankTransfer(sender, bankUrl, request));
    }

    public async Task ReconcilePendingTransfersAsync(
        CancellationToken cancellationToken)
    {
        await _reconciliationLock.WaitAsync(cancellationToken);

        try
        {
            foreach (var entry in _pendingTransfers.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                PendingInterbankTransfer pending = entry.Value;

                try
                {
                    using HttpResponseMessage response =
                        await _bankApiClient.GetTransferStatusAsync(
                            pending.BankUrl,
                            pending.Request.Reference);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                        continue;

                    InterbankTransferRequestDto? confirmed =
                        await response.Content.ReadFromJsonAsync
                            <InterbankTransferRequestDto>(
                                cancellationToken: cancellationToken);

                    if (confirmed == null)
                        continue;

                    bool matches =
                        confirmed.Reference == pending.Request.Reference &&
                        confirmed.SenderAccountNumber ==
                            pending.Request.SenderAccountNumber &&
                        confirmed.RecipientAccountNumber ==
                            pending.Request.RecipientAccountNumber &&
                        confirmed.Amount == pending.Request.Amount;

                    if (!matches)
                    {
                        Console.WriteLine(
                            $"\nWARNING: Interbank transfer details mismatch. " +
                            $"Reference: {pending.Request.Reference}");
                        continue;
                    }

                    if (_pendingTransfers.TryRemove(
                        pending.Request.Reference, out _))
                    {
                        Console.WriteLine(
                            $"\nInterbank transfer confirmed successful. " +
                            $"Reference: {pending.Request.Reference}");
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"\nInterbank status check failed for " +
                        $"{pending.Request.Reference}: {ex.Message}");
                }
            }
        }
        finally
        {
            _reconciliationLock.Release();
        }
    }

    private sealed record PendingInterbankTransfer(
        Account Sender,
        string BankUrl,
        InterbankTransferRequestDto Request);
}
