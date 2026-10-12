using System;
using Godot;
using Omerta.Domain.Combat.Weapons;

namespace Omerta.GodotAdapters.Combat;

public sealed partial class PlayerWeaponLoadoutController : Node
{
    public const string PistolId = "weapon.prototype-pistol";
    public const string ShotgunId = "weapon.prototype-shotgun";

    private HitscanWeaponController? _sidearm;
    private HitscanWeaponController? _longGun;
    private Node3D? _heldWeaponVisual;
    private Node3D? _backWeaponVisual;

    public event Action? Changed;
    public event Action<Vector3, Vector3, bool>? ShotResolved;

    public WeaponLoadout Loadout { get; private set; } = null!;
    public HitscanWeaponController SidearmController => _sidearm!;
    public HitscanWeaponController LongGunController => _longGun!;
    public WeaponRuntime? EquippedRuntime => Loadout.EquippedRuntime;
    public bool IsHolstered => Loadout.IsHolstered;
    public bool IsLongGunCarried => Loadout.GetWeaponInSlot(WeaponSlotIds.LongGun) is not null;
    public string EquippedWeaponId => EquippedRuntime?.Specification.Id ?? string.Empty;
    public Vector3 FireOriginPosition =>
        ResolveEquippedController()?.FireOriginPosition ?? Vector3.Zero;

    public override void _Ready()
    {
        CharacterBody3D player = GetParent<CharacterBody3D>();
        _sidearm = player.GetNode<HitscanWeaponController>("WeaponController");
        _longGun = player.GetNode<HitscanWeaponController>("LongGunWeaponController");
        _heldWeaponVisual = player.GetNode<Node3D>("Visual/Weapon");
        _backWeaponVisual = player.GetNode<Node3D>("Visual/BackWeapon");

        Loadout = new WeaponLoadout(new[] { WeaponSlotIds.Sidearm, WeaponSlotIds.LongGun });
        if (!Loadout.TryCarry(_sidearm.Runtime, WeaponSlotIds.Sidearm))
        {
            throw new InvalidOperationException("The prototype pistol is not compatible with the sidearm slot.");
        }

        Loadout.Changed += OnStateChanged;
        _sidearm.Runtime.Changed += OnStateChanged;
        _longGun.Runtime.Changed += OnStateChanged;
        _sidearm.ShotResolved += ForwardShot;
        _longGun.ShotResolved += ForwardShot;
        UpdateVisuals();
    }

    public override void _ExitTree()
    {
        if (Loadout is not null)
        {
            Loadout.Changed -= OnStateChanged;
        }

        if (_sidearm?.Runtime is not null)
        {
            _sidearm.Runtime.Changed -= OnStateChanged;
            _sidearm.ShotResolved -= ForwardShot;
        }

        if (_longGun?.Runtime is not null)
        {
            _longGun.Runtime.Changed -= OnStateChanged;
            _longGun.ShotResolved -= ForwardShot;
        }
    }

    public bool TryFire(Vector3 direction, bool isAiming)
    {
        if (IsHolstered)
        {
            return false;
        }

        HitscanWeaponController? controller = ResolveEquippedController();
        return controller?.TryFire(direction, isAiming) == true;
    }

    public bool TryReload() =>
        !IsHolstered && ResolveEquippedController()?.TryReload() == true;

    public bool TryEquipSidearm() => Loadout.TryEquipSlot(WeaponSlotIds.Sidearm);

    public bool TryEquipLongGun() => Loadout.TryEquipSlot(WeaponSlotIds.LongGun);

    public void ToggleHolster() => Loadout.SetHolstered(!Loadout.IsHolstered);

    public bool TryToggleLongGunStorage(out bool isNowCarried)
    {
        if (IsLongGunCarried)
        {
            bool stored = Loadout.TryStore(WeaponSlotIds.LongGun, out _);
            isNowCarried = !stored;
            return stored;
        }

        bool carried = Loadout.TryCarry(_longGun!.Runtime, WeaponSlotIds.LongGun);
        isNowCarried = carried;
        return carried;
    }

    public void ResetLoadout()
    {
        _sidearm?.ResetRuntime();
        _longGun?.ResetRuntime();
        if (IsLongGunCarried)
        {
            Loadout.TryStore(WeaponSlotIds.LongGun, out _);
        }

        Loadout.TryEquipSlot(WeaponSlotIds.Sidearm);
        Loadout.SetHolstered(false);
        OnStateChanged();
    }

    public void RestoreLoadout(
        int pistolMagazine,
        int pistolReserve,
        int shotgunMagazine,
        int shotgunReserve,
        bool shotgunCarried,
        string equippedWeaponId,
        bool holstered)
    {
        _sidearm!.RestoreRuntime(pistolMagazine, pistolReserve);
        _longGun!.RestoreRuntime(shotgunMagazine, shotgunReserve);

        if (shotgunCarried != IsLongGunCarried)
        {
            if (shotgunCarried)
            {
                Loadout.TryCarry(_longGun.Runtime, WeaponSlotIds.LongGun);
            }
            else
            {
                Loadout.TryStore(WeaponSlotIds.LongGun, out _);
            }
        }

        if (!Loadout.TryEquipWeapon(equippedWeaponId))
        {
            Loadout.TryEquipSlot(WeaponSlotIds.Sidearm);
        }

        Loadout.SetHolstered(holstered);
        OnStateChanged();
    }

    private HitscanWeaponController? ResolveEquippedController() => EquippedWeaponId switch
    {
        PistolId => _sidearm,
        ShotgunId => _longGun,
        _ => null
    };

    private void ForwardShot(Vector3 origin, Vector3 end, bool didHit) =>
        ShotResolved?.Invoke(origin, end, didHit);

    private void OnStateChanged()
    {
        UpdateVisuals();
        Changed?.Invoke();
    }

    private void UpdateVisuals()
    {
        if (_heldWeaponVisual is null || _backWeaponVisual is null || Loadout is null)
        {
            return;
        }

        bool holdingWeapon = !IsHolstered && EquippedRuntime is not null;
        _heldWeaponVisual.Visible = holdingWeapon;
        bool holdingLongGun = holdingWeapon && EquippedWeaponId == ShotgunId;
        _heldWeaponVisual.Position = holdingLongGun
            ? new Vector3(0.3f, 0.12f, 0.43f)
            : new Vector3(0.3f, 0.1f, 0.28f);
        _heldWeaponVisual.Scale = holdingLongGun
            ? new Vector3(1.2f, 1.05f, 2.35f)
            : Vector3.One;
        _backWeaponVisual.Visible = IsLongGunCarried && !holdingLongGun;
    }
}
