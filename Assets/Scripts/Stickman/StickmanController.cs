using UnityEngine;
using System.Collections;
using Unity.VisualScripting;


public class StickmanController : MonoBehaviour
{
    // Fields
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arrivalThreshold = 0.01f;
    [SerializeField] private float dimmedAlpha = 0.4f;

    private int gridX;
    private int gridY;
    private StickmanColor stickmanColor;
    private bool isMoving;
    private Renderer meshRenderer;

    // Properties
    public int GridX => gridX;
    public int GridY => gridY;
    public StickmanColor Color => stickmanColor;
    public bool IsMoving => isMoving;

    // Methods
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

    public void SetHighlighted()
    {
        if (meshRenderer == null)
            return;

        Color color = meshRenderer.material.color;
        meshRenderer.material.color = new Color(color.r, color.g, color.b, 1f);
    }

    public void SetDimmed()
    {
        if (meshRenderer == null)
            return;

        Color color = meshRenderer.material.color;
        meshRenderer.material.color = new Color(color.r, color.g, color.b, dimmedAlpha);
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