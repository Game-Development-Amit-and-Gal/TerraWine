using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BottleMiniGameManager : MonoBehaviour
{
    [Header("Game Configuration")]
    [Tooltip("The allowed offset error for a perfect fit.")]
    [SerializeField] private float epsilon = 0.6f;
    [Tooltip("Time limit per bottle level.")]
    [SerializeField] private float levelTime = 45f;
    [SerializeField] private float speedIncreasePerLevel = 2f;

    [Header("Bottle Library")]
    [Tooltip("Drag your different Bottle Prefabs here (Red, White, etc). Each prefab must have the parts as Children.")]
    [SerializeField] private GameObject[] availableBottlePrefabs;

    [Header("Rewards")]
    [Tooltip("The pool of items to give as rewards.")]
    [SerializeField] private List<ItemSO> rewardPool;

    [Header("Scene Setup")]
    [SerializeField] private Transform centerFrameLocation; // Where the bottle sits
    [SerializeField] private Transform leftSpawn;
    [SerializeField] private Transform rightSpawn;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    // --- State Management ---
    private int _currentLevel = 1;
    private float _currentSpeed = 5f;
    private float _timer;
    private bool _gameActive = false;

    // Current Bottle Logic
    private GameObject _currentGhostBottle; // The transparent target in the middle
    private List<SpriteRenderer> _ghostParts = new List<SpriteRenderer>(); // The individual parts of the ghost
    private int _currentPartIndex = 0; // Which part are we currently trying to place?
    private MovingPiece _activeMovingPiece; // The piece currently flying across screen

    // Rewards
    private List<ItemSO> _lootBag = new List<ItemSO>();

    void Start()
    {
        if (InventoryManager.Instance == null)
            Debug.LogWarning("InventoryManager missing. Start from WorldMap for rewards.");

        StartGame();
    }

    void Update()
    {
        if (!_gameActive) return;

        HandleTimer();
        HandleInput();
    }

    private void StartGame()
    {
        _currentLevel = 1;
        _currentSpeed = 5f;
        _lootBag.Clear();

        StartLevel();
    }

    /// <summary>
    /// Prepares the specific bottle for this level.
    /// </summary>
    private void StartLevel()
    {
        _gameActive = true;
        _timer = levelTime;
        _currentPartIndex = 0;

        // 1. Cleanup old bottle if exists
        if (_currentGhostBottle != null) Destroy(_currentGhostBottle);

        // 2. Select a bottle prefab based on level (or random)
        // Using modulo so if we have 2 prefabs but 4 levels, it cycles: 0, 1, 0, 1
        int prefabIndex = (_currentLevel - 1) % availableBottlePrefabs.Length;
        GameObject selectedPrefab = availableBottlePrefabs[prefabIndex];

        // 3. Instantiate the "Ghost" Bottle at the center
        _currentGhostBottle = Instantiate(selectedPrefab, centerFrameLocation.position, Quaternion.identity);

        // 4. Analyze the bottle structure
        _ghostParts.Clear();
        // Get all SpriteRenderers in the children (Base, Body, Neck, etc.)
        // We assume the order in the hierarchy is Bottom -> Top.
        foreach (Transform child in _currentGhostBottle.transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                _ghostParts.Add(sr);
                // Make it transparent (The "Ghost" effect)
                Color c = sr.color;
                c.a = 0.25f;
                sr.color = c;
            }
        }

        UpdateUI();
        SpawnNextPart();
    }

    /// <summary>
    /// Spawns the specific part we are currently looking for.
    /// </summary>
    private void SpawnNextPart()
    {
        // If we have placed all parts, the bottle is done
        if (_currentPartIndex >= _ghostParts.Count)
        {
            OnBottleCompleted();
            return;
        }

        // 1. Identify the target part
        SpriteRenderer targetPart = _ghostParts[_currentPartIndex];

        // 2. Decide direction
        bool startLeft = Random.Range(0, 2) == 0;
        Transform spawnPoint = startLeft ? leftSpawn : rightSpawn;
        int direction = startLeft ? 1 : -1;

        // 3. Create the moving piece (Copy of the ghost part)
        // We instantiate a copy of the target part's gameObject
        GameObject movingObj = Instantiate(targetPart.gameObject, spawnPoint.position, Quaternion.identity);

        // Reset its opacity to 1 (Solid)
        movingObj.GetComponent<SpriteRenderer>().color = Color.white;

        // Align Y exactly to the target so player only focuses on X
        // We must preserve the local offset if the parts are offset from the parent center
        float targetY = targetPart.transform.position.y;
        movingObj.transform.position = new Vector3(spawnPoint.position.x, targetY, 0);

        // 4. Add movement logic
        _activeMovingPiece = movingObj.AddComponent<MovingPiece>();
        _activeMovingPiece.Initialize(_currentSpeed, direction);
    }

    private void HandleInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _activeMovingPiece != null)
        {
            _activeMovingPiece.StopMovement();
            CheckPlacement();
        }
    }

    private void CheckPlacement()
    {
        // Target X is the center frame (usually 0)
        float targetX = centerFrameLocation.position.x;
        float currentX = _activeMovingPiece.transform.position.x;

        float distance = Mathf.Abs(currentX - targetX);

        if (distance <= epsilon)
        {
            // --- Success ---
            // 1. Destroy the moving piece (we don't need it anymore)
            Destroy(_activeMovingPiece.gameObject);
            _activeMovingPiece = null;

            // 2. "Fill in" the ghost part (make it opaque)
            SpriteRenderer ghostPart = _ghostParts[_currentPartIndex];
            Color c = ghostPart.color;
            c.a = 1f; // Solid
            ghostPart.color = c;

            // 3. Visuals
            ShowFeedback("Placed!");

            // 4. Next part
            _currentPartIndex++;
            SpawnNextPart();
        }
        else
        {
            // --- Fail ---
            ShowFeedback("Missed!");
            GameOver(false);
        }
    }

    private void OnBottleCompleted()
    {
        // 1. Add Reward
        if (rewardPool != null && rewardPool.Count > 0)
        {
            ItemSO prize = rewardPool[Random.Range(0, rewardPool.Count)];
            _lootBag.Add(prize);
            Debug.Log($"Level {_currentLevel} Done. Won: {prize.name}");
        }

        // 2. Increase Difficulty
        _currentLevel++;
        _currentSpeed += speedIncreasePerLevel;

        ShowFeedback("Bottle Complete!");

        // 3. Check Win Condition (e.g., 4 Levels)
        if (_currentLevel > 4)
        {
            GameOver(true);
        }
        else
        {
            StartCoroutine(WaitAndStartNextLevel());
        }
    }

    private IEnumerator WaitAndStartNextLevel()
    {
        _activeMovingPiece = null;
        yield return new WaitForSeconds(1.5f);
        StartLevel();
    }

    private void HandleTimer()
    {
        _timer -= Time.deltaTime;
        if (timerText) timerText.text = Mathf.Ceil(_timer).ToString();

        if (_timer <= 0)
        {
            ShowFeedback("Out of Time!");
            GameOver(false);
        }
    }

    private void GameOver(bool playerWonEverything)
    {
        _gameActive = false;

        string msg = playerWonEverything ? "ALL COMPLETE!" : "ROUND OVER";
        msg += $"\nCollected: {_lootBag.Count} Bottles";
        ShowFeedback(msg);

        // Grant Loot to Persistent Inventory
        if (InventoryManager.Instance != null && _lootBag.Count > 0)
        {
            foreach (var item in _lootBag)
            {
                InventoryManager.Instance.Add(item.id, 1);
            }
        }

        StartCoroutine(ReturnToMap());
    }

    private IEnumerator ReturnToMap()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("WorldMap");
    }

    private void ShowFeedback(string text)
    {
        if (feedbackText) feedbackText.text = text;
        if (_gameActive) Invoke(nameof(ClearFeedback), 1f);
    }
    private void ClearFeedback() => feedbackText.text = "";

    private void UpdateUI()
    {
        if (levelText) levelText.text = $"Level: {_currentLevel}";
    }
}