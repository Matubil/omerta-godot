using Godot;
using Omerta.GodotAdapters.Input;

namespace Omerta.GodotAdapters.World;

public sealed partial class WorldCursorProjector : Node3D
{
    [Export]
    public float ProjectionPlaneHeight { get; set; } = 0.06f;

    private Camera3D? _camera;
    private PlayerInputAdapter? _input;

    public override void _Process(double delta)
    {
        _camera ??= GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        if (_camera is null || _input is null)
        {
            Visible = false;
            return;
        }

        Vector2 screenPosition = _input.AimScreenPosition;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        if (Mathf.Abs(direction.Y) < 0.0001f)
        {
            Visible = false;
            return;
        }

        float distance = (ProjectionPlaneHeight - origin.Y) / direction.Y;
        if (distance <= 0.0f)
        {
            Visible = false;
            return;
        }

        GlobalPosition = origin + direction * distance;
        Visible = true;
    }
}
