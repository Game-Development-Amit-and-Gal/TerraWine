using UnityEngine;

public class WatchingState : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float reachDistance = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;

    private int idx = 0;
    private bool active = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();

        SetActiveEnemy(false); // start “deactivated”
    }

    public void ActivatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || patrolPoints[0] == null)
        {
            Debug.LogError("No patrol points assigned!");
            return;
        }

        Vector2 p = patrolPoints[0].position;

        Debug.Log($"[ActivatePatrol] P0={patrolPoints[0].name} world={p} " +
                  $"scene={patrolPoints[0].gameObject.scene.name} " +
                  $"enemy(before) rb={rb.position} tr={transform.position}");

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // hard set both (so nothing “half applies”)
        rb.position = p;
        transform.position = new Vector3(p.x, p.y, transform.position.z);

        Debug.Log($"[ActivatePatrol] enemy(after) rb={rb.position} tr={transform.position}");
        active = true;
    }


    public void SetActiveEnemy(bool on)
    {
        if (sr) sr.enabled = on;
        if (col) col.enabled = on;        // disable if you don't want interactions
        if (rb) rb.simulated = on;        // physics off when hidden
    }

    void FixedUpdate()
    {
        if (!active) return;

        Vector2 target = patrolPoints[idx].position;
        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Vector2.Distance(rb.position, target) <= reachDistance)
            idx = (idx + 1) % patrolPoints.Length;
    }
}
