using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the Bottle Assembly Mini-Game.
/// Handles spawning "Ghost" targets, spawning moving pieces, checking player input accuracy,
/// and managing the win/loss state, inventory rewards, and visual feedback.
/// </summary>
public class BottleMiniGameManager : MonoBehaviour
{
    // ========================================================================
    // --- CONSTANTS & CONFIGURATION (NO MAGIC NUMBERS / NO MAGIC STRINGS) ---
    // ========================================================================

    // --- SCENE NAMES ---
    private const string SCENE_NAME_WORLD_MAP = "WorldMap";

    // --- VISUAL SETTINGS ---
    private const float GHOST_PART_ALPHA = 0.25f; // Transparency of the target hint
    private const float SOLID_PART_ALPHA = 1.0f;  // Opacity of a placed/moving part

    // --- TIMING & DELAYS ---
    private const float LEVEL_TRANSITION_DELAY = 2.0f;     // Time between levels (admire the bottle)
    private const float GAME_OVER_SCENE_LOAD_DELAY = 4.0f; // Time to read loot before loading WorldMap
    private const float FEEDBACK_SHORT_DURATION = 0.8f;    // Duration for "Placed" or "Missed"
    private const float FEEDBACK_LONG_DURATION = 2.0f;     // Duration for "Round Over" / "Complete"

    // --- GAMEPLAY MECHANICS ---
    private const int INITIAL_LEVEL = 1;
    private const float INITIAL_SPEED = 5f;
    private const int MAX_LEVELS_TO_WIN = 4; // Levels > 4 triggers Win

    // --- RANDOMIZATION MATH ---
    private const int RANDOM_BINARY_MIN = 0;
    private const int RANDOM_BINARY_MAX_EXCLUSIVE = 2; // For Random.Range(0,2)
    private const int DIRECTION_RIGHT = 1;
    private const int DIRECTION_LEFT = -1;

    // --- UI MESSAGES & FORMATS ---
    private const string MSG_MISSED = "Missed!";
    private const string MSG_PLACED = "Placed!";
    private const string MSG_BOTTLE_COMPLETE = "Bottle Complete!";
    private const string MSG_OUT_OF_TIME = "Out of Time!";
    private const string MSG_ALL_COMPLETE = "ALL COMPLETE!";
    private const string MSG_ROUND_OVER = "ROUND OVER";

    // Formats
    private const string MSG_FORMAT_COLLECTED = "\nCollected: {0} Bottles";
    private const string MSG_FORMAT_LEVEL = "Level: {0}";
    private const string MSG_FORMAT_TIMER = "00:{0}"; // Keeps your specific time format

    // --- LOGIC KEYWORDS (For detecting message types) ---
    private const string KEYWORD_OVER = "OVER";
    private const string KEYWORD_COMPLETE = "COMPLETE";

    // --- DEBUG LOGS & ERRORS ---
    private const string LOG_WARN_INV_MISSING = "InventoryManager missing. Ensure you started from the WorldMap to collect rewards properly.";
    private const string LOG_ERR_NO_PREFABS = "No Bottle Prefabs assigned in Inspector!";
    private const string LOG_ERR_SPAWNERS_MISSING = "CRITICAL: LeftSpawn or RightSpawn has been destroyed or unassigned!";
    private const string LOG_TRANSFERRED_ITEMS = "[MiniGame] Transferred {0} items to Inventory.";

    // ========================================================================

    [Header("Game Configuration")]
    [Tooltip("The allowed offset error (in Unity units) for a perfect fit.")]
    [SerializeField] private float epsilon = 0.6f;

    [Tooltip("Time limit in seconds per bottle level.")]
    [SerializeField] private float levelTime = 45f;

    [Tooltip("How much faster the pieces move each subsequent level.")]
    [SerializeField] private float speedIncreasePerLevel = 2f;

    [Header("Bottle Library")]
    [Tooltip("Drag your different Bottle Prefabs here. Each prefab must have the parts as Children.")]
    [SerializeField] private GameObject[] availableBottlePrefabs;

    [Header("Rewards")]
    [Tooltip("The pool of items (ScriptableObjects) to give as rewards upon completion.")]
    [SerializeField] private List<ItemSO> rewardPool;

    [Header("Scene Setup")]
    [SerializeField] private Transform centerFrameLocation; // Where the ghost bottle sits
    [SerializeField] private Transform leftSpawn;           // Left side spawn point
    [SerializeField] private Transform rightSpawn;          // Right side spawn point

    [Header("Visuals (Juice)")]
    [Tooltip("Reference to the script handling particles and camera shake.")]
    [SerializeField] private MiniGameVisuals visualEffects;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    // --- State Management ---
    private int _currentLevel = INITIAL_LEVEL;
    private float _currentSpeed = INITIAL_SPEED;
    private float _timer;
    private bool _gameActive = false;

    // --- Current Bottle Logic ---
    private GameObject _currentGhostBottle;
    private List<SpriteRenderer> _ghostParts = new List<SpriteRenderer>();
    private int _currentPartIndex = 0;
    private MovingPiece _activeMovingPiece;

    // --- Loot ---
    private List<ItemSO> _lootBag = new List<ItemSO>();

    void Start()
    {
        if (InventoryManager.Instance == null)
            Debug.LogWarning(LOG_WARN_INV_MISSING);

        StartGame();
    }

    void Update()
    {
        if (!_gameActive) return;

        HandleTimer();

        // --- FAILSAFE LOGIC ---
        if (_activeMovingPiece != null)
        {
            // Check if object was destroyed by Unity (went off screen) or flagged itself
            bool isObjectDestroyed = _activeMovingPiece.gameObject == null;
            bool flaggedOutOfBounds = !isObjectDestroyed && _activeMovingPiece.HasGoneOutOfBounds;

            if (isObjectDestroyed || flaggedOutOfBounds)
            {
                _activeMovingPiece = null;
                ShowFeedback(MSG_MISSED);

                // Visual Juice: Shake on miss
                if (visualEffects != null) visualEffects.TriggerShake();

                GameOver(false); // Strict Game Over on miss
                return;
            }
        }

        HandleInput();
    }

    private void StartGame()
    {
        _currentLevel = INITIAL_LEVEL;
        _currentSpeed = INITIAL_SPEED;
        _lootBag.Clear();

        StartLevel();
    }

    private void StartLevel()
    {
        _gameActive = true;
        _timer = levelTime;
        _currentPartIndex = 0;

        if (_currentGhostBottle != null) Destroy(_currentGhostBottle);

        if (availableBottlePrefabs == null || availableBottlePrefabs.Length == 0)
        {
            Debug.LogError(LOG_ERR_NO_PREFABS);
            return;
        }

        // Cycle prefabs based on level
        int prefabIndex = (_currentLevel - 1) % availableBottlePrefabs.Length;
        GameObject selectedPrefab = availableBottlePrefabs[prefabIndex];

        _currentGhostBottle = Instantiate(selectedPrefab, centerFrameLocation.position, Quaternion.identity);

        _ghostParts.Clear();
        foreach (Transform child in _currentGhostBottle.transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _ghostParts.Add(sr);
                Color c = sr.color;
                c.a = GHOST_PART_ALPHA;
                sr.color = c;
            }
        }

        UpdateUI();
        SpawnNextPart();
    }

    private void SpawnNextPart()
    {
        // Check if bottle is complete
        if (_currentPartIndex >= _ghostParts.Count)
        {
            OnBottleCompleted();
            return;
        }

        if (leftSpawn == null || rightSpawn == null)
        {
            Debug.LogError(LOG_ERR_SPAWNERS_MISSING);
            _gameActive = false;
            return;
        }

        SpriteRenderer targetPart = _ghostParts[_currentPartIndex];

        if (targetPart == null || targetPart.gameObject == null)
        {
            StartLevel();
            return;
        }

        // Randomize Direction
        bool startLeft = Random.Range(RANDOM_BINARY_MIN, RANDOM_BINARY_MAX_EXCLUSIVE) == 0;
        Transform spawnPoint = startLeft ? leftSpawn : rightSpawn;
        int direction = startLeft ? DIRECTION_RIGHT : DIRECTION_LEFT;

        // Create Moving Piece
        GameObject movingObj = Instantiate(targetPart.gameObject, spawnPoint.position, Quaternion.identity);

        // Apply Scale & Position Fixes
        movingObj.transform.localScale = targetPart.transform.lossyScale;
        float targetY = targetPart.transform.position.y;
        movingObj.transform.position = new Vector3(spawnPoint.position.x, targetY, 0);

        // Reset Opacity
        SpriteRenderer movingSr = movingObj.GetComponent<SpriteRenderer>();
        if (movingSr != null)
        {
            Color c = movingSr.color;
            c.a = SOLID_PART_ALPHA;
            movingSr.color = c;
        }

        // Attach Logic
        _activeMovingPiece = movingObj.AddComponent<MovingPiece>();
        _activeMovingPiece.Initialize(_currentSpeed, direction);
    }

    private void HandleInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_activeMovingPiece != null && _activeMovingPiece.gameObject != null)
            {
                _activeMovingPiece.StopMovement();
                CheckPlacement();
            }
        }
    }

    private void CheckPlacement()
    {
        if (_activeMovingPiece == null || _activeMovingPiece.gameObject == null) return;

        float targetX = centerFrameLocation.position.x;
        float currentX = _activeMovingPiece.transform.position.x;
        float distance = Mathf.Abs(currentX - targetX);

        if (distance <= epsilon)
        {
            // --- SUCCESS ---
            Destroy(_activeMovingPiece.gameObject);
            _activeMovingPiece = null;

            // Fill Ghost
            if (_currentPartIndex < _ghostParts.Count)
            {
                SpriteRenderer ghostPart = _ghostParts[_currentPartIndex];
                if (ghostPart != null)
                {
                    Color c = ghostPart.color;
                    c.a = SOLID_PART_ALPHA;
                    ghostPart.color = c;
                }
            }

            ShowFeedback(MSG_PLACED);

            // Visual Juice: Confetti/Stars
            if (visualEffects != null)
                visualEffects.PlaySuccessEffect(centerFrameLocation.position);

            _currentPartIndex++;
            SpawnNextPart();
        }
        else
        {
            // --- FAIL ---
            Destroy(_activeMovingPiece.gameObject);
            _activeMovingPiece = null;

            ShowFeedback(MSG_MISSED);

            // Visual Juice: Shake
            if (visualEffects != null) visualEffects.TriggerShake();

            GameOver(false);
        }
    }

    private void OnBottleCompleted()
    {
        // 1. Award Loot
        if (rewardPool != null && rewardPool.Count > 0)
        {
            ItemSO prize = rewardPool[Random.Range(0, rewardPool.Count)];
            _lootBag.Add(prize);
        }

        // 2. Increase Difficulty
        _currentLevel++;
        _currentSpeed += speedIncreasePerLevel;

        ShowFeedback(MSG_BOTTLE_COMPLETE);

        // 3. Start the transition sequence
        // This ensures we always wait before deciding to Win or Continue
        StartCoroutine(LevelCompleteSequence());
    }

    /// <summary>
    /// Waits for the player to see the completed bottle, then decides next step.
    /// </summary>
    private IEnumerator LevelCompleteSequence()
    {
        _activeMovingPiece = null;

        // Wait so player sees "Bottle Complete" message
        yield return new WaitForSeconds(LEVEL_TRANSITION_DELAY);

        if (_currentLevel > MAX_LEVELS_TO_WIN)
        {
            GameOver(true);
        }
        else
        {
            StartLevel();
        }
    }

    private void HandleTimer()
    {
        _timer -= Time.deltaTime;
        // Use Format string to avoid magic string concatenation
        if (timerText) timerText.text = string.Format(MSG_FORMAT_TIMER, Mathf.Ceil(_timer));

        if (_timer <= 0)
        {
            ShowFeedback(MSG_OUT_OF_TIME);
            if (visualEffects != null) visualEffects.TriggerShake();
            GameOver(false);
        }
    }

    private void GameOver(bool playerWonEverything)
    {
        _gameActive = false;

        if (_activeMovingPiece != null && _activeMovingPiece.gameObject != null)
        {
            Destroy(_activeMovingPiece.gameObject);
        }
        _activeMovingPiece = null;

        // Construct Message
        string msg = playerWonEverything ? MSG_ALL_COMPLETE : MSG_ROUND_OVER;
        msg += string.Format(MSG_FORMAT_COLLECTED, _lootBag.Count);

        ShowFeedback(msg);

        // Grant Loot
        if (InventoryManager.Instance != null && _lootBag.Count > 0)
        {
            foreach (var item in _lootBag)
            {
                // Explicitly adding 1 item per loot entry
                InventoryManager.Instance.Add(item.id, 1);
            }
            Debug.Log(string.Format(LOG_TRANSFERRED_ITEMS, _lootBag.Count));
        }

        StartCoroutine(ReturnToMap());
    }

    private IEnumerator ReturnToMap()
    {
        yield return new WaitForSeconds(GAME_OVER_SCENE_LOAD_DELAY);
        // Uncommented for production logic
        SceneManager.LoadScene(SCENE_NAME_WORLD_MAP);
    }

    private void ShowFeedback(string text)
    {
        if (feedbackText) feedbackText.text = text;

        // Check content logic using constants
        bool isLongMessage = text.Contains(KEYWORD_OVER) || text.Contains(KEYWORD_COMPLETE);
        float duration = isLongMessage ? FEEDBACK_LONG_DURATION : FEEDBACK_SHORT_DURATION;

        if (_gameActive || isLongMessage)
            Invoke(nameof(ClearFeedback), duration);
    }

    private void ClearFeedback() => feedbackText.text = "";

    private void UpdateUI()
    {
        if (levelText) levelText.text = string.Format(MSG_FORMAT_LEVEL, _currentLevel);
    }
}