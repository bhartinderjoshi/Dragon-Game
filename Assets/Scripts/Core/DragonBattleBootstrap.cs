using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonBattle.Audio;
using DragonBattle.Combat;
using DragonBattle.VFX;
using DragonBattle.Player;
using DragonBattle.AI;
using DragonBattle.Abilities;
using DragonBattle.Camera;
using DragonBattle.UI;

namespace DragonBattle.Core
{
    [DefaultExecutionOrder(-100)]
    public class DragonBattleBootstrap : MonoBehaviour
    {
        [Header("Auto-Setup Settings")]
        [SerializeField] private bool autoConstructMissingSystems = true;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                ConstructSystems();
            }
        }

        public void ConstructSystems()
        {
            if (!autoConstructMissingSystems) return;

            EnsureCoreManagers();
            var player = EnsurePlayerDragon();
            var enemy = EnsureEnemyDragon();
            EnsureCamera(player, enemy);
            EnsureUI(player, enemy);
            EnsureGameManager(player, enemy);
        }

        private void EnsureCoreManagers()
        {
            var managers = GameObject.Find("Managers");
            if (managers == null)
            {
                var existingSM = FindAnyObjectByType<SoundManager>();
                if (existingSM == null)
                {
                    managers = new GameObject("Managers");
                    managers.AddComponent<SoundManager>();
                    managers.AddComponent<ParticleManager>();
                    managers.AddComponent<FloatingTextManager>();
                }
            }
        }

        private GameObject LoadPrefab(string relativePath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
#else
            return null;
#endif
        }

        private PlayerDragonController EnsurePlayerDragon()
        {
            var p = FindAnyObjectByType<PlayerDragonController>();
            if (p != null) return p;

            var go = new GameObject("PlayerDragon_Red");
            go.transform.position = new Vector3(-6f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.radius = 1.2f;
            col.height = 2.4f;

            p = go.AddComponent<PlayerDragonController>();
            SetField(p, "dragonName", "Player Dragon (Red)");
            SetField(p, "isPlayer", true);

            var health = go.GetComponent<Health>();
            health.SetMaxHealth(1000f);

            go.AddComponent<FireBreathAbility>();
            go.AddComponent<TailAttackAbility>();
            go.AddComponent<FlyAttackAbility>();

            // ── Load RedDragon 1.2 prefab (Red Dragon) for the Player ──────────────
            var redPrefab = LoadPrefab("Assets/RedDragon 1.2/Assets/Prefab/RedDragon1.1.prefab");
            if (redPrefab != null)
            {
                var visualObj = Instantiate(redPrefab, go.transform);
                visualObj.name = "DragonMesh";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;

                var anim = visualObj.GetComponent<Animator>();
                SetField(p, "animator", anim);
                SetField(p, "visualRoot", visualObj.transform);
            }

            p.InitializeAbilities();
            return p;
        }

        private DragonAIController EnsureEnemyDragon()
        {
            var enemy = FindAnyObjectByType<DragonAIController>();
            if (enemy != null) return enemy;

            var go = new GameObject("EnemyDragon_Black");
            go.transform.position = new Vector3(6f, 0f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.radius = 1.2f;
            col.height = 2.4f;

            enemy = go.AddComponent<DragonAIController>();
            SetField(enemy, "dragonName", "Enemy Dragon (Black)");
            SetField(enemy, "isPlayer", false);

            var health = go.GetComponent<Health>();
            health.SetMaxHealth(600f);

            enemy.MoveSpeed = 3.8f;

            go.AddComponent<FireBreathAbility>();
            go.AddComponent<TailAttackAbility>();
            go.AddComponent<FlyAttackAbility>();

            // Load BlackDragon prefab for Enemy
            var blackPrefab = LoadPrefab("Assets/RedDragon 1.2/Assets/Prefab/BlackDragon.prefab");
            if (blackPrefab != null)
            {
                var visualObj = Instantiate(blackPrefab, go.transform);
                visualObj.name = "DragonMesh";
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;

                var anim = visualObj.GetComponent<Animator>();
                SetField(enemy, "animator", anim);
                SetField(enemy, "visualRoot", visualObj.transform);
            }

            enemy.InitializeAbilities();
            return enemy;
        }

        private void EnsureCamera(PlayerDragonController player, DragonAIController enemy)
        {
            var mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                var camGO = new GameObject("Main Camera");
                mainCam = camGO.AddComponent<UnityEngine.Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }

            var battleCam = mainCam.GetComponent<BattleCameraController>();
            if (battleCam == null)
            {
                battleCam = mainCam.gameObject.AddComponent<BattleCameraController>();
            }
            if (player != null && enemy != null)
            {
                battleCam.SetTargets(player.transform, enemy.transform);
            }
        }

        private void EnsureUI(PlayerDragonController player, DragonAIController enemy)
        {
            var ui = FindAnyObjectByType<BattleUIManager>();
            if (ui != null) return;

            // Canvas
            var canvasGO = new GameObject("BattleCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Event System
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    es.AddComponent(inputModuleType);
                }
                else
                {
                    es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            var uiManager = canvasGO.AddComponent<BattleUIManager>();

            var playerHB = CreatePlayerHUDBar(canvasGO.transform);
            var enemyHB = CreateEnemyHUDBar(canvasGO.transform);
            var abilitySlots = CreateAbilityBar(canvasGO.transform);
            var timerText = CreateTopTimer(canvasGO.transform);
            var winnerScreen = CreateWinnerScreen(canvasGO.transform);

            SetField(uiManager, "playerHealthBar", playerHB);
            SetField(uiManager, "enemyHealthBar", enemyHB);
            SetField(uiManager, "abilitySlots", abilitySlots);
            SetField(uiManager, "timerText", timerText);
            SetField(uiManager, "gameOverPanel", winnerScreen.panel);
            SetField(uiManager, "winnerTitleText", winnerScreen.title);
            SetField(uiManager, "winnerSubtitleText", winnerScreen.subtitle);
            SetField(uiManager, "matchStatsText", winnerScreen.stats);
            SetField(uiManager, "restartButton", winnerScreen.restartBtn);

            uiManager.InitializeUI();
        }

        private OverheadHealthBar CreatePlayerHUDBar(Transform parent)
        {
            var go = new GameObject("PlayerTopHUDBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(35f, -30f);
            rect.sizeDelta = new Vector2(440f, 44f);

            var bg = new GameObject("Background").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
            bg.color = new Color(0.08f, 0.08f, 0.14f, 0.92f);

            var lagGO = new GameObject("DamageLagFill");
            lagGO.transform.SetParent(go.transform, false);
            var lagImg = lagGO.AddComponent<Image>();
            lagImg.rectTransform.anchorMin = Vector2.zero;
            lagImg.rectTransform.anchorMax = Vector2.one;
            lagImg.rectTransform.sizeDelta = Vector2.zero;
            lagImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            lagImg.type = Image.Type.Filled;
            lagImg.fillMethod = Image.FillMethod.Horizontal;

            var fillGO = new GameObject("HealthFill");
            fillGO.transform.SetParent(go.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.rectTransform.anchorMin = Vector2.zero;
            fillImg.rectTransform.anchorMax = Vector2.one;
            fillImg.rectTransform.sizeDelta = Vector2.zero;
            fillImg.color = new Color(0.18f, 0.82f, 0.44f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Name Text (Above Bar with TMP)
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(go.transform, false);
            var nText = nameGO.AddComponent<TextMeshProUGUI>();
            nText.rectTransform.anchorMin = new Vector2(0f, 1f);
            nText.rectTransform.anchorMax = new Vector2(1f, 1f);
            nText.rectTransform.pivot = new Vector2(0f, 0f);
            nText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            nText.rectTransform.sizeDelta = new Vector2(0f, 26f);
            nText.alignment = TextAlignmentOptions.MidlineLeft;
            nText.fontSize = 20;
            nText.fontStyle = FontStyles.Bold;
            nText.text = "⚡ PLAYER DRAGON (RED)";
            nText.color = new Color(1f, 0.45f, 0.45f);
            nameGO.AddComponent<Outline>().effectColor = Color.black;

            // Numeric HP Text (Centered inside Bar with TMP)
            var hpGO = new GameObject("HpNumericText");
            hpGO.transform.SetParent(go.transform, false);
            var hpText = hpGO.AddComponent<TextMeshProUGUI>();
            hpText.rectTransform.anchorMin = Vector2.zero;
            hpText.rectTransform.anchorMax = Vector2.one;
            hpText.rectTransform.sizeDelta = Vector2.zero;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.fontSize = 17;
            hpText.fontStyle = FontStyles.Bold;
            hpText.text = "1000 / 1000 HP";
            hpText.color = Color.white;
            hpGO.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

            var hb = go.AddComponent<OverheadHealthBar>();
            SetField(hb, "followWorldTarget", false);
            SetField(hb, "nameText", nText);
            SetField(hb, "healthFillImage", fillImg);
            SetField(hb, "damageLagFillImage", lagImg);
            SetField(hb, "hpText", hpText);

            return hb;
        }

        private OverheadHealthBar CreateEnemyHUDBar(Transform parent)
        {
            var go = new GameObject("EnemyTopHUDBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-35f, -30f);
            rect.sizeDelta = new Vector2(440f, 44f);

            var bg = new GameObject("Background").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.sizeDelta = Vector2.zero;
            bg.color = new Color(0.14f, 0.08f, 0.08f, 0.92f);

            var lagGO = new GameObject("DamageLagFill");
            lagGO.transform.SetParent(go.transform, false);
            var lagImg = lagGO.AddComponent<Image>();
            lagImg.rectTransform.anchorMin = Vector2.zero;
            lagImg.rectTransform.anchorMax = Vector2.one;
            lagImg.rectTransform.sizeDelta = Vector2.zero;
            lagImg.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            lagImg.type = Image.Type.Filled;
            lagImg.fillMethod = Image.FillMethod.Horizontal;

            var fillGO = new GameObject("HealthFill");
            fillGO.transform.SetParent(go.transform, false);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.rectTransform.anchorMin = Vector2.zero;
            fillImg.rectTransform.anchorMax = Vector2.one;
            fillImg.rectTransform.sizeDelta = Vector2.zero;
            fillImg.color = new Color(0.92f, 0.24f, 0.24f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Name Text (Above Bar with TMP)
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(go.transform, false);
            var nText = nameGO.AddComponent<TextMeshProUGUI>();
            nText.rectTransform.anchorMin = new Vector2(0f, 1f);
            nText.rectTransform.anchorMax = new Vector2(1f, 1f);
            nText.rectTransform.pivot = new Vector2(1f, 0f);
            nText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            nText.rectTransform.sizeDelta = new Vector2(0f, 26f);
            nText.alignment = TextAlignmentOptions.MidlineRight;
            nText.fontSize = 20;
            nText.fontStyle = FontStyles.Bold;
            nText.text = "🔥 ENEMY DRAGON (BLACK)";
            nText.color = new Color(0.85f, 0.85f, 0.9f);
            nameGO.AddComponent<Outline>().effectColor = Color.black;

            // Numeric HP Text (Centered inside Bar with TMP)
            var hpGO = new GameObject("HpNumericText");
            hpGO.transform.SetParent(go.transform, false);
            var hpText = hpGO.AddComponent<TextMeshProUGUI>();
            hpText.rectTransform.anchorMin = Vector2.zero;
            hpText.rectTransform.anchorMax = Vector2.one;
            hpText.rectTransform.sizeDelta = Vector2.zero;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.fontSize = 17;
            hpText.fontStyle = FontStyles.Bold;
            hpText.text = "600 / 600 HP";
            hpText.color = Color.white;
            hpGO.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

            var hb = go.AddComponent<OverheadHealthBar>();
            SetField(hb, "followWorldTarget", false);
            SetField(hb, "nameText", nText);
            SetField(hb, "healthFillImage", fillImg);
            SetField(hb, "damageLagFillImage", lagImg);
            SetField(hb, "hpText", hpText);

            return hb;
        }

        private System.Collections.Generic.List<AbilitySlotUI> CreateAbilityBar(Transform parent)
        {
            var barGO = new GameObject("AbilityHUDBar");
            barGO.transform.SetParent(parent, false);
            var rect = barGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(400f, 110f);

            var layout = barGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 25f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var list = new System.Collections.Generic.List<AbilitySlotUI>();
            string[] keys = new string[] { "1", "2", "3" };
            string[] names = new string[] { "Fire Breath", "Tail Whip", "Sky Dive" };

            for (int i = 0; i < 3; i++)
            {
                var slotGO = new GameObject($"AbilitySlot_{keys[i]}");
                slotGO.transform.SetParent(barGO.transform, false);
                var slotRect = slotGO.AddComponent<RectTransform>();
                slotRect.sizeDelta = new Vector2(90f, 90f);

                var slotBg = slotGO.AddComponent<Image>();
                slotBg.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);
                var btn = slotGO.AddComponent<Button>();

                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(slotGO.transform, false);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.rectTransform.anchorMin = Vector2.zero;
                iconImg.rectTransform.anchorMax = Vector2.one;
                iconImg.rectTransform.sizeDelta = new Vector2(-8, -8);
                iconImg.color = i == 0 ? new Color(1f, 0.45f, 0.1f) : (i == 1 ? new Color(0.2f, 0.7f, 1f) : new Color(0.9f, 0.8f, 0.2f));

                var cdGO = new GameObject("CooldownOverlay");
                cdGO.transform.SetParent(slotGO.transform, false);
                var cdImg = cdGO.AddComponent<Image>();
                cdImg.rectTransform.anchorMin = Vector2.zero;
                cdImg.rectTransform.anchorMax = Vector2.one;
                cdImg.rectTransform.sizeDelta = Vector2.zero;
                cdImg.color = new Color(0f, 0f, 0f, 0.75f);
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillClockwise = false;
                cdImg.fillAmount = 0f;

                var cdTextGO = new GameObject("CDText");
                cdTextGO.transform.SetParent(slotGO.transform, false);
                var cdTxt = cdTextGO.AddComponent<TextMeshProUGUI>();
                cdTxt.rectTransform.anchorMin = Vector2.zero;
                cdTxt.rectTransform.anchorMax = Vector2.one;
                cdTxt.rectTransform.sizeDelta = Vector2.zero;
                cdTxt.alignment = TextAlignmentOptions.Center;
                cdTxt.fontSize = 24;
                cdTxt.fontStyle = FontStyles.Bold;
                cdTxt.color = Color.white;
                cdTextGO.AddComponent<Outline>().effectColor = Color.black;

                var keyBadgeGO = new GameObject("KeyBadge");
                keyBadgeGO.transform.SetParent(slotGO.transform, false);
                var badgeRect = keyBadgeGO.AddComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0f, 1f);
                badgeRect.anchorMax = new Vector2(0f, 1f);
                badgeRect.pivot = new Vector2(0f, 1f);
                badgeRect.anchoredPosition = new Vector2(-4f, 4f);
                badgeRect.sizeDelta = new Vector2(28f, 28f);
                var badgeBg = keyBadgeGO.AddComponent<Image>();
                badgeBg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

                var badgeTxtGO = new GameObject("KeyText");
                badgeTxtGO.transform.SetParent(keyBadgeGO.transform, false);
                var badgeTxt = badgeTxtGO.AddComponent<TextMeshProUGUI>();
                badgeTxt.rectTransform.anchorMin = Vector2.zero;
                badgeTxt.rectTransform.anchorMax = Vector2.one;
                badgeTxt.rectTransform.sizeDelta = Vector2.zero;
                badgeTxt.alignment = TextAlignmentOptions.Center;
                badgeTxt.fontSize = 16;
                badgeTxt.fontStyle = FontStyles.Bold;
                badgeTxt.text = keys[i];
                badgeTxt.color = Color.white;

                var labelGO = new GameObject("NameLabel");
                labelGO.transform.SetParent(slotGO.transform, false);
                var lblTxt = labelGO.AddComponent<TextMeshProUGUI>();
                lblTxt.rectTransform.anchorMin = new Vector2(0, 0);
                lblTxt.rectTransform.anchorMax = new Vector2(1, 0);
                lblTxt.rectTransform.pivot = new Vector2(0.5f, 1);
                lblTxt.rectTransform.anchoredPosition = new Vector2(0, -6);
                lblTxt.rectTransform.sizeDelta = new Vector2(100, 24);
                lblTxt.alignment = TextAlignmentOptions.Center;
                lblTxt.fontSize = 13;
                lblTxt.fontStyle = FontStyles.Bold;
                lblTxt.text = names[i];
                lblTxt.color = new Color(0.9f, 0.9f, 0.9f);
                labelGO.AddComponent<Outline>().effectColor = Color.black;

                var slotComp = slotGO.AddComponent<AbilitySlotUI>();
                SetField(slotComp, "iconImage", iconImg);
                SetField(slotComp, "cooldownOverlayImage", cdImg);
                SetField(slotComp, "cooldownText", cdTxt);
                SetField(slotComp, "hotkeyText", badgeTxt);
                SetField(slotComp, "abilityNameText", lblTxt);
                SetField(slotComp, "actionButton", btn);

                list.Add(slotComp);
            }

            return list;
        }

        private TextMeshProUGUI CreateTopTimer(Transform parent)
        {
            var go = new GameObject("TopTimer");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            txt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            txt.rectTransform.pivot = new Vector2(0.5f, 1f);
            txt.rectTransform.anchoredPosition = new Vector2(0, -25);
            txt.rectTransform.sizeDelta = new Vector2(300, 40);
            txt.alignment = TextAlignmentOptions.Center;
            txt.fontSize = 22;
            txt.fontStyle = FontStyles.Bold;
            txt.text = "BATTLE TIME: 00:00";
            txt.color = new Color(0.9f, 0.95f, 1f);
            go.AddComponent<Outline>().effectColor = Color.black;
            return txt;
        }

        private (GameObject panel, TextMeshProUGUI title, TextMeshProUGUI subtitle, TextMeshProUGUI stats, Button restartBtn) CreateWinnerScreen(Transform parent)
        {
            var panelGO = new GameObject("WinnerScreenPanel");
            panelGO.transform.SetParent(parent, false);
            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.1f, 0.88f);

            var dialogGO = new GameObject("DialogBox");
            dialogGO.transform.SetParent(panelGO.transform, false);
            var dialogRect = dialogGO.AddComponent<RectTransform>();
            dialogRect.sizeDelta = new Vector2(540, 380);
            var dialogBg = dialogGO.AddComponent<Image>();
            dialogBg.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

            var titleGO = new GameObject("WinnerTitle");
            titleGO.transform.SetParent(dialogGO.transform, false);
            var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            titleTxt.rectTransform.anchoredPosition = new Vector2(0, 110);
            titleTxt.rectTransform.sizeDelta = new Vector2(500, 60);
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.fontSize = 44;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.text = "VICTORY!";
            titleTxt.color = new Color(1f, 0.85f, 0.2f);
            titleGO.AddComponent<Outline>().effectColor = Color.black;

            var subGO = new GameObject("WinnerSubtitle");
            subGO.transform.SetParent(dialogGO.transform, false);
            var subTxt = subGO.AddComponent<TextMeshProUGUI>();
            subTxt.rectTransform.anchoredPosition = new Vector2(0, 45);
            subTxt.rectTransform.sizeDelta = new Vector2(500, 40);
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.fontSize = 24;
            subTxt.fontStyle = FontStyles.Bold;
            subTxt.text = "Player Dragon Claims the Arena!";
            subTxt.color = Color.white;
            subGO.AddComponent<Outline>().effectColor = Color.black;

            var statsGO = new GameObject("MatchStats");
            statsGO.transform.SetParent(dialogGO.transform, false);
            var statsTxt = statsGO.AddComponent<TextMeshProUGUI>();
            statsTxt.rectTransform.anchoredPosition = new Vector2(0, -10);
            statsTxt.rectTransform.sizeDelta = new Vector2(500, 35);
            statsTxt.alignment = TextAlignmentOptions.Center;
            statsTxt.fontSize = 18;
            statsTxt.text = "Match Duration: 35s";
            statsTxt.color = new Color(0.75f, 0.8f, 0.9f);

            var btnGO = new GameObject("RestartButton");
            btnGO.transform.SetParent(dialogGO.transform, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0, -95);
            btnRect.sizeDelta = new Vector2(240, 56);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.7f, 0.35f);
            var btn = btnGO.AddComponent<Button>();

            var btnTxtGO = new GameObject("BtnText");
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnTxt = btnTxtGO.AddComponent<TextMeshProUGUI>();
            btnTxt.rectTransform.anchorMin = Vector2.zero;
            btnTxt.rectTransform.anchorMax = Vector2.one;
            btnTxt.rectTransform.sizeDelta = Vector2.zero;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = 22;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.text = "PLAY AGAIN (R)";
            btnTxt.color = Color.white;

            panelGO.SetActive(false);
            return (panelGO, titleTxt, subTxt, statsTxt, btn);
        }

        private void EnsureGameManager(PlayerDragonController player, DragonAIController enemy)
        {
            var gmGO = GameObject.Find("GameManager");
            if (gmGO == null) gmGO = new GameObject("GameManager");

            var gm = gmGO.GetComponent<GameManager>();
            if (gm == null) gm = gmGO.AddComponent<GameManager>();

            SetField(gm, "playerDragon", player);
            SetField(gm, "enemyDragon", enemy);
        }

        private void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private Material CreateSimpleMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }
    }
}
