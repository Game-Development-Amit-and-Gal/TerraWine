using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UITransparencyGroup : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private CanvasGroup targetUI;
    [SerializeField] private float fadedAlpha = 0.3f;
    [SerializeField] private float fadeDuration = 0.5f;

    private float originalAlpha;

    private void Awake()
    {
        targetUI = GetComponent<CanvasGroup>();
    }


     void Start()
    {
        if (!targetUI) return;

        originalAlpha = targetUI.alpha;

        StartCoroutine(FadeTo(fadedAlpha));
    }

    public void RestoreAlpha(float alpha)
    {
        if ((targetUI == null)) return;


        StartCoroutine(FadeTo(alpha));
    }


    private IEnumerator FadeTo(float alpha)
    {
        float time = 0f;
        float start = targetUI.alpha;

        while(time < fadeDuration)
        {
            time += Time.deltaTime;
            start = Mathf.Lerp(start, alpha, time / fadeDuration);
            yield return null;
        }

        targetUI.alpha = alpha;

    }
}
