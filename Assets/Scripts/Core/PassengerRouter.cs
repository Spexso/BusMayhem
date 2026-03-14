using UnityEngine;

public class PassengerRouter : MonoBehaviour
{
    // Fields
    public static PassengerRouter Instance { get; private set; }

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

    public void RoutePassenger(StickmanController stickman)
    {
        if (BusManager.Instance.IsTransitioning || BusManager.Instance.GetActiveBusColor() == StickmanColor.None)
        {
            WaitingAreaManager.Instance.AddToWaiting(stickman);
            return;
        }

        if (stickman.CColor == BusManager.Instance.GetActiveBusColor())
            BusManager.Instance.BoardStickman(stickman);
        else
            WaitingAreaManager.Instance.AddToWaiting(stickman);
    }
}