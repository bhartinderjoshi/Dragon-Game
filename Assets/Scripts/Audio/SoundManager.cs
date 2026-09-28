using System.Collections.Generic;
using UnityEngine;

namespace DragonBattle.Audio
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SoundManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("SoundManager");
                        instance = go.AddComponent<SoundManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Audio Clips (Optional Overrides)")]
        [SerializeField] private AudioClip fireBreathClip;
        [SerializeField] private AudioClip tailAttackClip;
        [SerializeField] private AudioClip flyLaunchClip;
        [SerializeField] private AudioClip flyImpactClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

        private AudioSource audio2DSource;
        private Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (audio2DSource == null)
            {
                audio2DSource = gameObject.AddComponent<AudioSource>();
                audio2DSource.playOnAwake = false;
                audio2DSource.spatialBlend = 0f; // 2D clean audio
            }

            GenerateProceduralSounds();
        }

        private void GenerateProceduralSounds()
        {
            // Procedural Fire Breath (low rumble + noise stream)
            proceduralClips["fire"] = CreateNoiseClip(1.2f, 22050, (t, p) =>
            {
                float env = Mathf.Sin(p * Mathf.PI);
                float noise = (Random.value * 2f - 1f) * 0.6f;
                float rumble = Mathf.Sin(t * 120f * Mathf.PI * 2f) * 0.4f;
                return (noise + rumble) * env * 0.6f;
            });

            // Procedural Tail Attack (quick whoosh + whip thud)
            proceduralClips["tail"] = CreateNoiseClip(0.35f, 22050, (t, p) =>
            {
                float env = Mathf.Pow(1f - p, 2f);
                float freq = Mathf.Lerp(400f, 80f, p);
                float wave = Mathf.Sin(t * freq * Mathf.PI * 2f);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                return (wave * 0.7f + noise * 0.3f) * env;
            });

            // Procedural Fly Launch (rising whistle + wing beat)
            proceduralClips["fly_launch"] = CreateNoiseClip(0.8f, 22050, (t, p) =>
            {
                float freq = Mathf.Lerp(120f, 480f, p);
                float wave = Mathf.Sin(t * freq * Mathf.PI * 2f);
                float env = Mathf.Sin(p * Mathf.PI);
                return wave * env * 0.5f;
            });

            // Procedural Fly Slam Impact (heavy sub bass explosion)
            proceduralClips["fly_impact"] = CreateNoiseClip(0.7f, 22050, (t, p) =>
            {
                float env = Mathf.Exp(-p * 6f);
                float freq = Mathf.Lerp(180f, 40f, p);
                float sub = Mathf.Sin(t * freq * Mathf.PI * 2f);
                float blast = (Random.value * 2f - 1f) * 0.6f;
                return (sub * 0.7f + blast * 0.3f) * env;
            });

            // Procedural Hit Impact (sharp punch/flesh hit)
            proceduralClips["hit"] = CreateNoiseClip(0.15f, 22050, (t, p) =>
            {
                float env = Mathf.Exp(-p * 18f);
                float tone = Mathf.Sin(t * 220f * Mathf.PI * 2f);
                float snap = (Random.value * 2f - 1f);
                return (tone * 0.4f + snap * 0.6f) * env;
            });

            // Procedural Death Roar
            proceduralClips["death"] = CreateNoiseClip(1.6f, 22050, (t, p) =>
            {
                float env = Mathf.Sin(p * Mathf.PI);
                float freq = Mathf.Lerp(260f, 60f, p);
                float roar = Mathf.Sin(t * freq * Mathf.PI * 2f) + Mathf.Sin(t * (freq * 1.5f) * Mathf.PI * 2f) * 0.5f;
                float growl = (Random.value * 2f - 1f) * 0.45f;
                return (roar + growl) * env * 0.75f;
            });

            // Procedural Grand Victory Fanfare (Multi-note triumphant brass + chime fanfare)
            proceduralClips["victory"] = CreateNoiseClip(2.8f, 22050, (t, p) =>
            {
                float env = Mathf.Clamp01((2.8f - t) / 2.8f);
                // Brass arpeggio sequence based on time
                float noteFreq = 523.25f; // C5
                if (t > 0.2f && t < 0.45f) noteFreq = 659.25f; // E5
                else if (t >= 0.45f && t < 0.75f) noteFreq = 783.99f; // G5
                else if (t >= 0.75f) noteFreq = 1046.50f; // High C6 (sustained triumph)

                float brassLead = Mathf.Sin(t * noteFreq * Mathf.PI * 2f) * 0.35f;
                float brassHarmonic = Mathf.Sin(t * (noteFreq * 2f) * Mathf.PI * 2f) * 0.15f;

                // Triumphant backing harmony chord
                float cMajor1 = Mathf.Sin(t * 261.63f * Mathf.PI * 2f) * 0.2f; // C4
                float cMajor2 = Mathf.Sin(t * 329.63f * Mathf.PI * 2f) * 0.15f; // E4
                float cMajor3 = Mathf.Sin(t * 392.00f * Mathf.PI * 2f) * 0.15f; // G4

                // Shimmer chime bell
                float bell = Mathf.Sin(t * 1567.98f * Mathf.PI * 2f) * Mathf.Exp(-p * 3.5f) * 0.2f;

                return (brassLead + brassHarmonic + cMajor1 + cMajor2 + cMajor3 + bell) * env * 0.95f;
            });

            // Procedural Tragic Defeat Dirge (Dark minor chords + doom gong)
            proceduralClips["defeat"] = CreateNoiseClip(3.0f, 22050, (t, p) =>
            {
                float env = Mathf.Clamp01((3.0f - t) / 3.0f);
                // Low Doom Gong & Heartbeat Thud
                float gong = Mathf.Sin(t * 65.41f * Mathf.PI * 2f) * Mathf.Exp(-p * 2.2f) * 0.55f; // C2 deep rumble

                // Melancholic descending minor triad
                float minorNote = 311.13f; // Eb4 (dark minor 3rd)
                if (t >= 0.8f && t < 1.6f) minorNote = 293.66f; // D4
                else if (t >= 1.6f) minorNote = 261.63f; // C4 (dismal resolution)

                float somberLead = Mathf.Sin(t * minorNote * Mathf.PI * 2f) * 0.3f;
                float bassDrone = Mathf.Sin(t * 130.81f * Mathf.PI * 2f) * 0.25f; // C3
                float lowAb = Mathf.Sin(t * 103.83f * Mathf.PI * 2f) * 0.2f; // Ab2 (somber tone)

                float breathLoss = (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-p * 1.5f);
                return (gong + somberLead + bassDrone + lowAb + breathLoss) * env * 0.95f;
            });

            // Procedural Game Start Announcer Fanfare / War Horn & Gong
            proceduralClips["start_battle"] = CreateNoiseClip(2.0f, 22050, (t, p) =>
            {
                float env = Mathf.Exp(-p * 2.5f);
                float gong = Mathf.Sin(t * 90f * Mathf.PI * 2f) * Mathf.Exp(-p * 4f) * 0.6f;
                float horn1 = Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.35f;
                float horn2 = Mathf.Sin(t * 277.18f * Mathf.PI * 2f) * 0.35f;
                float horn3 = Mathf.Sin(t * 329.63f * Mathf.PI * 2f) * 0.35f;
                float horn4 = Mathf.Sin(t * 440f * Mathf.PI * 2f) * 0.3f;
                float noise = (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-p * 10f);
                return (gong + (horn1 + horn2 + horn3 + horn4) * 0.5f + noise) * env;
            });

            // Procedural Dragon Roar
            proceduralClips["roar"] = CreateNoiseClip(1.4f, 22050, (t, p) =>
            {
                float env = Mathf.Sin(p * Mathf.PI);
                float freq = Mathf.Lerp(180f, 95f, p);
                float rumble = Mathf.Sin(t * freq * Mathf.PI * 2f) * 0.5f;
                float growl = (Random.value * 2f - 1f) * 0.5f;
                float formant = Mathf.Sin(t * (freq * 2.2f) * Mathf.PI * 2f) * 0.3f;
                return (rumble + growl + formant) * env * 0.85f;
            });

            // Procedural Cooldown Ready chime
            proceduralClips["ready"] = CreateNoiseClip(0.3f, 22050, (t, p) =>
            {
                float env = Mathf.Exp(-p * 8f);
                float chime1 = Mathf.Sin(t * 880f * Mathf.PI * 2f) * 0.35f;
                float chime2 = Mathf.Sin(t * 1320f * Mathf.PI * 2f) * 0.35f;
                return (chime1 + chime2) * env;
            });

            // Procedural UI Click
            proceduralClips["click"] = CreateNoiseClip(0.08f, 22050, (t, p) =>
            {
                float env = Mathf.Exp(-p * 25f);
                return Mathf.Sin(t * 880f * Mathf.PI * 2f) * env * 0.5f;
            });
        }

        private AudioClip CreateNoiseClip(float duration, int sampleRate, System.Func<float, float, float> generator)
        {
            int numSamples = (int)(duration * sampleRate);
            float[] samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / numSamples;
                samples[i] = generator(t, progress);
            }

            AudioClip clip = AudioClip.Create("SynthClip", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void PlayFireBreath(Vector3 position)
        {
            PlaySound(fireBreathClip, "fire", position, 0.9f);
        }

        public void PlayTailAttack(Vector3 position)
        {
            PlaySound(tailAttackClip, "tail", position, 1.0f);
        }

        public void PlayFlyLaunch(Vector3 position)
        {
            PlaySound(flyLaunchClip, "fly_launch", position, 0.95f);
        }

        public void PlayFlyImpact(Vector3 position)
        {
            PlaySound(flyImpactClip, "fly_impact", position, 1.1f);
        }

        public void PlayHitSound(Vector3 position)
        {
            PlaySound(hitClip, "hit", position, 0.85f);
        }

        public void PlayDeathSound(Vector3 position)
        {
            PlaySound(deathClip, "death", position, 1.0f);
        }

        public void PlayBattleStart()
        {
            AudioClip clip = proceduralClips.GetValueOrDefault("start_battle");
            if (clip != null && audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, masterVolume * 1.1f);
            }
        }

        public void PlayDragonRoar(Vector3 position)
        {
            PlaySound(null, "roar", position, 1.0f);
        }

        public void PlayVictory()
        {
            AudioClip clip = victoryClip != null ? victoryClip : proceduralClips.GetValueOrDefault("victory");
            if (clip != null && audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, masterVolume);
            }
        }

        public void PlayDefeat()
        {
            AudioClip clip = defeatClip != null ? defeatClip : proceduralClips.GetValueOrDefault("defeat");
            if (clip != null && audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, masterVolume * 0.95f);
            }
        }

        public void PlayAbilityReady()
        {
            AudioClip clip = proceduralClips.GetValueOrDefault("ready");
            if (clip != null && audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, masterVolume * 0.5f);
            }
        }

        public void PlayButtonClick()
        {
            AudioClip clip = buttonClickClip != null ? buttonClickClip : proceduralClips.GetValueOrDefault("click");
            if (clip != null && audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, masterVolume * 0.7f);
            }
        }

        private void PlaySound(AudioClip overrideClip, string proceduralKey, Vector3 position, float volumeMultiplier = 1.0f)
        {
            AudioClip clip = overrideClip != null ? overrideClip : proceduralClips.GetValueOrDefault(proceduralKey);
            if (clip == null) return;

            float vol = masterVolume * sfxVolume * volumeMultiplier;
            if (audio2DSource != null)
            {
                audio2DSource.PlayOneShot(clip, vol);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, position, vol);
            }
        }
    }
}
