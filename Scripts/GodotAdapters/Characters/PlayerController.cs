using Godot;
using Omerta.GodotAdapters.Camera;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Input;
using Omerta.GodotAdapters.World;
using Omerta.GodotAdapters.Vehicles;
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
    public float AimMovementMultiplier { get; set; } = 0.65f;

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
    private Combatant? _combatant;
    private WorldCursorProjector? _cursor;
    private PlayerTraversalController? _traversal;
    private PlayerCoverController? _cover;
    private bool _isCrouched;
    private bool _isAiming;
    private DriveableVehicleController? _currentVehicle;
    private uint _standingCollisionLayer;
    private uint _standingCollisionMask;

    public bool IsCrouched => _isCrouched;

    public bool IsAiming => _isAiming;

    public bool IsTraversing => _traversal?.IsVaulting == true;

    public bool IsInCover => _cover?.IsInCover == true;

    public CoverType CurrentCoverType => _cover?.CurrentType ?? CoverType.None;

    public float CollisionRadius => _capsule?.Radius ?? 0.35f;

    public float CurrentPlanarSpeed => new Vector2(Velocity.X, Velocity.Z).Length();

    public bool IsDriving => _currentVehicle is not null;

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
        _combatant = GetNodeOrNull<Combatant>("Combatant");
        _traversal = GetNode<PlayerTraversalController>("PlayerTraversalController");
        _cover = GetNode<PlayerCoverController>("PlayerCoverController");
        _standingCollisionLayer = CollisionLayer;
        _standingCollisionMask = CollisionMask;
        ApplyStanceDimensions();
    }

    public override void _PhysicsProcess(double delta)
    {
        ResolveDependencies();
        if (_input is null)
        {
            return;
        }

        if (IsDriving)
        {
            Velocity = Vector3.Zero;
            _hud?.SetMovementState(false, false, "CONDUCIENDO");
            return;
        }

        if (_combatant?.IsDefeated == true)
        {
            _traversal?.ResetTraversal();
            _cover?.ExitCover();
            _cameraRig?.SetCover(false, false);
            Velocity = Vector3.Zero;
            return;
        }

        if (_traversal?.IsVaulting == true)
        {
            _traversal.Advance(delta);
            Velocity = Vector3.Zero;
            _hud?.SetMovementState(false, false, "VAULT");
            return;
        }

        Vector2 moveInput = _input.Movement;
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
        Vector3 facingForward = GetVisualForward();

        if (_input.CoverPressed && _cover is not null)
        {
            bool changed = _cover.ToggleCover(facingForward);
            if (!changed)
            {
                _hud?.ShowStatus("No hay una cobertura válida cerca.");
            }

            _cameraRig?.SetCover(_cover.IsInCover, _cover.CurrentType == CoverType.Low);
        }

        if (_input.TraversePressed && _traversal is not null)
        {
            bool started;
            if (_cover?.IsInCover == true)
            {
                if (_cover.CurrentType == CoverType.High)
                {
                    _hud?.ShowStatus("La cobertura es demasiado alta para saltarla.");
                    started = false;
                }
                else
                {
                    started = _traversal.TryVault(-_cover.SurfaceNormal);
                    if (started)
                    {
                        _cover.ExitCover();
                        _cameraRig?.SetCover(false, false);
                    }
                }
            }
            else
            {
                Vector3 traversalDirection = moveDirection.LengthSquared() > 0.001f
                    ? moveDirection
                    : facingForward;
                started = _traversal.TryVault(traversalDirection);
            }

            if (started)
            {
                _hud?.SetMovementState(false, false, "VAULT");
                return;
            }
        }

        if (_input.CrouchPressed && _cover?.IsInCover != true)
        {
            TrySetCrouched(!_isCrouched);
        }

        if (_cover?.IsInCover == true &&
            _cover.TryGetMotion(moveInput.X, delta, out Vector3 coverVelocity, out Vector3 coverFacing))
        {
            Vector3 coverBodyVelocity = Velocity;
            coverBodyVelocity.X = coverVelocity.X;
            coverBodyVelocity.Z = coverVelocity.Z;
            coverBodyVelocity.Y = IsOnFloor() ? -0.5f : coverBodyVelocity.Y - Gravity * (float)delta;
            Velocity = coverBodyVelocity;
            FaceDirection(coverFacing, delta);
            MoveAndSlide();
            _cameraRig?.SetCover(true, _cover.CurrentType == CoverType.Low);
            string coverName = _cover.CurrentType == CoverType.High ? "COBERTURA ALTA" : "COBERTURA BAJA";
            _hud?.SetMovementState(_isCrouched, false, coverName);
            return;
        }

        _cameraRig?.SetCover(false, false);
        bool isSprinting = _input.IsSprinting && !_isCrouched && moveDirection.LengthSquared() > 0.001f;
        float stanceMultiplier = _isCrouched ? CrouchMultiplier : 1.0f;
        float sprintMultiplier = isSprinting ? SprintMultiplier : 1.0f;
        float aimMultiplier = _isAiming ? AimMovementMultiplier : 1.0f;
        float speed = MovementSpeed * stanceMultiplier * sprintMultiplier * aimMultiplier;

        Vector3 velocity = Velocity;
        velocity.X = moveDirection.X * speed;
        velocity.Z = moveDirection.Z * speed;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - Gravity * (float)delta;
        Velocity = velocity;

        Vector3 facingDirection = moveDirection;
        if (_isAiming && _cursor?.HasAimPoint == true)
        {
            facingDirection = _cursor.AimPoint - GlobalPosition;
            facingDirection.Y = 0.0f;
        }

        FaceDirection(facingDirection, delta);

        MoveAndSlide();
        _hud?.SetMovementState(_isCrouched, isSprinting, string.Empty);
    }

    private void ResolveDependencies()
    {
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _cameraRig ??= GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _camera ??= GetTree().GetFirstNodeInGroup("game_camera") as Camera3D;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _cursor ??= GetTree().GetFirstNodeInGroup("world_cursor") as WorldCursorProjector;
    }

    public void SetAiming(bool aiming) => _isAiming = aiming;

    public void EnterVehicleMode(DriveableVehicleController vehicle)
    {
        _currentVehicle = vehicle;
        _isAiming = false;
        _traversal?.ResetTraversal();
        _cover?.ExitCover();
        _cameraRig?.SetCover(false, false);
        _cameraRig?.SetAiming(false);
        Velocity = Vector3.Zero;
        if (_visual is not null)
        {
            _visual.Visible = false;
        }

        CollisionLayer = 0;
        CollisionMask = 0;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
    }

    public void SynchronizeWithVehicle(Vector3 position, float yaw)
    {
        if (!IsDriving)
        {
            return;
        }

        GlobalPosition = position;
        Rotation = new Vector3(0.0f, yaw, 0.0f);
    }

    public void ExitVehicleMode(Vector3 position)
    {
        _currentVehicle = null;
        GlobalPosition = position;
        Velocity = Vector3.Zero;
        CollisionLayer = _standingCollisionLayer;
        CollisionMask = _standingCollisionMask;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        if (_visual is not null)
        {
            _visual.Visible = true;
        }
    }

    public void ForceLeaveCurrentVehicle() => _currentVehicle?.ForceExitDriver();

    public bool TryStandForTraversal()
    {
        if (!_isCrouched)
        {
            return true;
        }

        TrySetCrouched(false);
        return !_isCrouched;
    }

    public bool TryEnterCoverForDiagnostics(Vector3 direction) =>
        _cover?.TryEnterCover(direction) == true;

    public bool TryVaultForDiagnostics(Vector3 direction) =>
        _traversal?.TryVault(direction) == true;

    public void ExitCoverForDiagnostics() => _cover?.ExitCover();

    public void ResetForEncounter(Vector3 position, Vector3 rotation)
    {
        ForceLeaveCurrentVehicle();
        GlobalPosition = position;
        Rotation = rotation;
        Velocity = Vector3.Zero;
        _isAiming = false;
        _traversal?.ResetTraversal();
        _cover?.ExitCover();
        _cameraRig?.SetCover(false, false);
        if (_isCrouched)
        {
            _isCrouched = false;
            ApplyStanceDimensions();
            _cameraRig?.SetCrouched(false);
        }
    }

    public void RestorePersistentPose(Vector3 position, Quaternion rotation)
    {
        ForceLeaveCurrentVehicle();
        Velocity = Vector3.Zero;
        _isAiming = false;
        _traversal?.ResetTraversal();
        _cover?.ExitCover();
        _cameraRig?.SetCover(false, false);
        if (_isCrouched)
        {
            _isCrouched = false;
            ApplyStanceDimensions();
            _cameraRig?.SetCrouched(false);
        }

        GlobalTransform = new Transform3D(new Basis(rotation.Normalized()), position);
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

    private bool TrySetCrouched(bool crouched)
    {
        if (!crouched && _standingClearance?.IsColliding() == true)
        {
            _hud?.ShowStatus("No hay espacio para ponerse de pie.");
            return false;
        }

        _isCrouched = crouched;
        ApplyStanceDimensions();
        _cameraRig?.SetCrouched(crouched);
        return true;
    }

    private Vector3 GetVisualForward()
    {
        Vector3 forward = _visual?.GlobalBasis.Z ?? Vector3.Forward;
        forward.Y = 0.0f;
        return forward.LengthSquared() > 0.001f ? forward.Normalized() : Vector3.Forward;
    }

    private void FaceDirection(Vector3 facingDirection, double delta)
    {
        facingDirection.Y = 0.0f;
        if (facingDirection.LengthSquared() <= 0.001f || _visual is null)
        {
            return;
        }

        float targetYaw = Mathf.Atan2(facingDirection.X, facingDirection.Z);
        Vector3 rotation = _visual.Rotation;
        rotation.Y = Mathf.LerpAngle(
            rotation.Y,
            targetYaw,
            1.0f - Mathf.Exp(-RotationSpeed * (float)delta));
        _visual.Rotation = rotation;
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
