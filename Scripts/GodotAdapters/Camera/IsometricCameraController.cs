using Godot;

namespace Omerta.GodotAdapters.Camera;

public sealed partial class IsometricCameraController : Node3D
{
    [Export(PropertyHint.Range, "12,24,0.5")]
    public float Distance { get; set; } = 18.0f;

    [Export(PropertyHint.Range, "25,75,1")]
    public float PitchDegrees { get; set; } = 58.0f;

    [Export(PropertyHint.Range, "-180,180,1")]
    public float YawDegrees { get; set; } = 45.0f;

    [Export]
    public float FocusHeight { get; set; } = 0.8f;

    [Export]
    public float FollowSharpness { get; set; } = 12.0f;

    [Export]
    public float CrouchHeightOffset { get; set; } = 0.25f;

    [Export]
    public float AimDistance { get; set; } = 14.5f;

    [Export]
    public float DefaultFieldOfView { get; set; } = 50.0f;

    [Export]
    public float AimFieldOfView { get; set; } = 44.0f;

    [Export]
    public float AimTransitionSpeed { get; set; } = 10.0f;

    private Node3D? _target;
    private Camera3D? _camera;
    private Vector3 _smoothedFocus;
    private bool _isCrouched;
    private bool _isAiming;
    private bool _hasInitialFocus;
    private float _currentDistance;
    private float _currentFieldOfView;

    public Camera3D? Camera => _camera;

    public override void _Ready()
    {
        AddToGroup("camera_rig");
        _target = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _camera = GetNode<Camera3D>("Camera3D");
        _camera.AddToGroup("game_camera");
        _currentDistance = Distance;
        _currentFieldOfView = DefaultFieldOfView;
        _camera.Fov = DefaultFieldOfView;
        UpdateCamera(1.0);
    }

    public override void _Process(double delta)
    {
        if (_target is null)
        {
            _target = GetTree().GetFirstNodeInGroup("player") as Node3D;
        }

        UpdateCamera(delta);
    }

    public void SetCrouched(bool crouched) => _isCrouched = crouched;

    public void SetAiming(bool aiming) => _isAiming = aiming;

    private void UpdateCamera(double delta)
    {
        if (_target is null || _camera is null)
        {
            return;
        }

        float height = FocusHeight - (_isCrouched ? CrouchHeightOffset : 0.0f);
        Vector3 desiredFocus = _target.GlobalPosition + Vector3.Up * height;
        float transitionBlend = 1.0f - Mathf.Exp(-AimTransitionSpeed * (float)delta);
        _currentDistance = Mathf.Lerp(
            _currentDistance,
            _isAiming ? AimDistance : Distance,
            transitionBlend);
        _currentFieldOfView = Mathf.Lerp(
            _currentFieldOfView,
            _isAiming ? AimFieldOfView : DefaultFieldOfView,
            transitionBlend);
        _camera.Fov = _currentFieldOfView;
        if (!_hasInitialFocus)
        {
            _smoothedFocus = desiredFocus;
            _hasInitialFocus = true;
        }
        else
        {
            float blend = 1.0f - Mathf.Exp(-FollowSharpness * (float)delta);
            _smoothedFocus = _smoothedFocus.Lerp(desiredFocus, blend);
        }

        float pitch = Mathf.DegToRad(PitchDegrees);
        float yaw = Mathf.DegToRad(YawDegrees);
        float horizontal = Mathf.Cos(pitch);
        Vector3 offset = new(
            Mathf.Sin(yaw) * horizontal,
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * horizontal);

        _camera.GlobalPosition = _smoothedFocus + offset * _currentDistance;
        _camera.LookAt(_smoothedFocus, Vector3.Up);
    }
}
