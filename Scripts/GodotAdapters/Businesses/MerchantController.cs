using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class MerchantController : StaticBody3D
{
    private Combatant? _combatant;
    private Node3D? _visual;
    private WorkshopController? _workshop;

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _visual = GetNode<Node3D>("Visual");
        _workshop = FindWorkshop();
        _combatant.Defeated += OnDefeated;
    }

    public override void _ExitTree()
    {
        if (_combatant is not null)
        {
            _combatant.Defeated -= OnDefeated;
        }
    }

    public void ResetMerchant()
    {
        _combatant?.RestoreToFull();
        if (_visual is not null)
        {
            _visual.Rotation = Vector3.Zero;
        }

        _workshop ??= FindWorkshop();
    }

    private void OnDefeated(DamageInfo damage)
    {
        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }

        _workshop ??= FindWorkshop();
        _workshop?.NotifyMerchantAvailability(false);
    }

    private WorkshopController? FindWorkshop()
    {
        Node? current = GetParent();
        while (current is not null && current is not WorkshopController)
        {
            current = current.GetParent();
        }

        return current as WorkshopController;
    }
}
