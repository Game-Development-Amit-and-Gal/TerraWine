using UnityEngine;

public class WineryWineTrigger : MonoBehaviour
{
    [SerializeField] private string sceneName = "wine";
    [SerializeField] private Vector2 playerSpawnPosition; // אם צריך ספאון גם שם

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        TutorialManager.Instance?.SetFlag("Rightdoor");

        // עדיף מעבר סצנה אחיד בכל הפרויקט
        GameManager.Instance.ChangeScene(sceneName, playerSpawnPosition);
    }
}
