using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonBattle.Core;
using DragonBattle.Player;
using DragonBattle.AI;
using DragonBattle.Audio;

namespace DragonBattle.UI
{
    public class BattleUIManager : MonoBehaviour
    {
        public static BattleUIManager Instance { get; private set; }

        [Header("HUD Slots")]
        [SerializeField] private List<AbilitySlotUI> abilitySlots = new List<AbilitySlotUI>();
        [SerializeField] private OverheadHealthBar playerHealthBar;
        [SerializeField] private OverheadHealthBar enemyHealthBar;

        [Header("Winner / Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI winnerTitleText;
        [SerializeField] private TextMeshProUGUI winnerSubtitleText;
        [SerializeField] private TextMeshProUGUI matchStatsText;
        [SerializeField] private Button restartButton;

        [Header("Top HUD (Optional)")]
        [SerializeField] private TextMeshProUGUI timerText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            AutoFindUIReferences();
            BindRestartButton();
        }

        private void Start()
        {
            AutoFindUIReferences();
            BindRestartButton();
            InitializeUI();
        }

        public void AutoFindUIReferences()
        {
            if (abilitySlots == null || abilitySlots.Count == 0)
            {
                abilitySlots = new List<AbilitySlotUI>(GetComponentsInChildren<AbilitySlotUI>(true));
            }

            if (playerHealthBar == null || enemyHealthBar == null)
            {
                var healthBars = GetComponentsInChildren<OverheadHealthBar>(true);
                if (healthBars.Length > 0 && playerHealthBar == null) playerHealthBar = healthBars[0];
                if (healthBars.Length > 1 && enemyHealthBar == null) enemyHealthBar = healthBars[1];
            }

            // Game Over / Winner Panel
            if (gameOverPanel == null)
            {
                var panelTransform = transform.Find("WinnerScreenPanel");
                if (panelTransform == null) panelTransform = transform.Find("WinnerPanel");
                if (panelTransform == null) panelTransform = transform.Find("GameOverPanel");
                if (panelTransform != null) gameOverPanel = panelTransform.gameObject;
            }

            if (gameOverPanel != null)
            {
                var tmps = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in tmps)
                {
                    string n = txt.gameObject.name.ToLower();
                    if (n.Contains("title") && winnerTitleText == null) winnerTitleText = txt;
                    else if (n.Contains("sub") && winnerSubtitleText == null) winnerSubtitleText = txt;
                    else if (n.Contains("stat") && matchStatsText == null) matchStatsText = txt;
                }

                if (winnerTitleText == null && tmps.Length > 0) winnerTitleText = tmps[0];
                if (winnerSubtitleText == null && tmps.Length > 1) winnerSubtitleText = tmps[1];
                if (matchStatsText == null && tmps.Length > 2) matchStatsText = tmps[2];

                if (restartButton == null)
                {
                    restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
                }
            }

            // Deactivate any start menu panels in scene
            var startP = transform.Find("StartMenuPanel");
            if (startP != null) startP.gameObject.SetActive(false);

            if (timerText == null)
            {
                var timerTransform = transform.Find("TopHUD/TopTimer");
                if (timerTransform == null) timerTransform = transform.Find("TopTimer");
                if (timerTransform != null) timerText = timerTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        private void BindRestartButton()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Update()
        {
            if (timerText != null && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Battle)
            {
                int seconds = Mathf.FloorToInt(GameManager.Instance.MatchDuration);
                timerText.text = $"BATTLE TIME: {seconds / 60:D2}:{seconds % 60:D2}";
            }

            // Keyboard shortcut to restart when Game Over screen is up
            if (gameOverPanel != null && gameOverPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    OnRestartClicked();
                }
            }

            // Auto re-bind if player or enemy health bars are not bound
            if (playerHealthBar != null && playerHealthBar.TargetHealth == null)
            {
                var player = FindAnyObjectByType<PlayerDragonController>();
                if (player != null && player.Health != null)
                {
                    playerHealthBar.BindHealth(player.Health, player.DragonName, true);
                }
            }

            if (enemyHealthBar != null && enemyHealthBar.TargetHealth == null)
            {
                var enemy = FindAnyObjectByType<DragonAIController>();
                if (enemy != null && enemy.Health != null)
                {
                    enemyHealthBar.BindHealth(enemy.Health, enemy.DragonName, false);
                }
            }
        }

        public void InitializeUI()
        {
            AutoFindUIReferences();
            BindRestartButton();

            var player = FindAnyObjectByType<PlayerDragonController>();
            var enemy = FindAnyObjectByType<DragonAIController>();

            // Bind Overhead Health Bars
            if (player != null && player.Health != null && playerHealthBar != null)
            {
                playerHealthBar.BindHealth(player.Health, player.DragonName, true);
            }
            if (enemy != null && enemy.Health != null && enemyHealthBar != null)
            {
                enemyHealthBar.BindHealth(enemy.Health, enemy.DragonName, false);
            }

            // Bind Ability Slots
            if (player != null)
            {
                player.InitializeAbilities();
                string[] hotkeys = new string[] { "1", "2", "3" };
                for (int i = 0; i < abilitySlots.Count && i < player.Abilities.Count; i++)
                {
                    if (abilitySlots[i] != null && player.Abilities[i] != null)
                    {
                        abilitySlots[i].BindAbility(player.Abilities[i], hotkeys[Mathf.Min(i, hotkeys.Length - 1)]);
                    }
                }
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void ShowGameOver(string winnerDragonName, bool isPlayerWinner, float duration)
        {
            AutoFindUIReferences();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            string title = isPlayerWinner ? "VICTORY!" : "DEFEAT";
            Color titleColor = isPlayerWinner ? new Color(1f, 0.85f, 0.2f) : new Color(0.95f, 0.25f, 0.25f);
            string subtitle = isPlayerWinner ? $"{winnerDragonName} Claims Victory!" : $"{winnerDragonName} Has Slain You!";
            int secs = Mathf.FloorToInt(duration);
            string stats = $"Match Duration: {secs} seconds";

            // Set TMP text if present
            if (winnerTitleText != null)
            {
                winnerTitleText.text = title;
                winnerTitleText.color = titleColor;
            }
            if (winnerSubtitleText != null)
            {
                winnerSubtitleText.text = subtitle;
            }
            if (matchStatsText != null)
            {
                matchStatsText.text = stats;
            }

            // Also search all legacy Text components in gameOverPanel to guarantee update
            if (gameOverPanel != null)
            {
                var legacyTexts = gameOverPanel.GetComponentsInChildren<Text>(true);
                foreach (var txt in legacyTexts)
                {
                    string n = txt.gameObject.name.ToLower();
                    if (n.Contains("title"))
                    {
                        txt.text = title;
                        txt.color = titleColor;
                    }
                    else if (n.Contains("sub"))
                    {
                        txt.text = subtitle;
                    }
                    else if (n.Contains("stat"))
                    {
                        txt.text = stats;
                    }
                }
            }
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.RestartMatch();
        }
    }
}
