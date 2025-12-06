using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class IntroController : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private GameObject mainMenuRoot;

    [Header("Intro Settings")]
    [SerializeField] private bool playIntro = true;
    [SerializeField] private VideoPlayer introVideoPlayer;
    [SerializeField] private string introFileName = "game_video.mp4";

    public IEnumerator PlayIntroIfNeeded()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(false);
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(false);

        if (!playIntro || introVideoPlayer == null)
        {
            Debug.LogWarning("[IntroController] Intro disabled or VideoPlayer missing. Skipping intro.");
            yield break;
        }

        string url = Path.Combine(Application.streamingAssetsPath, introFileName);
        Debug.Log("[IntroController] Intro video URL: " + url);

        introVideoPlayer.source = VideoSource.Url;
        introVideoPlayer.url = url;

        introVideoPlayer.Prepare();
        yield return new WaitUntil(() => introVideoPlayer.isPrepared);

        introVideoPlayer.Play();

        while (introVideoPlayer.isPlaying)
            yield return null;
    }
}

