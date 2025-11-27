using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterWineryDoorOut : MonoBehaviour
{
    [Tooltip("שם הסצנה של החדר אירוח / פנים היקב")]
    public string sceneName = "WineryReception";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
