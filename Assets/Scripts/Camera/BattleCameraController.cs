using UnityEngine;
using DragonBattle.Player;
using DragonBattle.AI;

namespace DragonBattle.Camera
{
    public class BattleCameraController : MonoBehaviour
    {
        public static BattleCameraController Instance { get; private set; }

        [Header("Target Tracking")]
        [SerializeField] private Transform targetA; // Player Dragon
        [SerializeField] private Transform targetB; // AI Dragon

        [Header("Framing & Perspective")]
        [SerializeField] private Vector3 cameraAngle = new Vector3(50f, 0f, 0f);
        [SerializeField] private float minDistance = 13f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float positionSmoothTime = 0.22f;
        [SerializeField] private float zoomSmoothTime = 0.3f;
        [SerializeField] private float zoomLimiter = 22f;
        [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Screen Shake")]
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0f;
        private Vector3 shakeOffset = Vector3.zero;

        private Vector3 positionVelocity;
        private float zoomVelocity;
        private Vector3 smoothedCenter;
        private Vector3 centerVelocity;
        private float currentCamDist;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            transform.rotation = Quaternion.Euler(cameraAngle);
            currentCamDist = minDistance;
        }

        private void Start()
        {
            FindTargetsIfNull();
            if (targetA != null || targetB != null)
            {
                smoothedCenter = GetCenterPoint() + centerOffset;
                transform.position = smoothedCenter + Quaternion.Euler(cameraAngle) * new Vector3(0f, 0f, -minDistance);
            }
        }

        public void SetTargets(Transform a, Transform b)
        {
            targetA = a;
            targetB = b;
        }

        private void FindTargetsIfNull()
        {
            if (targetA == null)
            {
                var p = FindAnyObjectByType<PlayerDragonController>();
                if (p != null) targetA = p.transform;
            }
            if (targetB == null)
            {
                var ai = FindAnyObjectByType<DragonAIController>();
                if (ai != null) targetB = ai.transform;
            }
        }

        private void LateUpdate()
        {
            if (targetA == null || targetB == null)
            {
                FindTargetsIfNull();
                if (targetA == null && targetB == null) return;
            }

            Vector3 rawCenter = GetCenterPoint() + centerOffset;
            smoothedCenter = Vector3.SmoothDamp(smoothedCenter, rawCenter, ref centerVelocity, positionSmoothTime * 0.5f);

            float distance = GetGreatestDistance();
            float targetCamDist = Mathf.Lerp(minDistance, maxDistance, Mathf.Clamp01(distance / zoomLimiter));
            currentCamDist = Mathf.SmoothDamp(currentCamDist, targetCamDist, ref zoomVelocity, zoomSmoothTime);

            Quaternion rot = Quaternion.Euler(cameraAngle);
            Vector3 offsetVector = rot * new Vector3(0f, 0f, -currentCamDist);

            Vector3 targetPosition = smoothedCenter + offsetVector;

            // Apply Screen Shake
            HandleScreenShake();
            targetPosition += shakeOffset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, positionSmoothTime);
            transform.rotation = rot;
        }

        private Vector3 GetCenterPoint()
        {
            if (targetA != null && targetB != null)
            {
                var bounds = new Bounds(targetA.position, Vector3.zero);
                bounds.Encapsulate(targetB.position);
                return bounds.center;
            }
            if (targetA != null) return targetA.position;
            if (targetB != null) return targetB.position;
            return Vector3.zero;
        }

        private float GetGreatestDistance()
        {
            if (targetA != null && targetB != null)
            {
                return Vector3.Distance(targetA.position, targetB.position);
            }
            return 8f;
        }

        public void Shake(float duration = 0.3f, float magnitude = 0.5f)
        {
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        }

        private void HandleScreenShake()
        {
            if (shakeDuration > 0f)
            {
                float seed = Time.time * 25f;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f,
                    0f
                ) * shakeMagnitude;

                shakeDuration -= Time.deltaTime;
                shakeMagnitude = Mathf.MoveTowards(shakeMagnitude, 0f, Time.deltaTime * 1.5f);
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }
    }
}
