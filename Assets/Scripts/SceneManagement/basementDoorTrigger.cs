using UnityEngine;
using UnityEngine.SceneManagement;

public class basementDoorTrigger : MonoBehaviour
{
    [Tooltip("שם הסצנה של החדר אירוח / פנים היקב")]
    public string sceneName = "basement";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
