using Godot;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Navigation;

namespace Omerta.GodotAdapters.Squad;

public enum SquadCompanionOrder
{
    Follow,
    Move,
    Attack
}

public sealed partial class SquadCompanionController : CharacterBody3D
{
    [Export]
    public bool StartsActive { get; set; } = true;

    [Export]
    public float MovementSpeed { get; set; } = 3.8f;

    [Export]
    public float FollowDistance { get; set; } = 2.2f;

    [Export]
    public float AttackDistance { get; set; } = 8.0f;

    [Export]
    public float DecisionInterval { get; set; } = 0.7f;

    private Combatant? _combatant;
    private HitscanWeaponController? _weapon;
    private Node3D? _leader;
    private Combatant? _attackTarget;
    private Vector3 _destination;
    private double _nextDecisionTime;
    private CollisionShape3D? _collision;
    private NavigationAgent3D? _navigation;

    public SquadCompanionOrder CurrentOrder { get; private set; } = SquadCompanionOrder.Follow;

    public bool IsActive { get; private set; }

    public bool IsDefeated => _combatant?.IsDefeated == true;

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _weapon = GetNode<HitscanWeaponController>("WeaponController");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");
        _navigation = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _leader = GetTree().GetFirstNodeInGroup("player") as Node3D;
        if (StartsActive)
        {
            Activate(GlobalPosition);
        }
        else
        {
            Deactivate();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _combatant?.IsDefeated != false)
        {
            Velocity = Vector3.Zero;
            return;
        }

        _leader ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
        Vector3 destination = GlobalPosition;
        bool shouldMove = false;
        if (CurrentOrder == SquadCompanionOrder.Follow && _leader is not null)
        {
            destination = _leader.GlobalPosition + Vector3.Right * 1.25f + Vector3.Back * 1.4f;
            shouldMove = GlobalPosition.DistanceSquaredTo(destination) > FollowDistance * FollowDistance;
        }
        else if (CurrentOrder == SquadCompanionOrder.Move)
        {
            destination = _destination;
            shouldMove = GlobalPosition.DistanceSquaredTo(destination) > 0.8f * 0.8f;
        }
        else if (CurrentOrder == SquadCompanionOrder.Attack)
        {
            if (_attackTarget?.IsDefeated != false)
            {
                Regroup();
                return;
            }

            destination = _attackTarget.Actor.GlobalPosition;
            float distance = GlobalPosition.DistanceTo(destination);
            shouldMove = distance > AttackDistance;
            TryAttack(distance);
        }

        Vector3 direction = shouldMove && _navigation is not null
            ? NavigationMotion.DirectionTo(this, _navigation, destination)
            : destination - GlobalPosition;
        direction.Y = 0.0f;
        Vector3 velocity = Velocity;
        if (shouldMove && direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();
            velocity.X = direction.X * MovementSpeed;
            velocity.Z = direction.Z * MovementSpeed;
            LookAt(GlobalPosition + direction, Vector3.Up);
        }
        else
        {
            velocity.X = 0.0f;
            velocity.Z = 0.0f;
        }

        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - 20.0f * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
    }

    public void Activate(Vector3 position)
    {
        GlobalPosition = position;
        Visible = true;
        IsActive = true;
        ProcessMode = ProcessModeEnum.Inherit;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        AddToGroup("companion");
        _combatant?.RestoreToFull();
        _weapon?.ResetRuntime();
        if (_navigation is not null)
        {
            _navigation.TargetPosition = position;
        }

        Regroup();
    }

    public void Deactivate()
    {
        IsActive = false;
        Visible = false;
        Velocity = Vector3.Zero;
        RemoveFromGroup("companion");
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public void MoveTo(Vector3 destination)
    {
        _destination = destination;
        _attackTarget = null;
        CurrentOrder = SquadCompanionOrder.Move;
    }

    public bool Attack(Combatant target)
    {
        if (!IsActive || IsDefeated || target.IsDefeated)
        {
            return false;
        }

        _attackTarget = target;
        CurrentOrder = SquadCompanionOrder.Attack;
        return true;
    }

    public void Regroup()
    {
        _attackTarget = null;
        CurrentOrder = SquadCompanionOrder.Follow;
    }

    private void TryAttack(float distance)
    {
        if (_weapon is null || _attackTarget is null || distance > AttackDistance)
        {
            return;
        }

        Vector3 targetPoint = _attackTarget.Actor.GlobalPosition + Vector3.Up * 0.9f;
        Vector3 direction = targetPoint - _weapon.FireOriginPosition;
        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + direction, Vector3.Up);
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
            _weapon.TryFire(targetPoint - _weapon.FireOriginPosition, true);
        }
    }
}
