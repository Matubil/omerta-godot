using System;

namespace Omerta.Domain.Businesses;

public enum BusinessCounterattackStatus
{
    Inactive,
    Warning,
    InProgress,
    Defended,
    Sabotaged
}

public sealed class BusinessCounterattack : IDisposable
{
    private readonly BusinessState _business;
    private readonly BusinessOperationsState _operations;
    private readonly string _defendingFactionId;
    private readonly bool[] _attackerAlive;
    private readonly bool[] _attackerAtObjective;
    private readonly float _warningDurationSeconds;
    private readonly float _sabotageDurationSeconds;

    public BusinessCounterattack(
        BusinessState business,
        BusinessOperationsState operations,
        string defendingFactionId,
        float warningDurationSeconds,
        float sabotageDurationSeconds,
        int attackerCount)
    {
        _business = business ?? throw new ArgumentNullException(nameof(business));
        _operations = operations ?? throw new ArgumentNullException(nameof(operations));
        if (_operations.BusinessId != _business.Id)
        {
            throw new ArgumentException("Operational state must belong to the business.");
        }

        if (string.IsNullOrWhiteSpace(defendingFactionId))
        {
            throw new ArgumentException("A defending faction is required.", nameof(defendingFactionId));
        }

        if (warningDurationSeconds <= 0.0f || sabotageDurationSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(warningDurationSeconds));
        }

        if (attackerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attackerCount));
        }

        _defendingFactionId = defendingFactionId;
        _warningDurationSeconds = warningDurationSeconds;
        _sabotageDurationSeconds = sabotageDurationSeconds;
        _attackerAlive = new bool[attackerCount];
        _attackerAtObjective = new bool[attackerCount];
        RemainingWarningSeconds = warningDurationSeconds;
        RemainingSabotageSeconds = sabotageDurationSeconds;
        _business.OwnerChanged += OnOwnerChanged;
        _operations.Changed += OnOperationsChanged;
    }

    public event Action? Changed;

    public event Action? AttackersRequested;

    public BusinessCounterattackStatus Status { get; private set; }

    public float RemainingWarningSeconds { get; private set; }

    public float RemainingSabotageSeconds { get; private set; }

    public int LivingAttackerCount
    {
        get
        {
            int count = 0;
            foreach (bool alive in _attackerAlive)
            {
                if (alive)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public bool IsSabotageInProgress
    {
        get
        {
            for (int index = 0; index < _attackerAlive.Length; index++)
            {
                if (_attackerAlive[index] && _attackerAtObjective[index])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool Advance(float deltaSeconds)
    {
        if (deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (Status == BusinessCounterattackStatus.Warning)
        {
            RemainingWarningSeconds = Math.Max(0.0f, RemainingWarningSeconds - deltaSeconds);
            if (RemainingWarningSeconds > 0.0f)
            {
                Changed?.Invoke();
                return false;
            }

            BeginAttack();
            return true;
        }

        if (Status != BusinessCounterattackStatus.InProgress)
        {
            return false;
        }

        if (!IsSabotageInProgress)
        {
            RemainingSabotageSeconds = _sabotageDurationSeconds;
            return false;
        }

        RemainingSabotageSeconds = Math.Max(0.0f, RemainingSabotageSeconds - deltaSeconds);
        if (RemainingSabotageSeconds > 0.0f)
        {
            Changed?.Invoke();
            return false;
        }

        CompleteSabotage();
        return true;
    }

    public bool SetAttackerAtObjective(int index, bool atObjective)
    {
        ValidateIndex(index);
        if (Status != BusinessCounterattackStatus.InProgress || !_attackerAlive[index] ||
            _attackerAtObjective[index] == atObjective)
        {
            return false;
        }

        _attackerAtObjective[index] = atObjective;
        if (!atObjective)
        {
            RemainingSabotageSeconds = _sabotageDurationSeconds;
        }

        Changed?.Invoke();
        return true;
    }

    public bool SetAttackerDefeated(int index)
    {
        ValidateIndex(index);
        if (Status != BusinessCounterattackStatus.InProgress || !_attackerAlive[index])
        {
            return false;
        }

        _attackerAlive[index] = false;
        _attackerAtObjective[index] = false;
        if (LivingAttackerCount == 0)
        {
            Status = BusinessCounterattackStatus.Defended;
        }

        Changed?.Invoke();
        return true;
    }

    public bool ForceSabotage()
    {
        if (Status != BusinessCounterattackStatus.InProgress)
        {
            return false;
        }

        CompleteSabotage();
        return true;
    }

    public void AcknowledgeCurrentOwnership()
    {
        Status = BusinessCounterattackStatus.Inactive;
        RemainingWarningSeconds = _warningDurationSeconds;
        RemainingSabotageSeconds = _sabotageDurationSeconds;
        Array.Clear(_attackerAlive);
        Array.Clear(_attackerAtObjective);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _business.OwnerChanged -= OnOwnerChanged;
        _operations.Changed -= OnOperationsChanged;
    }

    private void OnOwnerChanged(string previous, string current)
    {
        if (current == _defendingFactionId && _operations.IsOperational)
        {
            Schedule();
        }
        else
        {
            AcknowledgeCurrentOwnership();
        }
    }

    private void OnOperationsChanged()
    {
        if (!_operations.IsOperational)
        {
            if (Status == BusinessCounterattackStatus.Warning)
            {
                AcknowledgeCurrentOwnership();
            }

            return;
        }

        if (Status is BusinessCounterattackStatus.Inactive or
            BusinessCounterattackStatus.Sabotaged &&
            _business.OwnerId == _defendingFactionId)
        {
            Schedule();
        }
    }

    private void Schedule()
    {
        Status = BusinessCounterattackStatus.Warning;
        RemainingWarningSeconds = _warningDurationSeconds;
        RemainingSabotageSeconds = _sabotageDurationSeconds;
        Array.Clear(_attackerAlive);
        Array.Clear(_attackerAtObjective);
        Changed?.Invoke();
    }

    private void BeginAttack()
    {
        Status = BusinessCounterattackStatus.InProgress;
        Array.Fill(_attackerAlive, true);
        Array.Clear(_attackerAtObjective);
        Changed?.Invoke();
        AttackersRequested?.Invoke();
    }

    private void CompleteSabotage()
    {
        _operations.SabotageCriticalAsset();
        Status = BusinessCounterattackStatus.Sabotaged;
        RemainingSabotageSeconds = 0.0f;
        Changed?.Invoke();
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= _attackerAlive.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
