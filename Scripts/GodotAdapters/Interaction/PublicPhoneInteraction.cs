using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Squad;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class PublicPhoneInteraction : Area3D, IInteractable
{
    private AllyReinforcementController? _service;

    public bool CanInteract => ResolveService()?.Status == AllyReinforcementStatus.Available;

    public string InteractionPrompt => "Llamar aliado · llega en vehículo";

    public void Interact() => ResolveService()?.RequestReinforcements();

    private AllyReinforcementController? ResolveService()
    {
        _service ??= GetTree().GetFirstNodeInGroup("reinforcement_service")
            as AllyReinforcementController;
        return _service;
    }
}
