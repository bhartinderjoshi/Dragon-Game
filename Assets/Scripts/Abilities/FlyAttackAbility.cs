using System.Collections;
using UnityEngine;
using DragonBattle.Audio;
using DragonBattle.Camera;
using DragonBattle.Combat;
using DragonBattle.VFX;

namespace DragonBattle.Abilities
{
    public class FlyAttackAbility : AbilityBase
    {
        [Header("Flight Settings")]
        [SerializeField] private float flightHeight = 7.5f;
        [SerializeField] private float ascendDuration = 0.55f;
        [SerializeField] private float hoverDuration = 0.45f;
        [SerializeField] private float diveSpeed = 28f;
        [SerializeField] private float slamRadius = 6.0f;
        [SerializeField] private float slamKnockback = 18f;

        private Coroutine flightCoroutine;

        private void Reset()
        {
            abilityType = AbilityType.FlyAttack;
            abilityName = "Sky Dive";
            defaultKey = KeyCode.Alpha3;
            cooldown = 8.0f;
            baseDamage = 320f;
            effectiveRange = 14.0f;
            castDuration = 2.0f;
        }

        protected override void Execute()
        {
            if (flightCoroutine != null) StopCoroutine(flightCoroutine);
            flightCoroutine = StartCoroutine(FlyAttackRoutine());
        }

        private Vector3 GetCurrentTargetPosition()
        {
            Vector3 targetPos = transform.position;
            targetPos.y = 0f;

            var opponent = owner != null ? owner.GetOpponent() : null;
            if (opponent != null && opponent.IsAlive)
            {
                targetPos = opponent.Transform.position;
                targetPos.y = 0f;
            }
            else if (owner != null && owner.IsPlayer)
            {
                // Fallback: mouse cursor position on ground
                var cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.zero);
                    if (ground.Raycast(ray, out float enter))
                    {
                        targetPos = ray.GetPoint(enter);
                        targetPos.y = 0f;
                    }
                }
            }

            // Clamp strictly inside arena bounds
            float maxRadius = 12.5f;
            Vector2 flat = new Vector2(targetPos.x, targetPos.z);
            if (flat.magnitude > maxRadius)
            {
                flat = flat.normalized * maxRadius;
                targetPos.x = flat.x;
                targetPos.z = flat.y;
            }

            return targetPos;
        }

        private IEnumerator FlyAttackRoutine()
        {
            isCasting = true;
            owner.SetFlying(true);
            owner.SetAttacking(true);
            owner.TriggerAnimation("FlyUp");
            SoundManager.Instance?.PlayFlyLaunch(transform.position);

            Vector3 startPos = transform.position;
            startPos.y = 0f;
            Vector3 peakPos = startPos + Vector3.up * flightHeight;

            try
            {
                // 1. Ascend to peak flight height
                float elapsed = 0f;
                while (elapsed < ascendDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / ascendDuration);
                    transform.position = Vector3.Lerp(startPos, peakPos, t);

                    // Align rotation towards opponent while ascending
                    Vector3 targetGroundPos = GetCurrentTargetPosition();
                    Vector3 aimDir = targetGroundPos - transform.position;
                    aimDir.y = 0f;
                    if (aimDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir.normalized), Time.deltaTime * 12f);
                    }

                    yield return null;
                }

                // 2. Hover & Lock-on Target
                elapsed = 0f;
                while (elapsed < hoverDuration)
                {
                    elapsed += Time.deltaTime;
                    Vector3 targetGroundPos = GetCurrentTargetPosition();
                    Vector3 aimDir = targetGroundPos - transform.position;
                    aimDir.y = 0f;

                    if (aimDir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDir.normalized), Time.deltaTime * 16f);
                    }

                    yield return null;
                }

                // 3. Precision Homing Dive
                owner.TriggerAnimation("FlyDive");
                Vector3 diveStartPos = transform.position;
                Vector3 initialTargetPos = GetCurrentTargetPosition();
                float initialDistance = Vector3.Distance(diveStartPos, initialTargetPos);
                float diveDuration = Mathf.Clamp(initialDistance / diveSpeed, 0.35f, 0.65f);

                elapsed = 0f;
                Vector3 finalLandPos = initialTargetPos;

                while (elapsed < diveDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / diveDuration);

                    // Dynamic homing: continually track live target position
                    Vector3 liveTargetPos = GetCurrentTargetPosition();
                    finalLandPos = liveTargetPos;

                    // Interpolate 3D position with an accelerating plunge curve
                    float horizontalT = Mathf.SmoothStep(0f, 1f, t);
                    float verticalT = Mathf.Pow(t, 1.4f); // Accelerating downward dive

                    Vector3 horizontalPos = Vector3.Lerp(new Vector3(diveStartPos.x, 0f, diveStartPos.z), liveTargetPos, horizontalT);
                    float currentY = Mathf.Lerp(diveStartPos.y, 0f, verticalT);
                    Vector3 currentPos = new Vector3(horizontalPos.x, currentY, horizontalPos.z);

                    // Orient dragon towards dive trajectory
                    Vector3 diveVector = (liveTargetPos - transform.position);
                    if (diveVector.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(diveVector.normalized), Time.deltaTime * 20f);
                    }

                    transform.position = currentPos;
                    yield return null;
                }

                // Ensure exact landing on target at ground level
                finalLandPos.y = 0f;
                transform.position = finalLandPos;

                // Level out dragon rotation on ground
                Vector3 rotEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, rotEuler.y, 0f);

                // 4. Ground Slam Impact
                SoundManager.Instance?.PlayFlyImpact(transform.position);
                ParticleManager.Instance?.SpawnGroundSlam(transform.position);
                BattleCameraController.Instance?.Shake(0.45f, 0.7f);
                owner.TriggerAnimation("FlyLand");

                // Deal AoE damage
                var target = owner.GetOpponent();
                if (target != null && target.IsAlive)
                {
                    float dist = Vector3.Distance(transform.position, target.Transform.position);
                    if (dist <= slamRadius)
                    {
                        Vector3 knockDir = (target.Transform.position - transform.position + Vector3.up * 0.4f).normalized;
                        var dmg = new DamageInfo(
                            baseDamage,
                            gameObject,
                            target.Transform.position + Vector3.up,
                            knockDir,
                            slamKnockback,
                            true,
                            abilityName
                        );
                        target.TakeDamage(dmg);
                    }
                }

                yield return new WaitForSeconds(0.4f);
            }
            finally
            {
                if (owner != null)
                {
                    owner.SetFlying(false);
                    owner.SetAttacking(false);
                    Vector3 p = transform.position;
                    p.y = 0f;
                    transform.position = p;
                }
                isCasting = false;
                flightCoroutine = null;
            }
        }

        public override void Cancel()
        {
            if (flightCoroutine != null)
            {
                StopCoroutine(flightCoroutine);
                flightCoroutine = null;
            }
            if (owner != null)
            {
                owner.SetFlying(false);
                owner.SetAttacking(false);
                Vector3 p = transform.position;
                p.y = 0f;
                transform.position = p;
            }
            isCasting = false;
        }
    }
}
