using System;
using Godot;
using Omerta.Domain.Economy;
using Omerta.Domain.Vehicles;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Input;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Vehicles;

public sealed partial class DriveableVehicleController : CharacterBody3D
{
    public event Action<DriveableVehicleController>? UnauthorizedUseStarted;

    [Export]
    public string VehicleId { get; set; } = "vehicle.stolen.prototype";

    [Export]
    public string DisplayName { get; set; } = "Sedán de calle";

    [Export]
    public bool StartsFamilyOwned { get; set; }

    [Export]
    public bool PersistState { get; set; } = true;

    [Export]
    public bool StartsDriveEnabled { get; set; } = true;

    [Export]
    public float MaximumForwardSpeed { get; set; } = 11.0f;

    [Export]
    public float MaximumReverseSpeed { get; set; } = 4.5f;

    [Export]
    public float Acceleration { get; set; } = 10.0f;

    [Export]
    public float Braking { get; set; } = 15.0f;

    [Export]
    public float CoastingFriction { get; set; } = 7.0f;

    [Export]
    public float SteeringDegreesPerSecond { get; set; } = 88.0f;

    [Export]
    public int MaximumCondition { get; set; } = 100;

    [Export]
    public float MinimumDamagingImpactSpeed { get; set; } = 4.0f;

    [Export]
    public float DamagePerImpactSpeed { get; set; } = 9.0f;

    private PlayerInputAdapter? _input;
    private HudController? _hud;
    private Label3D? _ownershipLabel;
    private MeshInstance3D? _body;
    private float _speed;
    private float _entryCooldown;
    private float _impactCooldown;
    private bool _driveEnabled;
    private Vector3 _initialPosition;
    private Vector3 _initialRotation;

    public VehicleOwnership Ownership { get; private set; } = null!;
    public VehicleCondition Condition { get; private set; } = null!;
    public PlayerController? Driver { get; private set; }
    public bool IsFamilyOwned => Ownership.IsFamilyOwned;
    public bool IsDriven => Driver is not null;
    public bool CanEnterVehicle => Visible && _driveEnabled && Driver is null &&
        !Condition.IsDisabled;
    public bool CanUseArsenal => Visible && _driveEnabled && Driver is null &&
        IsFamilyOwned && Mathf.Abs(_speed) < 0.2f;
    public bool IsPoliceMarked { get; private set; }
    public bool IsDisabled => Condition.IsDisabled;
    public int CurrentCondition => Condition.Current;
    public int MaximumVehicleCondition => Condition.Maximum;
    public float CurrentSpeed => _speed;

    public override void _Ready()
    {
        AddToGroup("driveable_vehicle");
        Ownership = new VehicleOwnership(VehicleId, StartsFamilyOwned);
        Condition = new VehicleCondition(
            MaximumCondition,
            MinimumDamagingImpactSpeed,
            DamagePerImpactSpeed);
        Ownership.Changed += RefreshOwnershipPresentation;
        Condition.Changed += OnConditionChanged;
        _ownershipLabel = GetNodeOrNull<Label3D>("OwnershipLabel");
        _body = GetNodeOrNull<MeshInstance3D>("Body");
        _input = GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _hud = GetTree().GetFirstNodeInGroup("hud") as HudController;
        _driveEnabled = StartsDriveEnabled;
        _initialPosition = GlobalPosition;
        _initialRotation = Rotation;
        RefreshOwnershipPresentation();
    }

    public override void _ExitTree()
    {
        if (Ownership is not null)
        {
            Ownership.Changed -= RefreshOwnershipPresentation;
        }

        if (Condition is not null)
        {
            Condition.Changed -= OnConditionChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _entryCooldown = Mathf.Max(0.0f, _entryCooldown - (float)delta);
        _impactCooldown = Mathf.Max(0.0f, _impactCooldown - (float)delta);
        if (Driver is null)
        {
            Velocity = Vector3.Zero;
            return;
        }

        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        if (_entryCooldown <= 0.0f && _input?.InteractPressed == true)
        {
            ForceExitDriver();
            return;
        }

        float throttle = -(_input?.Movement.Y ?? 0.0f);
        float steering = _input?.Movement.X ?? 0.0f;
        AdvanceDriving(throttle, steering, (float)delta);
        Driver?.SynchronizeWithVehicle(GlobalPosition + Vector3.Up * 0.25f, Rotation.Y);
        _hud?.SetMovementState(
            false,
            false,
            $"CONDUCIENDO · {Mathf.Abs(_speed):0.0} m/s · MOTOR {Condition.Ratio * 100.0f:0}%");
    }

    public bool TryEnterDriver(PlayerController player)
    {
        if (!CanEnterVehicle || player.IsDriving)
        {
            return false;
        }

        Driver = player;
        _entryCooldown = 0.3f;
        player.EnterVehicleMode(this);
        player.GetNode<PlayerWeaponLoadoutController>("WeaponLoadout").Loadout.SetHolstered(true);
        player.SynchronizeWithVehicle(GlobalPosition + Vector3.Up * 0.25f, Rotation.Y);
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _hud?.ShowStatus(
            IsFamilyOwned
                ? $"Entraste a {DisplayName}."
                : $"Vehículo robado: llevalo al garaje de El Faro para registrarlo.",
            4.0f);
        if (!IsFamilyOwned && !IsPoliceMarked)
        {
            UnauthorizedUseStarted?.Invoke(this);
        }

        return true;
    }

    public void ForceExitDriver(bool placeBesideVehicle = true)
    {
        if (Driver is null)
        {
            return;
        }

        PlayerController player = Driver;
        Driver = null;
        _speed = 0.0f;
        Velocity = Vector3.Zero;
        Vector3 exitPosition = placeBesideVehicle
            ? GlobalPosition + GlobalBasis.X.Normalized() * 1.8f + Vector3.Up * 0.05f
            : player.GlobalPosition;
        player.ExitVehicleMode(exitPosition);
        _entryCooldown = 0.3f;
    }

    public void SetDriveEnabled(bool enabled)
    {
        _driveEnabled = enabled;
        if (!enabled)
        {
            ForceExitDriver();
            _speed = 0.0f;
            Velocity = Vector3.Zero;
        }
    }

    public VehicleRegistrationResult TryRegisterAtGarage(
        bool garageAvailable,
        EconomyService economy,
        int cost)
    {
        if (IsPoliceMarked)
        {
            return VehicleRegistrationResult.PoliceSearchActive;
        }

        return Ownership.TryRegister(garageAvailable, economy, cost);
    }

    public void MarkAsReportedStolen()
    {
        if (IsFamilyOwned || IsPoliceMarked)
        {
            return;
        }

        IsPoliceMarked = true;
        RefreshOwnershipPresentation();
    }

    public void ClearPoliceMark()
    {
        if (!IsPoliceMarked)
        {
            return;
        }

        IsPoliceMarked = false;
        RefreshOwnershipPresentation();
    }

    public int ApplyPoliceRamDamage(int damage)
    {
        int applied = Condition.ApplyDamage(damage);
        if (applied > 0)
        {
            _speed *= 0.45f;
            DamageDriver(Math.Max(1, applied / 6), "vehicle.police-impact");
            ShowDamageStatus("El patrullero golpeó el vehículo", applied);
            HandleDisabledEngine();
        }

        return applied;
    }

    public bool RepairToFull()
    {
        bool repaired = Condition.RepairToFull();
        if (repaired)
        {
            RefreshOwnershipPresentation();
        }

        return repaired;
    }

    public void ResetCondition()
    {
        Condition.RepairToFull();
        _impactCooldown = 0.0f;
        RefreshOwnershipPresentation();
    }

    public void RestorePersistentState(Vector3 position, Quaternion rotation, bool familyOwned)
    {
        ForceExitDriver(false);
        ClearPoliceMark();
        ResetCondition();
        Ownership.Restore(familyOwned);
        GlobalTransform = new Transform3D(new Basis(rotation.Normalized()), position);
        _speed = 0.0f;
        Velocity = Vector3.Zero;
    }

    public void ResetPersistentState()
    {
        ForceExitDriver(false);
        ClearPoliceMark();
        ResetCondition();
        Ownership.Reset();
        GlobalPosition = _initialPosition;
        Rotation = _initialRotation;
        _speed = 0.0f;
        Velocity = Vector3.Zero;
    }

    public void AdvanceDrivingForDiagnostics(float throttle, float steering, float deltaSeconds) =>
        AdvanceDriving(throttle, steering, deltaSeconds);

    private void AdvanceDriving(float throttle, float steering, float deltaSeconds)
    {
        throttle = Mathf.Clamp(throttle, -1.0f, 1.0f);
        steering = Mathf.Clamp(steering, -1.0f, 1.0f);
        float targetSpeed = throttle >= 0.0f
            ? throttle * MaximumForwardSpeed
            : throttle * MaximumReverseSpeed;
        float rate = Mathf.Abs(throttle) > 0.01f
            ? (_speed == 0.0f || Mathf.Sign(_speed) == Mathf.Sign(targetSpeed)
                ? Acceleration
                : Braking)
            : CoastingFriction;
        _speed = Mathf.MoveToward(_speed, targetSpeed, rate * deltaSeconds);

        float steeringStrength = Mathf.Clamp(Mathf.Abs(_speed) / 2.5f, 0.0f, 1.0f);
        float reverseDirection = _speed < 0.0f ? -1.0f : 1.0f;
        RotateY(
            Mathf.DegToRad(-steering * SteeringDegreesPerSecond * steeringStrength *
                reverseDirection * deltaSeconds));
        Vector3 forward = -GlobalBasis.Z;
        Vector3 intendedVelocity = new(forward.X * _speed, -0.5f, forward.Z * _speed);
        Velocity = intendedVelocity;
        MoveAndSlide();
        if (_impactCooldown <= 0.0f)
        {
            float strongestImpact = 0.0f;
            for (int index = 0; index < GetSlideCollisionCount(); index++)
            {
                KinematicCollision3D collision = GetSlideCollision(index);
                strongestImpact = Mathf.Max(
                    strongestImpact,
                    Mathf.Abs(intendedVelocity.Dot(collision.GetNormal())));
            }

            int damage = Condition.ApplyImpact(strongestImpact);
            if (damage > 0)
            {
                _impactCooldown = 0.55f;
                _speed *= 0.3f;
                DamageDriver(Math.Max(1, damage / 4), "vehicle.collision");
                ShowDamageStatus("Choque", damage);
                HandleDisabledEngine();
            }
        }
    }

    private void DamageDriver(int damage, string sourceId)
    {
        Driver?.GetNodeOrNull<Combatant>("Combatant")?.ReceiveDamage(
            new Omerta.Domain.Combat.Damage.DamageInfo(damage, sourceId));
    }

    private void ShowDamageStatus(string cause, int damage)
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _hud?.ShowStatus(
            $"{cause}: -{damage} vehículo · motor {Condition.Current}/{Condition.Maximum}",
            4.0f);
    }

    private void HandleDisabledEngine()
    {
        if (!Condition.IsDisabled)
        {
            return;
        }

        _speed = 0.0f;
        Velocity = Vector3.Zero;
        ForceExitDriver();
        _hud?.ShowStatus("Motor inutilizado. Tuviste que abandonar el vehículo.", 5.0f);
    }

    private void OnConditionChanged(int previous, int current) =>
        RefreshOwnershipPresentation();

    private void RefreshOwnershipPresentation()
    {
        if (IsFamilyOwned)
        {
            AddToGroup("family_vehicle");
        }
        else if (IsInGroup("family_vehicle"))
        {
            RemoveFromGroup("family_vehicle");
        }

        if (_ownershipLabel is not null)
        {
            string ownershipText = IsFamilyOwned
                ? $"{DisplayName.ToUpperInvariant()} · FAMILIA"
                : IsPoliceMarked
                    ? $"{DisplayName.ToUpperInvariant()} · BUSCADO"
                    : $"{DisplayName.ToUpperInvariant()} · ROBADO";
            string conditionText = Condition is null
                ? string.Empty
                : Condition.IsDisabled
                    ? " · MOTOR INUTILIZADO"
                    : $" · MOTOR {Condition.Ratio * 100.0f:0}%";
            _ownershipLabel.Text = ownershipText + conditionText;
            _ownershipLabel.Modulate = IsFamilyOwned
                ? new Color(0.48f, 0.95f, 0.62f)
                : new Color(1.0f, 0.66f, 0.28f);
        }

        if (_body is not null)
        {
            Color ownershipColor = IsFamilyOwned
                ? new Color(0.1f, 0.32f, 0.17f)
                : new Color(0.42f, 0.16f, 0.12f);
            float damageDarkening = Condition is null
                ? 0.0f
                : (1.0f - Condition.Ratio) * 0.65f;
            _body.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = ownershipColor.Lerp(
                    new Color(0.04f, 0.04f, 0.04f),
                    damageDarkening),
                Metallic = 0.35f,
                Roughness = 0.52f
            };
        }
    }
}
