using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingAreaManager : MonoBehaviour
{
    // Fields
    [SerializeField] private Transform waitingAreaOrigin;
    [SerializeField] private float slotSpacing = 1.2f;
    [SerializeField] private GameObject waitingSlotPrefab;
    [SerializeField] private GameObject DebugPlane;

    private List<StickmanController> waitingPassengers;
    private Vector3[] slotPositions;
    private int slotCount;
    private int reservedSlotCount;

    public event Action OnWaitingAreaFull;

    public static WaitingAreaManager Instance { get; private set; }

    // Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    public void Initialize(LevelData data)
    {
        // Temp
        HideDebugPlane();

        slotCount = data.WaitingAreaSize;
        waitingPassengers = new List<StickmanController>(slotCount);
        reservedSlotCount = 0;

        slotPositions = new Vector3[slotCount];

        // Origin is centered, so we calculate positions based on half the total width
        float totalWidth = (slotCount - 1) * slotSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < slotCount; i++)
        {
            slotPositions[i] = waitingAreaOrigin.position + Vector3.right * (startX + i * slotSpacing);
            Instantiate(waitingSlotPrefab, slotPositions[i], waitingSlotPrefab.transform.rotation, transform);
        }
    }

    public bool HasFreeSlot()
    {
        return reservedSlotCount < slotCount;
    }

    public bool HasAnyStickmans()
    {
        return waitingPassengers.Count > 0;
    }

    public void AddToWaiting(StickmanController stickman)
    {
        if (!HasFreeSlot())
        {
            OnWaitingAreaFull?.Invoke();
            return;
        }

        int slotIndex = reservedSlotCount;
        reservedSlotCount++;
        waitingPassengers.Add(stickman);
        stickman.DisableInteraction();

        Vector3 targetPosition = slotPositions[slotIndex];
        stickman.MoveToPoint(targetPosition, () =>
        {
            TryBoardWaitingPassengers();
        });

        if (!HasFreeSlot())
            OnWaitingAreaFull?.Invoke();
    }

    public void TryBoardWaitingPassengers()
    {
        // If no bus at the stop do not try to board passengers
        if (BusManager.Instance.IsTransitioning)
            return;

        StickmanColor activeColor = BusManager.Instance.GetActiveBusColor();

        for (int i = waitingPassengers.Count - 1; i >= 0; i--)
        {
            StickmanController stickman = waitingPassengers[i];

            if (stickman.CColor != activeColor)
                continue;

            if (stickman.IsMoving)
                continue;

            BusManager.Instance.BoardStickman(stickman);
            waitingPassengers.RemoveAt(i);
            reservedSlotCount--;
        }

        RefreshSlotPositions();
    }

    private void RefreshSlotPositions()
    {
        for (int index = 0; index < waitingPassengers.Count; index++)
        {
            waitingPassengers[index].MoveToPoint(slotPositions[index], null);
        }
    }

    private void HideDebugPlane()
    {
        DebugPlane.SetActive(false);
    }
}
