using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonBattle.Abilities;

namespace DragonBattle.UI
{
    public class AbilitySlotUI : MonoBehaviour
    {
        [Header("UI Bindings")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlayImage;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Text cooldownTextLegacy;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Text hotkeyTextLegacy;
        [SerializeField] private TextMeshProUGUI abilityNameText;
        [SerializeField] private Text abilityNameTextLegacy;
        [SerializeField] private Button actionButton;

        private AbilityBase boundAbility;

        private void Awake()
        {
            AutoFindReferences();
        }

        public void AutoFindReferences()
        {
            if (actionButton == null) actionButton = GetComponent<Button>();

            if (hotkeyText == null && hotkeyTextLegacy == null)
            {
                var h = transform.Find("KeyBadge/KeyText");
                if (h == null) h = transform.Find("KeyText");
                if (h != null)
                {
                    hotkeyText = h.GetComponent<TextMeshProUGUI>();
                    hotkeyTextLegacy = h.GetComponent<Text>();
                }
            }

            if (abilityNameText == null && abilityNameTextLegacy == null)
            {
                var n = transform.Find("AbilityName");
                if (n == null) n = transform.Find("NameText");
                if (n != null)
                {
                    abilityNameText = n.GetComponent<TextMeshProUGUI>();
                    abilityNameTextLegacy = n.GetComponent<Text>();
                }
            }

            if (cooldownText == null && cooldownTextLegacy == null)
            {
                var c = transform.Find("CDText");
                if (c == null) c = transform.Find("CooldownText");
                if (c != null)
                {
                    cooldownText = c.GetComponent<TextMeshProUGUI>();
                    cooldownTextLegacy = c.GetComponent<Text>();
                }
            }

            if (iconImage == null || cooldownOverlayImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.gameObject.name.Contains("Icon"))
                    {
                        if (iconImage == null) iconImage = img;
                    }
                    else if (img.gameObject.name.Contains("Cooldown") || img.gameObject.name.Contains("Overlay") || img.gameObject.name.Contains("Radial"))
                    {
                        if (cooldownOverlayImage == null) cooldownOverlayImage = img;
                    }
                }
            }

            if (cooldownOverlayImage != null)
            {
                if (cooldownOverlayImage.sprite == null) cooldownOverlayImage.sprite = OverheadHealthBar.GetSolidWhiteSprite();
                cooldownOverlayImage.type = Image.Type.Filled;
                cooldownOverlayImage.fillMethod = Image.FillMethod.Radial360;
                cooldownOverlayImage.fillOrigin = (int)Image.Origin360.Top;
                cooldownOverlayImage.fillClockwise = false;
            }
        }

        public void BindAbility(AbilityBase ability, string keyLabel)
        {
            AutoFindReferences();
            boundAbility = ability;

            if (hotkeyText != null) hotkeyText.text = keyLabel;
            if (hotkeyTextLegacy != null) hotkeyTextLegacy.text = keyLabel;

            if (ability != null)
            {
                if (abilityNameText != null) abilityNameText.text = ability.AbilityName;
                if (abilityNameTextLegacy != null) abilityNameTextLegacy.text = ability.AbilityName;
            }

            if (boundAbility != null)
            {
                if (iconImage != null && boundAbility.IconSprite != null)
                {
                    iconImage.sprite = boundAbility.IconSprite;
                }
                boundAbility.OnCooldownUpdated += UpdateCooldown;
                UpdateCooldown(boundAbility.CurrentCooldown, boundAbility.Cooldown);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                actionButton.onClick.AddListener(OnSlotClicked);
            }
        }

        private void OnSlotClicked()
        {
            if (boundAbility != null && boundAbility.CanExecute())
            {
                boundAbility.TryExecute();
            }
        }

        private void UpdateCooldown(float remaining, float maxCooldown)
        {
            float percent = Mathf.Clamp01(remaining / Mathf.Max(0.01f, maxCooldown));

            if (cooldownOverlayImage != null)
            {
                cooldownOverlayImage.fillAmount = percent;
                cooldownOverlayImage.enabled = remaining > 0.05f;
            }

            string cdStr = remaining > 0.05f ? (remaining > 1.0f ? remaining.ToString("F1") + "s" : remaining.ToString("F1")) : "";
            bool showCD = remaining > 0.05f;

            if (cooldownText != null)
            {
                cooldownText.text = cdStr;
                cooldownText.enabled = showCD;
            }
            if (cooldownTextLegacy != null)
            {
                cooldownTextLegacy.text = cdStr;
                cooldownTextLegacy.enabled = showCD;
            }

            if (actionButton != null)
            {
                actionButton.interactable = remaining <= 0.05f;
            }
        }

        private void OnDestroy()
        {
            if (boundAbility != null)
            {
                boundAbility.OnCooldownUpdated -= UpdateCooldown;
            }
        }
    }
}
