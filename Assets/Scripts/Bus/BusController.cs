using System;
using System.Collections.Generic;
using UnityEngine;

public class BusController : MonoBehaviour
{
    // Fields
    [SerializeField] private Renderer busRenderer;
    [SerializeField] public GameObject PassengerEntryPoint;
    [SerializeField] private Transform rowARoot;
    [SerializeField] private Transform rowBRoot;
    [SerializeField] private GameObject seatPrefab;
    [SerializeField] private GameObject stickmanPrefab;
    [SerializeField] private float seatSpacing = 0.6f;

    private StickmanColor color;
    private int capacity;
    private int boardedCount;
    private List<Transform> seatSlots = new List<Transform>();

    public event Action<BusController> onBusFull;

    public StickmanColor BusColor => color;
    public int BoardedCount => boardedCount;
    public int Capacity => capacity;
    public bool IsFull => boardedCount >= capacity;

    // Methods
    public void Initialize(BusData data)
    {
        color = data.Color;
        capacity = data.Capacity;
        boardedCount = 0;

        if (busRenderer != null && busRenderer.materials.Length > 1)
        {
            Material[] mats = busRenderer.materials;
            mats[1].color = ColorConverter.GetColor(color);
            busRenderer.materials = mats;
        }
        else
        {
            Debug.LogError($"[BusController] Expected at least 2 materials on busRenderer, found {busRenderer?.materials.Length ?? 0}.");
        }

        SpawnSeats();
    }

    public bool TryBoardPassenger(StickmanColor passengerColor)
    {
        if (IsFull)
            return false;

        SpawnSeatedPassenger(passengerColor, seatSlots[boardedCount]);

        boardedCount++;

        if (boardedCount >= capacity)
            onBusFull?.Invoke(this);

        return true;
    }

    private void SpawnSeats()
    {
        int rowACount = Mathf.Min(Mathf.CeilToInt(capacity / 2f), 2);
        int rowBCount = Mathf.Min(Mathf.FloorToInt(capacity / 2f), 2);

        SpawnRow(rowARoot, rowACount);
        SpawnRow(rowBRoot, rowBCount);
    }

    private void SpawnRow(Transform root, int count)
    {
        if (root == null)
        {
            Debug.LogError("[BusController] Row root transform is not assigned.");
            return;
        }

        float totalWidth = (count - 1) * seatSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector3 localPos = new Vector3(0f, 0f, startX + i * seatSpacing);
            GameObject seat = Instantiate(seatPrefab, root);
            seat.transform.localPosition = localPos;
            seatSlots.Add(seat.transform);
        }
    }

    private void SpawnSeatedPassenger(StickmanColor passengerColor, Transform seat)
    {
        if (stickmanPrefab == null)
        {
            Debug.LogError("[BusController] stickmanPrefab is not assigned.");
            return;
        }

        GameObject clone = Instantiate(stickmanPrefab, seat);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;

        StickmanController controller = clone.GetComponent<StickmanController>();
        if (controller != null)
            controller.SetColor(passengerColor);

        MonoBehaviour[] scripts = clone.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
            script.enabled = false;

        Animator animator = clone.GetComponentInChildren<Animator>();
        if (animator != null)
            animator.enabled = false;
    }
}
