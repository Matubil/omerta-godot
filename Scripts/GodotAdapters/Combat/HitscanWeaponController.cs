using System;
using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.Domain.Combat.Weapons;

namespace Omerta.GodotAdapters.Combat;

public sealed partial class HitscanWeaponController : Node
{
    [Export]
    public WeaponDefinitionResource Definition { get; set; } = null!;

    [Export]
    public int InitialReserveAmmo { get; set; } = 28;

    [Export]
    public uint CollisionMask { get; set; } = 1;

    private readonly RandomNumberGenerator _random = new();
    private CharacterBody3D? _ownerBody;
    private Combatant? _owner;
    private Node3D? _muzzle;

    public event Action<Vector3, Vector3, bool>? ShotResolved;

    public WeaponRuntime Runtime { get; private set; } = null!;

    public Vector3 FireOriginPosition => _muzzle?.GlobalPosition ?? _ownerBody?.GlobalPosition ?? Vector3.Zero;

    public override void _Ready()
    {
        _ownerBody = GetParent<CharacterBody3D>();
        _owner = _ownerBody.GetNode<Combatant>("Combatant");
        _muzzle = _ownerBody.GetNode<Node3D>("Visual/Weapon/Muzzle");
        Runtime = new WeaponRuntime(Definition.CreateSpec(), InitialReserveAmmo);
        _random.Randomize();
    }

    public override void _Process(double delta) => Runtime.Advance(CurrentTime);

    public bool TryFire(Vector3 direction, bool isAiming)
    {
        if (_owner?.IsDefeated != false || direction.LengthSquared() < 0.0001f ||
            !Runtime.TryFire(CurrentTime))
        {
            return false;
        }

        Vector3 shotDirection = ApplySpread(
            direction.Normalized(),
            Runtime.Specification.GetSpreadDegrees(isAiming));
        ResolveRaycast(shotDirection);
        return true;
    }

    public bool TryReload() =>
        _owner?.IsDefeated == false && Runtime.TryStartReload(CurrentTime);

    public void ResetRuntime() => Runtime.Reset();

    public void RestoreRuntime(int magazineAmmo, int reserveAmmo) =>
        Runtime.Restore(magazineAmmo, reserveAmmo);

    private double CurrentTime => Time.GetTicksMsec() / 1000.0;

    private Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
    {
        if (spreadDegrees <= 0.0f)
        {
            return direction;
        }

        float yaw = Mathf.DegToRad(_random.RandfRange(-spreadDegrees, spreadDegrees));
        float pitch = Mathf.DegToRad(_random.RandfRange(-spreadDegrees, spreadDegrees));
        Vector3 right = direction.Cross(Vector3.Up);
        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.Right;
        }

        return direction.Rotated(Vector3.Up, yaw).Rotated(right.Normalized(), pitch).Normalized();
    }

    private void ResolveRaycast(Vector3 direction)
    {
        if (_ownerBody is null || _owner is null)
        {
            return;
        }

        Vector3 origin = FireOriginPosition;
        Vector3 end = origin + direction * Runtime.Specification.EffectiveRange;
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end);
        query.CollisionMask = CollisionMask;
        query.Exclude = new Godot.Collections.Array<Rid> { _ownerBody.GetRid() };
        Godot.Collections.Dictionary result = _ownerBody.GetWorld3D().DirectSpaceState.IntersectRay(query);

        bool didHit = result.Count > 0;
        Vector3 traceEnd = didHit ? result["position"].AsVector3() : end;
        if (didHit)
        {
            Node? collider = result["collider"].AsGodotObject() as Node;
            Combatant? target = Combatant.FindFromCollider(collider);
            if (target is not null && target != _owner)
            {
                target.ReceiveDamage(new DamageInfo(Runtime.Specification.Damage, _owner.CombatantId));
            }
        }

        SpawnTracer(origin, traceEnd);
        ShotResolved?.Invoke(origin, traceEnd, didHit);
    }

    private void SpawnTracer(Vector3 origin, Vector3 end)
    {
        float length = origin.DistanceTo(end);
        if (length <= 0.001f)
        {
            return;
        }

        StandardMaterial3D material = new()
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1.0f, 0.78f, 0.22f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.46f, 0.05f)
        };
        MeshInstance3D tracer = new()
        {
            Mesh = new BoxMesh { Size = new Vector3(0.025f, 0.025f, length) },
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        GetTree().CurrentScene.AddChild(tracer);
        tracer.GlobalPosition = origin.Lerp(end, 0.5f);
        tracer.LookAt(end, Vector3.Up);
        GetTree().CreateTimer(0.09).Timeout += tracer.QueueFree;
    }
}
