using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.SceneManagement;

public class EnemyStateMachine : StateMachine
{
    [SerializeField] private IdleState idle;          // or TutorialWaitState
    [SerializeField] private WatchingState watcher;
    [SerializeField] private ThiefState thief;

    private static bool tutorialEnded = false;
    private bool watching = false;  

    private void Awake()
    {
        idle ??= GetComponent<IdleState>();
        watcher ??= GetComponent<WatchingState>();
        thief ??= GetComponent<ThiefState>();
            base
            .AddState(idle)
            .AddState(watcher)
            .AddState(thief)
            .AddTransition(idle, IsTutorialEnd, watcher)
            .AddTransition(watcher, () => watching && SceneLoader.playerIsNotInGarden , thief);

        // optional: make sure we start hidden/stopped
        watcher.Deactivate();
    }

    // Call THIS when tutorial finishes
    public static void EndTutorial()
    {
        tutorialEnded = true;
    }
    private bool IsTutorialEnd() {
        if (tutorialEnded)
        {
            watching = true;
            return true;
        }
        else
        {
            return false;
        }
        
    }
}
