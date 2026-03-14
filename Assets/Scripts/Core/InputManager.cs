using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Fields
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask stickmanLayer;

    private bool inputEnabled = true;
    private BusMayhemInputActions actions;

    // Methods
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        actions = new BusMayhemInputActions();
    }
    private void OnEnable()
    {
        actions.Gameplay.Tap.performed += OnTap;
        actions.Gameplay.Enable();
    }

    private void OnDisable()
    {
        actions.Gameplay.Tap.performed -= OnTap;
        actions.Gameplay.Disable();
    }

    private void OnTap(InputAction.CallbackContext context)
    {
        if (!inputEnabled)
            return;

        Vector2 screenPosition = actions.Gameplay.TapPosition.ReadValue<Vector2>();
        HandleInput(screenPosition);
    }

    private void OnDestroy()
    {
        actions.Disable();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void HandleInput(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, stickmanLayer))
            return;

        Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 2f);

        StickmanController stickman = hit.collider.GetComponentInParent<StickmanController>();
        if (stickman != null)
        {
            if (stickman.IsInteractionEnabled)
                GridManager.Instance?.MoveStickman(stickman);
            return;
        }

        HouseController house = hit.collider.GetComponentInParent<HouseController>();
        if (house != null)
        {
            if (BusManager.Instance != null && BusManager.Instance.IsTransitioning)
                return;

            house.OnTapped();
            return;
        }

        Debug.LogWarning("[InputManager] Hit object has no StickmanController or HouseController.");
    }
}
