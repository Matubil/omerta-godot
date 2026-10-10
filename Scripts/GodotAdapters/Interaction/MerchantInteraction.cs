using Godot;
using Omerta.Application.Interaction;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class MerchantInteraction : Area3D, IInteractable
{
    [Export]
    public string MerchantName { get; set; } = "Comerciante";

    [Export(PropertyHint.MultilineText)]
    public string Greeting { get; set; } = "El taller está abierto, pero no busco problemas.";

    public bool CanInteract => true;

    public string InteractionPrompt => $"Hablar con {MerchantName}";

    public void Interact()
    {
        HudController? hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        hud?.ShowStatus($"{MerchantName}: «{Greeting}»", 4.0f);
    }
}
