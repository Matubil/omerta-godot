using Godot;
using Omerta.Application.Interaction;
using Omerta.Domain.Vehicles;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.Vehicles;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class GarageRegistrationInteraction : Area3D, IInteractable
{
    [Export]
    public int RegistrationCost { get; set; } = 75;

    [Export]
    public float RegistrationRadius { get; set; } = 7.0f;

    private WorkshopController? _workshop;
    private EconomyController? _economy;
    private HudController? _hud;

    public bool CanInteract => true;

    public string InteractionPrompt
    {
        get
        {
            ResolveDependencies();
            if (!IsGarageAvailable)
            {
                return "Garaje bloqueado · conquistá y repará El Faro";
            }

            DriveableVehicleController? candidate = FindRegistrationCandidate();
            if (candidate is not null)
            {
                return $"Registrar {candidate.DisplayName} como vehículo familiar (${RegistrationCost})";
            }

            return HasPoliceMarkedVehicleNearby()
                ? "Perdé a la policía antes de registrar el vehículo"
                : "Estacioná aquí un vehículo robado para registrarlo";
        }
    }

    public override void _Ready()
    {
        AddToGroup("vehicle_garage");
        ResolveDependencies();
    }

    public void Interact()
    {
        ResolveDependencies();
        DriveableVehicleController? candidate = FindRegistrationCandidate();
        if (candidate is null)
        {
            _hud?.ShowStatus(
                IsGarageAvailable
                    ? HasPoliceMarkedVehicleNearby()
                        ? "Ese vehículo está siendo buscado. Perdé a la policía antes de registrarlo."
                        : "No hay un vehículo robado estacionado dentro del garaje."
                    : "El garaje requiere que Taller El Faro sea de la familia y esté operativo.",
                4.0f);
            return;
        }

        VehicleRegistrationResult result = candidate.TryRegisterAtGarage(
            IsGarageAvailable,
            _economy!.Economy,
            RegistrationCost);
        string message = result switch
        {
            VehicleRegistrationResult.Registered =>
                $"{candidate.DisplayName} registrado. Su baúl ahora accede al arsenal familiar.",
            VehicleRegistrationResult.InsufficientFunds =>
                $"Fondos insuficientes: el registro cuesta ${RegistrationCost}.",
            VehicleRegistrationResult.GarageUnavailable =>
                "El garaje de El Faro no está disponible.",
            VehicleRegistrationResult.PoliceSearchActive =>
                "Perdé a la policía antes de registrar ese vehículo.",
            _ => "Ese vehículo ya pertenece a la familia."
        };
        _hud?.ShowStatus(message, 5.0f);
    }

    public DriveableVehicleController? FindRegistrationCandidate()
    {
        DriveableVehicleController? nearest = null;
        float nearestDistanceSquared = RegistrationRadius * RegistrationRadius;
        foreach (Node node in GetTree().GetNodesInGroup("driveable_vehicle"))
        {
            if (node is not DriveableVehicleController vehicle || vehicle.IsFamilyOwned ||
                vehicle.IsPoliceMarked || vehicle.IsDriven || !vehicle.Visible)
            {
                continue;
            }

            float distanceSquared = GlobalPosition.DistanceSquaredTo(vehicle.GlobalPosition);
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            nearest = vehicle;
            nearestDistanceSquared = distanceSquared;
        }

        return nearest;
    }

    private bool HasPoliceMarkedVehicleNearby()
    {
        float radiusSquared = RegistrationRadius * RegistrationRadius;
        foreach (Node node in GetTree().GetNodesInGroup("driveable_vehicle"))
        {
            if (node is DriveableVehicleController vehicle && vehicle.IsPoliceMarked &&
                vehicle.Visible &&
                GlobalPosition.DistanceSquaredTo(vehicle.GlobalPosition) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGarageAvailable =>
        _workshop?.Business.OwnerId == WorkshopController.PlayerFactionId &&
        _workshop.Operations.IsOperational;

    private void ResolveDependencies()
    {
        _workshop ??= GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _economy ??= GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
    }
}
