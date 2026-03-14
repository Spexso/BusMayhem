using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinFlyEffect : MonoBehaviour
{
    // Fields

    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Vector2 coinSize = new Vector2(40f, 40f);
    [SerializeField] private RectTransform spawnOrigin;
    [SerializeField] private RectTransform coinTarget;
    [SerializeField] private int coinCount = 6;
    [SerializeField] private float flightDuration = 0.8f;
    [SerializeField] private float maxLaunchDelay = 0.4f;
    [SerializeField] private float arcStrength = 150f;

    private GameObject[] coinPool;
    private int landedCount;
    private Action OnComplete;

    // Methods

    private void Awake()
    {
        coinPool = new GameObject[coinCount];

        for (int i = 0; i < coinCount; i++)
        {
            GameObject coin = new GameObject("Coin_" + i, typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(transform, false);

            RectTransform rect = coin.GetComponent<RectTransform>();
            rect.sizeDelta = coinSize;

            Image image = coin.GetComponent<Image>();
            image.sprite = coinSprite;
            image.preserveAspect = true;

            coin.SetActive(false);
            coinPool[i] = coin;
        }

        Debug.Log($"[CoinFlyEffect] Pool initialized with {coinPool.Length} coins.");
    }

    public void Play(Action onComplete)
    {
        OnComplete = onComplete;
        landedCount = 0;

        for (int i = 0; i < coinPool.Length; i++)
        {
            float delay = UnityEngine.Random.Range(0f, maxLaunchDelay);
            StartCoroutine(FlyRoutine(coinPool[i], delay));
        }
    }

    private IEnumerator FlyRoutine(GameObject coin, float delay)
    {
        yield return new WaitForSeconds(delay);

        coin.SetActive(true);

        RectTransform rect = coin.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.Log("[CoinFlyEffect]: coin is missing RectTransform.");
            yield break;
        }

        RectTransform parentRect = transform as RectTransform;

        Vector2 spawnWorld = spawnOrigin.position;
        Vector2 targetWorld = coinTarget.position;

        Vector2 spawnLocal = parentRect.InverseTransformPoint(spawnWorld);
        Vector2 targetLocal = parentRect.InverseTransformPoint(targetWorld);

        Vector2 start = spawnLocal + UnityEngine.Random.insideUnitCircle * 60f;
        Vector2 end = targetLocal;
        Vector2 mid = (start + end) * 0.5f + UnityEngine.Random.insideUnitCircle.normalized * arcStrength;

        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            Vector2 a = Vector2.Lerp(start, mid, t);
            Vector2 b = Vector2.Lerp(mid, end, t);
            rect.anchoredPosition = Vector2.Lerp(a, b, t);

            yield return null;
        }

        rect.anchoredPosition = end;
        coin.SetActive(false);

        landedCount++;
        Debug.Log($"[CoinFlyEffect] Coin landed. landedCount: {landedCount} / {coinPool.Length}");
        if (landedCount >= coinPool.Length)
        {
            OnComplete?.Invoke();
        }
    }
}