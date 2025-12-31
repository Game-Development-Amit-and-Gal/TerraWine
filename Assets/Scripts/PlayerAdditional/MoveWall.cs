using UnityEngine;

public class MoveWall : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float wallSpeed = 0.5f;
    [SerializeField] private Transform leftWallBound;
    private const float stop = 0f;
    private float collisionEpsilon = 0.5f;
    void Start()
    {
        if( rb == null) rb = GetComponent<Rigidbody2D>();
        if( leftWallBound == null ) leftWallBound = GetComponent<Transform>();
        
    }

    // Update is called once per frame
    void Update()
    {
        float distance = rb.position.x - leftWallBound.position.x;
        bool FarFromWall = (distance >= collisionEpsilon);
        Debug.Log("Value of distance = " + distance);
    
        if (FarFromWall)
        {
            Vector2 moveWall = rb.linearVelocity;

            moveWall.x -= wallSpeed * Time.deltaTime;
            rb.linearVelocity = moveWall;
            
        }
        else
        {
            Vector2 stopWall = rb.linearVelocity;
            stopWall.x = stop;
            rb.linearVelocity = stopWall;
        }
    }
}
