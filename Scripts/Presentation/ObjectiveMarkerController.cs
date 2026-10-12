using Godot;
using Omerta.GodotAdapters.Missions;
using Omerta.GodotAdapters.StageFive;

namespace Omerta.Presentation;

public sealed partial class ObjectiveMarkerController : Control
{
    [Export]
    public float ScreenMargin { get; set; } = 56.0f;

    private Label? _label;
    private StageFiveController? _stage;
    private SabotageMissionController? _sabotageMission;
    private Camera3D? _camera;
    private Node3D? _player;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _label = GetNode<Label>("Marker");
        Visible = false;
    }

    public override void _Process(double delta)
    {
        _stage ??= GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        _sabotageMission ??= GetTree().GetFirstNodeInGroup("sabotage_mission") as SabotageMissionController;
        _camera ??= GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
        if (_stage is null || _camera is null || _player is null || _label is null)
        {
            Visible = false;
            return;
        }

        bool hasObjective = _stage.TryGetMissionObjective(
            out Vector3 target,
            out string objective);
        if (!hasObjective && _sabotageMission is not null)
        {
            hasObjective = _sabotageMission.TryGetMissionObjective(out target, out objective);
        }

        if (!hasObjective)
        {
            Visible = false;
            return;
        }

        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 screenPosition = _camera.UnprojectPosition(target + Vector3.Up * 1.5f);
        screenPosition.X = Mathf.Clamp(screenPosition.X, ScreenMargin, viewportSize.X - ScreenMargin);
        screenPosition.Y = Mathf.Clamp(screenPosition.Y, 108.0f, viewportSize.Y - ScreenMargin);
        float distance = _player.GlobalPosition.DistanceTo(target);
        _label.Text = $"◆ {objective} · {distance:0} m";
        Position = screenPosition - new Vector2(Size.X * 0.5f, Size.Y * 0.5f);
        Visible = !_camera.IsPositionBehind(target);
    }
}
