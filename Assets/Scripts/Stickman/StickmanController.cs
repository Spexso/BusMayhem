using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the behavior and interactions of a stickman character, including movement, color management, and
/// interaction states within the game environment.
/// </summary>
public class StickmanController : MonoBehaviour
{
    // Fields
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalThreshold = 0.01f;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private GameObject outlineMesh;
    [SerializeField] private SpriteRenderer blockedIcon;
    [SerializeField] private AudioSource blockedAudioSource;

    private int gridX;
    private int gridY;
    private StickmanColor stickmanColor;
    private bool isMoving;
    private bool IsInteractable = true;
    private Animator animator;

    private float iconScaleUpDuration = 0.15f;
    private float iconHoldDuration = 0.5f;
    private float iconScaleDownDuration = 0.15f;

    public int GridX => gridX;
    public int GridY => gridY;
    public StickmanColor CColor => stickmanColor;
    public bool IsMoving => isMoving;
    public bool IsInteractionEnabled => IsInteractable;

    // Methods
    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError($"[StickmanController] Animator not found on {gameObject.name} or its children.");
    }

    public void Initialize(int x, int y, StickmanColor color)
    {
        gridX = x;
        gridY = y;
        stickmanColor = color;

        if (meshRenderer == null)
        {
            Debug.LogError($"[StickmanController] Renderer not found on {gameObject.name}.");
            return;
        }

        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = ColorConverter.GetColor(color);
    }

    public void SetColor(StickmanColor color)
    {
        meshRenderer.material = new Material(meshRenderer.material);
        meshRenderer.material.color = ColorConverter.GetColor(color);
    }

    public void DisableInteraction()
    {
        IsInteractable = false;
    }

    public void SetHighlighted()
    {
        if (meshRenderer == null)
            return;

        meshRenderer.material.color = ColorConverter.GetColor(stickmanColor);

        if (outlineMesh != null)
            outlineMesh.SetActive(true);
    }

    public void SetDimmed()
    {
        if (meshRenderer == null)
            return;

        meshRenderer.material.color = ColorConverter.GetColor(stickmanColor);

        if (outlineMesh != null)
            outlineMesh.SetActive(false);
    }

    public void PlayBlockedFeedback()
    {
        if (blockedAudioSource != null)
            blockedAudioSource.Play();

        if (blockedIcon != null)
            StartCoroutine(BlockedIconCoroutine());
    }

    private IEnumerator BlockedIconCoroutine()
    {
        Vector3 fullScale = Vector3.one * 2.0f;

        blockedIcon.gameObject.SetActive(true);
        blockedIcon.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < iconScaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / iconScaleUpDuration);
            blockedIcon.transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, t);
            yield return null;
        }

        blockedIcon.transform.localScale = fullScale;
        yield return new WaitForSeconds(iconHoldDuration);

        elapsed = 0f;
        while (elapsed < iconScaleDownDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / iconScaleDownDuration);
            blockedIcon.transform.localScale = Vector3.Lerp(fullScale, Vector3.zero, t);
            yield return null;
        }

        blockedIcon.transform.localScale = Vector3.zero;
        blockedIcon.gameObject.SetActive(false);
    }

    public void MoveToExit(Vector3[] path, System.Action onComplete)
    {
        if (isMoving)
            return;

        StartCoroutine(FollowPath(path, onComplete));
    }

    public void MoveToPoint(Vector3 destination, System.Action onComplete)
    {
        if (isMoving)
            return;

        StartCoroutine(FollowPath(new Vector3[] { destination }, onComplete));
    }

    public IEnumerator FollowPath(Vector3[] path, System.Action onComplete)
    {
        isMoving = true;

        if (animator != null)
            animator.SetBool("IsWalking", true);

        foreach (Vector3 target in path)
        {
            while (Vector3.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
        }

        if (animator != null)
            animator.SetBool("IsWalking", false);

        isMoving = false;
        GameManager.Instance?.OnStickmanWalkEnded();
        onComplete?.Invoke();
    }
    public void StopMovement()
    {
        StopAllCoroutines();
        isMoving = false;

        if (animator != null)
            animator.SetBool("IsWalking", false);
    }
}