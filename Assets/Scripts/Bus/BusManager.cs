using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusManager : MonoBehaviour
{
    // Fields
    [SerializeField] private GameObject busPrefab;
    [SerializeField] private Transform busStopTransform;
    [SerializeField] private Transform busSpawnTransform;
    [SerializeField] private Transform busDepartTransform;
    [SerializeField] private float busMoveSpeed = 5f;

    private Queue<BusData> busQueue = new Queue<BusData>();
    private BusController activeBus;
    private bool isTransitioning;

    public event Action OnBusDepart;
    public event Action<StickmanColor> OnActiveBusChanged;

    // Misc
    public static BusManager Instance { get; private set; }
    public bool IsTransitioning => isTransitioning;

    // Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(LevelData data)
    {
        busQueue = new Queue<BusData>();

        foreach (BusData busData in data.BusSequence)
            busQueue.Enqueue(busData);

        SpawnNextBus();
    }

    public StickmanColor GetActiveBusColor()
    {
        if (activeBus != null)
            return activeBus.BusColor;

        return StickmanColor.None;
    }

    public void BoardStickman(StickmanController stickman)
    {
        if (activeBus == null || activeBus.IsFull)
            return;

        BusController targetBus = activeBus;
        Vector3 entryPoint = targetBus.PassengerEntryPoint.transform.position + Vector3.up * 0.5f;

        stickman.StopMovement();
        stickman.MoveToPoint(entryPoint, () =>
        {
            if (targetBus == null || targetBus.IsFull)
            {
                WaitingAreaManager.Instance.AddToWaiting(stickman);
                return;
            }

            bool boarded = targetBus.TryBoardPassenger(stickman.CColor);
            if (boarded)
                stickman.gameObject.SetActive(false);
            else
                WaitingAreaManager.Instance.AddToWaiting(stickman);
        });
    }

    public void SpawnNextBus()
    {
        if (busQueue.Count == 0)
        {
            OnBusDepart?.Invoke();
            return;
        }

        BusData nextBusData = busQueue.Dequeue();
        GameObject busObj = Instantiate(busPrefab, busSpawnTransform.position, Quaternion.identity);

        BusController bus = busObj.GetComponent<BusController>();
        if (bus == null)
        {
            Debug.LogError("Bus prefab does not have a BusController component.");
            Destroy(busObj);
            return;
        }

        bus.Initialize(nextBusData);
        bus.onBusFull += HandleBusFull;
        activeBus = bus;

        StartCoroutine(MoveBusToStop(bus));
        OnActiveBusChanged?.Invoke(bus.BusColor);
    }

    private void HandleBusFull(BusController bus)
    {
        bus.onBusFull -= HandleBusFull;
        StartCoroutine(DepartBus(bus));
    }

    private IEnumerator MoveBusToStop(BusController bus)
    {
        isTransitioning = true;

        while (Vector3.Distance(bus.transform.position, busStopTransform.position) > 0.1f)
        {
            bus.transform.position = Vector3.MoveTowards(bus.transform.position, busStopTransform.position, busMoveSpeed * Time.deltaTime);
            yield return null;
        }

        bus.transform.position = busStopTransform.position;
        isTransitioning = false;

        // After bus arrives try to board waiting passengers of the same color
        WaitingAreaManager.Instance.TryBoardWaitingPassengers();
    }

    private IEnumerator DepartBus(BusController bus)
    {
        isTransitioning = true;

        while (Vector3.Distance(bus.transform.position, busDepartTransform.position) > 0.1f)
        {
            bus.transform.position = Vector3.MoveTowards(bus.transform.position, busDepartTransform.position, busMoveSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(bus.gameObject);
        isTransitioning = false;

        OnBusDepart?.Invoke();
        SpawnNextBus();
    }
}
