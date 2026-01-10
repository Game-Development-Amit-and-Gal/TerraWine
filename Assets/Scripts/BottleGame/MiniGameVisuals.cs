using System.Collections;
using UnityEngine;

/// <summary>
/// Handles visual "Juice" for the mini-game: Screen Shake, Particles, and feedback.
/// </summary>
public class MiniGameVisuals : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ParticleSystem successParticlePrefab;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    private Vector3 _originalCamPos;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _originalCamPos = mainCamera.transform.position;
    }

    /// <summary>
    /// Spawns a particle effect at the specific position (e.g., where the bottle part connected).
    /// </summary>
    public void PlaySuccessEffect(Vector3 position)
    {
        if (successParticlePrefab != null)
        {
            // Spawn particles slightly in front of the bottle (Z -1) so they are visible
            Vector3 spawnPos = new Vector3(position.x, position.y, -1f);
            ParticleSystem ps = Instantiate(successParticlePrefab, spawnPos, Quaternion.identity);
            Destroy(ps.gameObject, 2f); // Auto cleanup
        }
    }

    /// <summary>
    /// Triggers a camera shake effect to indicate failure or impact.
    /// </summary>
    public void TriggerShake()
    {
        if (mainCamera == null) return;
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCamera.transform.position = new Vector3(_originalCamPos.x + x, _originalCamPos.y + y, _originalCamPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = _originalCamPos;
    }
}