using Godot;
using Omerta.Domain;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.AI;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Economy;
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
    private Vector3 _playerSpawn;
    private Vector3 _playerRotation;
    private Vector3 _rivalSpawn;
    private Vector3 _rivalRotation;

    public override void _Ready()
    {
        GD.Print($"{GameIdentity.CommercialName} [{GameIdentity.TechnicalName}] — Etapas 0 a 3");
        _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _rival = GetTree().GetFirstNodeInGroup("rival") as RivalController;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _workshop = GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _economy = GetTree().GetFirstNodeInGroup("economy") as EconomyController;
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
        GD.Print("Etapa 3 activa: presión, sabotaje, conquista e ingresos de Taller El Faro.");
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
        _hud?.ShowStatus("Encuentro reiniciado.");
    }

    private void OnPlayerDefeated(DamageInfo damage) =>
        _hud?.ShowStatus("Has caído. [T] reinicia el encuentro.", 6.0f);
}
