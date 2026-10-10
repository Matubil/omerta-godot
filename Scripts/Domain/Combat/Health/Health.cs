using System;
using Omerta.Domain.Combat.Damage;

namespace Omerta.Domain.Combat.Health;

public sealed class Health : IDamageable
{
    public Health(int maximum)
    {
        if (maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        Maximum = maximum;
        Current = maximum;
    }

    public event Action<int, int>? Changed;

    public event Action<DamageInfo>? Defeated;

    public int Maximum { get; }

    public int Current { get; private set; }

    public bool IsDefeated => Current == 0;

    public bool ReceiveDamage(DamageInfo damage)
    {
        if (IsDefeated)
        {
            return false;
        }

        int previous = Current;
        Current = Math.Max(0, Current - damage.Amount);
        Changed?.Invoke(previous, Current);
        if (IsDefeated)
        {
            Defeated?.Invoke(damage);
        }

        return true;
    }

    public bool RestoreToFull() => RestoreCurrent(Maximum);

    public bool RestoreCurrent(int current)
    {
        if (current < 0 || current > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(current));
        }

        if (Current == current)
        {
            return false;
        }

        int previous = Current;
        Current = current;
        Changed?.Invoke(previous, Current);
        return true;
    }
}
