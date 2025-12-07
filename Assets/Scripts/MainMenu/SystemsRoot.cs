using UnityEngine;

/// <summary>
/// A persistent root object that keeps all core game systems alive
/// when changing scenes. Any manager placed under this GameObject
/// will NOT be destroyed between scenes.
/// </summary>
public class SystemsRoot : MonoBehaviour
{
    private void Awake()
    {
        // Ensure this object (and all children managers under it)
        // stay alive across scene loads
        DontDestroyOnLoad(gameObject);
    }
}
