using Godot;

namespace Omerta.GodotAdapters.Characters;

public enum CoverType
{
    None,
    Low,
    High
}

public sealed partial class PlayerCoverController : Node
{
    [Export]
    public float DetectionDistance { get; set; } = 1.35f;

    [Export]
    public float MovementSpeed { get; set; } = 2.6f;

    [Export]
    public float SnapSpeed { get; set; } = 14.0f;

    [Export]
    public float LowProbeHeight { get; set; } = 0.65f;

    [Export]
    public float HighProbeHeight { get; set; } = 1.45f;

    [Export]
    public float SurfaceOffset { get; set; } = 0.43f;

    private PlayerController? _player;
    private Rid _surfaceRid;

    public bool IsInCover { get; private set; }

    public CoverType CurrentType { get; private set; }

    public Vector3 SurfaceNormal { get; private set; }

    public override void _Ready() => _player = GetParent<PlayerController>();

    public bool ToggleCover(Vector3 facingDirection)
    {
        if (IsInCover)
        {
            ExitCover();
            return true;
        }

        return TryEnterCover(facingDirection);
    }

    public bool TryEnterCover(Vector3 direction)
    {
        if (_player is null || !TryProbe(_player.GlobalPosition, direction, null, out CoverContact contact))
        {
            return false;
        }

        IsInCover = true;
        CurrentType = contact.Type;
        SurfaceNormal = contact.Normal;
        _surfaceRid = contact.SurfaceRid;
        _player.GlobalPosition = contact.SnapPosition;
        return true;
    }

    public bool TryGetMotion(
        float lateralInput,
        double delta,
        out Vector3 planarVelocity,
        out Vector3 facingDirection)
    {
        planarVelocity = Vector3.Zero;
        facingDirection = SurfaceNormal;
        if (_player is null || !IsInCover)
        {
            return false;
        }

        Vector3 towardSurface = -SurfaceNormal;
        if (!TryProbe(_player.GlobalPosition, towardSurface, _surfaceRid, out CoverContact current))
        {
            ExitCover();
            return false;
        }

        CurrentType = current.Type;
        SurfaceNormal = current.Normal;
        facingDirection = SurfaceNormal;

        Vector3 tangent = SurfaceNormal.Cross(Vector3.Up).Normalized();
        Vector3 lateralVelocity = tangent * lateralInput * MovementSpeed;
        if (Mathf.Abs(lateralInput) > 0.01f)
        {
            Vector3 candidate = _player.GlobalPosition + lateralVelocity * (float)delta;
            if (!TryProbe(candidate, -SurfaceNormal, _surfaceRid, out _))
            {
                lateralVelocity = Vector3.Zero;
            }
        }

        Vector3 correction = current.SnapPosition - _player.GlobalPosition;
        correction.Y = 0.0f;
        Vector3 correctionVelocity = correction * SnapSpeed;
        if (correctionVelocity.Length() > SnapSpeed)
        {
            correctionVelocity = correctionVelocity.Normalized() * SnapSpeed;
        }

        planarVelocity = lateralVelocity + correctionVelocity;
        return true;
    }

    public void ExitCover()
    {
        IsInCover = false;
        CurrentType = CoverType.None;
        SurfaceNormal = Vector3.Zero;
        _surfaceRid = default;
    }

    private bool TryProbe(
        Vector3 feetPosition,
        Vector3 direction,
        Rid? requiredSurface,
        out CoverContact contact)
    {
        contact = default;
        if (_player is null)
        {
            return false;
        }

        Vector3 horizontalDirection = new(direction.X, 0.0f, direction.Z);
        if (horizontalDirection.LengthSquared() < 0.001f)
        {
            return false;
        }

        horizontalDirection = horizontalDirection.Normalized();
        if (!TryRay(feetPosition + Vector3.Up * LowProbeHeight, horizontalDirection, out Godot.Collections.Dictionary lowHit))
        {
            return false;
        }

        Vector3 normal = lowHit["normal"].AsVector3();
        normal.Y = 0.0f;
        if (normal.LengthSquared() < 0.5f)
        {
            return false;
        }

        normal = normal.Normalized();
        if (normal.Dot(-horizontalDirection) < 0.45f)
        {
            return false;
        }

        Rid surfaceRid = lowHit["rid"].AsRid();
        if (requiredSurface.HasValue && surfaceRid != requiredSurface.Value)
        {
            return false;
        }

        CoverType type = CoverType.Low;
        if (TryRay(feetPosition + Vector3.Up * HighProbeHeight, horizontalDirection, out Godot.Collections.Dictionary highHit) &&
            highHit["rid"].AsRid() == surfaceRid)
        {
            type = CoverType.High;
        }

        Vector3 hitPoint = lowHit["position"].AsVector3();
        Vector3 snapPosition = hitPoint + normal * SurfaceOffset;
        snapPosition.Y = feetPosition.Y;
        Vector3 supportOrigin = snapPosition + Vector3.Up * 0.4f;
        if (!TryRay(supportOrigin, Vector3.Down, 1.1f, out Godot.Collections.Dictionary supportHit) ||
            supportHit["normal"].AsVector3().Dot(Vector3.Up) < 0.65f)
        {
            return false;
        }

        snapPosition.Y = supportHit["position"].AsVector3().Y + 0.02f;
        contact = new CoverContact(surfaceRid, normal, snapPosition, type);
        return true;
    }

    private bool TryRay(
        Vector3 origin,
        Vector3 direction,
        out Godot.Collections.Dictionary result) =>
        TryRay(origin, direction, DetectionDistance, out result);

    private bool TryRay(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out Godot.Collections.Dictionary result)
    {
        Godot.Collections.Array<Rid> excluded = new() { _player!.GetRid() };
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
            origin,
            origin + direction.Normalized() * distance,
            1u,
            excluded);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        result = _player.GetWorld3D().DirectSpaceState.IntersectRay(query);
        return result.Count > 0;
    }

    private readonly record struct CoverContact(
        Rid SurfaceRid,
        Vector3 Normal,
        Vector3 SnapPosition,
        CoverType Type);
}
