using Godot;

namespace Omerta.GodotAdapters.Input;

public sealed partial class PlayerInputAdapter : Node
{
    private static readonly StringName MoveLeft = "move_left";
    private static readonly StringName MoveRight = "move_right";
    private static readonly StringName MoveForward = "move_forward";
    private static readonly StringName MoveBack = "move_back";
    private static readonly StringName Sprint = "sprint";
    private static readonly StringName Crouch = "crouch";
    private static readonly StringName Interact = "interact";
    private static readonly StringName Fire = "fire";
    private static readonly StringName Aim = "aim";
    private static readonly StringName Reload = "reload";

    [Export(PropertyHint.Range, "0.5,2.0,0.05")]
    public float PointerSensitivity { get; set; } = 1.0f;

    public override void _Ready() => AddToGroup("player_input");

    public Vector2 Movement => Godot.Input.GetVector(
        MoveLeft,
        MoveRight,
        MoveForward,
        MoveBack);

    public bool IsSprinting => Godot.Input.IsActionPressed(Sprint);

    public bool CrouchPressed => Godot.Input.IsActionJustPressed(Crouch);

    public bool InteractPressed => Godot.Input.IsActionJustPressed(Interact);

    public bool FirePressed => Godot.Input.IsActionJustPressed(Fire);

    public bool IsAiming => Godot.Input.IsActionPressed(Aim);

    public bool ReloadPressed => Godot.Input.IsActionJustPressed(Reload);

    public Vector2 AimScreenPosition
    {
        get
        {
            Vector2 size = GetViewport().GetVisibleRect().Size;
            Vector2 center = size * 0.5f;
            Vector2 offset = (GetViewport().GetMousePosition() - center) * PointerSensitivity;
            Vector2 position = center + offset;
            return new Vector2(
                Mathf.Clamp(position.X, 0.0f, size.X),
                Mathf.Clamp(position.Y, 0.0f, size.Y));
        }
    }
}
