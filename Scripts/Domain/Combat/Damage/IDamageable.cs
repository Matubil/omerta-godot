namespace Omerta.Domain.Combat.Damage;

public interface IDamageable
{
    bool ReceiveDamage(DamageInfo damage);
}
