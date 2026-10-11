using System.Collections.Generic;
using Godot;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Input;
using Omerta.GodotAdapters.World;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Squad;

public sealed partial class SquadCommander : Node
{
    private readonly List<SquadCompanionController> _companions = new();
    private PlayerInputAdapter? _input;
    private WorldCursorProjector? _cursor;
    private HudController? _hud;

    public string LastOrderDescription { get; private set; } = "SIGUIENDO";

    public override void _Ready()
    {
        AddToGroup("squad_commander");
        ResolveDependencies();
        foreach (Node node in GetTree().GetNodesInGroup("companion"))
        {
            if (node is SquadCompanionController companion)
            {
                RegisterCompanion(companion);
            }
        }

        RefreshHud();
    }

    public override void _Process(double delta)
    {
        ResolveDependencies();
        RegisterActiveCompanions();
        if (_input?.SquadCommandPressed == true)
        {
            IssueContextCommand();
        }

        if (_input?.SquadRegroupPressed == true)
        {
            Regroup();
        }
    }

    public bool RegisterCompanion(SquadCompanionController companion)
    {
        if (_companions.Contains(companion))
        {
            return false;
        }

        _companions.Add(companion);
        companion.Regroup();
        RefreshHud();
        return true;
    }

    public void Regroup()
    {
        foreach (SquadCompanionController companion in _companions)
        {
            companion.Regroup();
        }

        LastOrderDescription = "SIGUIENDO";
        RefreshHud();
    }

    public void ResetTransientSquad()
    {
        _companions.RemoveAll(companion => !companion.IsActive);
        Regroup();
    }

    private void IssueContextCommand()
    {
        if (_cursor?.HasAimPoint != true)
        {
            return;
        }

        Combatant? target = _cursor.AimTarget;
        if (target is not null && !target.IsDefeated && target.Actor.IsInGroup("hostile"))
        {
            bool accepted = false;
            foreach (SquadCompanionController companion in _companions)
            {
                accepted |= companion.Attack(target);
            }

            if (accepted)
            {
                LastOrderDescription = $"ATACAR: {target.CombatantId}";
            }
        }
        else
        {
            int index = 0;
            foreach (SquadCompanionController companion in _companions)
            {
                companion.MoveTo(_cursor.AimPoint + Vector3.Right * index * 0.9f);
                index++;
            }

            LastOrderDescription = "MOVIÉNDOSE";
        }

        RefreshHud();
    }

    private void ResolveDependencies()
    {
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _cursor ??= GetTree().GetFirstNodeInGroup("world_cursor") as WorldCursorProjector;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
    }

    private void RegisterActiveCompanions()
    {
        foreach (Node node in GetTree().GetNodesInGroup("companion"))
        {
            if (node is SquadCompanionController companion)
            {
                RegisterCompanion(companion);
            }
        }
    }

    private void RefreshHud()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        int active = 0;
        foreach (SquadCompanionController companion in _companions)
        {
            if (companion.IsActive && !companion.IsDefeated)
            {
                active++;
            }
        }

        _hud?.SetSquad(LastOrderDescription, active);
    }
}
