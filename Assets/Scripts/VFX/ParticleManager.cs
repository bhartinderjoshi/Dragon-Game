using UnityEngine;

namespace DragonBattle.VFX
{
    public class ParticleManager : MonoBehaviour
    {
        private static ParticleManager instance;
        public static ParticleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<ParticleManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("ParticleManager");
                        instance = go.AddComponent<ParticleManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Custom Particle Prefabs (Optional)")]
        [SerializeField] private GameObject customFireBreathPrefab;
        [SerializeField] private GameObject customTailSwipePrefab;
        [SerializeField] private GameObject customGroundSlamPrefab;
        [SerializeField] private GameObject customHitSparkPrefab;

        private void Awake()
        {
            if (instance == null) instance = this;
            else if (instance != this) { Destroy(gameObject); return; }
        }

        public ParticleSystem SetupFireBreath(Transform dragonMouth)
        {
            if (customFireBreathPrefab != null)
            {
                var go = Instantiate(customFireBreathPrefab, dragonMouth);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                return go.GetComponentInChildren<ParticleSystem>();
            }
            return ProceduralVFXGenerator.CreateFireBreathSystem(dragonMouth);
        }

        public void SpawnTailSwipe(Vector3 position, Quaternion rotation)
        {
            if (customTailSwipePrefab != null)
            {
                var go = Instantiate(customTailSwipePrefab, position, rotation);
                Destroy(go, 1.5f);
                return;
            }
            ProceduralVFXGenerator.CreateTailSwipeVFX(position, rotation);
        }

        public void SpawnGroundSlam(Vector3 position)
        {
            if (customGroundSlamPrefab != null)
            {
                var go = Instantiate(customGroundSlamPrefab, position, Quaternion.identity);
                Destroy(go, 2.0f);
                return;
            }
            ProceduralVFXGenerator.CreateGroundSlamShockwave(position);
        }

        public void SpawnHitSpark(Vector3 position)
        {
            if (customHitSparkPrefab != null)
            {
                var go = Instantiate(customHitSparkPrefab, position, Quaternion.identity);
                Destroy(go, 1.0f);
                return;
            }
            ProceduralVFXGenerator.CreateHitSpark(position);
        }
    }
}
