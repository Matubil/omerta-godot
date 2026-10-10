using System;

namespace Omerta.Domain.Combat.Damage;

public readonly record struct DamageInfo
{
    public DamageInfo(int amount, string? sourceId = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage must be positive.");
        }

        Amount = amount;
        SourceId = sourceId;
    }

    public int Amount { get; }

    public string? SourceId { get; }
}
