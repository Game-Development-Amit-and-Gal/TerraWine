using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private WatchingState watcher;

    void Awake()
    {
        if (!watcher) watcher = GetComponent<WatchingState>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool on = true;
            watcher.SetActiveEnemy(on);
            watcher.ActivatePatrol();
        }
    }
}
