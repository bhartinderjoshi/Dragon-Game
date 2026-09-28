using System.Collections;
using UnityEngine;
using DragonBattle.Audio;
using DragonBattle.Combat;
using DragonBattle.VFX;

namespace DragonBattle.Abilities
{
    public class TailAttackAbility : AbilityBase
    {
        [Header("Tail Attack Settings")]
        [SerializeField] private float attackRadius = 3.8f;
        [SerializeField] private float knockbackPower = 12f;

        private Coroutine attackCoroutine;

        private void Reset()
        {
            abilityType = AbilityType.TailAttack;
            abilityName = "Tail Whip";
            defaultKey = KeyCode.Alpha2;
            cooldown = 3.0f;
            baseDamage = 180f;
            effectiveRange = 3.8f;
            castDuration = 0.5f;
        }

        protected override void Execute()
        {
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            attackCoroutine = StartCoroutine(TailAttackRoutine());
        }

        private IEnumerator TailAttackRoutine()
        {
            isCasting = true;
            owner.SetAttacking(true);
            owner.TriggerAnimation("TailAttack");

            try
            {
                // Spin or windup delay
                yield return new WaitForSeconds(0.15f);

                SoundManager.Instance?.PlayTailAttack(transform.position);

                Vector3 swipePos = transform.position + Vector3.up * 0.5f;
                ParticleManager.Instance?.SpawnTailSwipe(swipePos, transform.rotation);

                // Check hit
                var target = owner.GetOpponent();
                if (target != null && target.IsAlive)
                {
                    Vector3 toTarget = target.Transform.position - transform.position;
                    float distance = toTarget.magnitude;

                    if (distance <= attackRadius)
                    {
                        Vector3 knockDir = (toTarget + Vector3.up * 0.2f).normalized;
                        var dmg = new DamageInfo(
                            baseDamage,
                            gameObject,
                            target.Transform.position + Vector3.up,
                            knockDir,
                            knockbackPower,
                            true,
                            abilityName
                        );
                        target.TakeDamage(dmg);
                    }
                }

                yield return new WaitForSeconds(castDuration - 0.15f);
            }
            finally
            {
                isCasting = false;
                if (owner != null) owner.SetAttacking(false);
                attackCoroutine = null;
            }
        }

        public override void Cancel()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
            isCasting = false;
            if (owner != null) owner.SetAttacking(false);
        }
    }
}
