using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DragonBattle.Combat
{
    public class FloatingDamageText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textComponent;
        [SerializeField] private float floatSpeed = 2.5f;
        [SerializeField] private float lifetime = 1.0f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 0.2f, 1.3f);

        private float timer = 0f;
        private Color initialColor;
        private Vector3 startScale;
        private Transform camTransform;

        private void Awake()
        {
            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (UnityEngine.Camera.main != null)
            {
                camTransform = UnityEngine.Camera.main.transform;
            }
            startScale = transform.localScale;
        }

        public void Initialize(string text, Color color, float sizeScale = 1.0f)
        {
            if (textComponent == null) textComponent = GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
                initialColor = color;
            }
            transform.localScale = startScale * sizeScale;
            timer = 0f;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (camTransform == null && UnityEngine.Camera.main != null)
            {
                camTransform = UnityEngine.Camera.main.transform;
            }

            if (camTransform != null)
            {
                // Billboard to camera
                transform.rotation = Quaternion.LookRotation(transform.position - camTransform.position);
            }

            // Move upwards
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            float progress = timer / lifetime;

            // Fade out
            if (textComponent != null)
            {
                Color c = initialColor;
                c.a = Mathf.Clamp01(1f - progress);
                textComponent.color = c;
            }

            if (timer >= lifetime)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
