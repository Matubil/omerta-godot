using System;

namespace Omerta.Domain.Businesses;

public sealed class BusinessState
{
    public BusinessState(string id, string displayName, string initialOwnerId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A business id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(initialOwnerId))
        {
            throw new ArgumentException("An owner id is required.", nameof(initialOwnerId));
        }

        Id = id;
        DisplayName = displayName;
        OwnerId = initialOwnerId;
    }

    public event Action<string, string>? OwnerChanged;

    public string Id { get; }

    public string DisplayName { get; }

    public string OwnerId { get; private set; }

    public bool Capture(string newOwnerId)
    {
        if (string.IsNullOrWhiteSpace(newOwnerId))
        {
            throw new ArgumentException("An owner id is required.", nameof(newOwnerId));
        }

        if (newOwnerId == OwnerId)
        {
            return false;
        }

        string previousOwner = OwnerId;
        OwnerId = newOwnerId;
        OwnerChanged?.Invoke(previousOwner, newOwnerId);
        return true;
    }
}
