using System;
using Omerta.Domain.Economy;

namespace Omerta.Domain.Businesses;

public sealed class BusinessIncomeService
{
    private readonly string _beneficiaryFactionId;
    private readonly EconomyService _economy;
    private readonly BusinessState _business;
    private readonly BusinessOperationsState _operations;
    private readonly int _incomeAmount;
    private readonly float _intervalSeconds;
    private float _elapsedSeconds;

    public BusinessIncomeService(
        string beneficiaryFactionId,
        EconomyService economy,
        BusinessState business,
        BusinessOperationsState operations,
        int incomeAmount,
        float intervalSeconds)
    {
        if (string.IsNullOrWhiteSpace(beneficiaryFactionId))
        {
            throw new ArgumentException("A beneficiary faction is required.", nameof(beneficiaryFactionId));
        }

        _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        _business = business ?? throw new ArgumentNullException(nameof(business));
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
        if (operations.BusinessId != business.Id)
        {
            throw new ArgumentException("Operational state belongs to a different business.", nameof(operations));
        }

        if (incomeAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(incomeAmount));
        }

        if (intervalSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
        }

        _beneficiaryFactionId = beneficiaryFactionId;
        _incomeAmount = incomeAmount;
        _intervalSeconds = intervalSeconds;
    }

    public event Action<int>? IncomeCollected;

    public void Tick(float deltaSeconds)
    {
        if (deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (_business.OwnerId != _beneficiaryFactionId || !_operations.IsOperational)
        {
            _elapsedSeconds = 0.0f;
            return;
        }

        _elapsedSeconds += deltaSeconds;
        while (_elapsedSeconds >= _intervalSeconds)
        {
            _elapsedSeconds -= _intervalSeconds;
            _economy.AddMoney(_incomeAmount);
            IncomeCollected?.Invoke(_incomeAmount);
        }
    }

    public void ResetTiming() => _elapsedSeconds = 0.0f;
}
