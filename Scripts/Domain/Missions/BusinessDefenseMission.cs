using System;
using Omerta.Domain.Businesses;
using Omerta.Domain.Economy;

namespace Omerta.Domain.Missions;

public enum BusinessDefenseMissionStatus
{
    Waiting,
    Warning,
    Defending,
    Succeeded,
    Failed
}

public sealed class BusinessDefenseMission
{
    private readonly EconomyService _economy;

    public BusinessDefenseMission(EconomyService economy, int reward)
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

    public BusinessDefenseMissionStatus Status { get; private set; } =
        BusinessDefenseMissionStatus.Waiting;

    public bool RewardGranted { get; private set; }

    public bool IsActive => Status is BusinessDefenseMissionStatus.Warning or
        BusinessDefenseMissionStatus.Defending;

    public void Synchronize(BusinessCounterattackStatus counterattackStatus)
    {
        BusinessDefenseMissionStatus nextStatus = counterattackStatus switch
        {
            BusinessCounterattackStatus.Inactive => BusinessDefenseMissionStatus.Waiting,
            BusinessCounterattackStatus.Warning => BusinessDefenseMissionStatus.Warning,
            BusinessCounterattackStatus.InProgress => BusinessDefenseMissionStatus.Defending,
            BusinessCounterattackStatus.Defended => BusinessDefenseMissionStatus.Succeeded,
            BusinessCounterattackStatus.Sabotaged => BusinessDefenseMissionStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(counterattackStatus))
        };

        bool statusChanged = Status != nextStatus;
        Status = nextStatus;
        bool rewardChanged = false;
        if (Status == BusinessDefenseMissionStatus.Succeeded && !RewardGranted)
        {
            _economy.AddMoney(Reward);
            RewardGranted = true;
            rewardChanged = true;
        }

        if (statusChanged || rewardChanged)
        {
            Changed?.Invoke();
        }
    }

    public void ResetForLoad()
    {
        Status = BusinessDefenseMissionStatus.Waiting;
        RewardGranted = false;
        Changed?.Invoke();
    }

    public void RestoreProgress(BusinessDefenseMissionStatus status, bool rewardGranted)
    {
        if (status == BusinessDefenseMissionStatus.Succeeded && !rewardGranted)
        {
            throw new ArgumentException(
                "A successful defense must have granted its reward.");
        }

        if ((status is BusinessDefenseMissionStatus.Warning or
            BusinessDefenseMissionStatus.Defending or
            BusinessDefenseMissionStatus.Failed) && rewardGranted)
        {
            throw new ArgumentException(
                "An unfinished or failed defense cannot have granted its reward.");
        }

        Status = status;
        RewardGranted = rewardGranted;
        Changed?.Invoke();
    }
}
