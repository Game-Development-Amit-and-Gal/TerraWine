using UnityEngine;

public class CameraFollowX : MonoBehaviour
{
    public Transform target;     // השחקן
    public float offsetX = 0f;   // מרחק אופקי מהשחקן (אם תרצי שהשחקן לא יהיה באמצע בדיוק)
    public float smooth = 5f;    // כמה רך/חלק (0 = קפיצה, 10 = מאוד חלק)

    void LateUpdate()
    {
        if (target == null) return;

        // לוקחים את המיקום הנוכחי של המצלמה
        Vector3 pos = transform.position;

        // מעדכנים רק את ה-X לפי השחקן
        float targetX = target.position.x + offsetX;
        pos.x = Mathf.Lerp(pos.x, targetX, smooth * Time.deltaTime);

        // מחזירים את המיקום למצלמה
        transform.position = pos;
    }
}
