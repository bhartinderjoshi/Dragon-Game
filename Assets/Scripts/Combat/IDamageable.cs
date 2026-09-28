using UnityEngine;

namespace DragonBattle.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        Transform Transform { get; }
        void TakeDamage(DamageInfo damageInfo);
    }
}
