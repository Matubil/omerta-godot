using System;

namespace Omerta.Domain.Law;

public enum PoliceBribeDecision
{
    Accepted,
    NoActiveResponse,
    EscalatedAlert,
    MultipleWitnesses,
    OfficerDefeated,
    InsufficientFunds
}

public static class PoliceBribeRules
{
    public static PoliceBribeDecision Evaluate(
        bool hasActiveResponse,
        int wantedLevel,
        int witnessCount,
        bool officerWasDefeated,
        int availableFunds,
        int cost)
    {
        if (cost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cost));
        }

        if (!hasActiveResponse)
        {
            return PoliceBribeDecision.NoActiveResponse;
        }

        if (officerWasDefeated)
        {
            return PoliceBribeDecision.OfficerDefeated;
        }

        if (wantedLevel != 1)
        {
            return PoliceBribeDecision.EscalatedAlert;
        }

        if (witnessCount != 1)
        {
            return PoliceBribeDecision.MultipleWitnesses;
        }

        return availableFunds >= cost
            ? PoliceBribeDecision.Accepted
            : PoliceBribeDecision.InsufficientFunds;
    }
}
