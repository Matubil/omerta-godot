using Godot;
using Omerta.Domain.Combat.AI;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.AI;

public sealed partial class RivalController : CharacterBody3D
{
    [Export]
    public float ViewDistance { get; set; } = 13.0f;

    [Export]
    public float ViewAngleDegrees { get; set; } = 100.0f;

    [Export]
    public float DetectionTime { get; set; } = 1.2f;

    [Export]
    public float DetectionLossPerSecond { get; set; } = 0.75f;

    [Export]
    public float MovementSpeed { get; set; } = 2.25f;

    [Export]
    public float StoppingDistance { get; set; } = 7.0f;

    [Export]
    public float DecisionInterval { get; set; } = 0.9f;

    [Export]
    public float Gravity { get; set; } = 20.0f;

    private EnemyPerceptionState _perception = null!;
    private Combatant? _combatant;
    private Combatant? _target;
    private HitscanWeaponController? _weapon;
    private Node3D? _visual;
    private HudController? _hud;
    private Vector3 _lastKnownPosition;
    private bool _hasLastKnownPosition;
    private double _nextDecisionTime;

    public EnemyAwareness Awareness => _perception.Awareness;

    public bool IsTargetVisible { get; private set; }

    public override void _Ready()
    {
        AddToGroup("rival");
        _perception = new EnemyPerceptionState(DetectionTime, DetectionLossPerSecond);
        _combatant = GetNode<Combatant>("Combatant");
        _weapon = GetNode<HitscanWeaponController>("WeaponController");
        _visual = GetNode<Node3D>("Visual");
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _combatant.Damaged += OnDamaged;
        _combatant.Defeated += OnDefeated;
    }

    public override void _ExitTree()
    {
        if (_combatant is null)
        {
            return;
        }

        _combatant.Damaged -= OnDamaged;
        _combatant.Defeated -= OnDefeated;
    }

    public override void _PhysicsProcess(double delta)
    {
        _target ??= (GetTree().GetFirstNodeInGroup("player") as Node)?.GetNodeOrNull<Combatant>("Combatant");
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (_combatant is null || _target is null || _weapon is null)
        {
            return;
        }

        if (_combatant.IsDefeated || _target.IsDefeated)
        {
            Velocity = Vector3.Zero;
            UpdateHud();
            return;
        }

        IsTargetVisible = EvaluateTargetVisibility();
        if (IsTargetVisible)
        {
            _lastKnownPosition = _target.Actor.GlobalPosition;
            _hasLastKnownPosition = true;
        }

        _perception.Observe(IsTargetVisible, (float)delta);
        Vector3 horizontalVelocity = Vector3.Zero;
        if (_perception.IsAlert && _hasLastKnownPosition)
        {
            Vector3 direction = _lastKnownPosition - GlobalPosition;
            direction.Y = 0.0f;
            float distance = direction.Length();
            if (direction.LengthSquared() > 0.001f)
            {
                LookAt(GlobalPosition + direction, Vector3.Up);
            }

            if (distance > StoppingDistance && distance <= ViewDistance)
            {
                horizontalVelocity = direction.Normalized() * MovementSpeed;
            }

            if (IsTargetVisible && distance <= ViewDistance)
            {
                TryShoot(direction);
            }
        }

        Vector3 velocity = Velocity;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - Gravity * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
        UpdateHud();
    }

    public void ResetCombat(Vector3 position, Vector3 rotation)
    {
        GlobalPosition = position;
        Rotation = rotation;
        Velocity = Vector3.Zero;
        _combatant?.RestoreToFull();
        _weapon?.ResetRuntime();
        _perception.Reset();
        _hasLastKnownPosition = false;
        IsTargetVisible = false;
        _nextDecisionTime = 0;
        if (_visual is not null)
        {
            _visual.Rotation = Vector3.Zero;
        }
    }

    private bool EvaluateTargetVisibility()
    {
        if (_target is null)
        {
            return false;
        }

        Vector3 eye = GlobalPosition + Vector3.Up;
        Vector3 targetPoint = _target.Actor.GlobalPosition + Vector3.Up * 0.9f;
        Vector3 direction = targetPoint - eye;
        float distance = direction.Length();
        if (distance <= 0.001f || distance > ViewDistance)
        {
            return false;
        }

        Vector3 flatDirection = new(direction.X, 0.0f, direction.Z);
        Vector3 forward = -GlobalBasis.Z;
        forward.Y = 0.0f;
        if (Mathf.RadToDeg(forward.AngleTo(flatDirection.Normalized())) > ViewAngleDegrees * 0.5f)
        {
            return false;
        }

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(eye, targetPoint);
        query.CollisionMask = 1;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0)
        {
            return false;
        }

        Node? collider = result["collider"].AsGodotObject() as Node;
        return Combatant.FindFromCollider(collider) == _target;
    }

    private void TryShoot(Vector3 horizontalDirection)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _nextDecisionTime || _target is null || _weapon is null)
        {
            return;
        }

        _nextDecisionTime = now + DecisionInterval;
        if (_weapon.Runtime.AmmoInMagazine == 0)
        {
            _weapon.TryReload();
            return;
        }

        Vector3 targetPoint = _target.Actor.GlobalPosition + Vector3.Up * 0.9f;
        _weapon.TryFire(targetPoint - _weapon.FireOriginPosition, true);
    }

    private void OnDamaged(DamageInfo damage) => _perception.ForceAlert();

    private void OnDefeated(DamageInfo damage)
    {
        Velocity = Vector3.Zero;
        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }

        _hud?.ShowStatus("Rival derrotado. [T] reinicia el encuentro.", 5.0f);
    }

    private void UpdateHud()
    {
        if (_combatant?.Health is null)
        {
            return;
        }

        _hud?.SetEnemyState(
            _combatant.Health.Current,
            _combatant.Health.Maximum,
            _perception.Awareness,
            _perception.DetectionProgress);
    }
}
