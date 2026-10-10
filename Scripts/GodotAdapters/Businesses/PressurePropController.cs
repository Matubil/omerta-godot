using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class PressurePropController : StaticBody3D
{
    private Combatant? _combatant;
    private CollisionShape3D? _collision;
    private Node3D? _visual;
    private WorkshopController? _workshop;

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _collision = GetNode<CollisionShape3D>("Collision");
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

    public void ResetProp()
    {
        _combatant?.RestoreToFull();
        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        }

        if (_visual is not null)
        {
            _visual.Scale = Vector3.One;
        }
    }

    private void OnDefeated(DamageInfo damage)
    {
        if (damage.SourceId != "character.player")
        {
            return;
        }

        if (_collision is not null)
        {
            _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        }

        if (_visual is not null)
        {
            _visual.Scale = new Vector3(1.0f, 0.2f, 1.0f);
        }

        _workshop ??= FindWorkshop();
        _workshop?.NotifyPressurePropBroken();
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
