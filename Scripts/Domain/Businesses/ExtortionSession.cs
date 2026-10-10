using System;

namespace Omerta.Domain.Businesses;

public enum ExtortionPressureSource
{
    None,
    DirectThreat,
    EnvironmentalDamage
}

public enum MerchantExtortionReaction
{
    Calm,
    Defiant,
    AlarmedByDamage,
    Frightened,
    NearSurrender,
    Compliant
}

public enum MerchantTemperament
{
    Balanced,
    Cautious,
    Proud,
    Stoic
}

public sealed class ExtortionSession
{
    private readonly BusinessState _business;
    private readonly string _capturingFactionId;

    public ExtortionSession(
        BusinessState business,
        string capturingFactionId,
        int resistance,
        MerchantTemperament temperament = MerchantTemperament.Balanced)
    {
        _business = business ?? throw new ArgumentNullException(nameof(business));
        if (string.IsNullOrWhiteSpace(capturingFactionId))
        {
            throw new ArgumentException("A capturing faction is required.", nameof(capturingFactionId));
        }

        if (resistance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resistance));
        }

        _capturingFactionId = capturingFactionId;
        Resistance = resistance;
        Temperament = temperament;
        int tolerancePercent = GetEnvironmentalDamageTolerancePercent(temperament);
        EnvironmentalDamageTolerance = checked((resistance * tolerancePercent + 99) / 100);
        IsComplete = business.OwnerId == capturingFactionId;
        Pressure = IsComplete ? resistance : 0;
    }

    public event Action<int, int>? PressureChanged;

    public event Action? Completed;

    public int Pressure { get; private set; }

    public int Resistance { get; }

    public MerchantTemperament Temperament { get; }

    public int EnvironmentalPressureApplied { get; private set; }

    public int EnvironmentalDamageTolerance { get; }

    public bool HasExcessiveViolence { get; private set; }

    public bool IsComplete { get; private set; }

    public ExtortionPressureSource LastPressureSource { get; private set; }

    public MerchantExtortionReaction Reaction
    {
        get
        {
            if (IsComplete)
            {
                return MerchantExtortionReaction.Compliant;
            }

            float pressureRatio = (float)Pressure / Resistance;
            if (pressureRatio >= GetNearSurrenderThreshold(Temperament))
            {
                return MerchantExtortionReaction.NearSurrender;
            }

            if (pressureRatio >= GetFrightenedThreshold(Temperament))
            {
                return MerchantExtortionReaction.Frightened;
            }

            if (Pressure == 0)
            {
                return MerchantExtortionReaction.Calm;
            }

            return LastPressureSource == ExtortionPressureSource.EnvironmentalDamage
                ? MerchantExtortionReaction.AlarmedByDamage
                : MerchantExtortionReaction.Defiant;
        }
    }

    public bool ApplyPressure(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (IsComplete)
        {
            return false;
        }

        LastPressureSource = ExtortionPressureSource.DirectThreat;
        Pressure = Math.Min(Resistance, checked(Pressure + amount));
        bool reachedResistance = Pressure >= Resistance;
        if (reachedResistance)
        {
            _business.Capture(_capturingFactionId);
            IsComplete = true;
        }

        PressureChanged?.Invoke(Pressure, Resistance);
        if (reachedResistance)
        {
            Completed?.Invoke();
        }

        return reachedResistance;
    }

    public int ApplyEnvironmentalPressure(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (IsComplete)
        {
            return 0;
        }

        int previous = Pressure;
        int maximumEnvironmentalPressure = Math.Max(0, Resistance - 1);
        Pressure = Math.Min(maximumEnvironmentalPressure, checked(Pressure + amount));
        int applied = Pressure - previous;
        if (applied > 0)
        {
            EnvironmentalPressureApplied = checked(EnvironmentalPressureApplied + applied);
            HasExcessiveViolence = EnvironmentalPressureApplied > EnvironmentalDamageTolerance;
            LastPressureSource = ExtortionPressureSource.EnvironmentalDamage;
            PressureChanged?.Invoke(Pressure, Resistance);
        }

        return applied;
    }

    public void SynchronizeWithBusinessOwner()
    {
        IsComplete = _business.OwnerId == _capturingFactionId;
        Pressure = IsComplete ? Resistance : 0;
        EnvironmentalPressureApplied = 0;
        HasExcessiveViolence = false;
        LastPressureSource = ExtortionPressureSource.None;
        PressureChanged?.Invoke(Pressure, Resistance);
    }

    private static float GetFrightenedThreshold(MerchantTemperament temperament) =>
        temperament switch
        {
            MerchantTemperament.Cautious => 0.35f,
            MerchantTemperament.Proud => 0.55f,
            MerchantTemperament.Stoic => 0.65f,
            _ => 0.5f
        };

    private static float GetNearSurrenderThreshold(MerchantTemperament temperament) =>
        temperament switch
        {
            MerchantTemperament.Cautious => 0.65f,
            MerchantTemperament.Proud => 0.8f,
            MerchantTemperament.Stoic => 0.9f,
            _ => 0.75f
        };

    private static int GetEnvironmentalDamageTolerancePercent(MerchantTemperament temperament) =>
        temperament switch
        {
            MerchantTemperament.Cautious => 15,
            MerchantTemperament.Proud => 20,
            MerchantTemperament.Stoic => 35,
            _ => 30
        };
}
