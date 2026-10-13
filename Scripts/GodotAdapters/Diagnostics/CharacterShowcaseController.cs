using Godot;

namespace Omerta.GodotAdapters.Diagnostics;

public sealed partial class CharacterShowcaseController : Node3D
{
    public override void _Ready()
    {
        Node3D character = GetNode<Node3D>("Player");
        character.GetNode<Node3D>("Visual/Weapon").Visible = false;
        character.GetNode<Node3D>("Visual/BackWeapon").Visible = false;

        Camera3D camera = GetNode<Camera3D>("Camera3D");
        camera.LookAt(character.GlobalPosition + Vector3.Up * 0.95f, Vector3.Up);
    }
}
