using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene loading and placing the Player at the right position.
/// Also restores plant growth based on real-world elapsed time when needed.
/// This class is scene-agnostic (it does NOT know what scene represents what).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    
     private List<String> notInGarden = new() { "iron,basement,stone,Manager_Office,wine,Winery Reception,wood" };
     public static bool playerIsNotInGarden = false;
    /// <summary>
    /// Load a scene and place the Player at the given position.
    /// Does NOT restore any plants – only movement/teleportation.
    /// </summary>
    public IEnumerator LoadSceneAndPlacePlayer(string sceneName, Vector2 playerPos)
    {
        Debug.Log("[SceneLoader] Start loading scene: " + sceneName);

        // Begin asynchronous scene loading
        var op = SceneManager.LoadSceneAsync(sceneName);

        // Wait until scene finishes loading
        while (!op.isDone)
            yield return null;

        // Extra frame delay for stability
        yield return null;

        Debug.Log("[SceneLoader] Scene loaded, placing player");

        // Move player to requested location
        PlacePlayer(playerPos);
    }

    /// <summary>
    /// Load a scene, place the Player, and restore plant growth
    /// using the amount of real time that passed since last save.
    /// </summary>
    public IEnumerator LoadScenePlaceAndRestorePlants(string sceneName,
                                                      Vector2 playerPos,
                                                      long lastRealTimeTicks)
    {
        // Load the scene asynchronously
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        yield return null;

        // Place the Player after load
        PlacePlayer(playerPos);

        // Calculate how much real time has passed (in seconds)
        float deltaSeconds = CalculateDeltaSeconds(lastRealTimeTicks);

        // Restore plant states based on time passed
        PlantManager.Instance?.LoadAll(deltaSeconds);
    }

    /// <summary>
    /// Save the game, switch scenes, place the Player,
    /// and restore plants only if the destination contains plots.
    /// </summary>
    public IEnumerator ChangeScene(string sceneName,
                                   Vector2 newPlayerPos,
                                   long lastRealTimeTicks)
    {
        // Save before changing scenes
        GameManager.Instance.SaveGame();

        // Begin scene load
        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        yield return null;

        // Place the Player in the new location
        PlacePlayer(newPlayerPos);

        // If the new scene has plants, restore their growth
        if (PlantManager.Instance != null &&
            PlantManager.Instance.HasAnyPlotsInScene())
        {
            float deltaSeconds = CalculateDeltaSeconds(lastRealTimeTicks);
            PlantManager.Instance.LoadAll(deltaSeconds);

            Debug.Log("[SceneLoader] Loaded plants with deltaSeconds = " + deltaSeconds);
        }
        if (notInGarden.Contains(sceneName))
        {
            playerIsNotInGarden = true;
        }
        else {
            playerIsNotInGarden = false;
        }
    }

    /// <summary>
    /// Finds the Player in the scene and moves them to the given position,
    /// keeping the original Z coordinate.
    /// </summary>
    private void PlacePlayer(Vector2 pos)
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);
    }

    /// <summary>
    /// Calculates how many real-world seconds passed since lastRealTimeTicks.
    /// Used for simulating plant growth while the game was closed.
    /// </summary>
    private float CalculateDeltaSeconds(long lastRealTimeTicks)
    {
        int zero = 0;      // Magic-number replacement
        float zero_f = 0f; // Return value for "no data"

        // If there is no stored time, return early
        if (lastRealTimeTicks == zero)
            return zero_f;

        // Current real-world UTC time in ticks
        long nowTicks = DateTime.UtcNow.Ticks;

        // Convert ticks to seconds difference
        return (float)new TimeSpan(nowTicks - lastRealTimeTicks).TotalSeconds;
    }
}
