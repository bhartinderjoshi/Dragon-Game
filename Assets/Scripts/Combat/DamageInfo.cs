using UnityEngine;

namespace DragonBattle.Combat
{
    [System.Serializable]
    public struct DamageInfo
    {
        public float Amount;
        public GameObject Attacker;
        public Vector3 HitPoint;
        public Vector3 KnockbackDirection;
        public float KnockbackForce;
        public bool IsCritical;
        public string AbilityName;

        public DamageInfo(float amount, GameObject attacker, Vector3 hitPoint = default, Vector3 knockbackDir = default, float knockbackForce = 0f, bool isCritical = false, string abilityName = "")
        {
            Amount = amount;
            Attacker = attacker;
            HitPoint = hitPoint;
            KnockbackDirection = knockbackDir.normalized;
            KnockbackForce = knockbackForce;
            IsCritical = isCritical;
            AbilityName = abilityName;
        }
    }
}
