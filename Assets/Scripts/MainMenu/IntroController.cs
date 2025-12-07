using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Controls the intro cutscene on the main menu.
/// Hides the UI, plays the intro video if enabled, then returns control to the game.
/// </summary>
public class IntroController : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private Canvas mainMenuCanvas;  // Canvas object of the Main Menu UI
    [SerializeField] private GameObject mainMenuRoot; // Root object containing menu UI elements

    [Header("Intro Settings")]
    [SerializeField] private bool playIntro = true; // Should the intro play at startup?
    [SerializeField] private VideoPlayer introVideoPlayer; // VideoPlayer component for playing the intro video
    [SerializeField] private string introFileName = "game_video.mp4"; // Name of the video file located in StreamingAssets

    /// <summary>
    /// Plays the intro video only if settings allow it.
    /// Hides the menu while playing, then resumes when finished.
    /// </summary>
    public IEnumerator PlayIntroIfNeeded()
    {
        // Hide the Main Menu UI while intro video is playing
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        // If intro disabled or no VideoPlayer component → skip safely
        if (!playIntro || introVideoPlayer == null)
        {
            Debug.LogWarning("[IntroController] Intro disabled or VideoPlayer missing. Skipping intro.");
            yield break;
        }

        // Build the full path to the video file located in StreamingAssets
        string url = Path.Combine(Application.streamingAssetsPath, introFileName);
        Debug.Log("[IntroController] Intro video URL: " + url);

        // Configure VideoPlayer to load from a file path
        introVideoPlayer.source = VideoSource.Url;
        introVideoPlayer.url = url;

        // Prepare the video (load it into memory)
        introVideoPlayer.Prepare();
        yield return new WaitUntil(() => introVideoPlayer.isPrepared); // Wait until it's ready

        // Start playback
        introVideoPlayer.Play();

        // Wait until the video finishes playing
        while (introVideoPlayer.isPlaying)
            yield return null;
    }
}
