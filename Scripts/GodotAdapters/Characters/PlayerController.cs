using Godot;
using Omerta.GodotAdapters.Camera;
using Omerta.GodotAdapters.Input;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Characters;

public sealed partial class PlayerController : CharacterBody3D
{
    [Export]
    public float MovementSpeed { get; set; } = 5.0f;

    [Export]
    public float SprintMultiplier { get; set; } = 1.55f;

    [Export]
    public float CrouchMultiplier { get; set; } = 0.55f;

    [Export]
    public float RotationSpeed { get; set; } = 12.0f;

    [Export]
    public float Gravity { get; set; } = 20.0f;

    [Export]
    public float StandingHeight { get; set; } = 1.8f;

    [Export]
    public float CrouchedHeight { get; set; } = 1.2f;

    private PlayerInputAdapter? _input;
    private IsometricCameraController? _cameraRig;
    private Camera3D? _camera;
    private CollisionShape3D? _collision;
    private CapsuleShape3D? _capsule;
    private Node3D? _visual;
    private RayCast3D? _standingClearance;
    private HudController? _hud;
    private bool _isCrouched;

    public bool IsCrouched => _isCrouched;

    public override void _Ready()
    {
        AddToGroup("player");
        _input = GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _cameraRig = GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _camera = GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");
        _capsule = _collision.Shape as CapsuleShape3D;
        _visual = GetNode<Node3D>("Visual");
        _standingClearance = GetNode<RayCast3D>("StandingClearance");
        ApplyStanceDimensions();
    }

    public override void _PhysicsProcess(double delta)
    {
        ResolveDependencies();
        if (_input is null)
        {
            return;
        }

        if (_input.CrouchPressed)
        {
            TrySetCrouched(!_isCrouched);
        }

        Vector2 moveInput = _input.Movement;
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
        bool isSprinting = _input.IsSprinting && !_isCrouched && moveDirection.LengthSquared() > 0.001f;
        float stanceMultiplier = _isCrouched ? CrouchMultiplier : 1.0f;
        float sprintMultiplier = isSprinting ? SprintMultiplier : 1.0f;
        float speed = MovementSpeed * stanceMultiplier * sprintMultiplier;

        Vector3 velocity = Velocity;
        velocity.X = moveDirection.X * speed;
        velocity.Z = moveDirection.Z * speed;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - Gravity * (float)delta;
        Velocity = velocity;

        if (moveDirection.LengthSquared() > 0.001f && _visual is not null)
        {
            float targetYaw = Mathf.Atan2(moveDirection.X, moveDirection.Z);
            Vector3 rotation = _visual.Rotation;
            rotation.Y = Mathf.LerpAngle(
                rotation.Y,
                targetYaw,
                1.0f - Mathf.Exp(-RotationSpeed * (float)delta));
            _visual.Rotation = rotation;
        }

        MoveAndSlide();
        _hud?.SetMovementState(_isCrouched, isSprinting);
    }

    private void ResolveDependencies()
    {
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _cameraRig ??= GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _camera ??= GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (_camera is null)
        {
            return new Vector3(input.X, 0.0f, input.Y).Normalized();
        }

        Vector3 forward = -_camera.GlobalBasis.Z;
        forward.Y = 0.0f;
        forward = forward.Normalized();
        Vector3 right = _camera.GlobalBasis.X;
        right.Y = 0.0f;
        right = right.Normalized();
        Vector3 direction = forward * input.Y + right * input.X;
        return direction.LengthSquared() > 1.0f ? direction.Normalized() : direction;
    }

    private void TrySetCrouched(bool crouched)
    {
        if (!crouched && _standingClearance?.IsColliding() == true)
        {
            _hud?.ShowStatus("No hay espacio para ponerse de pie.");
            return;
        }

        _isCrouched = crouched;
        ApplyStanceDimensions();
        _cameraRig?.SetCrouched(crouched);
    }

    private void ApplyStanceDimensions()
    {
        if (_capsule is null || _collision is null || _visual is null)
        {
            return;
        }

        float height = _isCrouched ? CrouchedHeight : StandingHeight;
        _capsule.Height = height;
        _collision.Position = new Vector3(0.0f, height * 0.5f, 0.0f);
        _visual.Position = new Vector3(0.0f, height * 0.5f, 0.0f);
        _visual.Scale = new Vector3(1.0f, height / StandingHeight, 1.0f);
    }
}
