using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Vehicles;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class VehicleEntryInteraction : Area3D, IInteractable
{
    private DriveableVehicleController Vehicle => GetParent<DriveableVehicleController>();

    public bool CanInteract => Vehicle.CanEnterVehicle;

    public string InteractionPrompt => Vehicle.IsFamilyOwned
        ? $"Entrar a {Vehicle.DisplayName}"
        : $"Robar y conducir {Vehicle.DisplayName}";

    public void Interact()
    {
        PlayerController? player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (player is not null)
        {
            Vehicle.TryEnterDriver(player);
        }
    }
}
