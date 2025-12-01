using UnityEngine;

public class GlobalEventSys : MonoBehaviour
{
    private static GlobalEventSys instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Destroy(instance.gameObject);
        }
    }
}
