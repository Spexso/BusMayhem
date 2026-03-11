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

    public event Action onAllBusesDeparted;
    public event Action<StickmanColor> onActiveBusChanged;

    // Misc
    public static BusManager Instance { get; private set; }

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

    public void HandleStickmanArrival(StickmanController stickman)
    {
        if (activeBus == null || isTransitioning)
        {
            WaitingAreaManager.Instance.AddToWaiting(stickman);
            return;
        }

        if (stickman.CColor == activeBus.BusColor)
        {
            BoardStickman(stickman);
        }
        else
        {
            WaitingAreaManager.Instance.AddToWaiting(stickman);
        }
    }

    public void BoardStickman(StickmanController stickman)
    {
        if (activeBus == null || activeBus.IsFull)
            return;

        stickman.gameObject.SetActive(false);
        activeBus.TryBoardPassenger();
    }

    public void SpawnNextBus()
    {
        if (busQueue.Count == 0)
        {
            onAllBusesDeparted?.Invoke();
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
        onActiveBusChanged?.Invoke(bus.BusColor);
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

        SpawnNextBus();
    }
}
