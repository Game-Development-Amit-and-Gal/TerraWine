using UnityEngine;

public class EnterWineryDoor : MonoBehaviour
{
    [Tooltip("שם הסצנה שאליה נכנסים (למשל: WineryReception)")]
    public string sceneName = "WineryReception";

    [Tooltip("המיקום שבו הדמות תופיע בתוך הסצנה החדשה")]
    public Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
        }
    }
}
