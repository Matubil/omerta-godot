using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Businesses;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class MerchantInteraction : Area3D, IInteractable
{
    private IBusinessMerchantHost? _host;

    public bool CanInteract => ResolveHost()?.CanInteractWithMerchant == true;

    public string InteractionPrompt => ResolveHost()?.MerchantPrompt ?? string.Empty;

    public void Interact() => ResolveHost()?.InteractWithMerchant();

    private IBusinessMerchantHost? ResolveHost()
    {
        if (_host is not null)
        {
            return _host;
        }

        Node? current = GetParent();
        while (current is not null && current is not IBusinessMerchantHost)
        {
            current = current.GetParent();
        }

        _host = current as IBusinessMerchantHost;
        return _host;
    }
}
