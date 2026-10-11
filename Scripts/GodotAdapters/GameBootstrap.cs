using Godot;
using Omerta.Domain;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.AI;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.Persistence;
using Omerta.GodotAdapters.StageFive;
using Omerta.Presentation;

namespace Omerta.GodotAdapters;

public sealed partial class GameBootstrap : Node3D
{
    private static readonly StringName RestartEncounter = "restart_encounter";

    private PlayerController? _player;
    private PlayerCombatController? _playerCombat;
    private Combatant? _playerCombatant;
    private RivalController? _rival;
    private HudController? _hud;
    private WorkshopController? _workshop;
    private EconomyController? _economy;
    private PersistenceController? _persistence;
    private StageFiveController? _stageFive;
    private Vector3 _playerSpawn;
    private Vector3 _playerRotation;
    private Vector3 _rivalSpawn;
    private Vector3 _rivalRotation;

    public override void _Ready()
    {
        GD.Print(
            $"{GameIdentity.CommercialName} [{GameIdentity.TechnicalName}] — " +
            "Etapas 0 a 6 + navegación, cámara y traversal");
        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _rival = GetTree().GetFirstNodeInGroup("rival") as RivalController;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _workshop = GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _economy = GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _persistence = GetTree().GetFirstNodeInGroup("persistence") as PersistenceController;
        _stageFive = GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        if (_player is null || _rival is null)
        {
            GD.PushError("The combat slice requires one player and one rival.");
            return;
        }

        _playerCombat = _player.GetNode<PlayerCombatController>("PlayerCombatController");
        _playerCombatant = _player.GetNode<Combatant>("Combatant");
        _playerSpawn = _player.GlobalPosition;
        _playerRotation = _player.Rotation;
        _rivalSpawn = _rival.GlobalPosition;
        _rivalRotation = _rival.Rotation;
        _playerCombatant.Defeated += OnPlayerDefeated;
        GD.Print("Etapa 6 activa: vertical slice consolidado y listo para evaluación.");
        GD.Print(
            "Navegación activa: peatones y vehículos usan capas separadas; " +
            "peatones activos aplican evitamiento local.");
        GD.Print("Cámara activa: brazo con colisión y oclusores visuales atenuables.");
        GD.Print("Traversal activo: vault contextual y cobertura alta/baja.");
    }

    public override void _Process(double delta)
    {
        if (Godot.Input.IsActionJustPressed(RestartEncounter))
        {
            ResetEncounter();
        }
    }

    public override void _ExitTree()
    {
        if (_playerCombatant is not null)
        {
            _playerCombatant.Defeated -= OnPlayerDefeated;
        }
    }

    private void ResetEncounter()
    {
        if (_player is null || _playerCombat is null || _rival is null)
        {
            return;
        }

        _player.ResetForEncounter(_playerSpawn, _playerRotation);
        _playerCombat.ResetCombat();
        _rival.ResetCombat(_rivalSpawn, _rivalRotation);
        _workshop?.ResetBusinessEncounter();
        _economy?.ResetEconomy();
        _stageFive?.ResetStageFiveEncounter();
        _hud?.ShowStatus("Encuentro reiniciado.");
    }

    private void OnPlayerDefeated(DamageInfo damage)
    {
        if (_persistence is not null)
        {
            _persistence.ScheduleRecoveryAfterDefeat();
        }
        else
        {
            _hud?.ShowStatus("Has caído. [T] reinicia el encuentro.", 6.0f);
        }
    }
}
