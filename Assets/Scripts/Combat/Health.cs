using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBattle.Audio;
using DragonBattle.VFX;

namespace DragonBattle.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isInvulnerable = false;

        [Header("Hit Flash")]
        [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

        // Events
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<DamageInfo> OnDamaged;
        public event Action<GameObject> OnDied;

        private Rigidbody rb;
        private Coroutine flashCoroutine;
        private List<Color> originalColors = new List<Color>();
        private List<Material> instanceMaterials = new List<Material>();
        private bool isDead = false;

        public bool IsAlive => !isDead && currentHealth > 0;
        public Transform Transform => transform;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));

        private void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody>();
            SetupRenderers();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetupRenderers()
        {
            if (targetRenderers == null || targetRenderers.Count == 0)
            {
                targetRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
            }

            originalColors.Clear();
            instanceMaterials.Clear();

            foreach (var r in targetRenderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    instanceMaterials.Add(mats[i]);
                    if (mats[i].HasProperty("_BaseColor"))
                    {
                        originalColors.Add(mats[i].GetColor("_BaseColor"));
                    }
                    else if (mats[i].HasProperty("_Color"))
                    {
                        originalColors.Add(mats[i].GetColor("_Color"));
                    }
                    else
                    {
                        originalColors.Add(Color.white);
                    }
                }
            }
        }

        public void SetMaxHealth(float newMax, bool resetCurrent = true)
        {
            maxHealth = Mathf.Max(10f, newMax);
            if (resetCurrent) currentHealth = maxHealth;
            else currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (isDead || isInvulnerable || damageInfo.Amount <= 0) return;

            currentHealth -= damageInfo.Amount;
            currentHealth = Mathf.Max(0f, currentHealth);

            // Trigger floating combat text
            Vector3 textSpawnPos = transform.position + Vector3.up * 2.2f + UnityEngine.Random.insideUnitSphere * 0.3f;
            FloatingTextManager.Instance?.ShowDamage(damageInfo.Amount, textSpawnPos, damageInfo.IsCritical);

            // Trigger Hit VFX & SFX
            ParticleManager.Instance?.SpawnHitSpark(damageInfo.HitPoint != Vector3.zero ? damageInfo.HitPoint : textSpawnPos);
            SoundManager.Instance?.PlayHitSound(transform.position);

            // Trigger hit flash
            if (gameObject.activeInHierarchy)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine());
            }

            // Apply physical knockback if specified
            if (damageInfo.KnockbackForce > 0.1f)
            {
                ApplyKnockback(damageInfo.KnockbackDirection * damageInfo.KnockbackForce);
            }

            OnDamaged?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f && !isDead)
            {
                Die(damageInfo.Attacker);
            }
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            FloatingTextManager.Instance?.ShowHeal(amount, transform.position + Vector3.up * 2f);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyKnockback(Vector3 force)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
            else
            {
                StartCoroutine(SmoothKnockbackRoutine(force));
            }
        }

        private IEnumerator SmoothKnockbackRoutine(Vector3 force)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startVel = force * 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float factor = 1f - (elapsed / duration);
                transform.position += startVel * factor * Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            for (int i = 0; i < instanceMaterials.Count; i++)
            {
                var mat = instanceMaterials[i];
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", flashColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", flashColor);
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < instanceMaterials.Count; i++)
            {
                var mat = instanceMaterials[i];
                if (mat == null || i >= originalColors.Count) continue;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", originalColors[i]);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", originalColors[i]);
            }
        }

        private void Die(GameObject killer)
        {
            isDead = true;
            SoundManager.Instance?.PlayDeathSound(transform.position);
            OnDied?.Invoke(killer);
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SetupRenderers();
        }
    }
}
