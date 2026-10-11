using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.StageFive;

namespace Omerta.GodotAdapters.Law;

public sealed partial class PoliceBribeInteraction : Area3D, IInteractable
{
    private StageFiveController? _stage;

    public bool CanInteract => ResolveStage()?.CanAttemptPoliceBribe == true;

    public string InteractionPrompt => ResolveStage()?.PoliceBribePrompt ?? string.Empty;

    public void Interact() => ResolveStage()?.TryBribePolice();

    private StageFiveController? ResolveStage()
    {
        _stage ??= GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        return _stage;
    }
}
