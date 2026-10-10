namespace Omerta.Application.Interaction;

/// <summary>
/// Minimal application contract implemented by Godot world adapters.
/// </summary>
public interface IInteractable
{
    bool CanInteract { get; }

    string InteractionPrompt { get; }

    void Interact();
}
