using UnityEngine;

public class EnterWineryDoorOut : MonoBehaviour
{
    [Tooltip("שם הסצנה של החדר אירוח / פנים היקב")]
    public string sceneName = "WineryReception";

    public Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
    }
}