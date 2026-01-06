using UnityEngine;

public class WorldArrowFollow : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private bool bob = true;
    [SerializeField] private float bobAmplitude = 0f;
    [SerializeField] private float bobSpeed = 0f;

    private Transform target;
    private Vector3 baseOffset;

    public void AttachTo(Transform t, Vector3 offset)
    {
        target = t;
        baseOffset = offset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 pos = target.position + baseOffset;

        if (bob)
            pos += Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);

        transform.position = pos;
    }
}
