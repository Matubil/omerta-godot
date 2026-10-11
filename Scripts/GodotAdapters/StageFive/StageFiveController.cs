using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Omerta.Domain.Businesses;
using Omerta.Domain.Combat.Damage;
using Omerta.Domain.Law;
using Omerta.Domain.Missions;
using Omerta.Domain.Persistence;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.Law;
using Omerta.GodotAdapters.Squad;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.StageFive;

public sealed partial class StageFiveController : Node3D
{
    public const string ElFaroMissionId = "mission.el-faro-takeover";

    [Export]
    public int MissionReward { get; set; } = 150;

    [Export]
    public int PoliceBribeCost { get; set; } = 60;

    [Export]
    public float MissionArrivalRadius { get; set; } = 7.0f;

    [Export]
    public float WitnessHearingRadius { get; set; } = 18.0f;

    [Export]
    public float PoliceDispatchDelay { get; set; } = 3.0f;

    [Export]
    public float PoliceEscapeDuration { get; set; } = 8.0f;

    [Export]
    public float PoliceVehicleSpeed { get; set; } = 8.0f;

    [Export]
    public float CounterattackWarningDuration { get; set; } = 12.0f;

    [Export]
    public float CounterattackSabotageDuration { get; set; } = 6.0f;

    [Export]
    public Vector3 MissionArrivalPoint { get; set; } = new(0.0f, 0.0f, -3.2f);

    [Export]
    public Vector3 PoliceVehicleSpawn { get; set; } = new(18.0f, 0.45f, 18.0f);

    [Export]
    public Vector3 PoliceVehicleDestination { get; set; } = new(3.5f, 0.45f, 4.5f);

    [Export]
    public Vector3 CounterattackerSpawn { get; set; } = new(-17.0f, 0.05f, 15.0f);

    [Export]
    public Vector3 CounterattackObjective { get; set; } = new(0.0f, 0.05f, -3.4f);

    private PropertyTakeoverMission? _mission;
    private PoliceResponseState? _police;
    private BusinessCounterattack? _counterattack;
    private WorkshopController? _workshop;
    private EconomyController? _economy;
    private Node3D? _player;
    private Combatant? _playerCombatant;
    private HitscanWeaponController? _playerWeapon;
    private WitnessController? _witness;
    private PoliceOfficerController? _policeOfficer;
    private CounterattackerController? _counterattacker;
    private Node3D? _policeVehicle;
    private AllyReinforcementController? _reinforcements;
    private SquadCommander? _squad;
    private HudController? _hud;
    private Vector3 _lastKnownPlayerPosition;

    public bool CanStartMission => _mission?.CanStart == true;

    public string MissionInteractionPrompt => _mission?.Status == PropertyTakeoverMissionStatus.Failed
        ? "Reintentar encargo: conquistar Taller El Faro"
        : "Aceptar encargo: conquistar Taller El Faro";

    public bool CanAttemptPoliceBribe => _police?.Status == PoliceResponseStatus.Responding;

    public string PoliceBribePrompt => $"Sobornar al agente (${PoliceBribeCost})";

    public override void _Ready()
    {
        AddToGroup("stage_five");
        _workshop = GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _economy = GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
        if (_workshop is null || _economy is null || _player is null)
        {
            GD.PushError("Stage 5 requires the workshop, economy and player nodes.");
            SetProcess(false);
            return;
        }

        _playerCombatant = _player.GetNode<Combatant>("Combatant");
        _playerWeapon = _player.GetNode<HitscanWeaponController>("WeaponController");
        _witness = GetNode<WitnessController>("Witness");
        _policeVehicle = GetNode<Node3D>("PoliceVehicle");
        _policeOfficer = GetNode<PoliceOfficerController>("PoliceOfficer");
        _counterattacker = GetNode<CounterattackerController>("Counterattacker");
        _reinforcements = GetNode<AllyReinforcementController>("AllyReinforcement");
        _squad = GetNode<SquadCommander>("SquadCommander");

        _mission = new PropertyTakeoverMission(_economy.Economy, MissionReward);
        _police = new PoliceResponseState(PoliceDispatchDelay, PoliceEscapeDuration);
        _counterattack = new BusinessCounterattack(
            _workshop.Business,
            _workshop.Operations,
            WorkshopController.PlayerFactionId,
            CounterattackWarningDuration,
            CounterattackSabotageDuration,
            1);

        _mission.Changed += OnMissionChanged;
        _police.Changed += OnPoliceChanged;
        _counterattack.Changed += OnCounterattackChanged;
        _counterattack.AttackersRequested += OnAttackersRequested;
        _workshop.Business.OwnerChanged += OnWorkshopOwnerChanged;
        _playerWeapon.ShotResolved += OnPlayerShot;
        _playerCombatant.Defeated += OnPlayerDefeated;
        _policeOfficer.Defeated += OnPoliceOfficerDefeated;
        _counterattacker.ObjectivePresenceChanged += OnCounterattackerObjectivePresenceChanged;
        _counterattacker.Defeated += OnCounterattackerDefeated;

        ResetTransientSystems();
        RefreshHud();
        GD.Print("Etapa 5 activa: misión, crew, refuerzos, policía y represalia.");
    }

    public override void _ExitTree()
    {
        if (_mission is not null)
        {
            _mission.Changed -= OnMissionChanged;
        }

        if (_police is not null)
        {
            _police.Changed -= OnPoliceChanged;
        }

        if (_counterattack is not null)
        {
            _counterattack.Changed -= OnCounterattackChanged;
            _counterattack.AttackersRequested -= OnAttackersRequested;
            _counterattack.Dispose();
        }

        if (_workshop?.Business is not null)
        {
            _workshop.Business.OwnerChanged -= OnWorkshopOwnerChanged;
        }

        if (_playerWeapon is not null)
        {
            _playerWeapon.ShotResolved -= OnPlayerShot;
        }

        if (_playerCombatant is not null)
        {
            _playerCombatant.Defeated -= OnPlayerDefeated;
        }

        if (_policeOfficer is not null)
        {
            _policeOfficer.Defeated -= OnPoliceOfficerDefeated;
        }

        if (_counterattacker is not null)
        {
            _counterattacker.ObjectivePresenceChanged -= OnCounterattackerObjectivePresenceChanged;
            _counterattacker.Defeated -= OnCounterattackerDefeated;
        }
    }

    public override void _Process(double delta)
    {
        ResolveHud();
        if (_mission?.Status == PropertyTakeoverMissionStatus.ReachProperty && _player is not null &&
            _player.GlobalPosition.DistanceSquaredTo(MissionArrivalPoint) <=
            MissionArrivalRadius * MissionArrivalRadius)
        {
            _mission.MarkPropertyReached();
        }

        AdvancePolice((float)delta);
        _counterattack?.Advance((float)delta);
        RefreshHud();
    }

    public void StartMission()
    {
        if (_mission?.Start() != true)
        {
            return;
        }

        _hud?.ShowStatus("Don Vittorio: «El Faro debe responder a la familia.»", 5.0f);
    }

    public void TryBribePolice()
    {
        if (_police is null || _economy is null)
        {
            return;
        }

        PoliceBribeDecision decision = PoliceBribeRules.Evaluate(
            _police.IsActive,
            _police.WantedLevel,
            _police.WitnessCount,
            _police.HasDefeatedOfficer,
            _economy.Economy.Balance,
            PoliceBribeCost);
        if (decision == PoliceBribeDecision.Accepted &&
            _economy.Economy.TrySpendMoney(PoliceBribeCost) &&
            _police.ResolveByBribe())
        {
            _hud?.ShowStatus("El agente acepta el sobre. La búsqueda termina.", 5.0f);
            return;
        }

        string message = decision switch
        {
            PoliceBribeDecision.InsufficientFunds => "No alcanza el dinero para el soborno.",
            PoliceBribeDecision.EscalatedAlert => "La alerta es demasiado alta para arreglarla con un sobre.",
            PoliceBribeDecision.MultipleWitnesses => "Hay demasiados testigos para comprar silencio.",
            PoliceBribeDecision.OfficerDefeated => "Después de atacar a un agente ya no hay trato.",
            _ => "No hay una búsqueda activa que sobornar."
        };
        _hud?.ShowStatus(message, 4.0f);
    }

    public MissionProgressSaveData CreateMissionSaveData()
    {
        if (_mission is null)
        {
            throw new InvalidOperationException("Stage 5 has not been initialized.");
        }

        return new MissionProgressSaveData
        {
            MissionId = ElFaroMissionId,
            Status = _mission.Status.ToString(),
            RewardGranted = _mission.RewardGranted
        };
    }

    public MissionProgressSaveData? ValidateMissionProgress(IReadOnlyList<MissionProgressSaveData> missions)
    {
        MissionProgressSaveData? match = null;
        foreach (MissionProgressSaveData savedMission in missions)
        {
            if (savedMission.MissionId != ElFaroMissionId)
            {
                continue;
            }

            if (match is not null)
            {
                throw new InvalidDataException($"Mission '{ElFaroMissionId}' is duplicated in the save.");
            }

            match = savedMission;
        }

        if (match is not null)
        {
            PropertyTakeoverMissionStatus status = ParseMissionStatus(match.Status);
            if ((status == PropertyTakeoverMissionStatus.Completed) != match.RewardGranted)
            {
                throw new InvalidDataException("Mission completion and reward state are inconsistent.");
            }
        }

        return match;
    }

    public void RestoreAfterLoad(MissionProgressSaveData? savedMission)
    {
        if (_mission is null || _workshop is null)
        {
            return;
        }

        ResetTransientSystems();
        if (savedMission is null)
        {
            _mission.SynchronizeCapturedAfterLoad(
                _workshop.Business.OwnerId == WorkshopController.PlayerFactionId);
        }
        else
        {
            PropertyTakeoverMissionStatus status = ParseMissionStatus(savedMission.Status);
            _mission.RestoreProgress(status, savedMission.RewardGranted);
            if (status == PropertyTakeoverMissionStatus.ResolvePoliceResponse)
            {
                _mission.MarkPoliceResponseResolved();
            }
        }

        RefreshHud();
    }

    public void ResetStageFiveEncounter()
    {
        ResetTransientSystems();
        _witness?.ResetWitness();
        _mission?.RestoreProgress(PropertyTakeoverMissionStatus.Available, false);
        RefreshHud();
    }

    private void AdvancePolice(float deltaSeconds)
    {
        if (_police is null || _policeVehicle is null)
        {
            return;
        }

        if (_police.Status == PoliceResponseStatus.Dispatching)
        {
            _police.AdvanceDispatch(deltaSeconds);
        }
        else if (_police.Status == PoliceResponseStatus.EnRoute)
        {
            _policeVehicle.Visible = true;
            _policeVehicle.GlobalPosition = _policeVehicle.GlobalPosition.MoveToward(
                PoliceVehicleDestination,
                PoliceVehicleSpeed * deltaSeconds);
            if (_policeVehicle.GlobalPosition.DistanceSquaredTo(PoliceVehicleDestination) <= 0.05f &&
                _police.MarkArrived())
            {
                _policeOfficer?.Activate(
                    PoliceVehicleDestination + Vector3.Left * 1.4f,
                    _lastKnownPlayerPosition);
            }
        }
        else if (_police.Status == PoliceResponseStatus.Responding)
        {
            if (_policeOfficer?.HasPlayerLineOfSight == true && _player is not null)
            {
                _lastKnownPlayerPosition = _player.GlobalPosition;
                _policeOfficer.UpdateLastKnownPosition(_lastKnownPlayerPosition);
            }

            _police.AdvanceSearch(deltaSeconds, _policeOfficer?.HasPlayerLineOfSight == true);
        }
    }

    private void OnPlayerShot(Vector3 origin, Vector3 end, bool didHit)
    {
        if (_police is null || _witness?.CanReport != true ||
            origin.DistanceSquaredTo(_witness.GlobalPosition) >
            WitnessHearingRadius * WitnessHearingRadius)
        {
            return;
        }

        _lastKnownPlayerPosition = _player?.GlobalPosition ?? origin;
        _police.ReportGunfire(1);
        _policeOfficer?.UpdateLastKnownPosition(_lastKnownPlayerPosition);
        _hud?.ShowStatus("Un testigo oyó el disparo y llamó a la policía.", 5.0f);
    }

    private void OnWorkshopOwnerChanged(string previous, string current)
    {
        if (current == WorkshopController.PlayerFactionId)
        {
            _mission?.MarkPropertyCaptured(_police?.IsActive == true);
        }
    }

    private void OnPlayerDefeated(DamageInfo damage) => _mission?.Fail();

    private void OnPoliceOfficerDefeated()
    {
        if (_police?.ResolveOfficerDefeat() == true)
        {
            _hud?.ShowStatus("El agente cayó. La búsqueda terminó, pero habrá consecuencias.", 5.0f);
        }
    }

    private void OnPoliceChanged()
    {
        if (_police?.Status is PoliceResponseStatus.Resolved or
            PoliceResponseStatus.Escaped or PoliceResponseStatus.Bribed)
        {
            _mission?.MarkPoliceResponseResolved();
            _policeOfficer?.Deactivate();
            if (_policeVehicle is not null)
            {
                _policeVehicle.Visible = false;
            }
        }

        RefreshHud();
    }

    private void OnMissionChanged()
    {
        if (_mission?.Status == PropertyTakeoverMissionStatus.Completed)
        {
            _hud?.ShowStatus($"Encargo completado. Recompensa: ${MissionReward}.", 5.0f);
        }
        else if (_mission?.Status == PropertyTakeoverMissionStatus.Failed)
        {
            _hud?.ShowStatus("Encargo fallido. Podés reintentarlo con el contacto.", 5.0f);
        }

        RefreshHud();
    }

    private void OnAttackersRequested()
    {
        _counterattacker?.Activate(CounterattackerSpawn, CounterattackObjective);
        _hud?.ShowStatus("Represalia: un hombre del Círculo Ferro va hacia El Faro.", 5.0f);
    }

    private void OnCounterattackerObjectivePresenceChanged(bool atObjective) =>
        _counterattack?.SetAttackerAtObjective(0, atObjective);

    private void OnCounterattackerDefeated()
    {
        if (_counterattack?.SetAttackerDefeated(0) == true)
        {
            _hud?.ShowStatus("Represalia rechazada. El Faro sigue operativo.", 5.0f);
        }
    }

    private void OnCounterattackChanged()
    {
        if (_counterattack?.Status == BusinessCounterattackStatus.Sabotaged)
        {
            _hud?.ShowStatus("El atacante saboteó El Faro, pero la propiedad sigue siendo tuya.", 6.0f);
            _counterattacker?.Deactivate();
        }

        RefreshHud();
    }

    private void ResetTransientSystems()
    {
        _police?.Reset();
        _counterattack?.AcknowledgeCurrentOwnership();
        _policeOfficer?.Deactivate();
        _counterattacker?.Deactivate();
        if (_policeVehicle is not null)
        {
            _policeVehicle.GlobalPosition = PoliceVehicleSpawn;
            _policeVehicle.Visible = false;
        }

        _reinforcements?.ResetArrival();
        _squad?.ResetTransientSquad();
    }

    private void ResolveHud() => _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;

    private void RefreshHud()
    {
        ResolveHud();
        if (_mission is not null)
        {
            string objective = _mission.Status switch
            {
                PropertyTakeoverMissionStatus.Available => "Hablá con Don Vittorio",
                PropertyTakeoverMissionStatus.ReachProperty => "Llegá a Taller El Faro",
                PropertyTakeoverMissionStatus.TakeControl => "Tomá el control del taller",
                PropertyTakeoverMissionStatus.ResolvePoliceResponse => "Perdé, soborná o enfrentá a la policía",
                PropertyTakeoverMissionStatus.Completed => "El Faro pertenece a la familia",
                PropertyTakeoverMissionStatus.Failed => "Volvé con el contacto para reintentar",
                _ => string.Empty
            };
            _hud?.SetMission(objective, _mission.Status.ToString());
        }

        if (_police is not null)
        {
            _hud?.SetPolice(
                _police.Status.ToString(),
                _police.WantedLevel,
                _police.Status == PoliceResponseStatus.Responding
                    ? _police.RemainingEscapeSeconds
                    : _police.RemainingDispatchSeconds);
        }

        if (_counterattack is not null)
        {
            float remaining = _counterattack.Status == BusinessCounterattackStatus.Warning
                ? _counterattack.RemainingWarningSeconds
                : _counterattack.RemainingSabotageSeconds;
            _hud?.SetCounterattack(_counterattack.Status.ToString(), remaining);
        }
    }

    private static PropertyTakeoverMissionStatus ParseMissionStatus(string value)
    {
        if (!Enum.TryParse(value, false, out PropertyTakeoverMissionStatus status) ||
            !Enum.IsDefined(status))
        {
            throw new InvalidDataException($"Unknown mission status '{value}'.");
        }

        return status;
    }
}
