using System;
using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Navigation;

namespace Omerta.GodotAdapters.Law;

public sealed partial class PoliceOfficerController : CharacterBody3D
{
    [Export]
    public float MovementSpeed { get; set; } = 3.0f;

    [Export]
    public float StoppingDistance { get; set; } = 6.0f;

    [Export]
    public float ViewDistance { get; set; } = 14.0f;

    [Export]
    public float DecisionInterval { get; set; } = 0.8f;

    private Combatant? _combatant;
    private Combatant? _player;
    private HitscanWeaponController? _weapon;
    private CollisionShape3D? _collision;
    private Node3D? _visual;
    private NavigationAgent3D? _navigation;
    private NavigationAvoidanceMotor? _avoidance;
    private Vector3 _lastKnownPosition;
    private double _nextDecisionTime;

    public event Action? Defeated;

    public bool IsActive { get; private set; }

    public bool HasPlayerLineOfSight { get; private set; }

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _weapon = GetNode<HitscanWeaponController>("WeaponController");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");
        _visual = GetNode<Node3D>("Visual");
        _navigation = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _avoidance = new NavigationAvoidanceMotor(_navigation, MovementSpeed, false);
        _combatant.Defeated += OnDefeated;
        Deactivate();
    }

    public override void _ExitTree()
    {
        if (_combatant is not null)
        {
            _combatant.Defeated -= OnDefeated;
        }

        _avoidance?.Dispose();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _combatant?.IsDefeated != false)
        {
            _avoidance?.ResolveVelocity(Vector3.Zero);
            Velocity = Vector3.Zero;
            return;
        }

        _player ??= (GetTree().GetFirstNodeInGroup("player") as Node)?
            .GetNodeOrNull<Combatant>("Combatant");
        HasPlayerLineOfSight = EvaluatePlayerVisibility();
        if (HasPlayerLineOfSight && _player is not null)
        {
            _lastKnownPosition = _player.Actor.GlobalPosition;
        }

        Vector3 directDirection = _lastKnownPosition - GlobalPosition;
        directDirection.Y = 0.0f;
        Vector3 direction = _navigation is not null
            ? NavigationMotion.DirectionTo(this, _navigation, _lastKnownPosition)
            : directDirection;
        direction.Y = 0.0f;
        float distance = directDirection.Length();
        Vector3 horizontalVelocity = Vector3.Zero;
        if (distance > StoppingDistance && direction.LengthSquared() > 0.001f)
        {
            horizontalVelocity = direction.Normalized() * MovementSpeed;
        }

        horizontalVelocity = _avoidance?.ResolveVelocity(horizontalVelocity) ?? horizontalVelocity;

        if (direction.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + direction, Vector3.Up);
        }

        if (HasPlayerLineOfSight)
        {
            TryShoot();
        }

        Vector3 velocity = Velocity;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - 20.0f * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
    }

    public void Activate(Vector3 spawnPosition, Vector3 lastKnownPosition)
    {
        GlobalPosition = spawnPosition;
        _lastKnownPosition = lastKnownPosition;
        Velocity = Vector3.Zero;
        Visible = true;
        IsActive = true;
        HasPlayerLineOfSight = false;
        ProcessMode = ProcessModeEnum.Inherit;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        AddToGroup("hostile");
        _combatant?.RestoreToFull();
        _weapon?.ResetRuntime();
        if (_navigation is not null)
        {
            _navigation.TargetPosition = lastKnownPosition;
        }

        _avoidance?.SetActive(true);

        if (_visual is not null)
        {
            _visual.Rotation = Vector3.Zero;
        }
    }

    public void UpdateLastKnownPosition(Vector3 position) => _lastKnownPosition = position;

    public void Deactivate()
    {
        IsActive = false;
        HasPlayerLineOfSight = false;
        Visible = false;
        Velocity = Vector3.Zero;
        _avoidance?.SetActive(false);
        RemoveFromGroup("hostile");
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    private bool EvaluatePlayerVisibility()
    {
        if (_player?.IsDefeated != false)
        {
            return false;
        }

        Vector3 eye = GlobalPosition + Vector3.Up;
        Vector3 targetPoint = _player.Actor.GlobalPosition + Vector3.Up * 0.9f;
        if (eye.DistanceSquaredTo(targetPoint) > ViewDistance * ViewDistance)
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
        return Combatant.FindFromCollider(collider) == _player;
    }

    private void TryShoot()
    {
        if (_player is null || _weapon is null)
        {
            return;
        }

        double now = Time.GetTicksMsec() / 1000.0;
        if (now < _nextDecisionTime)
        {
            return;
        }

        _nextDecisionTime = now + DecisionInterval;
        if (_weapon.Runtime.AmmoInMagazine == 0)
        {
            _weapon.TryReload();
        }
        else
        {
            Vector3 targetPoint = _player.Actor.GlobalPosition + Vector3.Up * 0.9f;
            _weapon.TryFire(targetPoint - _weapon.FireOriginPosition, true);
        }
    }

    private void OnDefeated(DamageInfo damage)
    {
        _avoidance?.ResolveVelocity(Vector3.Zero);
        Velocity = Vector3.Zero;
        HasPlayerLineOfSight = false;
        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }

        Defeated?.Invoke();
    }
}
