using Godot;
using Omerta.Application.Interaction;
using Omerta.GodotAdapters.Combat;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Interaction;

public sealed partial class VehicleArsenalInteraction : Area3D, IInteractable
{
    private PlayerWeaponLoadoutController? _loadout;
    private HudController? _hud;

    public bool CanInteract => GetParent<Node3D>().Visible && ResolveLoadout() is not null;

    public string InteractionPrompt => ResolveLoadout()?.IsLongGunCarried == true
        ? "Guardar escopeta en el baúl"
        : "Sacar escopeta del baúl";

    public void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        PlayerWeaponLoadoutController? loadout = ResolveLoadout();
        if (loadout?.TryToggleLongGunStorage(out bool carried) != true)
        {
            return;
        }

        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _hud?.ShowStatus(
            carried
                ? "Escopeta retirada: ahora ocupa tu única ranura de arma larga."
                : "Escopeta guardada en el arsenal del vehículo familiar.",
            4.0f);
    }

    private PlayerWeaponLoadoutController? ResolveLoadout()
    {
        if (_loadout is not null)
        {
            return _loadout;
        }

        Node? player = GetTree().GetFirstNodeInGroup("player");
        _loadout = player?.GetNodeOrNull<PlayerWeaponLoadoutController>("WeaponLoadout");
        return _loadout;
    }
}
