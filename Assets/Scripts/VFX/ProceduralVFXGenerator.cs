using UnityEngine;

namespace DragonBattle.VFX
{
    public static class ProceduralVFXGenerator
    {
        private static Material defaultParticleMaterial;
        private static Texture2D softParticleTexture;

        public static Texture2D GetSoftParticleTexture()
        {
            if (softParticleTexture == null)
            {
                int size = 64;
                softParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                softParticleTexture.name = "Runtime_SoftParticle";
                float center = (size - 1) * 0.5f;
                float maxRadius = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        float normalized = Mathf.Clamp01(dist / maxRadius);
                        float alpha = Mathf.SmoothStep(1.0f, 0.0f, normalized);
                        softParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                softParticleTexture.Apply();
            }
            return softParticleTexture;
        }

        public static Material GetParticleMaterial()
        {
            if (defaultParticleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");

                defaultParticleMaterial = new Material(shader);
                defaultParticleMaterial.name = "Runtime_VFX_Material";

                var tex = GetSoftParticleTexture();
                if (defaultParticleMaterial.HasProperty("_BaseMap"))
                {
                    defaultParticleMaterial.SetTexture("_BaseMap", tex);
                }
                if (defaultParticleMaterial.HasProperty("_MainTex"))
                {
                    defaultParticleMaterial.SetTexture("_MainTex", tex);
                }
                defaultParticleMaterial.mainTexture = tex;
            }
            return defaultParticleMaterial;
        }

        public static ParticleSystem CreateFireBreathSystem(Transform parent)
        {
            var go = new GameObject("FireBreathParticles");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 1.2f, 1.0f);
            go.transform.localRotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1.0f;
            main.startLifetime = 0.65f;
            main.startSpeed = 12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            
            var colGrad = new Gradient();
            colGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 0.4f),
                    new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 0.8f),
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(colGrad);

            var emission = ps.emission;
            emission.rateOverTime = 80;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.2f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.3f);
            sizeCurve.AddKey(0.4f, 1.0f);
            sizeCurve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Add subtle light
            var lightGO = new GameObject("FireLight");
            lightGO.transform.SetParent(go.transform, false);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.45f, 0.1f);
            light.range = 7f;
            light.intensity = 2.5f;
            lightGO.SetActive(false);

            return ps;
        }

        public static GameObject CreateTailSwipeVFX(Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("TailSwipeFX");
            go.transform.position = position;
            go.transform.rotation = rotation;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = 0.35f;
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startColor = new Color(0.3f, 0.8f, 1f, 0.85f);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.arc = 180f;
            shape.radius = 2.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0f), new GradientColorKey(new Color(1f, 1f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var autoDestroy = go.AddComponent<AutoDestroyVFX>();
            autoDestroy.Lifetime = 0.6f;

            ps.Play();
            return go;
        }

        public static GameObject CreateGroundSlamShockwave(Vector3 position)
        {
            var go = new GameObject("GroundSlamVFX");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.8f;
            main.startLifetime = 0.7f;
            main.startSpeed = 8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startColor = new Color(1f, 0.6f, 0.1f, 0.9f);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            var autoDestroy = go.AddComponent<AutoDestroyVFX>();
            autoDestroy.Lifetime = 1.0f;

            ps.Play();
            return go;
        }

        public static GameObject CreateHitSpark(Vector3 position)
        {
            var go = new GameObject("HitSparkFX");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            pRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.25f;
            main.startLifetime = 0.25f;
            main.startSpeed = 5f;
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.9f, 0.3f, 1f);

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var autoDestroy = go.AddComponent<AutoDestroyVFX>();
            autoDestroy.Lifetime = 0.4f;

            ps.Play();
            return go;
        }
    }

    public class AutoDestroyVFX : MonoBehaviour
    {
        public float Lifetime = 1.0f;
        private void Start() => Destroy(gameObject, Lifetime);
    }
}
