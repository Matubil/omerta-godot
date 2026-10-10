using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Camera;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Input;
using Omerta.GodotAdapters.World;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Combat;

public sealed partial class PlayerCombatController : Node
{
    private PlayerController? _player;
    private Combatant? _combatant;
    private HitscanWeaponController? _weapon;
    private PlayerInputAdapter? _input;
    private IsometricCameraController? _camera;
    private WorldCursorProjector? _cursor;
    private HudController? _hud;

    public bool IsAiming { get; private set; }

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _combatant = _player.GetNode<Combatant>("Combatant");
        _weapon = _player.GetNode<HitscanWeaponController>("WeaponController");
        ResolveDependencies();
        _combatant.HealthChanged += OnHealthChanged;
        _combatant.Defeated += OnDefeated;
        _weapon.Runtime.Changed += RefreshHud;
        RefreshHud();
    }

    public override void _ExitTree()
    {
        if (_combatant is not null)
        {
            _combatant.HealthChanged -= OnHealthChanged;
            _combatant.Defeated -= OnDefeated;
        }

        if (_weapon?.Runtime is not null)
        {
            _weapon.Runtime.Changed -= RefreshHud;
        }
    }

    public override void _Process(double delta)
    {
        ResolveDependencies();
        if (_input is null || _combatant is null || _weapon is null)
        {
            return;
        }

        SetAiming(_input.IsAiming && !_combatant.IsDefeated);
        if (_combatant.IsDefeated)
        {
            return;
        }

        if (_input.ReloadPressed)
        {
            _weapon.TryReload();
        }

        if (_input.FirePressed && _cursor?.HasAimPoint == true)
        {
            _weapon.TryFire(_cursor.AimPoint - _weapon.FireOriginPosition, IsAiming);
        }
    }

    public void ResetCombat()
    {
        _combatant?.RestoreToFull();
        _weapon?.ResetRuntime();
        SetAiming(false);
        RefreshHud();
    }

    private void SetAiming(bool aiming)
    {
        if (IsAiming == aiming)
        {
            return;
        }

        IsAiming = aiming;
        _player?.SetAiming(aiming);
        _camera?.SetAiming(aiming);
        _hud?.SetAimState(aiming);
    }

    private void ResolveDependencies()
    {
        _input ??= GetTree().GetFirstNodeInGroup("player_input") as PlayerInputAdapter;
        _camera ??= GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _cursor ??= GetTree().GetFirstNodeInGroup("world_cursor") as WorldCursorProjector;
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
    }

    private void OnHealthChanged(int previous, int current) => RefreshHud();

    private void OnDefeated(DamageInfo damage) => SetAiming(false);

    private void RefreshHud()
    {
        if (_combatant?.Health is not null)
        {
            _hud?.SetHealth(_combatant.Health.Current, _combatant.Health.Maximum);
        }

        if (_weapon?.Runtime is not null)
        {
            _hud?.SetWeapon(
                _weapon.Runtime.Specification.DisplayName,
                _weapon.Runtime.AmmoInMagazine,
                _weapon.Runtime.ReserveAmmo,
                _weapon.Runtime.IsReloading);
        }
    }
}
