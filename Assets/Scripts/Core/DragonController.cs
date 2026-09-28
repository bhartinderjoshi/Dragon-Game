using System.Collections.Generic;
using UnityEngine;
using DragonBattle.Abilities;
using DragonBattle.Combat;

namespace DragonBattle.Core
{
    [RequireComponent(typeof(Health))]
    public class DragonController : MonoBehaviour
    {
        [Header("Dragon Identity")]
        [SerializeField] private string dragonName = "Dragon";
        [SerializeField] private bool isPlayer = false;

        [Header("Locomotion")]
        [SerializeField] protected float moveSpeed = 6.5f;
        [SerializeField] protected float acceleration = 22f;
        [SerializeField] protected float deceleration = 28f;
        [SerializeField] protected float rotationSpeed = 14f;
        [SerializeField] protected float arenaRadius = 13.5f;

        [Header("Visual & Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform mouthPoint;

        [Header("Configurable Animation Names / Triggers")]
        [Tooltip("Animation parameter or state name for Idle")]
        [SerializeField] private string idleAnim = "IdleSimple";
        [Tooltip("Animation parameter or state name for Walking / Moving")]
        [SerializeField] private string walkAnim = "Walk";
        [Tooltip("Animation parameter or state name for Fire Breath")]
        [SerializeField] private string fireAnim = "Drakaris";
        [Tooltip("Animation parameter or state name for Tail / Melee Attack")]
        [SerializeField] private string tailAnim = "Bite";
        [Tooltip("Animation parameter or state name for Fly TakeOff")]
        [SerializeField] private string takeOffAnim = "TakeOff";
        [Tooltip("Animation parameter or state name for Aerial Dive")]
        [SerializeField] private string flyDiveAnim = "FlyingAttack";
        [Tooltip("Animation parameter or state name for Fly Landing")]
        [SerializeField] private string flyLandAnim = "Lands";
        [Tooltip("Animation parameter or state name for Death")]
        [SerializeField] private string dieAnim = "Die";

        [Header("Abilities")]
        [SerializeField] private List<AbilityBase> abilities = new List<AbilityBase>();

        private Health health;
        private Rigidbody rb;
        private IDamageable opponent;
        private bool isAttacking = false;
        private bool isFlying = false;
        protected Vector3 currentVelocity = Vector3.zero;
        private float animSpeedFloat = 0f;

        public string DragonName => dragonName;
        public bool IsPlayer => isPlayer;
        public bool IsAlive => health != null && health.IsAlive;
        public bool IsAttacking => isAttacking;
        public bool IsFlying => isFlying;
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
        public float Acceleration { get => acceleration; set => acceleration = value; }
        public float Deceleration { get => deceleration; set => deceleration = value; }
        public Health Health => health;
        public List<AbilityBase> Abilities => abilities;
        public Transform MouthPoint => mouthPoint != null ? mouthPoint : transform;

        // Public getters/setters for animation names so any script or inspector can customize them
        public string IdleAnim { get => idleAnim; set => idleAnim = value; }
        public string WalkAnim { get => walkAnim; set => walkAnim = value; }
        public string FireAnim { get => fireAnim; set => fireAnim = value; }
        public string TailAnim { get => tailAnim; set => tailAnim = value; }
        public string TakeOffAnim { get => takeOffAnim; set => takeOffAnim = value; }
        public string FlyDiveAnim { get => flyDiveAnim; set => flyDiveAnim = value; }
        public string FlyLandAnim { get => flyLandAnim; set => flyLandAnim = value; }
        public string DieAnim { get => dieAnim; set => dieAnim = value; }

        protected virtual void Awake()
        {
            health = GetComponent<Health>();
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.mass = 50f;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visuals");
                if (visualRoot == null) visualRoot = transform.Find("DragonMesh");
                if (visualRoot == null) visualRoot = transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (animator != null)
            {
                animator.applyRootMotion = false;
                SetAnimatorBoolSafe("IdleSimple", true);
            }

            if (mouthPoint == null)
            {
                var mouth = transform.Find("MouthPoint");
                if (mouth == null)
                {
                    var mouthGO = new GameObject("MouthPoint");
                    mouthGO.transform.SetParent(transform, false);
                    mouthGO.transform.localPosition = new Vector3(0, 1.2f, 1.4f);
                    mouthPoint = mouthGO.transform;
                }
                else
                {
                    mouthPoint = mouth;
                }
            }

            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            InitializeAbilities();
        }

        public void PlayLocalSound(AudioClip clip, float volume = 1f)
        {
            var aSource = GetComponent<AudioSource>();
            if (aSource != null && clip != null)
            {
                aSource.PlayOneShot(clip, volume);
            }
        }

        protected virtual void Start()
        {
            FindOpponent();
            health.OnDied += HandleDeath;
        }

        public void InitializeAbilities()
        {
            if (abilities == null || abilities.Count == 0)
            {
                abilities = new List<AbilityBase>(GetComponentsInChildren<AbilityBase>());
            }

            foreach (var ab in abilities)
            {
                if (ab != null) ab.Initialize(this);
            }
        }

        public void SetOpponent(IDamageable target)
        {
            opponent = target;
        }

        public IDamageable GetOpponent()
        {
            if (opponent == null || !opponent.IsAlive)
            {
                FindOpponent();
            }
            return opponent;
        }

        private void FindOpponent()
        {
            var allDragons = FindObjectsByType<DragonController>(FindObjectsInactive.Exclude);
            if (allDragons == null) return;
            foreach (var d in allDragons)
            {
                if (d != this && d.IsAlive)
                {
                    opponent = d.Health;
                    break;
                }
            }
        }

        public virtual void Move(Vector3 direction)
        {
            if (!IsAlive)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            direction.y = 0;
            Vector3 targetVelocity = Vector3.zero;

            if (!isAttacking && !isFlying)
            {
                float inputMag = Mathf.Clamp01(direction.magnitude);
                if (inputMag > 0.01f)
                {
                    targetVelocity = direction.normalized * (moveSpeed * inputMag);
                }
            }

            // Smooth Acceleration / Deceleration
            float rate = targetVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration;
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, rate * Time.deltaTime);

            float currentSpeed = currentVelocity.magnitude;

            if (!isAttacking && !isFlying && currentSpeed > 0.01f)
            {
                Vector3 targetPos = transform.position + currentVelocity * Time.deltaTime;

                // Soft arena boundary clamping
                Vector2 flatPos = new Vector2(targetPos.x, targetPos.z);
                if (flatPos.magnitude > arenaRadius)
                {
                    flatPos = flatPos.normalized * arenaRadius;
                    targetPos.x = flatPos.x;
                    targetPos.z = flatPos.y;
                }

                targetPos.y = 0f;
                transform.position = targetPos;

                // Smooth rotation towards velocity vector
                RotateTowards(currentVelocity);
            }

            UpdateLocomotionAnimation(currentSpeed > 0.15f, currentSpeed / Mathf.Max(0.1f, moveSpeed));
        }

        private void UpdateLocomotionAnimation(bool isMoving, float normalizedSpeed)
        {
            if (animator == null) return;
            if (isAttacking || isFlying) return;

            animSpeedFloat = Mathf.MoveTowards(animSpeedFloat, normalizedSpeed, Time.deltaTime * 6f);

            SetAnimatorBoolSafe("IsMoving", isMoving);
            SetAnimatorFloatSafe("Speed", animSpeedFloat);

            if (isMoving)
            {
                PlayCustomAnimation(walkAnim);
            }
            else
            {
                PlayCustomAnimation(idleAnim);
            }
        }

        public void PlayCustomAnimation(string animName)
        {
            if (animator == null || string.IsNullOrEmpty(animName)) return;

            bool foundParam = false;
            foreach (var p in animator.parameters)
            {
                if (p.name == animName)
                {
                    foundParam = true;
                    if (p.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.SetTrigger(animName);
                    }
                    else if (p.type == AnimatorControllerParameterType.Bool)
                    {
                        ClearAllAnimationBools(animName);
                        animator.SetBool(animName, true);
                    }
                    return;
                }
            }

            if (!foundParam)
            {
                // Fallback: Direct state / clip playback if state exists in controller
                animator.CrossFade(animName, 0.15f);
            }
        }

        public void RotateTowards(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        public void RotateTowardsTarget(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            RotateTowards(dir);
        }

        public bool UseAbility(int index)
        {
            if (index >= 0 && index < abilities.Count && abilities[index] != null)
            {
                return abilities[index].TryExecute();
            }
            return false;
        }

        public bool UseAbility(AbilityType type)
        {
            var ab = abilities.Find(a => a != null && a.Type == type);
            if (ab != null)
            {
                return ab.TryExecute();
            }
            return false;
        }

        public AbilityBase GetAbility(AbilityType type)
        {
            return abilities.Find(a => a != null && a.Type == type);
        }

        public bool IsBusyWithOtherAbility(AbilityBase requester)
        {
            foreach (var ab in abilities)
            {
                if (ab != null && ab != requester && ab.IsCasting)
                {
                    return true;
                }
            }
            return false;
        }

        private static readonly string[] RedDragonAnimBools = new string[]
        {
            "IdleSimple", "IdleAgressive", "IdleRestless", "Walk", "BattleStance",
            "Bite", "Drakaris", "FlyingFWD", "FlyingAttack", "Hover", "Lands", "TakeOff", "Die"
        };

        public void ClearAllAnimationBools(string keepActive = null)
        {
            if (animator == null) return;
            foreach (var boolName in RedDragonAnimBools)
            {
                if (boolName == keepActive)
                {
                    SetAnimatorBoolSafe(boolName, true);
                }
                else
                {
                    SetAnimatorBoolSafe(boolName, false);
                }
            }
        }

        public void SetAttacking(bool attacking)
        {
            isAttacking = attacking;
            if (!attacking)
            {
                PlayCustomAnimation(idleAnim);
            }
        }

        public void SetFlying(bool flying)
        {
            isFlying = flying;
            health?.SetInvulnerable(flying);
        }

        public void TriggerAnimation(string triggerName)
        {
            if (animator == null) return;

            SetAnimatorTriggerSafe(triggerName);

            // Map standard trigger names to user configured animation parameters
            switch (triggerName)
            {
                case "FireAttack":
                    PlayCustomAnimation(fireAnim);
                    break;
                case "TailAttack":
                    PlayCustomAnimation(tailAnim);
                    break;
                case "FlyUp":
                    PlayCustomAnimation(takeOffAnim);
                    break;
                case "FlyDive":
                    PlayCustomAnimation(flyDiveAnim);
                    break;
                case "FlyLand":
                    PlayCustomAnimation(flyLandAnim);
                    break;
                case "Die":
                    PlayCustomAnimation(dieAnim);
                    break;
                default:
                    PlayCustomAnimation(triggerName);
                    break;
            }
        }

        private void SetAnimatorBoolSafe(string paramName, bool value)
        {
            if (animator == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                {
                    animator.SetBool(paramName, value);
                    return;
                }
            }
        }

        private void SetAnimatorFloatSafe(string paramName, float value)
        {
            if (animator == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Float && p.name == paramName)
                {
                    animator.SetFloat(paramName, value);
                    return;
                }
            }
        }

        private void SetAnimatorTriggerSafe(string paramName)
        {
            if (animator == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
                {
                    animator.SetTrigger(paramName);
                    return;
                }
            }
        }

        protected virtual void HandleDeath(GameObject killer)
        {
            if (animator != null)
            {
                animator.SetTrigger("Die");
                animator.SetBool("IsDead", true);
            }

            foreach (var ab in abilities)
            {
                if (ab != null) ab.Cancel();
            }
        }

        public virtual void ResetDragon(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            isAttacking = false;
            isFlying = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            health?.ResetHealth();

            foreach (var ab in abilities)
            {
                if (ab != null) ab.ResetCooldown();
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            FindOpponent();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Vector3.zero, arenaRadius);
        }
    }
}
