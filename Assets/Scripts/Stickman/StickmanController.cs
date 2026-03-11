using UnityEngine;
using System.Collections;
using Unity.VisualScripting;


public class StickmanController : MonoBehaviour
{
    // Fields
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalThreshold = 0.01f;

    private float activeEmissionColorStrength = 0.6f;
    private float passiveEmissionColorStrength = 0.4f;
    private int gridX;
    private int gridY;
    private StickmanColor stickmanColor;
    private bool isMoving;
    private Renderer meshRenderer;
    private bool IsInteractable = true;


    // Methods
    public int GridX => gridX;
    public int GridY => gridY;
    public StickmanColor CColor => stickmanColor;
    public bool IsMoving => isMoving;
    public bool IsInteractionEnabled => IsInteractable;

    public void Initialize(int x, int y, StickmanColor color)
    {
        gridX = x;
        gridY = y;
        stickmanColor = color;

        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"[StickmanController] Renderer not found on {gameObject.name}.");
            return;
        }

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

        meshRenderer.material.EnableKeyword("_EMISSION");
        Color baseColor = ColorConverter.GetColor(stickmanColor);
        meshRenderer.material.SetColor("_EmissionColor", baseColor * activeEmissionColorStrength);
        meshRenderer.material.color = baseColor;
    }

    public void SetDimmed()
    {
        if (meshRenderer == null)
            return;

        meshRenderer.material.DisableKeyword("_EMISSION");
        Color baseColor = ColorConverter.GetColor(stickmanColor);
        meshRenderer.material.color = baseColor * passiveEmissionColorStrength;
        meshRenderer.material.SetColor("_EmissionColor", Color.black);
    }

    public void MoveToExit(Vector3[] path, System.Action onComplete)
    {
        if (isMoving)
            return;

        StartCoroutine(FollowPath(path, onComplete));
    }

    public IEnumerator FollowPath(Vector3[] path, System.Action onComplete)
    {
        isMoving = true;
        foreach (Vector3 target in path)
        {
            while (Vector3.Distance(transform.position, target) > arrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
        }

        isMoving = false;
        onComplete?.Invoke();
    }
}