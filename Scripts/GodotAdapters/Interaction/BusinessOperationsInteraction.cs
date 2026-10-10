using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Businesses;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class BusinessOperationsInteraction : Area3D, IInteractable
{
    private WorkshopController? _workshop;

    public bool CanInteract => ResolveWorkshop()?.CanInteractWithOperations == true;

    public string InteractionPrompt => ResolveWorkshop()?.OperationsPrompt ?? string.Empty;

    public void Interact() => ResolveWorkshop()?.InteractWithOperations();

    private WorkshopController? ResolveWorkshop()
    {
        if (_workshop is not null)
        {
            return _workshop;
        }

        Node? current = GetParent();
        while (current is not null && current is not WorkshopController)
        {
            current = current.GetParent();
        }

        _workshop = current as WorkshopController;
        return _workshop;
    }
}
