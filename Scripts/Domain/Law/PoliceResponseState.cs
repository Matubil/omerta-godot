using System;

namespace Omerta.Domain.Law;

public enum PoliceResponseStatus
{
    Available,
    Dispatching,
    EnRoute,
    PursuingVehicle,
    Responding,
    Resolved,
    Escaped,
    Bribed,
    Fined,
    Arrested
}

public enum PoliceIncidentType
{
    None,
    Gunfire,
    VehicleTheft
}

public sealed class PoliceResponseState
{
    private readonly float _dispatchDelaySeconds;
    private readonly float _escapeDurationSeconds;

    public PoliceResponseState(float dispatchDelaySeconds, float escapeDurationSeconds)
    {
        if (dispatchDelaySeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(dispatchDelaySeconds));
        }

        if (escapeDurationSeconds <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(escapeDurationSeconds));
        }

        _dispatchDelaySeconds = dispatchDelaySeconds;
        _escapeDurationSeconds = escapeDurationSeconds;
    }

    public event Action? Changed;

    public PoliceResponseStatus Status { get; private set; } = PoliceResponseStatus.Available;

    public int WantedLevel { get; private set; }

    public int WitnessCount { get; private set; }

    public PoliceIncidentType IncidentType { get; private set; }

    public bool HasDefeatedOfficer { get; private set; }

    public float RemainingDispatchSeconds { get; private set; }

    public float RemainingEscapeSeconds { get; private set; }

    public bool IsActive => Status is PoliceResponseStatus.Dispatching or
        PoliceResponseStatus.EnRoute or PoliceResponseStatus.PursuingVehicle or
        PoliceResponseStatus.Responding;

    public void ReportGunfire(int witnessCount) =>
        ReportIncident(PoliceIncidentType.Gunfire, witnessCount);

    public void ReportVehicleTheft(int witnessCount) =>
        ReportIncident(PoliceIncidentType.VehicleTheft, witnessCount);

    private void ReportIncident(PoliceIncidentType incidentType, int witnessCount)
    {
        if (witnessCount <= 0)
        {
            return;
        }

        WitnessCount = Math.Max(WitnessCount, witnessCount);
        if (!IsActive)
        {
            IncidentType = incidentType;
            WantedLevel = 1;
            RemainingDispatchSeconds = _dispatchDelaySeconds;
            RemainingEscapeSeconds = _escapeDurationSeconds;
            HasDefeatedOfficer = false;
            Status = PoliceResponseStatus.Dispatching;
        }
        else
        {
            if (incidentType == PoliceIncidentType.VehicleTheft)
            {
                IncidentType = incidentType;
            }

            WantedLevel = Math.Min(3, WantedLevel + 1);
            if (Status is PoliceResponseStatus.Responding or
                PoliceResponseStatus.PursuingVehicle)
            {
                RemainingEscapeSeconds = _escapeDurationSeconds;
            }
        }

        Changed?.Invoke();
    }

    public bool AdvanceDispatch(float deltaSeconds)
    {
        ValidateDelta(deltaSeconds);
        if (Status != PoliceResponseStatus.Dispatching || deltaSeconds == 0.0f)
        {
            return false;
        }

        RemainingDispatchSeconds = Math.Max(0.0f, RemainingDispatchSeconds - deltaSeconds);
        if (RemainingDispatchSeconds > 0.0f)
        {
            Changed?.Invoke();
            return false;
        }

        Status = PoliceResponseStatus.EnRoute;
        Changed?.Invoke();
        return true;
    }

    public bool BeginVehiclePursuit()
    {
        if (Status is not (PoliceResponseStatus.EnRoute or PoliceResponseStatus.Responding) ||
            IncidentType != PoliceIncidentType.VehicleTheft)
        {
            return false;
        }

        Status = PoliceResponseStatus.PursuingVehicle;
        RemainingEscapeSeconds = _escapeDurationSeconds;
        Changed?.Invoke();
        return true;
    }

    public bool ContinueSearchOnFoot()
    {
        if (Status != PoliceResponseStatus.PursuingVehicle)
        {
            return false;
        }

        Status = PoliceResponseStatus.Responding;
        RemainingEscapeSeconds = _escapeDurationSeconds;
        Changed?.Invoke();
        return true;
    }

    public bool MarkArrived()
    {
        if (Status != PoliceResponseStatus.EnRoute)
        {
            return false;
        }

        Status = PoliceResponseStatus.Responding;
        RemainingEscapeSeconds = _escapeDurationSeconds;
        Changed?.Invoke();
        return true;
    }

    public bool AdvanceSearch(float deltaSeconds, bool hasVisualContact)
    {
        ValidateDelta(deltaSeconds);
        if (Status is not (PoliceResponseStatus.Responding or
            PoliceResponseStatus.PursuingVehicle) || deltaSeconds == 0.0f)
        {
            return false;
        }

        if (hasVisualContact)
        {
            RemainingEscapeSeconds = _escapeDurationSeconds;
            Changed?.Invoke();
            return false;
        }

        RemainingEscapeSeconds = Math.Max(0.0f, RemainingEscapeSeconds - deltaSeconds);
        if (RemainingEscapeSeconds > 0.0f)
        {
            Changed?.Invoke();
            return false;
        }

        WantedLevel = 0;
        Status = PoliceResponseStatus.Escaped;
        Changed?.Invoke();
        return true;
    }

    public bool ResolveByBribe()
    {
        if (Status != PoliceResponseStatus.Responding)
        {
            return false;
        }

        WantedLevel = 0;
        Status = PoliceResponseStatus.Bribed;
        Changed?.Invoke();
        return true;
    }

    public bool ResolveOfficerDefeat()
    {
        if (Status != PoliceResponseStatus.Responding)
        {
            return false;
        }

        HasDefeatedOfficer = true;
        WantedLevel = 0;
        Status = PoliceResponseStatus.Resolved;
        Changed?.Invoke();
        return true;
    }

    public bool ResolveByFine()
    {
        if (Status != PoliceResponseStatus.PursuingVehicle)
        {
            return false;
        }

        WantedLevel = 0;
        Status = PoliceResponseStatus.Fined;
        Changed?.Invoke();
        return true;
    }

    public bool ResolveByArrest()
    {
        if (Status != PoliceResponseStatus.PursuingVehicle)
        {
            return false;
        }

        WantedLevel = 0;
        Status = PoliceResponseStatus.Arrested;
        Changed?.Invoke();
        return true;
    }

    public void Reset()
    {
        Status = PoliceResponseStatus.Available;
        IncidentType = PoliceIncidentType.None;
        WantedLevel = 0;
        WitnessCount = 0;
        HasDefeatedOfficer = false;
        RemainingDispatchSeconds = 0.0f;
        RemainingEscapeSeconds = 0.0f;
        Changed?.Invoke();
    }

    private static void ValidateDelta(float deltaSeconds)
    {
        if (deltaSeconds < 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        }
    }
}
