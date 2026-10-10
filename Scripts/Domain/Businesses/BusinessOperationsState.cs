using System;

namespace Omerta.Domain.Businesses;

public sealed class BusinessOperationsState
{
    public BusinessOperationsState(string businessId)
    {
        if (string.IsNullOrWhiteSpace(businessId))
        {
            throw new ArgumentException("A business id is required.", nameof(businessId));
        }

        BusinessId = businessId;
    }

    public event Action? Changed;

    public string BusinessId { get; }

    public bool IsOperatorAvailable { get; private set; } = true;

    public bool IsCriticalAssetOperational { get; private set; } = true;

    public bool IsOperational => IsOperatorAvailable && IsCriticalAssetOperational;

    public bool SetOperatorAvailable(bool available)
    {
        if (IsOperatorAvailable == available)
        {
            return false;
        }

        IsOperatorAvailable = available;
        Changed?.Invoke();
        return true;
    }

    public bool SabotageCriticalAsset()
    {
        if (!IsCriticalAssetOperational)
        {
            return false;
        }

        IsCriticalAssetOperational = false;
        Changed?.Invoke();
        return true;
    }

    public bool RepairCriticalAsset()
    {
        if (IsCriticalAssetOperational)
        {
            return false;
        }

        IsCriticalAssetOperational = true;
        Changed?.Invoke();
        return true;
    }

    public void Restore(bool operatorAvailable, bool criticalAssetOperational)
    {
        bool changed = IsOperatorAvailable != operatorAvailable ||
            IsCriticalAssetOperational != criticalAssetOperational;
        IsOperatorAvailable = operatorAvailable;
        IsCriticalAssetOperational = criticalAssetOperational;
        if (changed)
        {
            Changed?.Invoke();
        }
    }
}
