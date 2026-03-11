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
        return waitingPassengers.Count < slotCount;
    }

    public void AddToWaiting(StickmanController stickman)
    {
        if (!HasFreeSlot())
        {
            CheckWaitingAreaFull();
            return;
        }

        waitingPassengers.Add(stickman);
        stickman.DisableInteraction();
        stickman.transform.position = slotPositions[waitingPassengers.Count - 1] + Vector3.up * 0.5f;

        CheckWaitingAreaFull();
    }

    public void TryBoardWaitingPassengers()
    {
        StickmanColor activeColor = BusManager.Instance.GetActiveBusColor();

        for (int i = waitingPassengers.Count - 1; i >= 0; i--)
        {
            if (waitingPassengers[i].CColor == activeColor)
            {
                BusManager.Instance.BoardStickman(waitingPassengers[i]);
                waitingPassengers.RemoveAt(i);
            }
        }

        RefreshSlotPositions();
        CheckWaitingAreaFull();
    }

    private void RefreshSlotPositions()
    {
        for (int i = 0; i < waitingPassengers.Count; i++)
            waitingPassengers[i].transform.position = slotPositions[i];
    }

    private void CheckWaitingAreaFull()
    {
        if (!HasFreeSlot())
            OnWaitingAreaFull?.Invoke();
    }

    private void HideDebugPlane()
    {
        DebugPlane.SetActive(false);
    }
}
