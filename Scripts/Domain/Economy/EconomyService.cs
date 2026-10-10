using System;

namespace Omerta.Domain.Economy;

public sealed class EconomyService
{
    public EconomyService(int initialBalance = 0)
    {
        if (initialBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBalance));
        }

        Balance = initialBalance;
    }

    public event Action<int, int>? BalanceChanged;

    public int Balance { get; private set; }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Income must be positive.");
        }

        int previousBalance = Balance;
        Balance = checked(Balance + amount);
        BalanceChanged?.Invoke(previousBalance, Balance);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A cost must be positive.");
        }

        if (amount > Balance)
        {
            return false;
        }

        int previousBalance = Balance;
        Balance -= amount;
        BalanceChanged?.Invoke(previousBalance, Balance);
        return true;
    }

    public void RestoreBalance(int balance)
    {
        if (balance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(balance));
        }

        int previousBalance = Balance;
        Balance = balance;
        BalanceChanged?.Invoke(previousBalance, Balance);
    }
}
