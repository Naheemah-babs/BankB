
using TatumWelcome2.DTOs;
using TatumWelcome2.Interfaces;
using TatumWelcome2.Models;

namespace TatumWelcome2.BLL;

public class InterbankTransferService
{
    private readonly IAccountRepository _repository;
    private readonly object _lock = new();

    private readonly Dictionary<string, InterbankTransferRequestDto>
        _processedTransfers = new(StringComparer.Ordinal);

    public InterbankTransferService(IAccountRepository repository)
    {
        _repository = repository;
    }

    public string ReceiveTransfer(InterbankTransferRequestDto request)
    {
        lock (_lock)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Reference) ||
                string.IsNullOrWhiteSpace(request.SenderAccountNumber) ||
                string.IsNullOrWhiteSpace(request.RecipientAccountNumber) ||
                request.Amount <= 0)
            {
                return "InvalidRequest";
            }

            if (_processedTransfers.TryGetValue(
                request.Reference, out var previous))
            {
                if (previous.SenderAccountNumber ==
                        request.SenderAccountNumber &&
                    previous.RecipientAccountNumber ==
                        request.RecipientAccountNumber &&
                    previous.Amount == request.Amount)
                {
                    return "AlreadyProcessed";
                }

                return "ReferenceConflict";
            }

            Account? recipient =
                _repository.FindByAccountNumber(
                    request.RecipientAccountNumber);

            if (recipient == null)
            {
                return "AccountNotFound";
            }

            bool credited = recipient.TransferIn(
                request.Amount,
                $"Interbank transfer from {request.SenderAccountNumber}",
                request.Reference);

            if (!credited)
            {
                return "InvalidRequest";
            }

            _processedTransfers.Add(
                request.Reference,
                new InterbankTransferRequestDto
                {
                    Reference = request.Reference,
                    SenderAccountNumber = request.SenderAccountNumber,
                    RecipientAccountNumber = request.RecipientAccountNumber,
                    Amount = request.Amount
                });

            return "Success";
        }
    }

    public InterbankTransferRequestDto? FindProcessedTransfer(
        string reference)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            if (!_processedTransfers.TryGetValue(
                reference, out var transfer))
            {
                return null;
            }

            return new InterbankTransferRequestDto
            {
                Reference = transfer.Reference,
                SenderAccountNumber = transfer.SenderAccountNumber,
                RecipientAccountNumber = transfer.RecipientAccountNumber,
                Amount = transfer.Amount
            };
        }
    }
}
