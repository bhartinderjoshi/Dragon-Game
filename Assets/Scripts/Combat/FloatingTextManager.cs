using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DragonBattle.Combat
{
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        [Header("Colors")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color critDamageColor = new Color(1f, 0.2f, 0.1f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.3f);

        [Header("Prefab & Pool")]
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private int initialPoolSize = 15;

        private Queue<FloatingDamageText> pool = new Queue<FloatingDamageText>();
        private Canvas worldCanvas;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            EnsureCanvas();
            InitializePool();
        }

        private void EnsureCanvas()
        {
            worldCanvas = GetComponentInChildren<Canvas>();
            if (worldCanvas == null)
            {
                var canvasGO = new GameObject("WorldFloatingTextCanvas");
                canvasGO.transform.SetParent(transform);
                worldCanvas = canvasGO.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
                canvasGO.AddComponent<CanvasScaler>();
            }
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewInstance();
            }
        }

        private FloatingDamageText CreateNewInstance()
        {
            GameObject obj;
            if (textPrefab != null)
            {
                obj = Instantiate(textPrefab, worldCanvas != null ? worldCanvas.transform : transform);
            }
            else
            {
                // Procedural generation fallback
                obj = new GameObject("DamageTextItem");
                obj.transform.SetParent(worldCanvas != null ? worldCanvas.transform : transform);
                var rect = obj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 50);
                rect.localScale = Vector3.one * 0.02f;

                var text = obj.AddComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 32;
                text.fontStyle = FontStyles.Bold;
                text.color = Color.white;

                var outline = obj.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                obj.AddComponent<FloatingDamageText>();
            }

            var comp = obj.GetComponent<FloatingDamageText>();
            obj.SetActive(false);
            pool.Enqueue(comp);
            return comp;
        }

        public void ShowDamage(float damage, Vector3 position, bool isCritical = false)
        {
            FloatingDamageText item = pool.Count > 0 ? pool.Dequeue() : CreateNewInstance();
            if (item == null) return;

            item.transform.position = position;
            string txt = Mathf.RoundToInt(damage).ToString();
            if (isCritical) txt += "!";

            Color col = isCritical ? critDamageColor : normalDamageColor;
            float scale = isCritical ? 1.5f : 1.0f;
            item.Initialize(txt, col, scale);

            pool.Enqueue(item);
        }

        public void ShowHeal(float amount, Vector3 position)
        {
            FloatingDamageText item = pool.Count > 0 ? pool.Dequeue() : CreateNewInstance();
            if (item == null) return;

            item.transform.position = position;
            item.Initialize("+" + Mathf.RoundToInt(amount), healColor, 1.1f);
            pool.Enqueue(item);
        }
    }
}
