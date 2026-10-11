using System.Collections.Generic;
using Godot;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Squad;

public enum AllyReinforcementStatus
{
    Available,
    EnRoute,
    Arrived
}

public sealed partial class AllyReinforcementController : Node
{
    [Export]
    public Vector3 RouteStart { get; set; } = new(0.0f, 0.45f, 24.0f);

    [Export]
    public Vector3 RouteDestination { get; set; } = new(0.0f, 0.45f, 8.0f);

    [Export]
    public float VehicleSpeed { get; set; } = 7.0f;

    private Node3D? _vehicle;
    private readonly List<SquadCompanionController> _passengers = new();
    private SquadCommander? _commander;
    private HudController? _hud;

    public AllyReinforcementStatus Status { get; private set; } = AllyReinforcementStatus.Available;

    public override void _Ready()
    {
        AddToGroup("reinforcement_service");
        _vehicle = GetParent().GetNode<Node3D>("ReinforcementVehicle");
        ResolvePassengers();
        _commander = GetTree().GetFirstNodeInGroup("squad_commander") as SquadCommander;
        ResetArrival();
    }

    public override void _Process(double delta)
    {
        if (Status != AllyReinforcementStatus.EnRoute || _vehicle is null)
        {
            return;
        }

        _vehicle.GlobalPosition = _vehicle.GlobalPosition.MoveToward(
            RouteDestination,
            VehicleSpeed * (float)delta);
        if (_vehicle.GlobalPosition.DistanceSquaredTo(RouteDestination) <= 0.05f)
        {
            DeployPassenger();
        }
    }

    public bool RequestReinforcements()
    {
        if (Status != AllyReinforcementStatus.Available || _vehicle is null)
        {
            return false;
        }

        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _vehicle.GlobalPosition = RouteStart;
        _vehicle.Visible = true;
        Status = AllyReinforcementStatus.EnRoute;
        _hud?.ShowStatus("Los refuerzos vienen en camino.", 4.0f);
        return true;
    }

    public void ResetArrival()
    {
        Status = AllyReinforcementStatus.Available;
        if (_vehicle is not null)
        {
            _vehicle.GlobalPosition = RouteStart;
            _vehicle.Visible = false;
        }

        ResolvePassengers();
        foreach (SquadCompanionController passenger in _passengers)
        {
            passenger.Deactivate();
        }
    }

    private void DeployPassenger()
    {
        ResolvePassengers();
        if (_vehicle is null || _passengers.Count == 0)
        {
            return;
        }

        _commander ??= GetTree().GetFirstNodeInGroup("squad_commander") as SquadCommander;
        for (int index = 0; index < _passengers.Count; index++)
        {
            SquadCompanionController passenger = _passengers[index];
            passenger.Activate(
                _vehicle.GlobalPosition + Vector3.Right * (1.1f + index * 1.0f));
            _commander?.RegisterCompanion(passenger);
        }

        Status = AllyReinforcementStatus.Arrived;
        _hud?.ShowStatus("El vehículo llegó. Dos aliados se suman a la crew.", 4.0f);
    }

    private void ResolvePassengers()
    {
        foreach (Node node in GetTree().GetNodesInGroup("reserve_companion"))
        {
            if (node is SquadCompanionController passenger && !_passengers.Contains(passenger))
            {
                _passengers.Add(passenger);
            }
        }
    }
}
