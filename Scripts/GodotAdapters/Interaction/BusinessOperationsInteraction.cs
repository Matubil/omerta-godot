using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Businesses;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class BusinessOperationsInteraction : Area3D, IInteractable
{
    private IBusinessOperationsHost? _host;

    public bool CanInteract => ResolveHost()?.CanInteractWithOperations == true;

    public string InteractionPrompt => ResolveHost()?.OperationsPrompt ?? string.Empty;

    public void Interact() => ResolveHost()?.InteractWithOperations();

    private IBusinessOperationsHost? ResolveHost()
    {
        if (_host is not null)
        {
            return _host;
        }

        Node? current = GetParent();
        while (current is not null && current is not IBusinessOperationsHost)
        {
            current = current.GetParent();
        }

        _host = current as IBusinessOperationsHost;
        return _host;
    }
}
