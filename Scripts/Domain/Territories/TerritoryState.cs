using System;
using System.Collections.Generic;
using Omerta.Domain.Businesses;

namespace Omerta.Domain.Territories;

public sealed class TerritoryState : IDisposable
{
    private readonly BusinessState[] _businesses;

    public TerritoryState(
        string id,
        string displayName,
        IReadOnlyList<BusinessState> businesses,
        string initialControllerId)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A territory id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A territory display name is required.", nameof(displayName));
        }

        if (businesses is null || businesses.Count == 0)
        {
            throw new ArgumentException("A territory requires at least one business.", nameof(businesses));
        }

        if (string.IsNullOrWhiteSpace(initialControllerId))
        {
            throw new ArgumentException("An initial controller is required.", nameof(initialControllerId));
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        _businesses = new BusinessState[businesses.Count];
        for (int index = 0; index < businesses.Count; index++)
        {
            BusinessState business = businesses[index] ??
                throw new ArgumentException("A territory business cannot be null.", nameof(businesses));
            if (!seen.Add(business.Id))
            {
                throw new ArgumentException(
                    $"Business '{business.Id}' is duplicated in territory '{id}'.",
                    nameof(businesses));
            }

            _businesses[index] = business;
        }

        foreach (BusinessState business in _businesses)
        {
            business.OwnerChanged += OnBusinessOwnerChanged;
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        ControllerId = initialControllerId.Trim();
        ReevaluateControl();
    }

    public event Action<string, string>? ControlChanged;

    public string Id { get; }

    public string DisplayName { get; }

    public string ControllerId { get; private set; }

    public IReadOnlyList<BusinessState> Businesses => _businesses;

    public int RequiredBusinessCount => _businesses.Length / 2 + 1;

    public int CountControlledBy(string factionId)
    {
        int count = 0;
        foreach (BusinessState business in _businesses)
        {
            if (business.OwnerId == factionId)
            {
                count++;
            }
        }

        return count;
    }

    public bool ReevaluateControl()
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (BusinessState business in _businesses)
        {
            counts.TryGetValue(business.OwnerId, out int current);
            counts[business.OwnerId] = current + 1;
        }

        string? majorityController = null;
        foreach ((string factionId, int count) in counts)
        {
            if (count >= RequiredBusinessCount)
            {
                majorityController = factionId;
                break;
            }
        }

        if (majorityController is null || majorityController == ControllerId)
        {
            return false;
        }

        string previous = ControllerId;
        ControllerId = majorityController;
        ControlChanged?.Invoke(previous, majorityController);
        return true;
    }

    public void Dispose()
    {
        foreach (BusinessState business in _businesses)
        {
            business.OwnerChanged -= OnBusinessOwnerChanged;
        }
    }

    private void OnBusinessOwnerChanged(string previous, string current) => ReevaluateControl();
}
