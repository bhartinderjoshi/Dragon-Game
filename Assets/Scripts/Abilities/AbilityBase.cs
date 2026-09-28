using System;
using UnityEngine;
using DragonBattle.Core;

namespace DragonBattle.Abilities
{
    public abstract class AbilityBase : MonoBehaviour
    {
        [Header("Ability Identification")]
        [SerializeField] protected AbilityType abilityType;
        [SerializeField] protected string abilityName = "Ability";
        [SerializeField] protected KeyCode defaultKey = KeyCode.Alpha1;
        [SerializeField] protected Sprite iconSprite;

        [Header("Balance Stats")]
        [SerializeField] protected float cooldown = 5.0f;
        [SerializeField] protected float baseDamage = 150f;
        [SerializeField] protected float effectiveRange = 6.0f;
        [SerializeField] protected float castDuration = 1.0f;

        protected float currentCooldown = 0f;
        protected bool isCasting = false;
        protected DragonController owner;

        public AbilityType Type => abilityType;
        public string AbilityName => abilityName;
        public KeyCode DefaultKey { get => defaultKey; set => defaultKey = value; }
        public Sprite IconSprite => iconSprite;
        public float Cooldown => cooldown;
        public float CurrentCooldown => currentCooldown;
        public float BaseDamage => baseDamage;
        public float EffectiveRange => effectiveRange;
        public float CastDuration => castDuration;
        public bool IsReady => currentCooldown <= 0.001f && !isCasting;
        public bool IsCasting => isCasting;
        public float CooldownPercent => Mathf.Clamp01(currentCooldown / Mathf.Max(0.1f, cooldown));

        public event Action<AbilityBase> OnAbilityExecuted;
        public event Action<float, float> OnCooldownUpdated; // remaining, max

        public virtual void Initialize(DragonController dragonOwner)
        {
            owner = dragonOwner;
            currentCooldown = 0f;
        }

        protected virtual void Update()
        {
            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
                if (currentCooldown < 0f) currentCooldown = 0f;
                OnCooldownUpdated?.Invoke(currentCooldown, cooldown);
            }
        }

        public virtual bool CanExecute()
        {
            if (owner == null || !owner.IsAlive) return false;
            if (!IsReady) return false;
            if (owner.IsBusyWithOtherAbility(this)) return false;
            return true;
        }

        public virtual bool TryExecute()
        {
            if (!CanExecute()) return false;
            StartCooldown();
            Execute();
            OnAbilityExecuted?.Invoke(this);
            return true;
        }

        public virtual void StartCooldown()
        {
            currentCooldown = cooldown;
            OnCooldownUpdated?.Invoke(currentCooldown, cooldown);
        }

        public virtual void ResetCooldown()
        {
            currentCooldown = 0f;
            isCasting = false;
            OnCooldownUpdated?.Invoke(0f, cooldown);
        }

        protected abstract void Execute();
        public abstract void Cancel();
    }
}
