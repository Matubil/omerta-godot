using System;

namespace Omerta.Domain.Vehicles;

public sealed class VehicleCondition
{
    public VehicleCondition(
        int maximum = 100,
        float minimumDamagingImpactSpeed = 4.0f,
        float damagePerImpactSpeed = 9.0f)
    {
        if (maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        if (minimumDamagingImpactSpeed < 0.0f || damagePerImpactSpeed <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumDamagingImpactSpeed));
        }

        Maximum = maximum;
        Current = maximum;
        MinimumDamagingImpactSpeed = minimumDamagingImpactSpeed;
        DamagePerImpactSpeed = damagePerImpactSpeed;
    }

    public event Action<int, int>? Changed;

    public int Maximum { get; }

    public int Current { get; private set; }

    public float MinimumDamagingImpactSpeed { get; }

    public float DamagePerImpactSpeed { get; }

    public bool IsDisabled => Current == 0;

    public float Ratio => (float)Current / Maximum;

    public int ApplyImpact(float impactSpeed)
    {
        if (!float.IsFinite(impactSpeed) || impactSpeed < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(impactSpeed));
        }

        if (impactSpeed <= MinimumDamagingImpactSpeed || IsDisabled)
        {
            return 0;
        }

        int damage = Math.Max(
            1,
            (int)MathF.Ceiling(
                (impactSpeed - MinimumDamagingImpactSpeed) * DamagePerImpactSpeed));
        return ApplyDamage(damage);
    }

    public int ApplyDamage(int damage)
    {
        if (damage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }

        if (IsDisabled)
        {
            return 0;
        }

        int previous = Current;
        Current = Math.Max(0, Current - damage);
        Changed?.Invoke(previous, Current);
        return previous - Current;
    }

    public bool RepairToFull() => Restore(Maximum);

    public bool Restore(int current)
    {
        if (current < 0 || current > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(current));
        }

        if (current == Current)
        {
            return false;
        }

        int previous = Current;
        Current = current;
        Changed?.Invoke(previous, Current);
        return true;
    }
}
