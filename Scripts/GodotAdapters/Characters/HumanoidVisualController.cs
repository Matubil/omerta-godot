using Godot;

namespace Omerta.GodotAdapters.Characters;

public enum HumanoidAnimationState
{
    Idle,
    Walk,
    Run,
    Crouch,
    Aim,
    Cover,
    Vault
}

public sealed partial class HumanoidVisualController : Node3D
{
    [Export]
    public float BlendSpeed { get; set; } = 12.0f;

    private PlayerController? _player;
    private Node3D? _torso;
    private Node3D? _leftArm;
    private Node3D? _rightArm;
    private Node3D? _leftLeg;
    private Node3D? _rightLeg;
    private float _locomotionPhase;

    public HumanoidAnimationState CurrentState { get; private set; }

    public override void _Ready()
    {
        _player = GetParent().GetParentOrNull<PlayerController>();
        _torso = GetNode<Node3D>("TorsoPivot");
        _leftArm = GetNode<Node3D>("LeftArmPivot");
        _rightArm = GetNode<Node3D>("RightArmPivot");
        _leftLeg = GetNode<Node3D>("LeftLegPivot");
        _rightLeg = GetNode<Node3D>("RightLegPivot");
    }

    public override void _Process(double delta)
    {
        if (_player is null)
        {
            return;
        }

        CurrentState = ResolveState();
        float speedRatio = Mathf.Clamp(
            _player.CurrentPlanarSpeed / Mathf.Max(0.01f, _player.MovementSpeed),
            0.0f,
            1.7f);
        if (CurrentState is HumanoidAnimationState.Walk or HumanoidAnimationState.Run)
        {
            _locomotionPhase += (float)delta * Mathf.Lerp(6.0f, 10.0f, speedRatio / 1.7f);
        }

        float stride = CurrentState switch
        {
            HumanoidAnimationState.Run => 0.78f,
            HumanoidAnimationState.Walk => 0.48f,
            HumanoidAnimationState.Crouch => 0.22f,
            _ => 0.0f
        };
        float wave = Mathf.Sin(_locomotionPhase) * stride;
        Vector3 torsoTarget = Vector3.Zero;
        Vector3 leftArmTarget = new(wave, 0.0f, 0.0f);
        Vector3 rightArmTarget = new(-wave, 0.0f, 0.0f);
        Vector3 leftLegTarget = new(-wave, 0.0f, 0.0f);
        Vector3 rightLegTarget = new(wave, 0.0f, 0.0f);

        switch (CurrentState)
        {
            case HumanoidAnimationState.Aim:
                torsoTarget.X = -0.08f;
                leftArmTarget = new(-1.18f, 0.0f, -0.18f);
                rightArmTarget = new(-1.34f, 0.0f, 0.16f);
                break;
            case HumanoidAnimationState.Cover:
                torsoTarget.Z = _player.CurrentCoverType == CoverType.High ? -0.14f : -0.24f;
                leftArmTarget = new(-0.42f, 0.0f, -0.16f);
                rightArmTarget = new(-0.58f, 0.0f, 0.16f);
                break;
            case HumanoidAnimationState.Vault:
                torsoTarget.X = -0.48f;
                leftArmTarget = new(-1.05f, 0.0f, -0.25f);
                rightArmTarget = new(-1.05f, 0.0f, 0.25f);
                leftLegTarget = new(0.78f, 0.0f, 0.0f);
                rightLegTarget = new(-0.38f, 0.0f, 0.0f);
                break;
            case HumanoidAnimationState.Crouch:
                torsoTarget.X = -0.12f;
                leftLegTarget.X += 0.38f;
                rightLegTarget.X += 0.38f;
                break;
        }

        float blend = 1.0f - Mathf.Exp(-BlendSpeed * (float)delta);
        _torso!.Rotation = _torso.Rotation.Lerp(torsoTarget, blend);
        _leftArm!.Rotation = _leftArm.Rotation.Lerp(leftArmTarget, blend);
        _rightArm!.Rotation = _rightArm.Rotation.Lerp(rightArmTarget, blend);
        _leftLeg!.Rotation = _leftLeg.Rotation.Lerp(leftLegTarget, blend);
        _rightLeg!.Rotation = _rightLeg.Rotation.Lerp(rightLegTarget, blend);

        float bob = CurrentState is HumanoidAnimationState.Walk or HumanoidAnimationState.Run
            ? Mathf.Abs(Mathf.Sin(_locomotionPhase * 2.0f)) * 0.035f
            : 0.0f;
        Position = new Vector3(0.0f, bob, 0.0f);
    }

    private HumanoidAnimationState ResolveState()
    {
        if (_player!.IsTraversing)
        {
            return HumanoidAnimationState.Vault;
        }

        if (_player.IsInCover)
        {
            return HumanoidAnimationState.Cover;
        }

        if (_player.IsAiming)
        {
            return HumanoidAnimationState.Aim;
        }

        if (_player.IsCrouched)
        {
            return HumanoidAnimationState.Crouch;
        }

        if (_player.CurrentPlanarSpeed > _player.MovementSpeed * 1.12f)
        {
            return HumanoidAnimationState.Run;
        }

        return _player.CurrentPlanarSpeed > 0.15f
            ? HumanoidAnimationState.Walk
            : HumanoidAnimationState.Idle;
    }
}
