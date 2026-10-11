using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.StageFive;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class MissionGiverInteraction : Area3D, IInteractable
{
    private StageFiveController? _stage;

    public bool CanInteract => ResolveStage()?.CanStartMission == true;

    public string InteractionPrompt => ResolveStage()?.MissionInteractionPrompt ?? string.Empty;

    public void Interact() => ResolveStage()?.StartMission();

    private StageFiveController? ResolveStage()
    {
        _stage ??= GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        return _stage;
    }
}
