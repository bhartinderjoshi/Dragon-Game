using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonBattle.Combat;

namespace DragonBattle.UI
{
    public class OverheadHealthBar : MonoBehaviour
    {
        [Header("Target & Positioning")]
        [SerializeField] private Health targetHealth;
        [SerializeField] private bool followWorldTarget = false;
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 3.2f, 0);

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Text nameTextLegacy;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image damageLagFillImage;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Text hpTextLegacy;

        [Header("Styling")]
        [SerializeField] private Color playerHealthColor = new Color(0.18f, 0.8f, 0.44f);
        [SerializeField] private Color enemyHealthColor = new Color(0.92f, 0.23f, 0.23f);

        private Transform camTransform;
        private float targetFill = 1f;

        public Health TargetHealth => targetHealth;

        private void Awake()
        {
            AutoFindReferences();
        }

        public void AutoFindReferences()
        {
            if (nameText == null)
            {
                var n = transform.Find("NameText");
                if (n == null) n = transform.Find("NameLabel");
                if (n != null)
                {
                    nameText = n.GetComponent<TextMeshProUGUI>();
                    if (nameTextLegacy == null) nameTextLegacy = n.GetComponent<Text>();
                }
            }

            if (hpText == null)
            {
                var h = transform.Find("HPText");
                if (h == null) h = transform.Find("HealthText");
                if (h != null)
                {
                    hpText = h.GetComponent<TextMeshProUGUI>();
                    if (hpTextLegacy == null) hpTextLegacy = h.GetComponent<Text>();
                }
            }

            if (nameText == null && nameTextLegacy == null)
            {
                var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTMP)
                {
                    if (t.gameObject.name.ToLower().Contains("name")) { nameText = t; break; }
                }
                var allText = GetComponentsInChildren<Text>(true);
                foreach (var t in allText)
                {
                    if (t.gameObject.name.ToLower().Contains("name")) { nameTextLegacy = t; break; }
                }
            }

            if (hpText == null && hpTextLegacy == null)
            {
                var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in allTMP)
                {
                    if (t.gameObject.name.ToLower().Contains("hp") || t.gameObject.name.ToLower().Contains("health")) { hpText = t; break; }
                }
                var allText = GetComponentsInChildren<Text>(true);
                foreach (var t in allText)
                {
                    if (t.gameObject.name.ToLower().Contains("hp") || t.gameObject.name.ToLower().Contains("health")) { hpTextLegacy = t; break; }
                }
            }

            if (healthFillImage == null || damageLagFillImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.gameObject.name.Contains("HealthFill") || img.gameObject.name.Contains("Foreground") || img.gameObject.name.Contains("Fill"))
                    {
                        if (healthFillImage == null) healthFillImage = img;
                    }
                    else if (img.gameObject.name.Contains("DamageLag") || img.gameObject.name.Contains("Lag"))
                    {
                        if (damageLagFillImage == null) damageLagFillImage = img;
                    }
                }
            }

            if (healthFillImage != null)
            {
                if (healthFillImage.sprite == null) healthFillImage.sprite = GetSolidWhiteSprite();
                healthFillImage.type = Image.Type.Filled;
                healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            if (damageLagFillImage != null)
            {
                if (damageLagFillImage.sprite == null) damageLagFillImage.sprite = GetSolidWhiteSprite();
                damageLagFillImage.type = Image.Type.Filled;
                damageLagFillImage.fillMethod = Image.FillMethod.Horizontal;
                damageLagFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        private static Sprite solidWhiteSprite;
        public static Sprite GetSolidWhiteSprite()
        {
            if (solidWhiteSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                solidWhiteSprite.name = "Runtime_SolidWhite";
            }
            return solidWhiteSprite;
        }

        public void BindHealth(Health health, string displayName, bool isPlayer)
        {
            AutoFindReferences();
            targetHealth = health;
            if (nameText != null) nameText.text = displayName;
            if (nameTextLegacy != null) nameTextLegacy.text = displayName;
            if (healthFillImage != null)
            {
                healthFillImage.color = isPlayer ? playerHealthColor : enemyHealthColor;
            }

            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged -= UpdateHealth;
                targetHealth.OnHealthChanged += UpdateHealth;
                UpdateHealth(targetHealth.CurrentHealth, targetHealth.MaxHealth);
                if (damageLagFillImage != null)
                {
                    damageLagFillImage.fillAmount = targetFill;
                }
            }
        }

        private void Start()
        {
            AutoFindReferences();
            if (UnityEngine.Camera.main != null) camTransform = UnityEngine.Camera.main.transform;
            if (targetHealth != null)
            {
                UpdateHealth(targetHealth.CurrentHealth, targetHealth.MaxHealth);
            }
        }

        private void LateUpdate()
        {
            if (targetHealth == null)
            {
                // Auto re-bind if dragon was added
                var allDragons = FindObjectsByType<Core.DragonController>(FindObjectsInactive.Exclude);
                foreach (var d in allDragons)
                {
                    if (d == null || d.Health == null) continue;
                    if (nameText != null && nameText.text.ToLower().Contains("enemy") && !d.IsPlayer)
                    {
                        BindHealth(d.Health, d.DragonName, false);
                        break;
                    }
                    else if (d.IsPlayer)
                    {
                        BindHealth(d.Health, d.DragonName, true);
                        break;
                    }
                }

                if (targetHealth == null) return;
            }

            if (followWorldTarget)
            {
                // Position above dragon in world-space
                transform.position = targetHealth.transform.position + worldOffset;

                if (camTransform == null && UnityEngine.Camera.main != null) camTransform = UnityEngine.Camera.main.transform;
                if (camTransform != null)
                {
                    transform.rotation = camTransform.rotation;
                }
            }

            // Smooth damage lag bar interpolation
            if (damageLagFillImage != null)
            {
                if (damageLagFillImage.fillAmount > targetFill)
                {
                    damageLagFillImage.fillAmount = Mathf.MoveTowards(damageLagFillImage.fillAmount, targetFill, Time.deltaTime * 0.75f);
                }
                else
                {
                    damageLagFillImage.fillAmount = targetFill;
                }
            }
        }

        private void UpdateHealth(float current, float max)
        {
            targetFill = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = targetFill;
            }
            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
            if (hpTextLegacy != null)
            {
                hpTextLegacy.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void OnDestroy()
        {
            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged -= UpdateHealth;
            }
        }
    }
}
