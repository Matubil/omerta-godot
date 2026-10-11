using System;
using Omerta.Domain.Economy;

namespace Omerta.Domain.Missions;

public enum PropertyTakeoverMissionStatus
{
    Available,
    ReachProperty,
    TakeControl,
    ResolvePoliceResponse,
    Completed,
    Failed
}

public sealed class PropertyTakeoverMission
{
    private readonly EconomyService _economy;

    public PropertyTakeoverMission(EconomyService economy, int reward)
    {
        _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        if (reward <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reward));
        }

        Reward = reward;
    }

    public event Action? Changed;

    public int Reward { get; }

    public PropertyTakeoverMissionStatus Status { get; private set; } =
        PropertyTakeoverMissionStatus.Available;

    public bool RewardGranted { get; private set; }

    public bool CanStart => Status is PropertyTakeoverMissionStatus.Available or
        PropertyTakeoverMissionStatus.Failed;

    public bool IsActive => Status is PropertyTakeoverMissionStatus.ReachProperty or
        PropertyTakeoverMissionStatus.TakeControl or
        PropertyTakeoverMissionStatus.ResolvePoliceResponse;

    public bool Start()
    {
        if (!CanStart)
        {
            return false;
        }

        Status = PropertyTakeoverMissionStatus.ReachProperty;
        Changed?.Invoke();
        return true;
    }

    public bool MarkPropertyReached()
    {
        if (Status != PropertyTakeoverMissionStatus.ReachProperty)
        {
            return false;
        }

        Status = PropertyTakeoverMissionStatus.TakeControl;
        Changed?.Invoke();
        return true;
    }

    public bool MarkPropertyCaptured(bool hasActivePoliceResponse)
    {
        if (!IsActive)
        {
            return false;
        }

        if (hasActivePoliceResponse)
        {
            Status = PropertyTakeoverMissionStatus.ResolvePoliceResponse;
            Changed?.Invoke();
            return true;
        }

        Complete();
        return true;
    }

    public bool MarkPoliceResponseResolved()
    {
        if (Status != PropertyTakeoverMissionStatus.ResolvePoliceResponse)
        {
            return false;
        }

        Complete();
        return true;
    }

    public bool Fail()
    {
        if (!IsActive)
        {
            return false;
        }

        Status = PropertyTakeoverMissionStatus.Failed;
        Changed?.Invoke();
        return true;
    }

    public void RestoreProgress(PropertyTakeoverMissionStatus status, bool rewardGranted)
    {
        if ((status == PropertyTakeoverMissionStatus.Completed) != rewardGranted)
        {
            throw new ArgumentException("Mission completion and reward state must be consistent.");
        }

        Status = status;
        RewardGranted = rewardGranted;
        Changed?.Invoke();
    }

    public void SynchronizeCapturedAfterLoad(bool propertyIsOwnedByPlayer)
    {
        if (!propertyIsOwnedByPlayer)
        {
            RestoreProgress(PropertyTakeoverMissionStatus.Available, false);
            return;
        }

        RestoreProgress(PropertyTakeoverMissionStatus.Completed, true);
    }

    private void Complete()
    {
        Status = PropertyTakeoverMissionStatus.Completed;
        if (!RewardGranted)
        {
            _economy.AddMoney(Reward);
            RewardGranted = true;
        }

        Changed?.Invoke();
    }
}
