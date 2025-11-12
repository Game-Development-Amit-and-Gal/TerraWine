using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // את מי לעקוב? (השחקן)
    public float smoothSpeed = 5f; // כמה חלק המעקב יהיה

    void LateUpdate()
    {
        if (target == null)
            return;

        // המצלמה עוקבת אחרי X,Y של השחקן, אבל שומרת את ה-Z שלה כמו שהוא
        Vector3 desiredPosition = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        // תנועה חלקה בין מיקום המצלמה למיקום הרצוי
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
