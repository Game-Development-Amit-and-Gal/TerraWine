using System.Collections.Generic;
using UnityEngine;

public class ThiefState : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TilemapPathfinder2D tilemapPathfinder;

    [Header("Move")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float waypointReach = 0.05f;

    private GardenManager garden;
    private GardenBed target;

    private Rigidbody2D rb;

    private List<Vector3> path;
    private int pathIndex;
    private bool moving;

    private void Awake()
    {
        Debug.Log("Activated Thief Mode");
        garden = FindFirstObjectByType<GardenManager>();
        tilemapPathfinder = FindFirstObjectByType<TilemapPathfinder2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    public bool TryStartSteal()
    {
        if (!garden || !tilemapPathfinder || !rb) return false;
        if (!garden.TryGetRandomStealable(out target)) return false;

        // ensure we are on a valid walkable tile (ONLY ONCE)
        rb.position = tilemapPathfinder.SnapToNearestWalkable(rb.position);

        // choose a walkable tile next to the bed (bed itself is obstacle)
        if (!tilemapPathfinder.TryGetApproachTile(target.transform.position, rb.position, out Vector2 goal))
            return false;

        path = tilemapPathfinder.FindPath(rb.position, goal);
        if (path == null || path.Count == 0) return false;

        // Usually path[0] == current cell, so start from 1
        pathIndex = (path.Count > 1) ? 1 : 0;
        moving = true;
        return true;
    }

    private void FixedUpdate()
    {
        if (!moving) return;
        if (path == null || pathIndex >= path.Count) { Arrive(); return; }

        Vector2 wp = (Vector2)path[pathIndex];
        Vector2 next = Vector2.MoveTowards(rb.position, wp, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Vector2.Distance(rb.position, wp) <= waypointReach)
        {
            pathIndex++;
            if (pathIndex >= path.Count)
                Arrive();
        }
    }

    private void Arrive()
    {
        moving = false;
        path = null;

        // reached the approach tile -> steal
        OnReachTarget();
    }

    public void OnReachTarget()
    {
        if (target == null) return;

        target.ClearCrop();   // remove grapes from bed (bed stays)
        target = null;        // just forget target
    }

    public void Deactivate()
    {
        moving = false;
        path = null;
        pathIndex = 0;

        if (rb)
            rb.linearVelocity = Vector2.zero;

        target = null;
    }
}
