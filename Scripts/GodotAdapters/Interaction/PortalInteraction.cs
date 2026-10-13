using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Characters;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class PortalInteraction : Area3D, IInteractable
{
    [Export]
    public string Label { get; set; } = "Entrar";

    [Export]
    public Vector3 Destination { get; set; }

    [Export]
    public string ArrivalMessage { get; set; } = string.Empty;

    public bool CanInteract => true;

    public string InteractionPrompt => Label;

    public void Interact()
    {
        if (GetTree().GetFirstNodeInGroup("player") is not PlayerController player)
        {
            return;
        }

        player.TeleportTo(Destination);
        if (!string.IsNullOrWhiteSpace(ArrivalMessage))
        {
            (GetTree().GetFirstNodeInGroup("hud") as HudController)?.ShowStatus(ArrivalMessage);
        }
    }
}
