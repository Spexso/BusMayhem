using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Provides functionality to display or hide a UI panel with a smooth scaling animation over a configurable duration.
/// </summary>
public class UIScalePanel : MonoBehaviour
{
    // Fields
    [SerializeField] private float duration = 0.2f;

    // Methods
    public void Show(Action onComplete = null)
    {
        gameObject.SetActive(true);
        StartCoroutine(ScaleCoroutine(Vector3.zero, Vector3.one, onComplete));
    }

    public void Hide(Action onComplete = null)
    {
        StartCoroutine(ScaleCoroutine(Vector3.one, Vector3.zero, () =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    private IEnumerator ScaleCoroutine(Vector3 from, Vector3 to, Action onComplete)
    {
        transform.localScale = from;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.localScale = to;
        onComplete?.Invoke();
    }
}