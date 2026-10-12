using System.Collections.Generic;
using Godot;
using Omerta.GodotAdapters.Navigation;
using Omerta.Presentation;
using Omerta.GodotAdapters.Vehicles;

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

    private DriveableVehicleController? _vehicle;
    private NavigationAgent3D? _vehicleNavigation;
    private readonly List<SquadCompanionController> _passengers = new();
    private SquadCommander? _commander;
    private HudController? _hud;

    public AllyReinforcementStatus Status { get; private set; } = AllyReinforcementStatus.Available;

    public override void _Ready()
    {
        AddToGroup("reinforcement_service");
        _vehicle = GetParent().GetNode<DriveableVehicleController>("ReinforcementVehicle");
        _vehicleNavigation = _vehicle.GetNode<NavigationAgent3D>("NavigationAgent3D");
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

        Vector3 direction = _vehicleNavigation is not null
            ? NavigationMotion.DirectionTo(_vehicle, _vehicleNavigation, RouteDestination)
            : RouteDestination - _vehicle.GlobalPosition;
        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.001f)
        {
            float distance = Mathf.Min(VehicleSpeed * (float)delta, direction.Length());
            _vehicle.GlobalPosition += direction.Normalized() * distance;
            _vehicle.LookAt(_vehicle.GlobalPosition + direction, Vector3.Up);
        }

        Vector3 direct = RouteDestination - _vehicle.GlobalPosition;
        direct.Y = 0.0f;
        if (direct.LengthSquared() <= 0.09f)
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
        _vehicle.SetDriveEnabled(false);
        if (_vehicleNavigation is not null)
        {
            _vehicleNavigation.TargetPosition = RouteDestination;
        }

        Status = AllyReinforcementStatus.EnRoute;
        _hud?.ShowStatus("Los refuerzos vienen en camino.", 4.0f);
        return true;
    }

    public void ResetArrival()
    {
        Status = AllyReinforcementStatus.Available;
        if (_vehicle is not null)
        {
            _vehicle.SetDriveEnabled(false);
            _vehicle.GlobalPosition = RouteStart;
            _vehicle.Visible = false;
            if (_vehicleNavigation is not null)
            {
                _vehicleNavigation.TargetPosition = RouteStart;
            }
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
        _vehicle.SetDriveEnabled(true);
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
