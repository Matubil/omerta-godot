using System;
using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.Domain.Combat.Health;

namespace Omerta.GodotAdapters.Combat;

public sealed partial class Combatant : Node, IDamageable
{
    [Export]
    public string CombatantId { get; set; } = "character.unknown";

    [Export]
    public int MaximumHealth { get; set; } = 100;

    public event Action<int, int>? HealthChanged;

    public event Action<DamageInfo>? Damaged;

    public event Action<DamageInfo>? Defeated;

    public Health Health { get; private set; } = null!;

    public bool IsDefeated => Health.IsDefeated;

    public Node3D Actor => (Node3D)GetParent();

    public override void _Ready()
    {
        Health = new Health(MaximumHealth);
        Health.Changed += OnHealthChanged;
        Health.Defeated += OnDefeated;
    }

    public override void _ExitTree()
    {
        if (Health is null)
        {
            return;
        }

        Health.Changed -= OnHealthChanged;
        Health.Defeated -= OnDefeated;
    }

    public bool ReceiveDamage(DamageInfo damage)
    {
        bool applied = Health.ReceiveDamage(damage);
        if (applied)
        {
            Damaged?.Invoke(damage);
        }

        return applied;
    }

    public void RestoreToFull() => Health.RestoreToFull();

    public static Combatant? FindFromCollider(Node? collider)
    {
        Node? current = collider;
        while (current is not null)
        {
            if (current is Combatant combatant)
            {
                return combatant;
            }

            Combatant? child = current.GetNodeOrNull<Combatant>("Combatant");
            if (child is not null)
            {
                return child;
            }

            current = current.GetParent();
        }

        return null;
    }

    private void OnHealthChanged(int previous, int current) =>
        HealthChanged?.Invoke(previous, current);

    private void OnDefeated(DamageInfo damage) => Defeated?.Invoke(damage);
}
