using UnityEngine;

public class TutorialArrowBounce : MonoBehaviour
{
    public Vector3 direction = Vector3.up;
    public float distance = 20f;
    public float speed = 2f;

    Vector3 startPos;

    void OnEnable()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float t = Mathf.Sin(Time.time * speed);
        transform.localPosition = startPos + direction * t * distance;
    }
}
