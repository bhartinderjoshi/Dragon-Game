using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DragonBattle.Audio;
using DragonBattle.Player;
using DragonBattle.AI;
using DragonBattle.UI;

namespace DragonBattle.Core
{
    public enum GameState
    {
        Intro,
        Battle,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Dragons")]
        [SerializeField] private PlayerDragonController playerDragon;
        [SerializeField] private DragonAIController enemyDragon;

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 playerSpawnPos = new Vector3(-6f, 0f, 0f);
        [SerializeField] private Quaternion playerSpawnRot = Quaternion.Euler(0, 90f, 0);
        [SerializeField] private Vector3 enemySpawnPos = new Vector3(6f, 0f, 0f);
        [SerializeField] private Quaternion enemySpawnRot = Quaternion.Euler(0, -90f, 0);

        [Header("State")]
        private GameState currentState = GameState.Battle;
        private float matchDuration = 0f;
        private string winnerName = "";

        public GameState CurrentState => currentState;
        public float MatchDuration => matchDuration;
        public PlayerDragonController PlayerDragon => playerDragon;
        public DragonAIController EnemyDragon => enemyDragon;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        private void Start()
        {
            FindDragonsIfNull();
            BindDragonEvents();
            StartBattle();
        }

        private void Update()
        {
            if (currentState == GameState.Battle)
            {
                matchDuration += Time.deltaTime;

                if (playerDragon == null || enemyDragon == null)
                {
                    FindDragonsIfNull();
                    BindDragonEvents();
                }

                // Continuous health check for instant reliable end-game triggers
                if (playerDragon != null && playerDragon.Health != null && !playerDragon.Health.IsAlive)
                {
                    HandlePlayerDeath(enemyDragon != null ? enemyDragon.gameObject : null);
                }
                else if (enemyDragon != null && enemyDragon.Health != null && !enemyDragon.Health.IsAlive)
                {
                    HandleEnemyDeath(playerDragon != null ? playerDragon.gameObject : null);
                }
            }
        }

        private void FindDragonsIfNull()
        {
            if (playerDragon == null) playerDragon = FindAnyObjectByType<PlayerDragonController>();
            if (enemyDragon == null) enemyDragon = FindAnyObjectByType<DragonAIController>();
        }

        private void BindDragonEvents()
        {
            if (playerDragon != null && playerDragon.Health != null)
            {
                playerDragon.Health.OnDied -= HandlePlayerDeath;
                playerDragon.Health.OnDied += HandlePlayerDeath;
            }

            if (enemyDragon != null && enemyDragon.Health != null)
            {
                enemyDragon.Health.OnDied -= HandleEnemyDeath;
                enemyDragon.Health.OnDied += HandleEnemyDeath;
            }
        }

        public void StartBattle()
        {
            currentState = GameState.Battle;
            matchDuration = 0f;
            SoundManager.Instance?.PlayBattleStart();
        }

        private void HandlePlayerDeath(GameObject killer)
        {
            if (currentState == GameState.GameOver) return;
            currentState = GameState.GameOver;
            winnerName = enemyDragon != null ? enemyDragon.DragonName : "AI Dragon (Black)";
            SoundManager.Instance?.PlayDefeat();
            BattleUIManager.Instance?.ShowGameOver(winnerName, isPlayerWinner: false, matchDuration);
        }

        private void HandleEnemyDeath(GameObject killer)
        {
            if (currentState == GameState.GameOver) return;
            currentState = GameState.GameOver;
            winnerName = playerDragon != null ? playerDragon.DragonName : "Player Dragon (Red)";
            SoundManager.Instance?.PlayVictory();
            BattleUIManager.Instance?.ShowGameOver(winnerName, isPlayerWinner: true, matchDuration);
        }

        public void RestartMatch()
        {
            SoundManager.Instance?.PlayButtonClick();
            string currentScene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(currentScene))
            {
                SceneManager.LoadScene(currentScene);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
            }
        }

        public void ReloadScene()
        {
            RestartMatch();
        }
    }
}
