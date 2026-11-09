
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public static class BillPurchaseService
{
    private const string AppAUrl = "http://localhost:5000";
    private const int MaxAttempts = 2;

    private sealed class PendingPayment
    {
        public required Account Account { get; init; }

        public required BillPaymentRequestDto Request { get; init; }

        public bool IsProcessing { get; set; }
    }

    private static readonly ConcurrentDictionary<string, PendingPayment>
        PendingPayments = new();

    public static int PendingCount => PendingPayments.Count;

    public static async Task<BillPurchaseResult> PurchaseAsync(
        Account account,
        BillPaymentRequestDto request,
        IBankApiClient bankApiClient,
        string description)
    {
        if (request.BankId != "BankB" ||
            request.AccountNumber != account.AccountNumber ||
            string.IsNullOrWhiteSpace(request.Reference))
        {
            return new BillPurchaseResult(
                TransactionResult.PaymentFailed,
                false,
                request.Reference);
        }

        // Save a copy of the original request.
        BillPaymentRequestDto savedRequest = CopyRequest(request);

        string fingerprint =
            JsonSerializer.Serialize(savedRequest);

        TransactionResult reservation =
            account.ReserveBillPayment(
                savedRequest.Amount,
                savedRequest.Reference,
                description,
                fingerprint,
                out bool alreadyCompleted);

        if (reservation != TransactionResult.Success)
        {
            return new BillPurchaseResult(
                reservation,
                false,
                savedRequest.Reference);
        }

        if (alreadyCompleted)
        {
            return new BillPurchaseResult(
                TransactionResult.Success,
                false,
                savedRequest.Reference);
        }

        PendingPayment pending = new()
        {
            Account = account,
            Request = savedRequest
        };

        PendingPayment tracked =
            PendingPayments.GetOrAdd(
                savedRequest.Reference,
                pending);

        // Reject a reference used with different details.
        if (!ReferenceEquals(tracked.Account, account) ||
            JsonSerializer.Serialize(tracked.Request) != fingerprint)
        {
            return new BillPurchaseResult(
                TransactionResult.PaymentFailed,
                false,
                savedRequest.Reference);
        }

        return await ProcessPendingAsync(
            tracked,
            bankApiClient,
            MaxAttempts,
            CancellationToken.None);
    }

    public static async Task ReconcilePendingAsync(
        IBankApiClient bankApiClient,
        CancellationToken cancellationToken)
    {
        PendingPayment[] snapshot =
            PendingPayments.Values.ToArray();

        foreach (PendingPayment payment in snapshot)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                await ProcessPendingAsync(
                    payment,
                    bankApiClient,
                    1,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static async Task<BillPurchaseResult> ProcessPendingAsync(
        PendingPayment payment,
        IBankApiClient bankApiClient,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        string reference = payment.Request.Reference;

        // Check whether another operation is already
        // processing this particular payment.
        lock (payment)
        {
            if (payment.IsProcessing)
            {
                return new BillPurchaseResult(
                    TransactionResult.PaymentFailed,
                    true,
                    reference);
            }

            payment.IsProcessing = true;
        }

        try
        {
            
            if (!PendingPayments.TryGetValue(
                reference,
                out PendingPayment? current) ||
                !ReferenceEquals(current, payment))
            {
                return new BillPurchaseResult(
                    TransactionResult.PaymentFailed,
                    true,
                    reference);
            }

            for (int attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using HttpResponseMessage httpResponse =
                        await bankApiClient.SendBillPaymentAsync(
                            AppAUrl,
                            payment.Request);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        BillPaymentResponseDto? response =
                            await httpResponse.Content
                                .ReadFromJsonAsync<BillPaymentResponseDto>(
                                    cancellationToken: cancellationToken);

                        if (response != null &&
                            response.Reference == reference)
                        {
                            bool completed =
                                payment.Account.CompleteBillPayment(
                                    reference,
                                    response.Success);

                            if (completed)
                            {
                                
                                PendingPayments.TryRemove(
                                    reference,
                                    out _);

                                

                                return new BillPurchaseResult(
                                    response.Success
                                        ? TransactionResult.Success
                                        : TransactionResult.PaymentFailed,
                                    false,
                                    reference);
                            }
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Connection failed. Keep the payment pending.
                }
                catch (TaskCanceledException)
                    when (!cancellationToken.IsCancellationRequested)
                {
                    // HTTP request timed out. Keep the payment pending.
                }
                catch (JsonException)
                {
                    // Invalid response. Keep the payment pending.
                }
                catch (NotSupportedException)
                {
                    // Unsupported response. Keep the payment pending.
                }

                if (attempt < maxAttempts)
                {
                    await Task.Delay(
                        500 * attempt,
                        cancellationToken);
                }
            }

            
            // Keep the money reserved for reconciliation.
            return new BillPurchaseResult(
                TransactionResult.PaymentFailed,
                true,
                reference);
        }
        finally
        {
            // Allow a later reconciliation attempt.
            lock (payment)
            {
                payment.IsProcessing = false;
            }
        }
    }

    private static BillPaymentRequestDto CopyRequest(
        BillPaymentRequestDto request)
    {
        return new BillPaymentRequestDto
        {
            Reference = request.Reference,
            BankId = request.BankId,
            AccountNumber = request.AccountNumber,
            PaymentType = request.PaymentType,
            Network = request.Network,
            PhoneNumber = request.PhoneNumber,
            Amount = request.Amount,
            DataPlanName = request.DataPlanName
        };
    }
}
