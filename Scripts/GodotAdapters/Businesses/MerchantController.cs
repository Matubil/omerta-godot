using Godot;
using Omerta.Domain.Combat.Damage;
using Omerta.GodotAdapters.Combat;

namespace Omerta.GodotAdapters.Businesses;

public sealed partial class MerchantController : StaticBody3D
{
    private Combatant? _combatant;
    private Node3D? _visual;
    private IBusinessMerchantHost? _host;

    public override void _Ready()
    {
        _combatant = GetNode<Combatant>("Combatant");
        _visual = GetNode<Node3D>("Visual");
        _host = FindHost();
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

        _host ??= FindHost();
    }

    public int CurrentHealth => _combatant?.Health.Current ?? 0;

    public void RestoreHealth(int currentHealth)
    {
        _combatant?.RestoreCurrent(currentHealth);
        if (_visual is not null)
        {
            _visual.Rotation = currentHealth == 0
                ? new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f)
                : Vector3.Zero;
        }
    }

    private void OnDefeated(DamageInfo damage)
    {
        if (_visual is not null)
        {
            _visual.Rotation = new Vector3(0.0f, 0.0f, Mathf.Pi * 0.5f);
        }

        _host ??= FindHost();
        _host?.NotifyMerchantAvailability(false);
    }

    private IBusinessMerchantHost? FindHost()
    {
        Node? current = GetParent();
        while (current is not null && current is not IBusinessMerchantHost)
        {
            current = current.GetParent();
        }

        return current as IBusinessMerchantHost;
    }
}
