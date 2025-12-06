using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene loading and placing the player,
/// and optionally restoring plants based on real-world time.
/// This class does NOT know anything about specific scenes (like world map).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Load a scene and place the player at the given position.
    /// Does not restore plants, only moves the player.
    /// </summary>
    public IEnumerator LoadSceneAndPlacePlayer(string sceneName, Vector2 playerPos)
    {
        Debug.Log("[SceneLoader] Start loading scene: " + sceneName);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
        yield return null;

        Debug.Log("[SceneLoader] Scene loaded, placing player");

        PlacePlayer(playerPos);
    }

    /// <summary>
    /// Load a scene, place the player and restore plants according
    /// to the real time that passed.
    /// </summary>
    public IEnumerator LoadScenePlaceAndRestorePlants(string sceneName,
                                                      Vector2 playerPos,
                                                      long lastRealTimeTicks)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
        yield return null;

        PlacePlayer(playerPos);

        float deltaSeconds = CalculateDeltaSeconds(lastRealTimeTicks);
        PlantManager.Instance?.LoadAll(deltaSeconds);
    }

    /// <summary>
    /// Save the game, change scene, place the player and restore plants
    /// in the new scene (if there are any plots).
    /// </summary>
    public IEnumerator ChangeScene(string sceneName,
                                   Vector2 newPlayerPos,
                                   long lastRealTimeTicks)
    {
        GameManager.Instance.SaveGame();

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;
        yield return null;

        PlacePlayer(newPlayerPos);

        if (PlantManager.Instance != null &&
            PlantManager.Instance.HasAnyPlotsInScene())
        {
            float deltaSeconds = CalculateDeltaSeconds(lastRealTimeTicks);
            PlantManager.Instance.LoadAll(deltaSeconds);
            Debug.Log("[SceneLoader] Loaded plants with deltaSeconds = " + deltaSeconds);
        }
    }

    /// <summary>
    /// Find the player by tag and move it to the given position (keeping Z).
    /// </summary>
    private void PlacePlayer(Vector2 pos)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);
    }

    /// <summary>
    /// Calculate how many seconds passed since lastRealTimeTicks.
    /// </summary>
    private float CalculateDeltaSeconds(long lastRealTimeTicks)
    {
        if (lastRealTimeTicks == 0)
            return 0f;

        long nowTicks = DateTime.UtcNow.Ticks;
        return (float)new TimeSpan(nowTicks - lastRealTimeTicks).TotalSeconds;
    }
}
