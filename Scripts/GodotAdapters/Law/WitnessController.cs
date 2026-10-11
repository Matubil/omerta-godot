using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Law;

public sealed partial class WitnessController : StaticBody3D
{
    private Combatant? _combatant;
    private Node3D? _visual;

    public bool CanReport => _combatant?.IsDefeated == false;

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _visual = GetNode<Node3D>("Visual");
        _combatant.Defeated += OnDefeated;
    }

    public override void _ExitTree()
    {
        if (_combatant is not null)
        {
            _combatant.Defeated -= OnDefeated;
        }
    }

    public void ResetWitness()
    {
        _combatant?.RestoreToFull();
        if (_visual is not null)
        {
            _visual.Rotation = Vector3.Zero;
        }
    }

    private void OnDefeated(DamageInfo damage)
    {
        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }
    }
}
