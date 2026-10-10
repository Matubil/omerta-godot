using Godot;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Input;

namespace Omerta.GodotAdapters.World;

public sealed partial class WorldCursorProjector : Node3D
{
    [Export]
    public float ProjectionPlaneHeight { get; set; } = 0.06f;

    private Camera3D? _camera;
    private PlayerInputAdapter? _input;

    public bool HasAimPoint { get; private set; }

    public Vector3 AimPoint { get; private set; }

    public override void _Ready() => AddToGroup("world_cursor");

    public override void _PhysicsProcess(double delta)
    {
        _camera ??= GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        if (_camera is null || _input is null)
        {
            Visible = false;
            HasAimPoint = false;
            return;
        }

        Vector2 screenPosition = _input.AimScreenPosition;
        Vector3 origin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 direction = _camera.ProjectRayNormal(screenPosition);
        if (Mathf.Abs(direction.Y) < 0.0001f)
        {
            Visible = false;
            HasAimPoint = false;
            return;
        }

        float distance = (ProjectionPlaneHeight - origin.Y) / direction.Y;
        if (distance <= 0.0f)
        {
            Visible = false;
            HasAimPoint = false;
            return;
        }

        Vector3 groundPoint = origin + direction * distance;
        GlobalPosition = groundPoint;
        AimPoint = new Vector3(groundPoint.X, 1.0f, groundPoint.Z);
        Vector3 rayEnd = origin + direction * 250.0f;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, rayEnd);
        query.CollisionMask = 1;
        if (GetTree().GetFirstNodeInGroup("player") is CollisionObject3D player)
        {
            query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
        }

        Godot.Collections.Dictionary result = _camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            Node? collider = result["collider"].AsGodotObject() as Node;
            Combatant? combatant = Combatant.FindFromCollider(collider);
            if (combatant is not null)
            {
                AimPoint = combatant.Actor.GlobalPosition + Vector3.Up * 0.9f;
            }
        }

        HasAimPoint = true;
        Visible = true;
    }
}
