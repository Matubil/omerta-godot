using System;
using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class CounterattackerController : CharacterBody3D
{
    [Export]
    public float MovementSpeed { get; set; } = 3.2f;

    [Export]
    public float ObjectiveRadius { get; set; } = 1.2f;

    [Export]
    public float AttackDistance { get; set; } = 7.5f;

    [Export]
    public float DecisionInterval { get; set; } = 0.85f;

    private Combatant? _combatant;
    private Combatant? _player;
    private HitscanWeaponController? _weapon;
    private CollisionShape3D? _collision;
    private Node3D? _visual;
    private Vector3 _objective;
    private double _nextDecisionTime;
    private bool _atObjective;

    public event Action<bool>? ObjectivePresenceChanged;

    public event Action? Defeated;

    public bool IsActive { get; private set; }

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _weapon = GetNode<HitscanWeaponController>("WeaponController");
        _collision = GetNode<CollisionShape3D>("CollisionShape3D");
        _visual = GetNode<Node3D>("Visual");
        _combatant.Defeated += OnDefeated;
        Deactivate();
    }

    public override void _ExitTree()
    {
        if (_combatant is not null)
        {
            _combatant.Defeated -= OnDefeated;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _combatant?.IsDefeated != false)
        {
            Velocity = Vector3.Zero;
            return;
        }

        _player ??= (GetTree().GetFirstNodeInGroup("player") as Node)?
            .GetNodeOrNull<Combatant>("Combatant");
        Vector3 direction = _objective - GlobalPosition;
        direction.Y = 0.0f;
        bool reached = direction.LengthSquared() <= ObjectiveRadius * ObjectiveRadius;
        if (reached != _atObjective)
        {
            _atObjective = reached;
            ObjectivePresenceChanged?.Invoke(_atObjective);
        }

        Vector3 horizontalVelocity = reached || direction.LengthSquared() < 0.001f
            ? Vector3.Zero
            : direction.Normalized() * MovementSpeed;
        if (direction.LengthSquared() > 0.001f)
        {
            LookAt(GlobalPosition + direction, Vector3.Up);
        }

        TryShootPlayer();
        Vector3 velocity = Velocity;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;
        velocity.Y = IsOnFloor() ? -0.5f : velocity.Y - 20.0f * (float)delta;
        Velocity = velocity;
        MoveAndSlide();
    }

    public void Activate(Vector3 spawnPosition, Vector3 objective)
    {
        GlobalPosition = spawnPosition;
        _objective = objective;
        _atObjective = false;
        Velocity = Vector3.Zero;
        Visible = true;
        IsActive = true;
        ProcessMode = ProcessModeEnum.Inherit;
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        AddToGroup("hostile");
        _combatant?.RestoreToFull();
        _weapon?.ResetRuntime();
        if (_visual is not null)
        {
            _visual.Rotation = Vector3.Zero;
        }
    }

    public void Deactivate()
    {
        if (_atObjective)
        {
            ObjectivePresenceChanged?.Invoke(false);
        }

        _atObjective = false;
        IsActive = false;
        Visible = false;
        Velocity = Vector3.Zero;
        RemoveFromGroup("hostile");
        _collision?.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    private void TryShootPlayer()
    {
        if (_player?.IsDefeated != false || _weapon is null ||
            GlobalPosition.DistanceSquaredTo(_player.Actor.GlobalPosition) > AttackDistance * AttackDistance)
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
        Velocity = Vector3.Zero;
        if (_atObjective)
        {
            _atObjective = false;
            ObjectivePresenceChanged?.Invoke(false);
        }

        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }

        Defeated?.Invoke();
    }
}
