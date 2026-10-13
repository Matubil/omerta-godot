using System.Collections.Generic;
using Godot;

namespace Omerta.GodotAdapters.Camera;

public sealed partial class IsometricCameraController : Node3D
{
    public const uint CameraCollisionLayer = 4u;
    public const uint CameraFadeLayer = 8u;

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
    public float LowCoverHeightOffset { get; set; } = 0.2f;

    [Export]
    public float CoverLateralOffset { get; set; } = 0.45f;

    [Export]
    public float AimDistance { get; set; } = 14.5f;

    [Export]
    public float DefaultFieldOfView { get; set; } = 50.0f;

    [Export]
    public float AimFieldOfView { get; set; } = 44.0f;

    [Export]
    public float AimTransitionSpeed { get; set; } = 10.0f;

    [Export]
    public float MinimumCollisionDistance { get; set; } = 0.1f;

    [Export]
    public float CollisionPadding { get; set; } = 0.45f;

    [Export]
    public float OcclusionRecoverySharpness { get; set; } = 7.0f;

    [Export(PropertyHint.Range, "0,1,0.05")]
    public float OccluderTransparency { get; set; } = 1.0f;

    [Export]
    public float OccluderFadeSpeed { get; set; } = 10.0f;

    private Node3D? _target;
    private Camera3D? _camera;
    private Vector3 _smoothedFocus;
    private bool _isCrouched;
    private bool _isAiming;
    private bool _isInCover;
    private bool _isInLowCover;
    private bool _hasInitialFocus;
    private float _requestedDistance;
    private float _currentDistance;
    private float _currentFieldOfView;
    private readonly Dictionary<GeometryInstance3D, float> _trackedOccluders = new();
    private readonly HashSet<GeometryInstance3D> _activeOccluders = new();

    public Camera3D? Camera => _camera;

    public float CurrentDistance => _currentDistance;

    public float CurrentFieldOfView => _currentFieldOfView;

    public bool IsArmOccluded { get; private set; }

    public int FadedGeometryCount => _activeOccluders.Count;

    public override void _Ready()
    {
        AddToGroup("camera_rig");
        _target = GetTree().GetFirstNodeInGroup("player") as Node3D;
        _camera = GetNode<Camera3D>("Camera3D");
        _camera.AddToGroup("game_camera");
        _requestedDistance = Distance;
        _currentDistance = Distance;
        _currentFieldOfView = DefaultFieldOfView;
        _camera.Fov = DefaultFieldOfView;
        UpdateCamera(1.0);
    }

    public override void _ExitTree()
    {
        foreach ((GeometryInstance3D geometry, float originalTransparency) in _trackedOccluders)
        {
            if (GodotObject.IsInstanceValid(geometry))
            {
                geometry.Transparency = originalTransparency;
            }
        }
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

    public void SetCover(bool inCover, bool lowCover)
    {
        _isInCover = inCover;
        _isInLowCover = inCover && lowCover;
    }

    public CameraOcclusionProbe ProbeOcclusion(Vector3 focus, Vector3 desiredCameraPosition)
    {
        float requestedDistance = focus.DistanceTo(desiredCameraPosition);
        float limitedDistance = ResolveCollisionDistance(
            focus,
            desiredCameraPosition,
            requestedDistance,
            out bool collided);
        int fadeOccluders = FindFadeOccluders(focus, desiredCameraPosition, null);
        return new CameraOcclusionProbe(
            requestedDistance,
            limitedDistance,
            fadeOccluders,
            collided);
    }

    private void UpdateCamera(double delta)
    {
        if (_target is null || _camera is null)
        {
            return;
        }

        float yaw = Mathf.DegToRad(YawDegrees);
        float height = FocusHeight - (_isCrouched ? CrouchHeightOffset : 0.0f) -
            (_isInLowCover ? LowCoverHeightOffset : 0.0f);
        Vector3 cameraRight = new(Mathf.Cos(yaw), 0.0f, -Mathf.Sin(yaw));
        Vector3 desiredFocus = _target.GlobalPosition + Vector3.Up * height +
            cameraRight * (_isInCover ? CoverLateralOffset : 0.0f);
        float transitionBlend = 1.0f - Mathf.Exp(-AimTransitionSpeed * (float)delta);
        _requestedDistance = Mathf.Lerp(
            _requestedDistance,
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
        float horizontal = Mathf.Cos(pitch);
        Vector3 offset = new(
            Mathf.Sin(yaw) * horizontal,
            Mathf.Sin(pitch),
            Mathf.Cos(yaw) * horizontal);

        Vector3 desiredCameraPosition = _smoothedFocus + offset * _requestedDistance;
        float collisionDistance = ResolveCollisionDistance(
            _smoothedFocus,
            desiredCameraPosition,
            _requestedDistance,
            out bool collided);
        IsArmOccluded = collided;
        if (collisionDistance < _currentDistance)
        {
            _currentDistance = collisionDistance;
        }
        else
        {
            float recoveryBlend = 1.0f - Mathf.Exp(
                -OcclusionRecoverySharpness * (float)delta);
            _currentDistance = Mathf.Lerp(_currentDistance, collisionDistance, recoveryBlend);
        }

        UpdateOccluderTransparency(
            _smoothedFocus,
            desiredCameraPosition,
            (float)delta);
        _camera.GlobalPosition = _smoothedFocus + offset * _currentDistance;
        _camera.LookAt(_smoothedFocus, Vector3.Up);
    }

    private float ResolveCollisionDistance(
        Vector3 focus,
        Vector3 desiredCameraPosition,
        float requestedDistance,
        out bool collided)
    {
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
            focus,
            desiredCameraPosition,
            CameraCollisionLayer);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        collided = result.Count > 0;
        if (!collided)
        {
            return requestedDistance;
        }

        Vector3 hitPosition = result["position"].AsVector3();
        return Mathf.Clamp(
            focus.DistanceTo(hitPosition) - CollisionPadding,
            MinimumCollisionDistance,
            requestedDistance);
    }

    private void UpdateOccluderTransparency(
        Vector3 focus,
        Vector3 desiredCameraPosition,
        float deltaSeconds)
    {
        _activeOccluders.Clear();
        FindFadeOccluders(focus, desiredCameraPosition, _activeOccluders);
        foreach (GeometryInstance3D geometry in _activeOccluders)
        {
            if (!_trackedOccluders.ContainsKey(geometry))
            {
                _trackedOccluders.Add(geometry, geometry.Transparency);
            }
        }

        foreach ((GeometryInstance3D geometry, float originalTransparency) in _trackedOccluders)
        {
            if (!GodotObject.IsInstanceValid(geometry))
            {
                continue;
            }

            float targetTransparency = _activeOccluders.Contains(geometry)
                ? Mathf.Max(originalTransparency, OccluderTransparency)
                : originalTransparency;
            geometry.Transparency = Mathf.MoveToward(
                geometry.Transparency,
                targetTransparency,
                OccluderFadeSpeed * deltaSeconds);
        }
    }

    private int FindFadeOccluders(
        Vector3 focus,
        Vector3 desiredCameraPosition,
        HashSet<GeometryInstance3D>? geometry)
    {
        Godot.Collections.Array<Rid> excluded = new();
        int occluderCount = 0;
        for (int hitIndex = 0; hitIndex < 8; hitIndex++)
        {
            PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
                focus,
                desiredCameraPosition,
                CameraFadeLayer,
                excluded);
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;
            Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count == 0)
            {
                break;
            }

            Rid rid = result["rid"].AsRid();
            excluded.Add(rid);
            Node? collider = result["collider"].AsGodotObject() as Node;
            if (collider?.IsInGroup("camera_fade_occluder") != true)
            {
                continue;
            }

            occluderCount++;
            if (geometry is not null)
            {
                CollectGeometry(collider, geometry);
            }
        }

        return occluderCount;
    }

    private static void CollectGeometry(Node node, ISet<GeometryInstance3D> geometry)
    {
        if (node is GeometryInstance3D instance)
        {
            geometry.Add(instance);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectGeometry(child, geometry);
        }
    }
}

public readonly record struct CameraOcclusionProbe(
    float RequestedDistance,
    float LimitedDistance,
    int FadeOccluderCount,
    bool HasCollision);
