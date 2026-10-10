using System;

namespace Omerta.Domain.Businesses;

public sealed class ForcedTakeoverSession
{
    private readonly BusinessState _business;
    private readonly string _attackerId;
    private readonly bool[] _defeatedDefenders;

    public ForcedTakeoverSession(BusinessState business, string attackerId, int defenderCount)
    {
        _business = business ?? throw new ArgumentNullException(nameof(business));
        if (string.IsNullOrWhiteSpace(attackerId))
        {
            throw new ArgumentException("An attacker faction is required.", nameof(attackerId));
        }

        if (defenderCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(defenderCount));
        }

        _attackerId = attackerId;
        _defeatedDefenders = new bool[defenderCount];
    }

    public event Action? ReadinessChanged;

    public int DefenderCount => _defeatedDefenders.Length;

    public int DefeatedDefenderCount { get; private set; }

    public bool CanDemandSurrender =>
        _business.OwnerId != _attackerId && DefeatedDefenderCount == DefenderCount;

    public bool SetDefenderDefeated(int defenderIndex, bool defeated)
    {
        if (defenderIndex < 0 || defenderIndex >= _defeatedDefenders.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(defenderIndex));
        }

        if (_defeatedDefenders[defenderIndex] == defeated)
        {
            return false;
        }

        _defeatedDefenders[defenderIndex] = defeated;
        DefeatedDefenderCount += defeated ? 1 : -1;
        ReadinessChanged?.Invoke();
        return true;
    }

    public bool DemandSurrender() => CanDemandSurrender && _business.Capture(_attackerId);
}
