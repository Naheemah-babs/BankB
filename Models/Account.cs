
namespace TatumWelcome2.Models;

public class Account
{
    private static readonly object BalanceLock = new();

    public string Name { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    private decimal _balance;
    private bool _isFrozen;

    private readonly List<Transaction> _transactions = new();

    private readonly Dictionary<string, BillReservation>
        _billReservations = new();

    private class BillReservation
    {
        public decimal Amount { get; init; }
        public string Description { get; init; } = string.Empty;
        public string Fingerprint { get; init; } = string.Empty;
        public string Status { get; set; } = "Pending";
    }

    private decimal ReservedAmountCore()
    {
        return _billReservations.Values
            .Where(payment => payment.Status == "Pending")
            .Sum(payment => payment.Amount);
    }

    public decimal Balance
    {
        get
        {
            lock (BalanceLock)
            {
                return _balance;
            }
        }
        private set
        {
            lock (BalanceLock)
            {
                _balance = value;
            }
        }
    }

    public decimal AvailableBalance
    {
        get
        {
            lock (BalanceLock)
            {
                return _balance - ReservedAmountCore();
            }
        }
    }

    public List<Transaction> Transactions
    {
        get
        {
            lock (BalanceLock)
            {
                return new List<Transaction>(_transactions);
            }
        }
        private set
        {
            lock (BalanceLock)
            {
                _transactions.Clear();
                _transactions.AddRange(value);
            }
        }
    }

    public bool IsFrozen
    {
        get
        {
            lock (BalanceLock)
            {
                return _isFrozen;
            }
        }
        private set
        {
            lock (BalanceLock)
            {
                _isFrozen = value;
            }
        }
    }

    public bool Deposit(decimal amount, string description)
    {
        lock (BalanceLock)
        {
            if (_isFrozen || amount <= 0)
                return false;

            decimal newBalance;

            try
            {
                newBalance = checked(_balance + amount);
            }
            catch (OverflowException)
            {
                return false;
            }

            _transactions.Add(new Transaction
            {
                Type = TransactionType.Deposit,
                Amount = amount,
                Date = DateTime.Now,
                Description = description
            });

            _balance = newBalance;
            return true;
        }
    }

    public TransactionResult CheckWithdraw(decimal amount)
    {
        lock (BalanceLock)
        {
            return CheckWithdrawCore(amount);
        }
    }

    private TransactionResult CheckWithdrawCore(decimal amount)
    {
        if (_isFrozen)
            return TransactionResult.AccountFrozen;

        if (amount <= 0)
            return TransactionResult.InvalidAmount;

        if (amount > _balance - ReservedAmountCore())
            return TransactionResult.InsufficientFunds;

        return TransactionResult.Success;
    }

    public TransactionResult Withdraw(
        decimal amount,
        string description)
    {
        lock (BalanceLock)
        {
            TransactionResult check = CheckWithdrawCore(amount);

            if (check != TransactionResult.Success)
                return check;

            _transactions.Add(new Transaction
            {
                Type = TransactionType.Withdrawal,
                Amount = amount,
                Date = DateTime.Now,
                Description = description
            });

            _balance -= amount;
            return TransactionResult.Success;
        }
    }

    public TransactionResult TransferOut(
        decimal amount,
        string description,
        string? reference = null)
    {
        lock (BalanceLock)
        {
            TransactionResult check = CheckWithdrawCore(amount);

            if (check != TransactionResult.Success)
                return check;

            _transactions.Add(new Transaction
            {
                Type = TransactionType.TransferOut,
                Amount = amount,
                Date = DateTime.Now,
                Description = description,
                Reference = reference
            });

            _balance -= amount;
            return TransactionResult.Success;
        }
    }

    public bool TransferIn(
        decimal amount,
        string description,
        string? reference = null)
    {
        lock (BalanceLock)
        {
            if (amount <= 0)
                return false;

            decimal newBalance;

            try
            {
                newBalance = checked(_balance + amount);
            }
            catch (OverflowException)
            {
                return false;
            }

            _transactions.Add(new Transaction
            {
                Type = TransactionType.TransferIn,
                Amount = amount,
                Date = DateTime.Now,
                Description = description,
                Reference = reference
            });

            _balance = newBalance;
            return true;
        }
    }

    public static TransactionResult TransferBetween(
        Account sender,
        Account recipient,
        decimal amount)
    {
        lock (BalanceLock)
        {
            if (ReferenceEquals(sender, recipient) ||
                sender.AccountNumber == recipient.AccountNumber)
            {
                return TransactionResult.SameAccount;
            }

            TransactionResult check =
                sender.CheckWithdrawCore(amount);

            if (check != TransactionResult.Success)
                return check;

            decimal recipientNewBalance;

            try
            {
                recipientNewBalance =
                    checked(recipient._balance + amount);
            }
            catch (OverflowException)
            {
                return TransactionResult.InvalidAmount;
            }

            DateTime transactionDate = DateTime.Now;
            string reference = Guid.NewGuid().ToString("N");

            Transaction outgoingTransaction = new Transaction
            {
                Type = TransactionType.TransferOut,
                Amount = amount,
                Date = transactionDate,
                Description =
                    $"Transfer to {recipient.Name} " +
                    $"({recipient.AccountNumber})",
                Reference = reference
            };

            Transaction incomingTransaction = new Transaction
            {
                Type = TransactionType.TransferIn,
                Amount = amount,
                Date = transactionDate,
                Description =
                    $"Transfer from {sender.Name} " +
                    $"({sender.AccountNumber})",
                Reference = reference
            };

            sender._transactions.Add(outgoingTransaction);

            try
            {
                recipient._transactions.Add(incomingTransaction);
            }
            catch
            {
                sender._transactions.Remove(outgoingTransaction);
                throw;
            }

            sender._balance -= amount;
            recipient._balance = recipientNewBalance;

            return TransactionResult.Success;
        }
    }

    public TransactionResult ReserveBillPayment(
        decimal amount,
        string reference,
        string description,
        string fingerprint,
        out bool alreadyCompleted)
    {
        lock (BalanceLock)
        {
            alreadyCompleted = false;

            if (string.IsNullOrWhiteSpace(reference) ||
                string.IsNullOrWhiteSpace(fingerprint))
            {
                return TransactionResult.PaymentFailed;
            }

            if (_billReservations.TryGetValue(
                reference, out BillReservation? existing))
            {
                if (existing.Fingerprint != fingerprint ||
                    existing.Amount != amount)
                {
                    return TransactionResult.PaymentFailed;
                }

                if (existing.Status == "Failed")
                    return TransactionResult.PaymentFailed;

                alreadyCompleted = existing.Status == "Completed";
                return TransactionResult.Success;
            }

            TransactionResult check = CheckWithdrawCore(amount);

            if (check != TransactionResult.Success)
                return check;

            _billReservations.Add(reference, new BillReservation
            {
                Amount = amount,
                Description = description,
                Fingerprint = fingerprint,
                Status = "Pending"
            });

            return TransactionResult.Success;
        }
    }


    public bool CompleteBillPayment(
        string reference,
        bool approved)
    {
        lock (BalanceLock)
        {
            if (!_billReservations.TryGetValue(
                reference, out BillReservation? payment))
            {
                return false;
            }

            if (payment.Status == "Completed")
                return approved;

            if (payment.Status == "Failed")
                return !approved;

            if (!approved)
            {
                _transactions.Add(new Transaction
                {
                    Type = TransactionType.FailedBillPayment,
                    Amount = payment.Amount,
                    Date = DateTime.Now,
                    Description =
                        $"{payment.Description} - Failed, funds released",
                    Reference = reference
                });

                payment.Status = "Failed";
                return true;
            }

            
            _transactions.Add(new Transaction
            {
                Type = TransactionType.Withdrawal,
                Amount = payment.Amount,
                Date = DateTime.Now,
                Description = payment.Description,
                Reference = reference
            });

            _balance -= payment.Amount;
            payment.Status = "Completed";

            return true;
        }
    }


    public void Freeze()
    {
        lock (BalanceLock)
        {
            _isFrozen = true;
        }
    }

    public void Unfreeze()
    {
        lock (BalanceLock)
        {
            _isFrozen = false;
        }
    }
}
