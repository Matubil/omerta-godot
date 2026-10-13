using Godot;

namespace Omerta.GodotAdapters.World;

public sealed partial class InteriorVisibilityController : Node3D
{
    [Export(PropertyHint.Range, "1,20,0.1")]
    public float HalfWidth { get; set; } = 5.0f;

    [Export(PropertyHint.Range, "1,20,0.1")]
    public float HalfDepth { get; set; } = 5.0f;

    [Export]
    public float MinimumHeight { get; set; } = -1.0f;

    [Export]
    public float MaximumHeight { get; set; } = 4.5f;

    private Node3D? _player;
    private Node3D? _roof;

    public bool IsPlayerInside { get; private set; }

    public override void _Ready()
    {
        _roof = GetNode<Node3D>("Roof");
        RefreshVisibility();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
        if (_player is null || _roof is null)
        {
            return;
        }

        Vector3 localPosition = ToLocal(_player.GlobalPosition);
        IsPlayerInside = Mathf.Abs(localPosition.X) <= HalfWidth &&
            Mathf.Abs(localPosition.Z) <= HalfDepth &&
            localPosition.Y >= MinimumHeight &&
            localPosition.Y <= MaximumHeight;
        _roof.Visible = !IsPlayerInside;
    }
}
