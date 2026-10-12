using System;
using Omerta.Domain.Businesses;
using Omerta.Domain.Territories;
using NUnit.Framework;

namespace Omerta.Domain.Tests;

public sealed class TerritoryStateTests
{
    private const string Player = "faction.player";
    private const string Rival = "faction.rival";
    private const string Neutral = "faction.neutral";

    [Test]
    public void InitialMajorityDeterminesController()
    {
        BusinessState[] businesses = CreateBusinesses(Rival, Rival, Neutral);
        using TerritoryState territory = CreateTerritory(businesses, Neutral);

        Assert.That(territory.ControllerId, Is.EqualTo(Rival));
        Assert.That(territory.RequiredBusinessCount, Is.EqualTo(2));
        Assert.That(territory.CountControlledBy(Rival), Is.EqualTo(2));
    }

    [Test]
    public void CapturingSecondBusinessChangesControlWithoutPolling()
    {
        BusinessState[] businesses = CreateBusinesses(Rival, Rival, Rival);
        using TerritoryState territory = CreateTerritory(businesses, Rival);
        int changeCount = 0;
        string previous = string.Empty;
        string current = string.Empty;
        territory.ControlChanged += (oldController, newController) =>
        {
            changeCount++;
            previous = oldController;
            current = newController;
        };

        businesses[0].Capture(Player);
        Assert.That(territory.ControllerId, Is.EqualTo(Rival));
        Assert.That(changeCount, Is.Zero);

        businesses[1].Capture(Player);
        Assert.That(territory.ControllerId, Is.EqualTo(Player));
        Assert.That(changeCount, Is.EqualTo(1));
        Assert.That(previous, Is.EqualTo(Rival));
        Assert.That(current, Is.EqualTo(Player));
    }

    [Test]
    public void TieRetainsPreviousController()
    {
        BusinessState[] businesses = CreateBusinesses(Player, Player, Rival, Rival);
        using TerritoryState territory = CreateTerritory(businesses, Neutral);

        Assert.That(territory.ControllerId, Is.EqualTo(Neutral));
        Assert.That(territory.ReevaluateControl(), Is.False);
    }

    [Test]
    public void DuplicateBusinessIdsAreRejected()
    {
        BusinessState duplicate = new("business.same", "Same", Player);
        BusinessState[] businesses = { duplicate, duplicate };

        Assert.Throws<ArgumentException>(() => CreateTerritory(businesses, Rival));
    }

    private static TerritoryState CreateTerritory(
        BusinessState[] businesses,
        string initialController) =>
        new("territory.south-district", "South District", businesses, initialController);

    private static BusinessState[] CreateBusinesses(params string[] owners)
    {
        BusinessState[] businesses = new BusinessState[owners.Length];
        for (int index = 0; index < owners.Length; index++)
        {
            businesses[index] = new BusinessState(
                $"business.{index + 1}",
                $"Business {index + 1}",
                owners[index]);
        }

        return businesses;
    }
}
