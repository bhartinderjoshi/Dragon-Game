using System.Collections;
using UnityEngine;
using DragonBattle.Audio;
using DragonBattle.Combat;
using DragonBattle.VFX;

namespace DragonBattle.Abilities
{
    public class FireBreathAbility : AbilityBase
    {
        [Header("Fire Breath Settings")]
        [SerializeField] private float coneAngle = 50f;
        [SerializeField] private int tickCount = 6;
        [SerializeField] private ParticleSystem fireParticleSystem;

        private Coroutine fireCoroutine;

        private void Reset()
        {
            abilityType = AbilityType.FireBreath;
            abilityName = "Fire Breath";
            defaultKey = KeyCode.Q;
            cooldown = 4.0f;
            baseDamage = 220f; // Total damage across ticks
            effectiveRange = 7.5f;
            castDuration = 1.3f;
        }

        public override void Initialize(Core.DragonController dragonOwner)
        {
            base.Initialize(dragonOwner);
            if (fireParticleSystem == null && ParticleManager.Instance != null)
            {
                fireParticleSystem = ParticleManager.Instance.SetupFireBreath(owner.transform);
            }
        }

        protected override void Execute()
        {
            if (fireCoroutine != null) StopCoroutine(fireCoroutine);
            fireCoroutine = StartCoroutine(FireBreathRoutine());
        }

        private IEnumerator FireBreathRoutine()
        {
            isCasting = true;
            owner.SetAttacking(true);
            owner.TriggerAnimation("FireAttack");
            SoundManager.Instance?.PlayFireBreath(transform.position);

            if (fireParticleSystem == null && ParticleManager.Instance != null)
            {
                fireParticleSystem = ParticleManager.Instance.SetupFireBreath(owner.transform);
            }

            try
            {
                if (fireParticleSystem != null)
                {
                    fireParticleSystem.Play();
                }

                float tickInterval = castDuration / tickCount;
                float damagePerTick = baseDamage / tickCount;

                for (int i = 0; i < tickCount; i++)
                {
                    yield return new WaitForSeconds(tickInterval);
                    DealConeDamage(damagePerTick);
                }
            }
            finally
            {
                if (fireParticleSystem != null)
                {
                    fireParticleSystem.Stop();
                }

                isCasting = false;
                if (owner != null) owner.SetAttacking(false);
                fireCoroutine = null;
            }
        }

        private void DealConeDamage(float damage)
        {
            if (owner == null) return;

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 forward = transform.forward;
            var target = owner.GetOpponent();

            if (target != null && target.IsAlive)
            {
                Vector3 toTarget = target.Transform.position - origin;
                float distance = toTarget.magnitude;

                if (distance <= effectiveRange)
                {
                    toTarget.y = 0;
                    float angle = Vector3.Angle(forward, toTarget);
                    if (angle <= coneAngle * 0.5f)
                    {
                        var dmg = new DamageInfo(
                            damage,
                            gameObject,
                            target.Transform.position + Vector3.up,
                            toTarget.normalized,
                            1.5f,
                            false,
                            abilityName
                        );
                        target.TakeDamage(dmg);
                    }
                }
            }
        }

        public override void Cancel()
        {
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
                fireCoroutine = null;
            }
            if (fireParticleSystem != null) fireParticleSystem.Stop();
            isCasting = false;
            if (owner != null) owner.SetAttacking(false);
        }
    }
}
