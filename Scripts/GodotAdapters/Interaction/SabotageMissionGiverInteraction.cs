using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Missions;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class SabotageMissionGiverInteraction : Area3D, IInteractable
{
    private SabotageMissionController? _mission;

    public bool CanInteract => ResolveMission()?.CanStartMission == true;

    public string InteractionPrompt => ResolveMission()?.MissionInteractionPrompt ?? string.Empty;

    public void Interact() => ResolveMission()?.StartMission();

    private SabotageMissionController? ResolveMission()
    {
        _mission ??= GetTree().GetFirstNodeInGroup("sabotage_mission") as SabotageMissionController;
        return _mission;
    }
}
