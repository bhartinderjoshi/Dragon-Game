using UnityEngine;
using DragonBattle.Abilities;
using DragonBattle.Combat;
using DragonBattle.Core;

namespace DragonBattle.AI
{
    /// <summary>
    /// Simple and readable 3-State AI State Machine (Idle, Chase, Attack).
    /// Manages distance-based ability selection, cooldown compliance, and balanced pacing.
    /// </summary>
    public enum AIState
    {
        Idle,   // Pausing, breathing room between attacks or when player is out of range
        Chase,  // Moving towards or circling around the player to get in attack range
        Attack  // Facing the player and executing an ability (Tail, Fire, or Fly)
    }

    public class DragonAIController : DragonController
    {
        [Header("State Machine")]
        [SerializeField] private AIState currentState = AIState.Idle;

        [Header("Distance Thresholds")]
        [SerializeField] private float closeRangeDistance = 4.0f;     // Range for Tail Attack
        [SerializeField] private float mediumRangeDistance = 8.0f;    // Range for Fire Breath
        [SerializeField] private float longRangeDistance = 13.0f;    // Range for Fly Attack
        [SerializeField] private float preferredCombatDistance = 4.5f;

        [Header("Independent Ability Cooldowns (Tunable)")]
        [Tooltip("Independent cooldown for Enemy Fire Breath (Seconds)")]
        [SerializeField] private float fireCooldown = 5.0f;
        [Tooltip("Independent cooldown for Enemy Tail Attack (Seconds)")]
        [SerializeField] private float tailCooldown = 4.0f;
        [Tooltip("Independent cooldown for Enemy Fly Attack (Seconds)")]
        [SerializeField] private float flyCooldown = 10.0f;

        [Header("Live Cooldown Monitor (Read-Only)")]
        [SerializeField] private float fireRemainingCD = 0f;
        [SerializeField] private float tailRemainingCD = 0f;
        [SerializeField] private float flyRemainingCD = 0f;

        [Header("Pacing & Difficulty Balancing")]
        [SerializeField] private float decisionInterval = 0.4f;       // Evaluation tick rate
        [SerializeField] private float attackRecoveryDuration = 2.0f; // Generous recovery breathing room pause after attacks
        [SerializeField] private float matchStartDelay = 2.5f;        // Initial preparation delay before first attack
        [SerializeField] private float circleSpeedMultiplier = 0.65f; // Slower maneuvering speed

        private float decisionTimer = 0f;
        private float postAttackTimer = 0f;
        private int circleDirection = 1;                              // 1 = Clockwise, -1 = Counter-clockwise
        private float circleSwitchTimer = 0f;
        private AbilityType lastUsedAbility = (AbilityType)(-1);
        private int abilityCycleIndex = 0;
        private Vector3 smoothSteerDir = Vector3.zero;

        public AIState CurrentState => currentState;

        public float FireCooldown { get => fireCooldown; set => fireCooldown = value; }
        public float TailCooldown { get => tailCooldown; set => tailCooldown = value; }
        public float FlyCooldown { get => flyCooldown; set => flyCooldown = value; }

        protected override void Awake()
        {
            base.Awake();
            moveSpeed = 3.6f;
            acceleration = 14f;
            deceleration = 20f;
            rotationSpeed = 8f;
            ApplyCustomCooldowns();
        }

        protected override void Start()
        {
            base.Start();
            circleDirection = Random.value > 0.5f ? 1 : -1;
            postAttackTimer = matchStartDelay;
            ApplyCustomCooldowns();
        }

        private void ApplyCustomCooldowns()
        {
            var fire = GetAbility(AbilityType.FireBreath);
            if (fire != null) SetAbilityCooldown(fire, fireCooldown);

            var tail = GetAbility(AbilityType.TailAttack);
            if (tail != null) SetAbilityCooldown(tail, tailCooldown);

            var fly = GetAbility(AbilityType.FlyAttack);
            if (fly != null) SetAbilityCooldown(fly, flyCooldown);
        }

        private void SetAbilityCooldown(AbilityBase ability, float newCd)
        {
            var field = typeof(AbilityBase).GetField("cooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(ability, newCd);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Live debug cooldown monitoring in Inspector
            var f = GetAbility(AbilityType.FireBreath);
            if (f != null) fireRemainingCD = f.CurrentCooldown;

            var t = GetAbility(AbilityType.TailAttack);
            if (t != null) tailRemainingCD = t.CurrentCooldown;

            var fl = GetAbility(AbilityType.FlyAttack);
            if (fl != null) flyRemainingCD = fl.CurrentCooldown;

            // Handle breathing room recovery timer
            if (postAttackTimer > 0f)
            {
                postAttackTimer -= Time.deltaTime;
            }

            var target = GetOpponent();
            if (target == null || !target.IsAlive)
            {
                SetState(AIState.Idle);
                Move(Vector3.zero);
                return;
            }

            // Periodic decision making
            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                UpdateDecisions(target);
            }

            // Execute action for current state
            ExecuteState(target);
        }

        /// <summary>
        /// Evaluates distance, cooldowns, and history to dynamically select from all available abilities.
        /// </summary>
        private void UpdateDecisions(IDamageable target)
        {
            if (IsAttacking || IsFlying)
            {
                SetState(AIState.Attack);
                return;
            }

            // When opponent dragon is flying, pause and remain vulnerable / still
            var opponentDragon = target.Transform.GetComponent<DragonController>();
            if (opponentDragon != null && opponentDragon.IsFlying)
            {
                SetState(AIState.Idle);
                return;
            }

            if (postAttackTimer > 0f)
            {
                SetState(AIState.Chase);
                return;
            }

            float distance = Vector3.Distance(transform.position, target.Transform.position);

            var tailAb = GetAbility(AbilityType.TailAttack);
            var fireAb = GetAbility(AbilityType.FireBreath);
            var flyAb = GetAbility(AbilityType.FlyAttack);

            bool tailReady = tailAb != null && tailAb.IsReady;
            bool fireReady = fireAb != null && fireAb.IsReady;
            bool flyReady = flyAb != null && flyAb.IsReady;

            // 1. Close Range Combat (Melee whip priority)
            if (distance <= closeRangeDistance)
            {
                if (tailReady && (lastUsedAbility != AbilityType.TailAttack || !fireReady))
                {
                    PerformAbility(AbilityType.TailAttack, target);
                    return;
                }
                if (fireReady)
                {
                    PerformAbility(AbilityType.FireBreath, target);
                    return;
                }
                if (tailReady)
                {
                    PerformAbility(AbilityType.TailAttack, target);
                    return;
                }
            }

            // 2. Medium Range Combat (Fire Breath & Tactical Sky Dive)
            if (distance > closeRangeDistance && distance <= mediumRangeDistance)
            {
                if (fireReady && lastUsedAbility != AbilityType.FireBreath)
                {
                    PerformAbility(AbilityType.FireBreath, target);
                    return;
                }
                if (flyReady && Random.value < 0.65f)
                {
                    PerformAbility(AbilityType.FlyAttack, target);
                    return;
                }
                if (fireReady)
                {
                    PerformAbility(AbilityType.FireBreath, target);
                    return;
                }
            }

            // 3. Long Range Combat (Sky Dive or Advance)
            if (distance > mediumRangeDistance && distance <= longRangeDistance)
            {
                if (flyReady && lastUsedAbility != AbilityType.FlyAttack)
                {
                    PerformAbility(AbilityType.FlyAttack, target);
                    return;
                }
            }

            // Fallback: If any ability is ready regardless of strict category, execute if in valid range
            if (distance <= closeRangeDistance + 0.5f && tailReady)
            {
                PerformAbility(AbilityType.TailAttack, target);
                return;
            }
            if (distance <= mediumRangeDistance && fireReady)
            {
                PerformAbility(AbilityType.FireBreath, target);
                return;
            }
            if (distance >= 5.0f && distance <= longRangeDistance && flyReady)
            {
                PerformAbility(AbilityType.FlyAttack, target);
                return;
            }

            // If no ability ready or positioning is required, Chase & maneuver
            SetState(AIState.Chase);
        }

        private void PerformAbility(AbilityType type, IDamageable target)
        {
            SetState(AIState.Attack);
            RotateTowardsTarget(target.Transform.position);
            bool executed = UseAbility(type);

            if (executed)
            {
                lastUsedAbility = type;
                postAttackTimer = attackRecoveryDuration;
            }
        }

        private void ExecuteState(IDamageable target)
        {
            Vector3 toTarget = target.Transform.position - transform.position;
            toTarget.y = 0;
            float distance = toTarget.magnitude;

            var opponentDragon = target.Transform.GetComponent<DragonController>();
            if (opponentDragon != null && opponentDragon.IsFlying)
            {
                // Stand ground still, track incoming airborne opponent
                smoothSteerDir = Vector3.MoveTowards(smoothSteerDir, Vector3.zero, Time.deltaTime * 10f);
                Move(Vector3.zero);
                RotateTowardsTarget(target.Transform.position);
                return;
            }

            switch (currentState)
            {
                case AIState.Idle:
                    RotateTowardsTarget(target.Transform.position);
                    Move(Vector3.zero);
                    break;

                case AIState.Chase:
                    circleSwitchTimer += Time.deltaTime;
                    if (circleSwitchTimer > 3.5f)
                    {
                        circleSwitchTimer = 0f;
                        circleDirection = -circleDirection;
                    }

                    Vector3 tangent = Vector3.Cross(Vector3.up, toTarget.normalized) * circleDirection;
                    Vector3 moveDir;

                    // Dynamically adjust desired distance based on next preferred attack
                    var tailAb = GetAbility(AbilityType.TailAttack);
                    float targetDistance = (tailAb != null && tailAb.IsReady) ? 2.8f : preferredCombatDistance;

                    if (distance > targetDistance + 1.2f)
                    {
                        // Advance directly towards player
                        moveDir = (toTarget.normalized * 0.85f + tangent * 0.15f).normalized;
                    }
                    else if (distance < targetDistance - 0.8f)
                    {
                        // Back off slightly while strafing
                        moveDir = (tangent * 0.7f - toTarget.normalized * 0.3f).normalized;
                    }
                    else
                    {
                        // Flank and circle around player
                        moveDir = (tangent * 0.75f + toTarget.normalized * 0.25f).normalized;
                    }

                    smoothSteerDir = Vector3.MoveTowards(smoothSteerDir, moveDir, Time.deltaTime * 5f);
                    Move(smoothSteerDir * circleSpeedMultiplier);
                    RotateTowardsTarget(target.Transform.position);
                    break;

                case AIState.Attack:
                    smoothSteerDir = Vector3.MoveTowards(smoothSteerDir, Vector3.zero, Time.deltaTime * 8f);
                    if (!IsFlying)
                    {
                        RotateTowardsTarget(target.Transform.position);
                        Move(Vector3.zero);
                    }
                    break;
            }
        }

        private void SetState(AIState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
            }
        }

        public override void ResetDragon(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            base.ResetDragon(spawnPosition, spawnRotation);
            postAttackTimer = matchStartDelay;
            lastUsedAbility = (AbilityType)(-1);
            SetState(AIState.Idle);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = currentState == AIState.Attack ? Color.red : (currentState == AIState.Chase ? Color.yellow : Color.cyan);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.4f);
        }
    }
}
