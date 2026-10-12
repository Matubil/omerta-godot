using System;
using Omerta.Domain.Economy;

namespace Omerta.Domain.Missions;

public enum BusinessSabotageMissionStatus
{
    Locked,
    Available,
    ReachProperty,
    SabotageTarget,
    Completed,
    Failed
}

public sealed class BusinessSabotageMission
{
    private readonly EconomyService _economy;

    public BusinessSabotageMission(EconomyService economy, int reward)
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

    public BusinessSabotageMissionStatus Status { get; private set; } =
        BusinessSabotageMissionStatus.Locked;

    public bool RewardGranted { get; private set; }

    public bool CanStart => Status is BusinessSabotageMissionStatus.Available or
        BusinessSabotageMissionStatus.Failed;

    public bool IsActive => Status is BusinessSabotageMissionStatus.ReachProperty or
        BusinessSabotageMissionStatus.SabotageTarget;

    public bool Unlock()
    {
        if (Status != BusinessSabotageMissionStatus.Locked)
        {
            return false;
        }

        Status = BusinessSabotageMissionStatus.Available;
        Changed?.Invoke();
        return true;
    }

    public bool Start()
    {
        if (!CanStart)
        {
            return false;
        }

        Status = BusinessSabotageMissionStatus.ReachProperty;
        Changed?.Invoke();
        return true;
    }

    public bool MarkPropertyReached()
    {
        if (Status != BusinessSabotageMissionStatus.ReachProperty)
        {
            return false;
        }

        Status = BusinessSabotageMissionStatus.SabotageTarget;
        Changed?.Invoke();
        return true;
    }

    public bool MarkSabotaged()
    {
        if (!IsActive)
        {
            return false;
        }

        Status = BusinessSabotageMissionStatus.Completed;
        if (!RewardGranted)
        {
            _economy.AddMoney(Reward);
            RewardGranted = true;
        }

        Changed?.Invoke();
        return true;
    }

    public bool Fail()
    {
        if (!IsActive)
        {
            return false;
        }

        Status = BusinessSabotageMissionStatus.Failed;
        Changed?.Invoke();
        return true;
    }

    public void ResetForLoad()
    {
        Status = BusinessSabotageMissionStatus.Locked;
        RewardGranted = false;
        Changed?.Invoke();
    }

    public void RestoreProgress(BusinessSabotageMissionStatus status, bool rewardGranted)
    {
        if ((status == BusinessSabotageMissionStatus.Completed) != rewardGranted)
        {
            throw new ArgumentException("Sabotage completion and reward state must be consistent.");
        }

        Status = status;
        RewardGranted = rewardGranted;
        Changed?.Invoke();
    }

    public void SynchronizeAfterLoad(bool prerequisiteCompleted, bool targetAlreadySabotaged)
    {
        if (!prerequisiteCompleted)
        {
            RestoreProgress(BusinessSabotageMissionStatus.Locked, false);
        }
        else if (targetAlreadySabotaged)
        {
            RestoreProgress(BusinessSabotageMissionStatus.Completed, true);
        }
        else
        {
            RestoreProgress(BusinessSabotageMissionStatus.Available, false);
        }
    }
}
