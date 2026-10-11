using Godot;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Characters;

public sealed partial class PlayerTraversalController : Node
{
    [Export]
    public float MinimumVaultHeight { get; set; } = 0.4f;

    [Export]
    public float MaximumVaultHeight { get; set; } = 1.25f;

    [Export]
    public float CheckDistance { get; set; } = 1.25f;

    [Export]
    public float MaximumVaultWidth { get; set; } = 1.5f;

    [Export]
    public float VaultDuration { get; set; } = 0.55f;

    private PlayerController? _player;
    private CollisionShape3D? _collision;
    private PlayerCombatController? _combat;
    private Vector3 _start;
    private Vector3 _apex;
    private Vector3 _end;
    private float _elapsed;

    public bool IsVaulting { get; private set; }

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _collision = _player.GetNode<CollisionShape3D>("CollisionShape3D");
        _combat = _player.GetNodeOrNull<PlayerCombatController>("PlayerCombatController");
    }

    public bool TryVault(Vector3 direction)
    {
        if (_player is null || IsVaulting || !TryFindVault(direction, out Vector3 apex, out Vector3 landing))
        {
            return false;
        }

        if (!_player.TryStandForTraversal())
        {
            return false;
        }

        _start = _player.GlobalPosition;
        _apex = apex;
        _end = landing;
        _elapsed = 0.0f;
        IsVaulting = true;
        _player.Velocity = Vector3.Zero;
        _combat?.SetTraversalBlocked(true);
        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        }

        return true;
    }

    public void Advance(double delta)
    {
        if (!IsVaulting || _player is null)
        {
            return;
        }

        _elapsed += (float)delta;
        float normalized = Mathf.Clamp(_elapsed / Mathf.Max(VaultDuration, 0.05f), 0.0f, 1.0f);
        if (normalized < 0.5f)
        {
            _player.GlobalPosition = _start.Lerp(_apex, SmoothStep(normalized * 2.0f));
        }
        else
        {
            _player.GlobalPosition = _apex.Lerp(_end, SmoothStep((normalized - 0.5f) * 2.0f));
        }

        if (normalized >= 1.0f)
        {
            FinishVault();
        }
    }

    public void ResetTraversal()
    {
        if (!IsVaulting)
        {
            return;
        }

        FinishVault();
    }

    private bool TryFindVault(Vector3 direction, out Vector3 apex, out Vector3 landing)
    {
        apex = Vector3.Zero;
        landing = Vector3.Zero;
        if (_player is null)
        {
            return false;
        }

        Vector3 forward = new(direction.X, 0.0f, direction.Z);
        if (forward.LengthSquared() < 0.001f)
        {
            return false;
        }

        forward = forward.Normalized();
        float probeHeight = Mathf.Lerp(MinimumVaultHeight, MaximumVaultHeight, 0.35f);
        Vector3 frontOrigin = _player.GlobalPosition + Vector3.Up * probeHeight;
        if (!TryRay(frontOrigin, forward, CheckDistance, out Godot.Collections.Dictionary frontHit))
        {
            return false;
        }

        Rid obstacleRid = frontHit["rid"].AsRid();
        Vector3 frontPoint = frontHit["position"].AsVector3();
        Vector3 topOrigin = frontPoint + forward * 0.08f + Vector3.Up * (MaximumVaultHeight + 0.35f);
        if (!TryRay(topOrigin, Vector3.Down, MaximumVaultHeight + 0.7f, out Godot.Collections.Dictionary topHit) ||
            topHit["rid"].AsRid() != obstacleRid)
        {
            return false;
        }

        Vector3 topPoint = topHit["position"].AsVector3();
        float obstacleHeight = topPoint.Y - _player.GlobalPosition.Y;
        if (obstacleHeight < MinimumVaultHeight || obstacleHeight > MaximumVaultHeight)
        {
            return false;
        }

        Vector3 rearOrigin = frontPoint + forward * MaximumVaultWidth;
        rearOrigin.Y = frontOrigin.Y;
        if (!TryRay(rearOrigin, -forward, MaximumVaultWidth + 0.1f, out Godot.Collections.Dictionary rearHit) ||
            rearHit["rid"].AsRid() != obstacleRid)
        {
            return false;
        }

        Vector3 rearPoint = rearHit["position"].AsVector3();
        float radius = _player.CollisionRadius;
        Vector3 landingOrigin = rearPoint + forward * (radius + 0.3f);
        landingOrigin.Y = topPoint.Y + MaximumVaultHeight;
        if (!TryRay(landingOrigin, Vector3.Down, MaximumVaultHeight * 2.0f + 0.5f, out Godot.Collections.Dictionary landingHit))
        {
            return false;
        }

        if (landingHit["normal"].AsVector3().Dot(Vector3.Up) < 0.65f)
        {
            return false;
        }

        landing = landingHit["position"].AsVector3() + Vector3.Up * 0.02f;
        apex = new Vector3(
            (frontPoint.X + rearPoint.X) * 0.5f,
            topPoint.Y + 0.12f,
            (frontPoint.Z + rearPoint.Z) * 0.5f);
        return HasClearance(apex) && HasClearance(landing);
    }

    private bool HasClearance(Vector3 feetPosition)
    {
        if (_player is null)
        {
            return false;
        }

        float height = Mathf.Max(_player.StandingHeight - 0.1f, 0.5f);
        CapsuleShape3D capsule = new()
        {
            Radius = Mathf.Max(_player.CollisionRadius * 0.9f, 0.1f),
            Height = height
        };
        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = capsule,
            Transform = new Transform3D(Basis.Identity, feetPosition + Vector3.Up * (height * 0.5f + 0.05f)),
            CollisionMask = 1u,
            CollideWithAreas = false,
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() }
        };
        return _player.GetWorld3D().DirectSpaceState.IntersectShape(query, 8).Count == 0;
    }

    private bool TryRay(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out Godot.Collections.Dictionary result)
    {
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
            origin,
            origin + direction.Normalized() * distance,
            1u,
            new Godot.Collections.Array<Rid> { _player!.GetRid() });
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        result = _player.GetWorld3D().DirectSpaceState.IntersectRay(query);
        return result.Count > 0;
    }

    private void FinishVault()
    {
        IsVaulting = false;
        _elapsed = 0.0f;
        if (_player is not null)
        {
            _player.GlobalPosition = _end;
            _player.Velocity = Vector3.Zero;
        }

        _combat?.SetTraversalBlocked(false);
        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        }
    }

    private static float SmoothStep(float value) => value * value * (3.0f - 2.0f * value);
}
