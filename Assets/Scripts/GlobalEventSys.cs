using UnityEngine;

/// <summary>
/// A global persistent singleton object intended for handling events.
/// Ensures that only one instance exists across all scenes.
/// </summary>
public class GlobalEventSys : MonoBehaviour
{
    /// <summary>
    /// Static reference to the global instance.
    /// </summary>
    private static GlobalEventSys instance;

    private void Awake()
    {
        // If another instance already exists, destroy this duplicate.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise assign and persist across scene loads.
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // Only clear the instance if THIS object is being destroyed.
        if (instance == this)
        {
            instance = null;
        }
    }
}
