using System;

namespace Omerta.Domain.Law;

public enum PoliceInterceptionOutcome
{
    Fine,
    Arrest
}

public sealed class VehicleInterceptionState
{
    private readonly float _requiredStoppedSeconds;
    private readonly float _maximumStoppedSpeed;

    public VehicleInterceptionState(float requiredStoppedSeconds, float maximumStoppedSpeed)
    {
        if (requiredStoppedSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredStoppedSeconds));
        }

        if (maximumStoppedSpeed < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStoppedSpeed));
        }

        _requiredStoppedSeconds = requiredStoppedSeconds;
        _maximumStoppedSpeed = maximumStoppedSpeed;
        RemainingSeconds = requiredStoppedSeconds;
    }

    public float RemainingSeconds { get; private set; }

    public bool IsComplete { get; private set; }

    public bool Advance(
        float deltaSeconds,
        bool policeWithinInterceptionRange,
        float vehicleSpeed,
        bool vehicleDisabled)
    {
        if (deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }

        if (IsComplete || deltaSeconds == 0.0f)
        {
            return IsComplete;
        }

        bool stopped = vehicleDisabled || MathF.Abs(vehicleSpeed) <= _maximumStoppedSpeed;
        if (!policeWithinInterceptionRange || !stopped)
        {
            RemainingSeconds = _requiredStoppedSeconds;
            return false;
        }

        RemainingSeconds = Math.Max(0.0f, RemainingSeconds - deltaSeconds);
        IsComplete = RemainingSeconds == 0.0f;
        return IsComplete;
    }

    public void Reset()
    {
        RemainingSeconds = _requiredStoppedSeconds;
        IsComplete = false;
    }
}

public static class PoliceInterceptionRules
{
    public static PoliceInterceptionOutcome Evaluate(
        int wantedLevel,
        int availableFunds,
        int fineCost)
    {
        if (wantedLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wantedLevel));
        }

        if (availableFunds < 0 || fineCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(availableFunds));
        }

        return wantedLevel == 1 && availableFunds >= fineCost
            ? PoliceInterceptionOutcome.Fine
            : PoliceInterceptionOutcome.Arrest;
    }
}
